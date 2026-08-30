using System.Collections;
using TMPro;
using UnityEngine;

// 판정 요약을 접었다 펴는 패널. 평소엔 화살표 버튼만 보이고, 누르면 왼쪽(슬롯머신 뒤)에서 밀려 나온다.
//
// 요약을 전장 위에 늘 띄워두면 성문·유닛과 겹쳐 읽기도 나쁘고 화면도 지저분해진다.
// 필요할 때만 꺼내 보는 쪽이 맞다.
public class UIInGameSummary : MonoBehaviour
{
    [SerializeField] private RectTransform m_Panel;
    [SerializeField] private TextMeshProUGUI m_BodyText;
    [SerializeField] private TextMeshProUGUI m_ArrowText;

    // 닫힘 위치는 슬롯머신 뒤로 숨는 좌표다. 그리기 순서상 슬롯머신이 위에 있어 가려진다.
    [SerializeField] private float m_ClosedX = -420f;
    [SerializeField] private float m_OpenX = 56f;
    [SerializeField] private float m_SlideDuration = 0.22f;

    private const string ARROW_OPEN = "◀";
    private const string ARROW_CLOSED = "▶";

    private bool m_isOpen;
    private Coroutine m_SlideRoutine;

    [SerializeField] private UIButton m_ToggleButton;

    private void Awake()
    {
        ApplyImmediate(false);

        if (m_ToggleButton == null)
        {
            Logger.Error("[UIInGameSummary] Awake Failed! ToggleButton 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        // 프리팹에 영구 호출을 두지 않고 여기서 붙인다. 복제로 만든 오브젝트가 남의 이벤트를
        // 물고 오는 사고를 오늘 세 번 겪었다. 등록 전 해제로 중복 구독도 막는다.
        m_ToggleButton.onClick.RemoveListener(OnClickToggleButton);
        m_ToggleButton.onClick.AddListener(OnClickToggleButton);
    }

    public void SetText(string _text)
    {
        if (m_BodyText != null)
            m_BodyText.text = _text;
    }

    // 버튼이 직접 부른다. 런타임 AddListener로 붙이므로 프리팹 영구 호출은 두지 않는다.
    public void OnClickToggleButton()
    {
        SetOpen(m_isOpen == false);
    }

    public void SetOpen(bool _isOpen)
    {
        if (m_isOpen == _isOpen)
            return;

        m_isOpen = _isOpen;

        if (m_ArrowText != null)
            m_ArrowText.text = (m_isOpen == true) ? ARROW_OPEN : ARROW_CLOSED;

        if (m_SlideRoutine != null)
            StopCoroutine(m_SlideRoutine);

        if (gameObject.activeInHierarchy == false)
        {
            ApplyImmediate(m_isOpen);
            return;
        }

        m_SlideRoutine = StartCoroutine(CoSlide(m_isOpen ? m_OpenX : m_ClosedX));
    }

    private void ApplyImmediate(bool _isOpen)
    {
        m_isOpen = _isOpen;

        if (m_Panel != null)
            m_Panel.anchoredPosition = new Vector2(_isOpen ? m_OpenX : m_ClosedX, m_Panel.anchoredPosition.y);

        if (m_ArrowText != null)
            m_ArrowText.text = (_isOpen == true) ? ARROW_OPEN : ARROW_CLOSED;
    }

    private IEnumerator CoSlide(float _targetX)
    {
        if (m_Panel == null)
            yield break;

        float startX = m_Panel.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < m_SlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_SlideDuration);

            // 끝에서 부드럽게 멈추도록 감속만 준다. 과한 이징은 픽셀아트에서 미끄러져 보인다.
            t = 1f - (1f - t) * (1f - t);

            m_Panel.anchoredPosition = new Vector2(Mathf.Lerp(startX, _targetX, t), m_Panel.anchoredPosition.y);
            yield return null;
        }

        m_Panel.anchoredPosition = new Vector2(_targetX, m_Panel.anchoredPosition.y);
        m_SlideRoutine = null;
    }
}
