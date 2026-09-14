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
    // 좌표 규칙은 FieldLayout 이 소유한다(2026-09-14). 여기서 다시 정의하면 소유자가 둘이 된다.

    // 심볼이 32px이라 ×3. 정수 배율만 허용된다.
    private const float SLOT_SIZE = 96f;

    private List<UIInGameFieldSlot> m_ListSlot = new List<UIInGameFieldSlot>();

    // 드래그로 자리를 바꾸려면 명부와 스프라이트 풀을 다시 그릴 때까지 들고 있어야 한다.
    // 소유자는 RunData다 — 여기 있는 것은 표시를 위한 참조일 뿐 사본이 아니다.
    private RunRoster m_Roster;
    private IReadOnlyList<HouseSlotSymbolSprite> m_SpritePool;

    // 그리기 순서 정렬용 버퍼. 매번 새로 만들면 배치를 옮길 때마다 할당이 생긴다.
    private List<UIInGameFieldSlot> m_ListDrawOrder = new List<UIInGameFieldSlot>();

    // 어느 종족의 전장인가. 고유 유닛 스프라이트를 찾을 때 쓴다(캐시는 HouseSpriteLoader 가 갖는다).
    private string m_PatternHouseKey = string.Empty;

    // 드래그 중에만 켜지는 배치 가능 영역. **직렬화하지 않고 런타임에 만든다** —
    // 씬·프리팹을 고치지 않아도 되고, 크기가 FieldLayout 의 상수에서 파생되므로
    // 인스펙터 값과 코드가 갈릴 여지가 없다(9칸이 0으로 겹쳤던 사고와 같은 이유다).
    private RectTransform m_PlacementArea;

    // 영역 바탕. 전장 배경을 가리지 않을 만큼만 옅게 깐다.
    private static readonly Color PLACEMENT_AREA_COLOR = new Color(1f, 1f, 1f, 0.12f);

    public void Apply()
    {
        BuildSlots();
        LayoutSlots();
        Clear();
    }

    // 칸의 크기와 기준점만 잡는다. **자리는 여기서 정하지 않는다** —
    // 2026-09-14 자유 배치 이후 위치의 정본은 명부(RunUnit.FieldPosition)이고
    // RefreshSlots 가 그 값을 읽어 놓는다. 여기서 좌표를 또 계산하면 소유자가 둘이 된다.
    private void LayoutSlots()
    {
        for (int cell = 0; cell < m_ListSlot.Count && cell < JudgeResult.GRID_SIZE; ++cell)
        {
            RectTransform rectTransform = m_ListSlot[cell].transform as RectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(SLOT_SIZE, SLOT_SIZE);

            // 유닛이 붙기 전까지는 격자 기본 자리에 둔다. 비어 있으면 어차피 안 보인다.
            rectTransform.anchoredPosition = FieldLayout.GetDefaultPosition(cell);
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
    public void Show(RunRoster _roster, JudgeResult _result, IReadOnlyList<HouseSlotSymbolSprite> _spritePool,
        string _houseKey)
    {
        Clear();

        m_PatternHouseKey = _houseKey;

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

            Sprite sprite = HouseSpriteLoader.FindUnitSprite(runUnit, m_PatternHouseKey, m_SpritePool);
            if (sprite == null)
                continue;

            m_ListSlot[cell].SetUnit(sprite, runUnit.Grade);

            RectTransform rectTransform = m_ListSlot[cell].transform as RectTransform;
            rectTransform.anchoredPosition = runUnit.FieldPosition;
        }

        RefreshDrawOrder();
    }

    // 앞(아래)에 선 유닛이 뒤에 선 유닛을 가리도록 그리기 순서를 맞춘다.
    // 격자였을 때는 칸 번호가 곧 깊이라 SetSiblingIndex(cell + 1) 로 끝났는데,
    // 자유 배치에서는 **y 가 낮을수록 앞**이므로 좌표로 다시 정렬해야 한다.
    private void RefreshDrawOrder()
    {
        m_ListDrawOrder.Clear();

        for (int cell = 0; cell < m_ListSlot.Count && cell < RunRoster.FIELD_SIZE; ++cell)
        {
            if (m_Roster.GetFieldUnit(cell) == null)
                continue;

            m_ListDrawOrder.Add(m_ListSlot[cell]);
        }

        // y 내림차순 — 뒤(위)에 있는 것을 먼저 그린다. 같은 y 면 순서를 유지한다.
        for (int i = 1; i < m_ListDrawOrder.Count; ++i)
        {
            UIInGameFieldSlot moving = m_ListDrawOrder[i];
            float movingY = GetSlotY(moving);

            int insert = i;
            while (insert > 0 && GetSlotY(m_ListDrawOrder[insert - 1]) < movingY)
            {
                m_ListDrawOrder[insert] = m_ListDrawOrder[insert - 1];
                insert -= 1;
            }

            m_ListDrawOrder[insert] = moving;
        }

        // 0번은 비활성 템플릿 자리라 +1 한다.
        for (int i = 0; i < m_ListDrawOrder.Count; ++i)
        {
            m_ListDrawOrder[i].transform.SetSiblingIndex(i + 1);
        }
    }

    private static float GetSlotY(UIInGameFieldSlot _slot)
    {
        RectTransform rectTransform = _slot.transform as RectTransform;
        return (rectTransform != null) ? rectTransform.anchoredPosition.y : 0f;
    }

    // 드래그로 유닛을 옮긴다. 명부가 거부하면(영역 밖으로 잘린 뒤에도 다른 유닛과 너무 가까우면) false.
    // 드래그 중에만 배치 가능 영역을 보여준다.
    public void ShowPlacementArea(bool _isVisible)
    {
        if (_isVisible == false)
        {
            if (m_PlacementArea != null)
                m_PlacementArea.gameObject.SetActive(false);

            return;
        }

        if (m_PlacementArea == null)
            m_PlacementArea = CreatePlacementArea();

        if (m_PlacementArea == null)
            return;

        m_PlacementArea.gameObject.SetActive(true);

        // 칸이 나중에 만들어져도 늘 맨 뒤에 깔린다.
        m_PlacementArea.SetAsFirstSibling();
    }

    // 그 자리에 놓을 수 있는가. 드래그 중 매 프레임 물어보는 경로라 할당하지 않는다.
    //
    // **영역 밖과 간격 위반을 함께 본다.** 실제 거부는 간격 위반뿐이고 영역 밖은
    // SetFieldPosition 이 잘라서 받지만, 끄는 사람 입장에선 "의도한 자리에 못 놓는다"는
    // 점이 같다. 잘려서 엉뚱한 곳에 붙는 것을 미리 알려주는 편이 낫다.
    public bool IsPlacementValid(int _cell, Vector2 _position)
    {
        if (m_Roster == null)
            return false;

        if (FieldLayout.IsInside(_position) == false)
            return false;

        return m_Roster.IsPositionFree(_position, _cell);
    }

    private RectTransform CreatePlacementArea()
    {
        if (m_SlotRoot == null)
            return null;

        GameObject areaObject = new GameObject("PlacementArea", typeof(RectTransform));
        areaObject.transform.SetParent(m_SlotRoot, false);

        UnityEngine.UI.Image areaImage = areaObject.AddComponent<UnityEngine.UI.Image>();
        areaImage.color = PLACEMENT_AREA_COLOR;

        // 끌고 있는 칸의 포인터 판정을 가로채면 안 된다.
        areaImage.raycastTarget = false;

        RectTransform areaRect = areaObject.transform as RectTransform;
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.zero;
        areaRect.pivot = Vector2.zero;

        // 칸의 pivot 이 (0,0)이라 anchoredPosition 은 **좌하단 모서리**다.
        // 그래서 영역이 실제로 덮는 범위는 모서리 허용 범위 + 칸 한 변이다.
        areaRect.anchoredPosition = new Vector2(FieldLayout.AREA_MIN_X, FieldLayout.AREA_MIN_Y);
        areaRect.sizeDelta = new Vector2(
            FieldLayout.AREA_MAX_X - FieldLayout.AREA_MIN_X + SLOT_SIZE,
            FieldLayout.AREA_MAX_Y - FieldLayout.AREA_MIN_Y + SLOT_SIZE);

        return areaRect;
    }

    public bool TryMoveUnit(int _cell, Vector2 _position)
    {
        if (m_Roster == null)
            return false;

        RunUnit unit = m_Roster.GetFieldUnit(_cell);
        if (unit == null)
            return false;

        if (m_Roster.SetFieldPosition(unit, _position) == false)
            return false;

        // 명부가 좌표를 잘랐을 수 있으므로 저장된 값으로 다시 그린다.
        RefreshSlots();
        return true;
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

    // ---------------- 드래그 배치 ----------------
    //
    // 2026-09-14 자유 배치로 바뀌면서 **중재가 단순해졌다.** 예전에는 어느 칸에 놓였는지
    // 판정해야 해서 출발지·목적지를 모아 뒀는데(IDropHandler), 이제는 놓인 좌표를 그대로 쓴다.
    // 칸은 자기 위치를 옮기고 끝에 TryMoveUnit 을 부르기만 한다.

    // 명부를 들고 있을 때만 배치를 바꿀 수 있다. 전투 시작 시 Clear()가 명부를 놓으므로
    // 전투 중 드래그는 별도 플래그 없이 막힌다.
    public bool IsDragAllowed()
    {
        return (m_Roster != null);
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
