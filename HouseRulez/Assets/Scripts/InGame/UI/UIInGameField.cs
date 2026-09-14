using System.Collections.Generic;
using UnityEngine;

// 전장 3×3. 릴 3×3과 칸이 1:1로 대응한다(GDD §전투와 배치).
// 릴 1열=후열, 2열=중열, 3열=전열이라는 역할 구분은 전투가 붙을 때 여기에 얹는다.
//
// 칸 인덱스는 0~8 행 우선이며 Judge·UIHouseSlotMachine과 같은 좌표계다.
public class UIInGameField : MonoBehaviour
{
    [SerializeField] private Transform m_SlotRoot;
    [SerializeField] private UIInGameFieldSlot m_SlotTemplate;
    // 요약은 접이식 패널이 맡는다. 전장 위에 늘 띄우면 성문·유닛과 겹친다.
    [SerializeField] private UIInGameSummary m_Summary;

    // 배치 규칙 — GDD §전투와 배치
    //  · 릴의 **열**이 전장의 깊이다. 1열=후열(원거리) · 2열=중열 · 3열=전열(탱커).
    //    적이 우측에서 진격하므로 전열이 오른쪽 끝에 선다.
    //  · 릴의 **행**은 3개 레인이다. 뒤 레인일수록 화면 위쪽으로 올리고 오른쪽으로 밀어
    //    바닥면이 비스듬히 깔린 것처럼 보이게 한다.
    //
    // 크기는 레인마다 줄이지 않는다. 심볼이 32px 픽셀아트라 ×3(96px) 같은 정수 배율만
    // 허용된다 — 원근을 준다고 80px 같은 값을 쓰면 픽셀이 뭉개진다.
    // 직렬화하지 않는다. 컴포넌트가 씬에 저장된 뒤 필드를 추가하면 그 값이 0으로 들어와
    // 9칸이 전부 같은 자리에 겹치는 사고가 실제로 났다. 배치의 소유자는 코드 한 곳이다.
    private const float COLUMN_SPACING = 108f;
    private const float LANE_STEP_Y = 52f;
    private const float LANE_STEP_X = 30f;

    // 심볼이 32px이라 ×3. 정수 배율만 허용된다.
    private const float SLOT_SIZE = 96f;

    private const int COLUMN_COUNT = 3;

    private List<UIInGameFieldSlot> m_ListSlot = new List<UIInGameFieldSlot>();

    // 드래그로 자리를 바꾸려면 명부와 스프라이트 풀을 다시 그릴 때까지 들고 있어야 한다.
    // 소유자는 RunData다 — 여기 있는 것은 표시를 위한 참조일 뿐 사본이 아니다.
    private RunRoster m_Roster;
    private IReadOnlyList<HouseSlotSymbolSprite> m_SpritePool;

    private int m_DragFromCell = RunRoster.CELL_NONE;
    private int m_DragToCell = RunRoster.CELL_NONE;

    public void Apply()
    {
        BuildSlots();
        LayoutSlots();
        Clear();
    }

    // 칸을 열·레인 좌표로 직접 놓는다. GridLayoutGroup은 균일 격자만 만들 수 있어
    // 레인별 x 밀기(원근)를 표현하지 못한다.
    private void LayoutSlots()
    {
        for (int cell = 0; cell < m_ListSlot.Count && cell < JudgeResult.GRID_SIZE; ++cell)
        {
            int row = cell / COLUMN_COUNT;
            int column = cell % COLUMN_COUNT;

            // row 0이 가장 뒤 레인이라 화면에서 가장 위로 간다.
            int laneFromFront = (COLUMN_COUNT - 1) - row;

            RectTransform rectTransform = m_ListSlot[cell].transform as RectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(SLOT_SIZE, SLOT_SIZE);
            rectTransform.anchoredPosition = new Vector2(
                column * COLUMN_SPACING + laneFromFront * LANE_STEP_X,
                laneFromFront * LANE_STEP_Y);

            // 앞 레인이 뒤 레인을 가리도록 그리기 순서를 맞춘다.
            // 셀 인덱스가 행 우선이라 그대로 쓰면 row 0(가장 뒤)이 먼저, row 2(가장 앞)가 나중에 그려진다.
            // 0번은 비활성 템플릿 자리라 +1 한다.
            rectTransform.SetSiblingIndex(cell + 1);
        }
    }

