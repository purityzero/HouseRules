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

    // 버튼 하나로 **탭을 돌린다** — 닫힘 → 결과 → 족보 → 유닛 → 닫힘.
    //
    // 탭 버튼을 따로 세우려면 씬을 고쳐야 하는데, 이 패널은 버튼이 하나뿐이고
    // 슬라이드로 열리는 구조다. 순환은 씬을 안 건드리고 같은 것을 보여준다.
    // (2026-09-14. 탭 줄이 필요해지면 그때 씬에 얹는다 — 글을 만드는 쪽은 HandbookText 라
    //  화면이 바뀌어도 그대로 쓰인다)
    private eSummaryTab m_Tab = eSummaryTab.Result;

    // 지금 보여줄 것을 다시 만들려면 재료를 들고 있어야 한다.
    // **소유자는 InGameScene 이다** — 여기 있는 것은 참조일 뿐 사본이 아니다.
    private JudgeResult m_Result;
    private RunRoster m_Roster;
    private string m_HouseKey = string.Empty;

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

    // 이번 스핀의 재료를 넘겨받는다. 어느 탭을 보고 있든 그 자리에서 다시 그린다.
    public void Apply(JudgeResult _result, RunRoster _roster, string _houseKey)
    {
        m_Result = _result;
        m_Roster = _roster;
        m_HouseKey = _houseKey;

        RefreshBody();
    }

    private void RefreshBody()
    {
        if (m_BodyText == null)
            return;

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        string title = string.Empty;
        string body = string.Empty;

        switch (m_Tab)
        {
            case eSummaryTab.Pattern:
                title = stringTable.GetString("SummaryTabPattern");
                body = HandbookText.BuildPattern(m_HouseKey);
                break;

            case eSummaryTab.Unit:
                title = stringTable.GetString("SummaryTabUnit");
                body = HandbookText.BuildUnit(m_HouseKey, m_Roster);
                break;

            default:
                title = stringTable.GetString("SummaryTabResult");
                body = (m_Result != null) ? BuildResultText(m_Result) : string.Empty;
                break;
        }

        m_BodyText.text = $"[{title}]{System.Environment.NewLine}{body}";
    }

    // 이번 스핀이 왜 이만큼 나왔는가. 예전에는 UIInGameField 가 만들었는데,
    // 탭이 생기면서 **보여주는 쪽이 셋을 다 만드는** 편이 자연스러워졌다.
    private static string BuildResultText(JudgeResult _result)
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

    // 버튼이 직접 부른다. 런타임 AddListener로 붙이므로 프리팹 영구 호출은 두지 않는다.
    public void OnClickToggleButton()
    {
        // 닫혀 있으면 첫 탭으로 연다.
        if (m_isOpen == false)
        {
            m_Tab = eSummaryTab.Result;
            RefreshBody();
            SetOpen(true);
            return;
        }

        // 열려 있으면 다음 탭. 마지막 탭에서 한 번 더 누르면 닫힌다.
        if (m_Tab >= eSummaryTab.Unit)
        {
            SetOpen(false);
            return;
        }

        m_Tab = m_Tab + 1;
        RefreshBody();
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
