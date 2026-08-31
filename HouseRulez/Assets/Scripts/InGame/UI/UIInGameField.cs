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
        }
    }

    public void Clear()
    {
        for (int i = 0; i < m_ListSlot.Count; ++i)
        {
            m_ListSlot[i].Clear();
        }

        if (m_Summary != null)
            m_Summary.SetText(string.Empty);
    }

    // 판정 결과를 전장에 세운다. _grid는 스핀 결과(칸별 심볼 인덱스)이고,
    // _spritePool은 슬롯머신이 이미 만들어 둔 것을 그대로 넘겨받는다 — 여기서 다시 로드하면 같은 파일을 두 번 읽는다.
    public void ShowSummon(JudgeResult _result, int[] _grid, IReadOnlyList<HouseSlotSymbolSprite> _spritePool)
    {
        Clear();

        if (_result == null || _grid == null || _spritePool == null)
        {
            Logger.Error("[UIInGameField] ShowSummon Failed! 인자 null (기대: 판정 결과·그리드·스프라이트 풀)");
            return;
        }

        for (int i = 0; i < _result.ListSummon.Count; ++i)
        {
            SummonSlot summon = _result.ListSummon[i];
            if (summon.Cell < 0 || summon.Cell >= m_ListSlot.Count)
                continue;

            // 판정기가 심볼을 직접 정했으면 그걸 쓴다(윷). 아니면 그 칸에 나온 심볼을 쓴다.
            int symbolType = (summon.SymbolType >= 0) ? summon.SymbolType : _grid[summon.Cell];
            if (symbolType < 0 || symbolType >= _spritePool.Count)
                continue;

            m_ListSlot[summon.Cell].SetUnit(_spritePool[symbolType].NormalSprite, summon.Grade);
        }

        if (m_Summary != null)
            m_Summary.SetText($"{_result.PatternName}" + System.Environment.NewLine + $"전력 {_result.Power:F1}  ·  소환 {_result.summonCount}기");
    }
}