    // 템플릿을 9개로 늘린다. 매번 파괴/재생성하지 않고 부족분만 만든다.
    private void BuildSlots()
    {
        if (m_SlotRoot == null || m_SlotTemplate == null)
        {
            Logger.Error("[UIInGameField] BuildSlots Failed! SlotRoot 또는 SlotTemplate 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        m_SlotTemplate.gameObject.SetActive(false);

        m_ListSlot.Clear();
        m_SlotRoot.GetComponentsInChildren(true, m_ListSlot);
        m_ListSlot.Remove(m_SlotTemplate);

        while (m_ListSlot.Count < JudgeResult.GRID_SIZE)
        {
            UIInGameFieldSlot slot = Instantiate(m_SlotTemplate, m_SlotRoot);
            m_ListSlot.Add(slot);
        }

        for (int i = 0; i < m_ListSlot.Count; ++i)
        {
            m_ListSlot[i].gameObject.SetActive(i < JudgeResult.GRID_SIZE);
            m_ListSlot[i].transform.SetSiblingIndex(i + 1);

            // 칸 인덱스를 알려주고 포인터를 받을 준비를 시킨다.
            // 씬의 템플릿에는 raycast용 Image가 없어서 칸이 스스로 붙인다.
            m_ListSlot[i].Setup(this, i);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < m_ListSlot.Count; ++i)
        {
            m_ListSlot[i].Clear();
        }

        // 명부를 놓으면 드래그가 막힌다. 전투 시작 시 이 함수가 불리므로
        // "전투 중에는 배치를 못 바꾼다"가 별도 플래그 없이 성립한다.
        m_Roster = null;
        m_SpritePool = null;
        m_DragFromCell = RunRoster.CELL_NONE;
        m_DragToCell = RunRoster.CELL_NONE;

        if (m_Summary != null)
            m_Summary.SetText(string.Empty);
    }

    // 전장을 그린다. 칸에 무엇이 서 있는지는 **명부**가 정본이다(2026-09-11) —
    // 예전엔 판정 결과를 직접 그렸는데, 유닛이 스핀을 넘어 누적되기 시작하면서
    // "이번 스핀에 나온 것"과 "지금 전장에 서 있는 것"이 달라졌다.
    //
    // 판정 요약(_result)은 여전히 스핀 1회의 것이다. 그건 "왜 이만큼 나왔나"를 설명하는 식이라
    // 누적 상태가 아니라 방금 굴린 결과를 보여줘야 한다.
    //
    // _spritePool은 슬롯머신이 이미 만들어 둔 것을 넘겨받는다 — 여기서 다시 로드하면 같은 파일을 두 번 읽는다.
    public void Show(RunRoster _roster, JudgeResult _result, IReadOnlyList<HouseSlotSymbolSprite> _spritePool)
    {
        Clear();

        if (_roster == null || _spritePool == null)
        {
            Logger.Error("[UIInGameField] Show Failed! 인자 null (기대: 명부·스프라이트 풀)");
            return;
        }

        m_Roster = _roster;
        m_SpritePool = _spritePool;

        RefreshSlots();

        if (m_Summary != null && _result != null)
            m_Summary.SetText(BuildSummaryText(_result));
    }

    // 칸만 다시 그린다. **판정 요약은 건드리지 않는다** —
    // 요약은 "방금 굴린 스핀"의 것이라 드래그로 자리를 바꿨다고 달라지지 않는다.
    private void RefreshSlots()
    {
        if (m_Roster == null || m_SpritePool == null)
            return;

        for (int cell = 0; cell < m_ListSlot.Count && cell < RunRoster.FIELD_SIZE; ++cell)
        {
            m_ListSlot[cell].Clear();

            RunUnit runUnit = m_Roster.GetFieldUnit(cell);
            if (runUnit == null)
                continue;

            if (runUnit.SymbolType < 0 || runUnit.SymbolType >= m_SpritePool.Count)
            {
                Logger.Error($"[UIInGameField] RefreshSlots - 심볼이 풀 범위 밖이라 건너뛴다: {runUnit.SymbolType} (기대: 0~{m_SpritePool.Count - 1})");
                continue;
            }

            m_ListSlot[cell].SetUnit(m_SpritePool[runUnit.SymbolType].NormalSprite, runUnit.Grade);
        }
    }

    // 연차 이동 중 9칸에 제자리걸음을 켜고 끈다. 빈 칸은 칸 쪽에서 걸러진다.
    public void SetWalking(bool _isWalking)
    {
        for (int i = 0; i < m_ListSlot.Count; ++i)
        {
            if (m_ListSlot[i] == null)
                continue;

            m_ListSlot[i].SetWalking(_isWalking);
        }
    }

    // ---------------- 드래그 배치 (2026-09-13) ----------------
    //
    // 칸은 자기가 끌렸다는 것만 알고, 어디에 놓였는지와 명부를 고치는 일은 여기가 맡는다.
    // Unity의 호출 순서는 OnBeginDrag -> OnDrag... -> (대상 칸의)OnDrop -> OnEndDrag 이므로,
    // 출발지와 목적지를 모아 두고 OnEndDrag 시점에 한 번만 처리한다.

    // 명부를 들고 있을 때만 배치를 바꿀 수 있다. 전투 시작 시 Clear()가 명부를 놓으므로
    // 전투 중 드래그는 별도 플래그 없이 막힌다.
    public bool IsDragAllowed()
    {
        return (m_Roster != null);
    }

    public void OnSlotDragBegin(int _cell)
    {
        m_DragFromCell = _cell;
        m_DragToCell = RunRoster.CELL_NONE;
    }

    public void OnSlotDrop(int _cell)
    {
        m_DragToCell = _cell;
    }

    // 드래그가 끝났다. 유효한 목적지가 있으면 **명부에서** 자리를 맞바꾼다.
    // 화면만 바꾸면 전투는 옛 배치로 싸운다 — 전장의 정본은 RunRoster다(2026-09-11).
    public void OnSlotDragEnd()
    {
        int from = m_DragFromCell;
        int to = m_DragToCell;

        m_DragFromCell = RunRoster.CELL_NONE;
        m_DragToCell = RunRoster.CELL_NONE;

        if (m_Roster == null)
            return;

        // 칸 밖에 놓았으면 목적지가 없다. 제자리에 놓은 것도 바꿀 것이 없다.
        if (from < 0 || to < 0)
            return;

        if (from >= to && from <= to)
            return;

        if (m_Roster.SwapField(from, to) == false)
            return;

        RefreshSlots();
    }

    // 결과만 적으면 종족 규칙을 배울 수가 없다. 무엇이 몇 개 성립해 얼마가 됐는지 식으로 적는다.
    //
    //   포 넘기 2 · 대포 1
    //   포 넘기 2 × 3.8 = 7.6
    //   대포 1 × 3.8 = 3.8
    //   전력 11.4  →  소환 9기
    //
    // "전력 1.9인데 왜 2기?"(반올림)도 마지막 줄에서 함께 풀린다.
    private static string BuildSummaryText(JudgeResult _result)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(_result.PatternName);

        for (int i = 0; i < _result.ListTerm.Count; ++i)
        {
            JudgeTerm term = _result.ListTerm[i];
            builder.Append(System.Environment.NewLine);
            builder.Append($"{term.Label} {term.Value:0.##} × {term.Coef:0.##} = {term.total:0.##}");
        }

        builder.Append(System.Environment.NewLine);
        builder.Append($"전력 {_result.Power:F1}  →  소환 {_result.summonCount}기");

        return builder.ToString();
    }
}
