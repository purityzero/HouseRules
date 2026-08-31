using DG.Tweening;
using TMPro;
using UnityEngine;

// 화면 한가운데 한 줄을 크게 띄웠다 지우는 배너. 지금은 무료 스핀 알림에만 쓴다.
//
// Glory에 UIToastMessage / UIManager.ShowToast가 이미 있지만 **쓸 수 없다** —
// 로드 대상인 Resources/Prefabs/UI/UIToastMessage 프리팹이 프로젝트에 없고,
// 그 시스템이 기대는 TweenEffectPlayer도 이 프로젝트의 어떤 씬·프리팹에서도 쓰인 적이 없다.
// 검증된 적 없는 뼈대에 알림을 걸기보다 필요한 만큼만 여기서 직접 만든다.
// (토스트를 되살리는 건 별개 작업으로 UNFINISHED에 남겼다)
public class UIInGameBanner : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private TextMeshProUGUI m_MessageText;

    [SerializeField] private float m_FadeInDuration = 0.12f;
    [SerializeField] private float m_HoldDuration = 0.9f;
    [SerializeField] private float m_FadeOutDuration = 0.35f;

    // 작게 시작해 튀어나오게. 픽셀아트라 과한 이징은 미끄러져 보여 살짝만 준다.
    [SerializeField] private float m_PunchFromScale = 0.7f;

    private Sequence m_Sequence;

    private void Awake()
    {
        // 씬에 켜둔 채로 두면 시작하자마자 문구가 보인다. 항상 투명에서 출발한다.
        if (m_CanvasGroup != null)
            m_CanvasGroup.alpha = 0f;
    }

    public void Show(string _message)
    {
        if (m_CanvasGroup == null || m_MessageText == null)
        {
            Logger.Error("[UIInGameBanner] Show Failed! CanvasGroup 또는 MessageText 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        m_MessageText.text = _message;

        // 이전 연출이 살아 있으면 알파와 스케일이 중간값에서 시작한다. 죽이고 원점부터.
        KillSequence();

        RectTransform rectTransform = transform as RectTransform;
        rectTransform.localScale = Vector3.one * m_PunchFromScale;
        m_CanvasGroup.alpha = 0f;

        m_Sequence = DOTween.Sequence();
        m_Sequence.Append(m_CanvasGroup.DOFade(1f, m_FadeInDuration));
        m_Sequence.Join(rectTransform.DOScale(1f, m_FadeInDuration).SetEase(Ease.OutBack));
        m_Sequence.AppendInterval(m_HoldDuration);
        m_Sequence.Append(m_CanvasGroup.DOFade(0f, m_FadeOutDuration));
        m_Sequence.OnKill(() => m_Sequence = null);
    }

    private void KillSequence()
    {
        if (m_Sequence == null)
            return;

        m_Sequence.Kill();
        m_Sequence = null;
    }

    private void OnDisable()
    {
        // 트윈이 살아 있는 채로 오브젝트가 꺼지면 다음에 켤 때 중간 알파로 남는다.
        KillSequence();

        if (m_CanvasGroup != null)
            m_CanvasGroup.alpha = 0f;
    }
}
