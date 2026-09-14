using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전장 3x3의 한 칸. 소환된 유닛 하나를 보여주고, 드래그로 자리를 옮긴다.
// 아군 유닛 아트를 따로 만들지 않고 릴 심볼 스프라이트를 그대로 쓴다 —
// "릴에 나온 말이 그대로 전장에 선다"는 GDD 컨셉과 맞고 아트 비용도 들지 않는다.
//
// 드래그는 이 칸이 시작하고 **명부 수정은 UIInGameField가 한다.** 칸에 명부를 고칠 권한은 없다.
//
// 2026-09-14 자유 배치 — 예전에는 격자 칸끼리 교환이라 "어디에 놓였는지"를 판정해야 했지만
// (그래서 IDropHandler 가 필요했다), 이제는 **놓인 좌표 그대로** 선다. 칸이 자기 위치를 옮기고
// 끝에 그 좌표를 넘기면 된다.
public class UIInGameFieldSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image m_SymbolImage;
    [SerializeField] private TextMeshProUGUI m_GradeText;

    // 1성은 등급 표시를 하지 않는다. 대부분이 1성이라(체스 95%) 전부 표시하면 화면이 숫자로 덮인다.
    private const int GRADE_HIDE_BELOW = 2;

    // 드래그 중 원본을 흐리게 해 "들어올렸다"를 보여준다.
    private const float DRAG_ALPHA = 0.45f;

    // 드래그 중 지금 자리에 놓을 수 있는지를 색으로 알린다.
    // 놓아봐야 아는 것이 아니라 끌면서 알 수 있어야 한다(2026-09-14 사용자 요청).
    //
    // **채도를 높게 잡는다.** DRAG_ALPHA 가 0.45 라 옅은 색은 화면에서 거의 안 읽힌다
    // (2026-09-14 QA: "구분은 되지만 특히 초록이 꽤 옅어 즉시성이 약하다").
    // 알파를 올리는 대신 채도로 해결한 것은 "들어올렸다"는 반투명 표현을 지키기 위해서다.
    private static readonly Color DRAG_TINT_VALID = new Color(0.3f, 1f, 0.3f);
    private static readonly Color DRAG_TINT_INVALID = new Color(1f, 0.3f, 0.3f);

    private UIInGameField m_Owner;
    private int m_Cell = RunRoster.CELL_NONE;

    // 칸 전체가 포인터를 받게 하는 투명 이미지.
    // **빈 칸에도 놓을 수 있어야 하므로 심볼 이미지로는 대신할 수 없다** — 빈 칸은 심볼이 꺼져 있다.
    private Image m_RaycastImage;

    private RectTransform m_SymbolRect;
    private Vector2 m_SymbolHomePosition;

    // 자유 배치에서는 **칸 자체가 움직인다.** 놓은 자리가 곧 유닛의 자리이기 때문이다.
    // (격자 시절에는 심볼만 옮기고 끝에 되돌렸다 — 자리는 칸 번호가 정했으니까)
    private RectTransform m_RectTransform;
    private Vector2 m_DragHomePosition;
    private bool m_isDragging;
    private float m_CanvasScale = 1f;

    // 드래그로 올린 그리기 순서를 되돌리기 위해 기억한다. -1이면 "올린 적 없음"이다.
    // 복원 값을 cell + 1로 계산하지 않는 이유 — 그 규칙은 UIInGameField.LayoutSlots의 것이고,
    // 칸이 그것을 알면 두 곳이 같은 규칙을 각자 들게 된다.
    private const int SIBLING_HOME_NONE = -1;
    private int m_SiblingHomeIndex = SIBLING_HOME_NONE;

    // 연차 이동 중 제자리걸음. 스프라이트에 걷기 프레임이 없어(종족마다 단일 이미지)
    // 위아래 까딱임으로 대체한다 — 아트 추가 없이 "걷고 있다"가 읽힌다.
    private const float WALK_BOB_HEIGHT = 6f;
    private const float WALK_BOB_DURATION = 0.22f;

    private bool m_isWalking;

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

        // 유닛이 붙을 때 SetUnit 이 켠다. 빈 칸은 포인터를 받지 않는다.
        m_RaycastImage.raycastTarget = false;

        if (m_RectTransform == null)
            m_RectTransform = transform as RectTransform;

        if (m_SymbolRect == null && m_SymbolImage != null)
        {
            m_SymbolRect = m_SymbolImage.transform as RectTransform;
            m_SymbolHomePosition = m_SymbolRect.anchoredPosition;
        }
    }

    public void Clear()
    {
        // 무한 루프 트윈이라 여기서 끊지 않으면 빈 칸이 계속 까딱인다.
        SetWalking(false);

        // 드래그 중에 전투가 시작되는 등으로 칸이 비워질 수 있다. 표시 상태를 먼저 되돌린다.
        RestoreSymbolTransform();

        if (m_SymbolImage != null)
        {
            m_SymbolImage.enabled = false;
            m_SymbolImage.sprite = null;
        }

        if (m_GradeText != null)
            m_GradeText.gameObject.SetActive(false);

        if (m_RaycastImage != null)
            m_RaycastImage.raycastTarget = false;
    }

    public void SetUnit(Sprite _symbolSprite, int _grade)
    {
        if (m_SymbolImage != null)
        {
            m_SymbolImage.sprite = _symbolSprite;
            m_SymbolImage.enabled = (_symbolSprite != null);
        }

        // 유닛이 있을 때만 포인터를 받는다. 자유 배치에서는 빈 칸이 드롭 대상이 아니고,
        // 켜 두면 보이지 않는 빈 칸이 유닛 위를 덮어 드래그를 가로챈다.
        if (m_RaycastImage != null)
            m_RaycastImage.raycastTarget = (_symbolSprite != null);

        if (m_GradeText != null)
        {
            bool showGrade = (_grade >= GRADE_HIDE_BELOW);
            m_GradeText.gameObject.SetActive(showGrade);
            if (showGrade == true)
                m_GradeText.text = $"★{_grade}";
        }
    }

    // 연차 이동 중 제자리걸음을 켜고 끈다. 배경만 흐르고 유닛이 굳어 있으면
    // "우리가 이동한다"가 아니라 "배경이 움직인다"로 보인다.
    public void SetWalking(bool _isWalking)
    {
        if (m_SymbolRect == null)
            return;

        if (_isWalking == false)
        {
            m_isWalking = false;
            m_SymbolRect.DOKill();
            m_SymbolRect.anchoredPosition = m_SymbolHomePosition;
            return;
        }

        // 드래그 중이면 걸지 않는다 — 포인터를 따라가는 이동과 같은 값을 서로 덮어쓴다.
        if (m_isDragging == true)
            return;

        // 빈 칸은 걸을 것이 없다.
        if (m_SymbolImage == null || m_SymbolImage.enabled == false)
            return;

        if (m_isWalking == true)
            return;

        m_isWalking = true;
        m_SymbolRect.DOKill();
        m_SymbolRect.anchoredPosition = m_SymbolHomePosition;

        // 무한 루프다. SetWalking(false) · Clear() · OnDestroy 에서 반드시 끊는다.
        m_SymbolRect.DOAnchorPosY(m_SymbolHomePosition.y + WALK_BOB_HEIGHT, WALK_BOB_DURATION)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnBeginDrag(PointerEventData _eventData)
    {
        if (m_Owner == null || m_RectTransform == null)
            return;

        // 전투 중이거나 명부가 없으면 배치를 바꿀 수 없다.
        if (m_Owner.IsDragAllowed() == false)
            return;

        // 빈 칸은 끌 것이 없다.
        if (m_SymbolImage == null || m_SymbolImage.enabled == false)
            return;

        // 걷기 트윈이 돌고 있으면 포인터 이동과 같은 값을 서로 덮어쓴다.
        SetWalking(false);

        m_isDragging = true;
        m_DragHomePosition = m_RectTransform.anchoredPosition;
        m_CanvasScale = GetCanvasScale();

        SetSymbolColor(DRAG_TINT_VALID, DRAG_ALPHA);

        // 어디까지 둘 수 있는지 끄는 동안 보여준다.
        m_Owner.ShowPlacementArea(true);
        RefreshDragTint();

        // 끌고 있는 칸이 포인터를 먹으면 아래 칸의 판정이 흐려진다.
        m_RaycastImage.raycastTarget = false;
        m_SymbolImage.raycastTarget = false;

        // 끄는 동안은 맨 위에 그린다. 놓은 뒤의 순서는 UIInGameField 가 y 로 다시 정한다.
        m_SiblingHomeIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (m_isDragging == false)
            return;

        // **칸 자체를 옮긴다.** 놓은 자리가 곧 유닛의 자리다.
        m_RectTransform.anchoredPosition += _eventData.delta / m_CanvasScale;

        RefreshDragTint();
    }

    // 지금 자리에 놓을 수 있는지를 색으로 알린다. 매 프레임 도는 경로라 할당하지 않는다
    // (유닛 9기라 명부 쪽 비교는 최대 8회다).
    private void RefreshDragTint()
    {
        if (m_Owner == null || m_RectTransform == null)
            return;

        bool isValid = m_Owner.IsPlacementValid(m_Cell, m_RectTransform.anchoredPosition);

        SetSymbolColor((isValid == true) ? DRAG_TINT_VALID : DRAG_TINT_INVALID, DRAG_ALPHA);
    }

    public void OnEndDrag(PointerEventData _eventData)
    {
        if (m_isDragging == false)
            return;

        m_isDragging = false;

        SetSymbolColor(Color.white, 1f);

        if (m_Owner != null)
            m_Owner.ShowPlacementArea(false);

        m_RaycastImage.raycastTarget = true;

        if (m_SymbolImage != null)
            m_SymbolImage.raycastTarget = true;

        // 명부에 저장한다. 거부되면(영역 밖이 아니라 다른 유닛과 너무 가까우면) 원래 자리로.
        bool moved = (m_Owner != null)
            && m_Owner.TryMoveUnit(m_Cell, m_RectTransform.anchoredPosition);

        if (moved == false)
        {
            m_RectTransform.anchoredPosition = m_DragHomePosition;

            if (m_SiblingHomeIndex > SIBLING_HOME_NONE)
                transform.SetSiblingIndex(m_SiblingHomeIndex);
        }

        // 성공했으면 UIInGameField 가 RefreshSlots 에서 좌표와 그리기 순서를 다시 잡는다.
        m_SiblingHomeIndex = SIBLING_HOME_NONE;
    }

    private void RestoreSymbolTransform()
    {
        // **드래그 상태를 여기서 끊는다.** Clear 로도 들어오는데, 드래그 도중 전투가 시작되면
        // OnEndDrag 가 오지 않아 m_isDragging 이 true 로 남는다. 그러면 SetWalking 의 가드에
        // 영영 걸려 **그 칸만 연차 이동에서 걷지 않는다** — 다시 false 가 되는 경로가
        // OnEndDrag 하나뿐이라 스스로 풀리지 않는다.
        // (2026-09-14 QA 발견. 영역 표시를 넣기 전부터 있던 누수인데 같은 경로라 함께 고쳤다)
        m_isDragging = false;
        m_SiblingHomeIndex = SIBLING_HOME_NONE;

        if (m_SymbolRect != null)
            m_SymbolRect.anchoredPosition = m_SymbolHomePosition;

        SetSymbolColor(Color.white, 1f);

        // 같은 이유로 영역도 여기서 끈다. 안 그러면 켜진 채 남는다.
        if (m_Owner != null)
            m_Owner.ShowPlacementArea(false);

        if (m_RaycastImage != null)
            m_RaycastImage.raycastTarget = true;

        if (m_SymbolImage != null)
            m_SymbolImage.raycastTarget = true;
    }

    // 색조와 투명도를 함께 다룬다. 드래그 피드백이 붙으면서 알파만으로는 모자라졌고,
    // 둘을 따로 쓰면 한쪽만 복원하는 실수가 난다.
    private void SetSymbolColor(Color _tint, float _alpha)
    {
        if (m_SymbolImage == null)
            return;

        m_SymbolImage.color = new Color(_tint.r, _tint.g, _tint.b, _alpha);
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

    // 걷기가 무한 루프 트윈이라, 살아 있는 채로 파괴되면 사라진 대상을 계속 건드린다.
    private void OnDestroy()
    {
        if (m_SymbolRect != null)
            m_SymbolRect.DOKill();
    }
}
