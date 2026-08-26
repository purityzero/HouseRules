using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image의 알파와 스케일을 동시에 목표값까지 갔다가 되돌아오는 것을 무한 yoyo로 반복하는 연출(로고 숨쉬기 등).
/// 붙인 뒤 인스펙터에서 값을 지정해야 동작 — 기본값은 무해한 범위(과하지 않은 숨쉬기)로 잡혀 있다.
/// </summary>
[RequireComponent(typeof(Image))]
public class PulseLoopEffect : MonoBehaviour
{
    [SerializeField] private Image m_TargetImage;
    [SerializeField] private float m_TargetAlpha = 0.6f;
    [SerializeField] private float m_TargetScale = 0.92f;
    [SerializeField] private float m_Duration = 1.2f;
    [SerializeField] private Ease m_Ease = Ease.InOutSine;
    [SerializeField] private float m_StartDelay = 0f;

    private Sequence m_Sequence;
    private float m_OriginalAlpha;
    private Vector3 m_OriginalScale;

    private void OnEnable()
    {
        if (m_TargetImage == null)
            m_TargetImage = GetComponent<Image>();

        m_OriginalAlpha = m_TargetImage.color.a;
        m_OriginalScale = transform.localScale;

        m_Sequence = TweenSequenceBuilder.Create()
            .Delay(m_StartDelay)
            .Append(TweenUtil.Fade(m_TargetImage, m_TargetAlpha, m_Duration).SetEase(m_Ease))
            .Join(TweenUtil.Scale(transform, Vector3.one * m_TargetScale, m_Duration).SetEase(m_Ease))
            .Loops(-1, LoopType.Yoyo)
            .Play();
    }

    private void OnDisable()
    {
        if (m_Sequence != null)
        {
            m_Sequence.Kill();
            m_Sequence = null;
        }

        Color color = m_TargetImage.color;
        color.a = m_OriginalAlpha;
        m_TargetImage.color = color;
        transform.localScale = m_OriginalScale;
    }
}
