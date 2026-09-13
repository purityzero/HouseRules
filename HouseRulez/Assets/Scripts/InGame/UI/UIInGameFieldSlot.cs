using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전장 3x3의 한 칸. 소환된 유닛 하나를 보여주고, 드래그로 자리를 옮긴다.
// 아군 유닛 아트를 따로 만들지 않고 릴 심볼 스프라이트를 그대로 쓴다 —
// "릴에 나온 말이 그대로 전장에 선다"는 GDD 컨셉과 맞고 아트 비용도 들지 않는다.
//
// 드래그는 이 칸이 시작하고, **놓을 자리 판정과 명부 수정은 UIInGameField가 한다.**
// 칸 혼자서는 어디에 놓였는지 알 수 없고, 명부를 고칠 권한도 없어야 한다.
public class UIInGameFieldSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image m_SymbolImage;
    [SerializeField] private TextMeshProUGUI m_GradeText;

    // 1성은 등급 표시를 하지 않는다. 대부분이 1성이라(체스 95%) 전부 표시하면 화면이 숫자로 덮인다.
    private const int GRADE_HIDE_BELOW = 2;

    // 드래그 중 원본을 흐리게 해 "들어올렸다"를 보여준다.
    private const float DRAG_ALPHA = 0.45f;

    private UIInGameField m_Owner;
    private int m_Cell = RunRoster.CELL_NONE;

    // 칸 전체가 포인터를 받게 하는 투명 이미지.
    // **빈 칸에도 놓을 수 있어야 하므로 심볼 이미지로는 대신할 수 없다** — 빈 칸은 심볼이 꺼져 있다.
    private Image m_RaycastImage;

    private RectTransform m_SymbolRect;
    private Vector2 m_SymbolHomePosition;
    private bool m_isDragging;
    private float m_CanvasScale = 1f;

    // 드래그로 올린 그리기 순서를 되돌리기 위해 기억한다. -1이면 "올린 적 없음"이다.
    // 복원 값을 cell + 1로 계산하지 않는 이유 — 그 규칙은 UIInGameField.LayoutSlots의 것이고,
    // 칸이 그것을 알면 두 곳이 같은 규칙을 각자 들게 된다.
    private const int SIBLING_HOME_NONE = -1;
    private int m_SiblingHomeIndex = SIBLING_HOME_NONE;

    public int cell => m_Cell;

    // 칸 인덱스를 받고 드래그를 받을 준비를 한다. UIInGameField가 칸을 만든 직후 부른다.
    //
    // raycast용 Image를 코드로 붙인다 — 칸은 템플릿을 Instantiate 한 런타임 오브젝트라
    // 인스펙터에서 미리 연결할 대상이 없다(CODE.MD 「컴포넌트 참조는 직렬화 필드로」의 예외).
    // 씬의 SlotTemplate 에는 RectTransform 과 이 스크립트뿐이어서 그대로는 포인터를 못 받는다.
    public void Setup(UIInGameField _owner, int _cell)
    {
        m_Owner = _owner;
        m_Cell = _cell;

        if (m_RaycastImage == null)
        {
            m_RaycastImage = GetComponent<Image>();
            if (m_RaycastImage == null)
                m_RaycastImage = gameObject.AddComponent<Image>();
        }

        // 보이지 않지만 포인터는 받는다. alpha가 0이어도 raycastTarget이 켜져 있으면 잡힌다.
        m_RaycastImage.color = new Color(1f, 1f, 1f, 0f);
        m_RaycastImage.raycastTarget = true;

        if (m_SymbolRect == null && m_SymbolImage != null)
        {
            m_SymbolRect = m_SymbolImage.transform as RectTransform;
            m_SymbolHomePosition = m_SymbolRect.anchoredPosition;
        }
    }

    public void Clear()
    {
        // 드래그 중에 전투가 시작되는 등으로 칸이 비워질 수 있다. 표시 상태를 먼저 되돌린다.
        RestoreSymbolTransform();

        if (m_SymbolImage != null)
        {
            m_SymbolImage.enabled = false;
            m_SymbolImage.sprite = null;
        }

        if (m_GradeText != null)
            m_GradeText.gameObject.SetActive(false);
    }

    public void SetUnit(Sprite _symbolSprite, int _grade)
    {
        if (m_SymbolImage != null)
        {
            m_SymbolImage.sprite = _symbolSprite;
            m_SymbolImage.enabled = (_symbolSprite != null);
        }

        if (m_GradeText != null)
        {
            bool showGrade = (_grade >= GRADE_HIDE_BELOW);
            m_GradeText.gameObject.SetActive(showGrade);
            if (showGrade == true)
                m_GradeText.text = $"★{_grade}";
        }
    }

    public void OnBeginDrag(PointerEventData _eventData)
    {
        if (m_Owner == null || m_SymbolRect == null)
            return;

        // 전투 중이거나 명부가 없으면 배치를 바꿀 수 없다.
        if (m_Owner.IsDragAllowed() == false)
            return;

        // 빈 칸은 끌 것이 없다.
        if (m_SymbolImage == null || m_SymbolImage.enabled == false)
            return;

        m_isDragging = true;
        m_SymbolHomePosition = m_SymbolRect.anchoredPosition;
        m_CanvasScale = GetCanvasScale();

        SetSymbolAlpha(DRAG_ALPHA);

        // 끌고 있는 칸이 포인터를 먹으면 그 아래 칸이 드롭을 받지 못한다.
        m_RaycastImage.raycastTarget = false;
        m_SymbolImage.raycastTarget = false;

        // 다른 칸 위로 올려 그린다. 안 올리면 앞 레인 칸에 가려진다(LayoutSlots가 앞 레인을 나중에 그린다).
        // 끝낼 때 되돌려야 한다 — 안 되돌리면 뒤 레인이 앞 레인 위에 남는다(2026-09-13 QA 지적).
        m_SiblingHomeIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();

        m_Owner.OnSlotDragBegin(m_Cell);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (m_isDragging == false)
            return;

        // 부모(칸)는 그대로 두고 심볼만 움직인다 — 끝낼 때 anchoredPosition 하나로 되돌아온다.
        m_SymbolRect.anchoredPosition += _eventData.delta / m_CanvasScale;
    }

    public void OnEndDrag(PointerEventData _eventData)
    {
        if (m_isDragging == false)
            return;

        m_isDragging = false;
        RestoreSymbolTransform();

        // 교환 성사 여부와 무관하게 여기서 마무리를 알린다.
        // Unity는 OnDrop을 OnEndDrag보다 먼저 부르므로, 이 시점에 목적지가 이미 정해져 있다.
        if (m_Owner != null)
            m_Owner.OnSlotDragEnd();
    }

    // 다른 칸이 이 칸 위에 놓였다. 목적지만 알려주고 교환은 UIInGameField가 한다.
    public void OnDrop(PointerEventData _eventData)
    {
        if (m_Owner == null)
            return;

        m_Owner.OnSlotDrop(m_Cell);
    }

    private void RestoreSymbolTransform()
    {
        // 드래그로 올린 적이 있을 때만 되돌린다. 이 함수는 Clear()에서도 불리므로
        // 무조건 SetSiblingIndex를 부르면 드래그하지 않은 칸의 순서를 망친다.
        if (m_SiblingHomeIndex > SIBLING_HOME_NONE)
        {
            transform.SetSiblingIndex(m_SiblingHomeIndex);
            m_SiblingHomeIndex = SIBLING_HOME_NONE;
        }

        if (m_SymbolRect != null)
            m_SymbolRect.anchoredPosition = m_SymbolHomePosition;

        SetSymbolAlpha(1f);

        if (m_RaycastImage != null)
            m_RaycastImage.raycastTarget = true;

        if (m_SymbolImage != null)
            m_SymbolImage.raycastTarget = true;
    }

    private void SetSymbolAlpha(float _alpha)
    {
        if (m_SymbolImage == null)
            return;

        Color color = m_SymbolImage.color;
        color.a = _alpha;
        m_SymbolImage.color = color;
    }

    // 캔버스가 배율을 쓰면 포인터 delta와 anchoredPosition의 눈금이 다르다.
    // 드래그 시작 때 한 번만 재고 드래그 중에는 다시 찾지 않는다(매 프레임 탐색 비용).
    private float GetCanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return 1f;

        return (canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;
    }
}
