using System.Collections.Generic;
using UnityEngine;

public enum eBattleResult
{
    Running = 0,
    Victory,
    Defeat,
}

// 웨이브 한 판. 아군은 판정 소환 결과에서, 적은 WaveTable에서 만들어 서로 진격시킨다.
//
// 최소 수직 슬라이스다. 레인(릴의 행) 안에서만 교전하고 레인 간 간섭은 없다 —
// 전열/중열/후열의 역할 차이(GDD §FieldLayout)는 전투가 실제로 도는 걸 본 뒤에 얹는다.
public class UIInGameBattle : MonoBehaviour
{
    [SerializeField] private RectTransform m_UnitRoot;
    [SerializeField] private BattleUnit m_UnitTemplate;

    // 아군이 출발하는 x와 적이 나타나는 x. 적이 이 왼쪽 끝을 넘으면 본거지가 맞는다.
    [SerializeField] private float m_AllyStartX = 0f;
    [SerializeField] private float m_EnemySpawnX = 900f;
    [SerializeField] private float m_HomeLineX = -60f;

    private const int LANE_COUNT = 3;
    private const float LANE_STEP_Y = 52f;
    private const float LANE_STEP_X = 30f;

    private List<BattleUnit> m_ListUnit = new List<BattleUnit>();

    // 그리기 순서 계산용 버퍼. 매 프레임 새 리스트를 만들면 그만큼 GC가 돈다.
    private List<BattleUnit> m_ListDepthOrder = new List<BattleUnit>();
    private List<BattleUnit> m_LastDepthOrder = new List<BattleUnit>();
    private eBattleResult m_Result = eBattleResult.Running;
    private int m_HomeHit;

    public eBattleResult result => m_Result;
    public int homeHit => m_HomeHit;
    public bool isRunning => m_Result == eBattleResult.Running;

    public void Clear()
    {
        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            if (m_ListUnit[i] != null)
                Destroy(m_ListUnit[i].gameObject);
        }

        m_ListUnit.Clear();

        // 파괴된 유닛이 남아 있으면 다음 웨이브의 "순서가 바뀌었나" 비교가 어긋난다.
        m_ListDepthOrder.Clear();
        m_LastDepthOrder.Clear();

