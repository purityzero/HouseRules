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

            int symbolType = _grid[summon.Cell];
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

        // 적 아트가 아직 없다. 스프라이트 없이 세워 실루엣만 보이게 하고, 아트가 생기면 여기서 물린다.
        for (int i = 0; i < count; ++i)
        {
            int lane = i % LANE_COUNT;
            int rank = i / LANE_COUNT;

            BattleUnit unit = Instantiate(m_UnitTemplate, m_UnitRoot);
            unit.Setup(eBattleSide.Enemy, lane, null, 1,
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

        CheckHomeLine();
        CheckResult();
    }

    // 같은 레인의 반대편 중 가장 가까운 하나를 고른다.
    private BattleUnit FindTarget(BattleUnit _unit)
    {
        BattleUnit nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < m_ListUnit.Count; ++i)
        {
            BattleUnit other = m_ListUnit[i];
            if (other == null || other.isAlive == false)
                continue;

            if (other.side == _unit.side)
                continue;

            if (other.lane != _unit.lane)
                continue;

            float distance = Mathf.Abs(other.positionX - _unit.positionX);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
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
