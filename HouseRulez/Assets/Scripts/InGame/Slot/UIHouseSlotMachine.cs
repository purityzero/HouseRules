using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 3x3 슬롯머신 컨트롤러. 릴 3개를 묶고, 현재 종족의 말 스프라이트 풀/프레임을 세팅하고, 스핀을 지휘한다.
// 판정(결과 확정)은 아직 규칙이 없어 이 클래스가 하지 않는다 — 결과가 안 오면 무작위로 굴러간다.
public class UIHouseSlotMachine : MonoBehaviour
{
    [SerializeField] private Image m_FrameImage;
    [SerializeField] private Image m_HeaderBarImage; // 종족 AccentColor를 물릴 헤더 바(없으면 비워둔다)
    [SerializeField] private UIHouseSlotReel[] m_ReelList;
    [SerializeField] private UIHouseSlotSymbol m_SymbolTemplate; // 비활성 원본 1개, 릴마다 복제된다

    // 보이는 3칸 + 위아래 버퍼 3칸씩 = 9칸(NewSlot 원본 릴과 같은 버퍼 크기).
    // Glory 릴은 m_SymbolList 전체를 순환 스크롤 버퍼로 쓰고 보이는 창을 가운데(GetVisibleStartIndex)로 잡는다.
    // 버퍼는 "보이는 칸 + 정착 칸 수"를 감당할 만큼 넉넉해야 한다 — 모자라면 정착 위치에서 창이 빈다.
    [SerializeField] private int m_SymbolCountPerReel = 9;

    // 화면(ReelWindow)에 실제로 보이는 세로 칸 수. 릴 창 크기와 페이라인 테이블의 행 번호가 이 값을 전제로 한다.
    [SerializeField] private int m_VisibleSymbolCount = 3;

    // 스핀이 끝날 때 릴이 마지막으로 더 내려가 멈추는 칸 수(감속해서 정착하는 구간).
    // 버퍼가 이 값을 감당해야 한다 — (버퍼 - 보이는 칸)/2 + 이 값 + 보이는 칸 <= 버퍼.
    [SerializeField] private int m_SettleStepCount = 3;

    [SerializeField] private float m_ReelStopInterval = 0.2f;

    // 당첨 칸이 좌우로 기우는 각도(도)와 지속 시간.
    [SerializeField] private float m_WinShakeAngle = 12f;
    [SerializeField] private float m_WinShakeDuration = 0.5f;

    // 절반만 성립한 칸은 주판정 칸보다 약하게 흔든다. 같은 세기면 둘이 구분되지 않는다.
    private const float PARTIAL_SHAKE_RATIO = 0.45f;

    private List<HouseSlotSymbolSprite> m_SpritePool = new List<HouseSlotSymbolSprite>();
    private HouseRecord m_Record;
    private Coroutine m_StopRoutine;
    private bool m_HasExternalResult;