        m_Result = eBattleResult.Running;
        m_HomeHit = 0;
    }

    // 판정 결과와 웨이브를 받아 양쪽 유닛을 세운다.
    public void Begin(JudgeResult _judgeResult, int[] _grid,
        IReadOnlyList<HouseSlotSymbolSprite> _spritePool, WaveRecord _wave)
    {
        Clear();

        if (m_UnitRoot == null || m_UnitTemplate == null)
        {
            Logger.Error("[UIInGameBattle] Begin Failed! UnitRoot 또는 UnitTemplate 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        m_UnitTemplate.gameObject.SetActive(false);

        SpawnAllies(_judgeResult, _grid, _spritePool);
        SpawnEnemies(_wave);
    }

    private Vector2 GetLanePosition(int _lane, float _x)
    {
        // 전장 표시와 같은 규칙 — 뒤 레인일수록 위로, 오른쪽으로.
        int laneFromFront = (LANE_COUNT - 1) - _lane;
        return new Vector2(_x + laneFromFront * LANE_STEP_X, laneFromFront * LANE_STEP_Y);
    }

    private void SpawnAllies(JudgeResult _judgeResult, int[] _grid, IReadOnlyList<HouseSlotSymbolSprite> _spritePool)
    {
        if (_judgeResult == null || _grid == null || _spritePool == null)
            return;

        UnitGradeTable gradeTable = TableManager.instance.GetTable<UnitGradeTable>();
        if (gradeTable == null)
        {
            Logger.Error("[UIInGameBattle] SpawnAllies Failed! UnitGradeTable not found");
            return;
        }

        for (int i = 0; i < _judgeResult.ListSummon.Count; ++i)
        {
            SummonSlot summon = _judgeResult.ListSummon[i];
            if (summon.Cell < 0 || summon.Cell >= _grid.Length)
                continue;

            // 판정기가 심볼을 직접 정했으면 그걸 쓴다(윷). 아니면 그 칸에 나온 심볼을 쓴다.
            int symbolType = (summon.SymbolType >= 0) ? summon.SymbolType : _grid[summon.Cell];
            if (symbolType < 0 || symbolType >= _spritePool.Count)
                continue;

            UnitGradeRecord grade = gradeTable.GetRecord(summon.Grade);
            if (grade == null)
                continue;

            int lane = summon.Cell / LANE_COUNT;
            int column = summon.Cell % LANE_COUNT;

            BattleUnit unit = Instantiate(m_UnitTemplate, m_UnitRoot);
            unit.Setup(eBattleSide.Ally, lane, _spritePool[symbolType].NormalSprite, summon.Grade,
                grade.Hp, grade.Atk, grade.AtkSpeed, grade.Range, grade.MoveSpeed,
                GetLanePosition(lane, m_AllyStartX + column * 108f));
            m_ListUnit.Add(unit);
        }
    }

    private void SpawnEnemies(WaveRecord _wave)
    {
        if (_wave == null)
            return;

        EnemyTable enemyTable = TableManager.instance.GetTable<EnemyTable>();
        GameConfigTable config = TableManager.instance.GetTable<GameConfigTable>();
        if (enemyTable == null || config == null)
        {
            Logger.Error("[UIInGameBattle] SpawnEnemies Failed! EnemyTable 또는 GameConfigTable not found");
            return;
        }

        EnemyRecord enemy = enemyTable.GetRecordByKey(_wave.EnemyKey);
        if (enemy == null)
        {
            Logger.Error($"[UIInGameBattle] SpawnEnemies Failed! 적 없음 - {_wave.EnemyKey} (기대: EnemyTable.csv에 해당 행)");
            return;
        }

        int basePower = config.GetValue(GameConfigTable.KEY_WAVE_BASE_POWER, 6);
        int count = WaveTable.GetSpawnCount(_wave, enemy, basePower);

        // 풀은 루프 밖에서 한 번만 읽는다. 마리마다 Resources를 두드리면 같은 폴더를 count번 조회하게 된다.
        // 로드에 실패하면 BattleUnit이 Symbol Image를 꺼서 HP 바만 뜬다 — 조용히 사라지지 않게 로그를 남긴다.
        List<Sprite> enemyPool = EnemyHouseResolver.LoadPool(_wave, enemy);
        if (enemyPool.Count <= 0)
            Logger.Error($"[UIInGameBattle] SpawnEnemies - 적 스프라이트 풀이 비었다: {_wave.EnemyHouse}/{enemy.Key} (기대: Resources 아래 해당 폴더에 png)");

        // 보스 웨이브는 그 종족의 최상위 말 하나로 세운다. 일반 웨이브는 풀에서 무작위로 섞는다 —
        // 한 종류로만 줄을 세우면 "다른 종족이 쳐들어왔다"가 아니라 "같은 적 복제"로 보인다.
        int bossIndex = EnemyHouseResolver.GetBossSymbolIndex(_wave);

        for (int i = 0; i < count; ++i)
        {
            int lane = i % LANE_COUNT;
            int rank = i / LANE_COUNT;

            Sprite enemySprite = null;
            if (enemyPool.Count > 0)
            {
                int symbolIndex = (bossIndex >= 0) ? bossIndex : Random.Range(0, enemyPool.Count);
                enemySprite = enemyPool[Mathf.Clamp(symbolIndex, 0, enemyPool.Count - 1)];
            }

            BattleUnit unit = Instantiate(m_UnitTemplate, m_UnitRoot);
            unit.Setup(eBattleSide.Enemy, lane, enemySprite, 1,
                enemy.Hp, enemy.Atk, enemy.AtkSpeed, enemy.Range, enemy.MoveSpeed,
                GetLanePosition(lane, m_EnemySpawnX + rank * 108f));
            m_ListUnit.Add(unit);
        }
    }

    // 매 프레임 진행. 승패가 나면 그 자리에서 멈춘다.
    public void Tick(float _deltaTime)
    {
        if (m_Result != eBattleResult.Running)
            return;

        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            BattleUnit unit = m_ListUnit[i];
            if (unit == null || unit.isAlive == false)
                continue;

            unit.Tick(_deltaTime, FindTarget(unit));
        }

        RefreshDepthOrder();
        CheckHomeLine();
        CheckResult();
    }

    // 앞(아래)에 선 유닛이 뒤(위)를 가리도록 그리기 순서를 y로 맞춘다.
    //
    // 배치할 때 한 번 정해두는 걸로는 부족하다 — 유닛이 목표를 따라 레인을 넘나들면
    // 스폰 당시 순서가 그대로 남아 뒤에 있는 유닛이 앞을 덮는다.
    //
    // 매 프레임 SetSiblingIndex를 부르면 그때마다 캔버스가 다시 그려진다.
    // 그래서 **순서가 실제로 바뀐 프레임에만** 적용한다.
    private void RefreshDepthOrder()
    {
        m_ListDepthOrder.Clear();
        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            if (m_ListUnit[i] == null)
                continue;

            m_ListDepthOrder.Add(m_ListUnit[i]);
        }

        // y가 큰(뒤에 있는) 유닛이 먼저 그려져야 앞 유닛에 가려진다.
        m_ListDepthOrder.Sort((left, right) => right.position.y.CompareTo(left.position.y));

        bool isChanged = (m_ListDepthOrder.Count != m_LastDepthOrder.Count);
        if (isChanged == false)
        {
            for (int i = 0; i < m_ListDepthOrder.Count; ++i)
            {
                if (m_ListDepthOrder[i] == m_LastDepthOrder[i])
                    continue;

                isChanged = true;
                break;
            }
        }

        if (isChanged == false)
            return;

        for (int i = 0; i < m_ListDepthOrder.Count; ++i)
        {
            m_ListDepthOrder[i].transform.SetSiblingIndex(i);
        }

        m_LastDepthOrder.Clear();
        m_LastDepthOrder.AddRange(m_ListDepthOrder);
    }

    // 반대편 중 **가장 가까운** 하나. 레인을 가리지 않는다.
    //
    // 예전엔 같은 레인만 봤다. 그래서 한 레인이 비면 그쪽 적은 아무 저항 없이 본거지까지 걸어갔고,
    // 옆 레인에 아군이 놀고 있어도 손을 못 댔다. 특히 윷은 평균 2.63기만 소환해서
    // 레인이 자주 비는데(2026-08-31-0 기록), 그 종족만 전력 대비 훨씬 약해지는 원인이었다.
    //
    // 거리는 2D로 잰다. x만 재면 다른 레인의 바로 위 적이 "가깝다"고 잡혀 엉뚱하게 달려간다.
    private BattleUnit FindTarget(BattleUnit _unit)
    {
        BattleUnit nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            BattleUnit other = m_ListUnit[i];
            if (other == null || other.isAlive == false)
                continue;

            if (other.side == _unit.side)
                continue;

            // 제곱 거리로 비교한다. 크기 순서만 필요해서 제곱근을 뽑을 이유가 없다.
            float sqrDistance = (other.position - _unit.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = other;
            }
        }

        return nearest;
    }

    // 적이 본거지 선을 넘으면 그 적은 사라지고 본거지가 한 대 맞는다.
    private void CheckHomeLine()
    {
        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            BattleUnit unit = m_ListUnit[i];
            if (unit == null || unit.isAlive == false)
                continue;

            if (unit.side != eBattleSide.Enemy)
                continue;

            if (unit.positionX > m_HomeLineX)
                continue;

            m_HomeHit++;
            unit.TakeDamage(int.MaxValue);
        }
    }

    private void CheckResult()
    {
        bool anyEnemy = false;
        bool anyAlly = false;

        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            BattleUnit unit = m_ListUnit[i];
            if (unit == null || unit.isAlive == false)
                continue;

            if (unit.side == eBattleSide.Enemy)
                anyEnemy = true;
            else
                anyAlly = true;
        }

        if (anyEnemy == false)
        {
            m_Result = eBattleResult.Victory;
            return;
        }

        // 아군이 전멸하면 남은 적이 그대로 본거지로 간다 — 그 웨이브는 패배로 끝낸다.
        if (anyAlly == false)
            m_Result = eBattleResult.Defeat;
    }
}