    private void Awake()
    {
        if (m_SymbolTemplate != null)
            m_SymbolTemplate.gameObject.SetActive(false);

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            m_ReelList[index].BuildSymbols(m_SymbolTemplate, m_SymbolCountPerReel, m_VisibleSymbolCount, m_SettleStepCount);
            m_ReelList[index].Init(index);
        }
    }

    // 종족이 바뀔 때(또는 최초 진입 시) 호출 — 스프라이트 풀/프레임/색을 다시 세팅한다.
    public void Apply(HouseRecord _record)
    {
        if (_record == null)
        {
            Logger.Error("[UIHouseSlotMachine] Apply Failed! record == null");
            return;
        }

        m_Record = _record;

        BuildSpritePool(_record);
        ApplyFrame(_record);

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            m_ReelList[index].ApplySpritePool(m_SpritePool);
            m_ReelList[index].Open();

            // Open()이 모든 칸을 타입 0으로 되돌리므로 무작위 채우기는 반드시 그 뒤여야 한다.
            m_ReelList[index].FillRandomSymbols();

            // Open()이 릴을 기준 위치에 두고 끝나므로, 스핀 종료 후와 같은 정착 위치로 맞춰준다.
            m_ReelList[index].ResetToSettledPosition();
        }
    }

    private void BuildSpritePool(HouseRecord _record)
    {
        m_SpritePool.Clear();

        // 풀 로딩은 여기서 한 번만 한다 — 심볼 칸마다 조회하면 칸 수만큼 중복 조회가 된다.
        List<Sprite> normalSprites = HouseSpriteLoader.Load(_record);
        Dictionary<string, Sprite> dicBlur = HouseSpriteLoader.LoadBlurDictionary(_record);

        for (int index = 0; index < normalSprites.Count; ++index)
        {
            Sprite normalSprite = normalSprites[index];

            // 블러가 없는 종족/파일이 있을 수 있어 없으면 null로 두고 UIHouseSlotSymbol이 원본으로 대체한다.
            Sprite blurSprite = null;
            dicBlur.TryGetValue(normalSprite.name, out blurSprite);

            HouseSlotSymbolSprite spriteSet = new HouseSlotSymbolSprite();
            spriteSet.NormalSprite = normalSprite;
            spriteSet.BlurSprite = blurSprite;
            m_SpritePool.Add(spriteSet);
        }
    }

    private void ApplyFrame(HouseRecord _record)
    {
        if (m_FrameImage != null)
        {
            Sprite frameSprite = ResUtil.Load<Sprite>($"Image/InGame/Reel/frame_{_record.Key}");
            if (frameSprite != null)
                m_FrameImage.sprite = frameSprite;
        }

        if (m_HeaderBarImage != null)
        {
            Color accentColor;
            if (ColorUtility.TryParseHtmlString($"#{_record.AccentColor}", out accentColor) == true)
                m_HeaderBarImage.color = accentColor;
            else
                Logger.Error($"[UIHouseSlotMachine] AccentColor 파싱 실패 - {_record.AccentColor}");
        }
    }

    // 릴 3개를 동시에 돌리기 시작한다.
    public void Spin()
    {
        if (m_StopRoutine != null)
        {
            StopCoroutine(m_StopRoutine);
            m_StopRoutine = null;
        }

        m_HasExternalResult = false;

        StopWinEffect();

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            // 이전 스핀이 정착 위치에 릴을 두고 끝났다 — 화면을 유지한 채 기준 위치로 되돌리고 굴린다.
            m_ReelList[index].ResetToBasePosition();
            m_ReelList[index].fsm.SetState(eReelState.Spin);
        }
    }

    // 판정기가 결과를 확정하면 호출한다. 배열 순서 = 릴 인덱스, 각 원소는 그 릴의 보이는 칸 결과(스프라이트 풀 인덱스).
    // TODO: 전력 -> 소환 수 환산 규칙이 확정되면 판정기가 이 메서드를 Spin()과 StopAll() 사이에서 호출해 연결한다.
    // 규칙이 아직 없으므로 지금은 이 메서드가 호출되지 않아도(무작위 결과로) 정상 동작한다.
    public void SetResult(int[][] _resultSymbolTypesByReel)
    {
        if (_resultSymbolTypesByReel == null)
        {
            Logger.Error("[UIHouseSlotMachine] SetResult Failed! resultSymbolTypesByReel == null");
            return;
        }

        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            if (index >= _resultSymbolTypesByReel.Length)
                continue;

            m_ReelList[index].SetResult(_resultSymbolTypesByReel[index]);
        }

        m_HasExternalResult = true;
    }

    // 무작위 3×3 결과를 만들어 돌려준다. 판정기가 이 값을 읽어 전력을 내고,
    // 같은 값을 SetResultByGrid()로 릴에 넣어 화면과 판정이 어긋나지 않게 한다.
    //
    // 좌표계는 **셀 인덱스 0~8(행 우선)**로 통일한다 — Judge가 쓰는 것과 같다.
    // 릴은 열이고 칸은 행이라 cell = row * 릴수 + reel 로 옮긴다. 이 변환을 호출부마다
    // 다시 쓰면 한쪽만 틀렸을 때 판정과 화면이 조용히 달라진다.
    public int[] CreateRandomGrid()
    {
        int poolCount = m_SpritePool.Count;
        int[] grid = new int[m_ReelList.Length * m_VisibleSymbolCount];

        if (poolCount <= 0)
        {
            Logger.Error("[UIHouseSlotMachine] CreateRandomGrid Failed! 스프라이트 풀이 비었다 (기대: Apply() 선행 호출)");
            return grid;
        }

        for (int reelIndex = 0; reelIndex < m_ReelList.Length; ++reelIndex)
        {
            for (int rowIndex = 0; rowIndex < m_VisibleSymbolCount; ++rowIndex)
            {
                grid[rowIndex * m_ReelList.Length + reelIndex] = Random.Range(0, poolCount);
            }
        }

        return grid;
    }

    // 셀 인덱스(0~8) 배열을 릴 기준 배열로 바꿔 릴에 넣는다.
    public void SetResultByGrid(int[] _grid)
    {
        int need = m_ReelList.Length * m_VisibleSymbolCount;
        if (_grid == null || _grid.Length < need)
        {
            Logger.Error($"[UIHouseSlotMachine] SetResultByGrid Failed! grid 길이 부족 - {(_grid == null ? "null" : _grid.Length.ToString())} (기대: {need})");
            return;
        }

        int[][] byReel = new int[m_ReelList.Length][];
        for (int reelIndex = 0; reelIndex < m_ReelList.Length; ++reelIndex)
        {
            byReel[reelIndex] = new int[m_VisibleSymbolCount];
            for (int rowIndex = 0; rowIndex < m_VisibleSymbolCount; ++rowIndex)
            {
                byReel[reelIndex][rowIndex] = _grid[rowIndex * m_ReelList.Length + reelIndex];
            }
        }

        SetResult(byReel);
    }

    public IReadOnlyList<HouseSlotSymbolSprite> spritePool => m_SpritePool;

    // 릴마다 시간차를 두고 순차 정지시킨다.
    public void StopAll()
    {
        if (m_HasExternalResult == false)
            ApplyRandomResult();

        m_StopRoutine = StartCoroutine(CoStopReelsSequentially());
    }

    // 판정기가 아직 없어 SetResult가 호출되지 않았을 때 무작위 결과로 채운다.
    private void ApplyRandomResult()
    {
        int poolCount = m_SpritePool.Count;
        if (poolCount <= 0)
            return;

        int[][] randomResult = new int[m_ReelList.Length][];
        for (int reelIndex = 0; reelIndex < m_ReelList.Length; ++reelIndex)
        {
            randomResult[reelIndex] = new int[m_VisibleSymbolCount];
            for (int rowIndex = 0; rowIndex < randomResult[reelIndex].Length; ++rowIndex)
            {
                randomResult[reelIndex][rowIndex] = Random.Range(0, poolCount);
            }
        }

        SetResult(randomResult);
    }

    private IEnumerator CoStopReelsSequentially()
    {
        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            if (m_ReelList[index].IsState(eReelState.Spin) == false)
                continue;

            m_ReelList[index].fsm.SetState(eReelState.Stop);
            yield return new WaitForSeconds(m_ReelStopInterval);
        }

        // 결과가 보이는 칸에 들어가는 건 릴이 Result 상태의 정착 트윈까지 마치고 Idle로 돌아온 뒤다.
        // 여기서 안 기다리면 아직 굴러가는 중인 칸을 읽어 당첨 판정이 엉뚱하게 나온다.
        while (IsAllReelIdle() == false)
        {
            yield return null;
        }

        PlayJudgeEffect();

        m_StopRoutine = null;
    }

    private bool IsAllReelIdle()
    {
        for (int index = 0; index < m_ReelList.Length; ++index)
        {
            if (m_ReelList[index].IsState(eReelState.Idle) == false)
                return false;
        }

        return true;
    }

    // 이번 스핀의 판정 결과. 릴이 멈춘 뒤 어느 칸을 반짝일지 정하는 데만 쓴다.
    private JudgeResult m_JudgeResult;

    // 릴을 멈추기 전에 판정 결과를 넘겨둔다. 정지 코루틴이 릴 정착까지 끝난 시점을 알고 있어서
    // 연출 타이밍을 밖에서 다시 재현하는 것보다 여기에 맡기는 쪽이 정확하다.
    public void SetJudgeResult(JudgeResult _result)
    {
        m_JudgeResult = _result;
    }

    // 판정에 실제로 걸린 칸을 반짝인다.
    //
    // 예전에는 SlotLineTable을 훑어 "라인 3칸이 전부 같은 심볼"이면 반짝였다. 그건 종족과 무관한
    // 슬롯머신 규칙이라 **7종족 중 6종족에서 거짓 신호였다** —
    // 장기(A-B-A)는 아예 안 반짝이고, 화투·마작·윷은 "같은 심볼"이 판정 근거가 아닌데도 반짝였다.
    // 플레이어는 반짝인 걸 보고 규칙을 유추하므로, 틀린 신호는 없느니만 못하다.
    //
    // 이제 판정기가 채운 칸을 그대로 쓴다. 종족마다 자기 문법대로 반짝인다.
    private void PlayJudgeEffect()
    {
        if (m_JudgeResult == null)
            return;

        PlayCellEffect(m_JudgeResult.ListHitCell, m_WinShakeAngle, m_WinShakeDuration);

        // 절반만 성립한 칸(체스 반정렬, 장기 진, 포커 페어, 슬롯 2매치)은 약하게 흔든다.
        // 전력은 나왔는데 릴이 조용하면 "왜 소환됐지"가 되고, 똑같이 흔들면 주판정과 구분이 안 된다.
        PlayCellEffect(m_JudgeResult.ListPartialCell,
            m_WinShakeAngle * PARTIAL_SHAKE_RATIO, m_WinShakeDuration * PARTIAL_SHAKE_RATIO);
    }

    private void PlayCellEffect(List<int> _listCell, float _angle, float _duration)
    {
        if (_listCell == null)
            return;

        for (int i = 0; i < _listCell.Count; ++i)
        {
            int cell = _listCell[i];
            if (cell < 0 || cell >= JudgeResult.GRID_SIZE)
                continue;

            // 칸 인덱스는 행 우선(cell = row * 릴수 + 릴), Judge·전장과 같은 좌표계다.
            int row = cell / m_ReelList.Length;
            int reelIndex = cell % m_ReelList.Length;

            UIHouseSlotSymbol symbol = m_ReelList[reelIndex].GetVisibleSymbol(row);
            if (symbol == null)
                continue;

            symbol.PlayWinEffect(_angle, _duration);
        }
    }

    private void StopWinEffect()
    {
        for (int reelIndex = 0; reelIndex < m_ReelList.Length; ++reelIndex)
        {
            for (int rowIndex = 0; rowIndex < m_VisibleSymbolCount; ++rowIndex)
            {
                UIHouseSlotSymbol symbol = m_ReelList[reelIndex].GetVisibleSymbol(rowIndex);
                if (symbol == null)
                    continue;

                symbol.StopWinEffect();
            }
        }
    }
}
