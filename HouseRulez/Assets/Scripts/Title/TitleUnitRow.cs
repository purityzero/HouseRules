using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면 아래에 서 있는 말 몇 개. 현재 종족의 말 중에서 매번 다르게 뽑는다.
// 종족 선택 화면에서 종족을 바꾸면 여기도 같이 갈린다.
public class TitleUnitRow : MonoBehaviour
{
    [SerializeField] private Image[] m_SlotImages;

    // 스프라이트 원본이 32px 기준이라 정수 배율로만 키운다
    [SerializeField] private float m_Scale = 3f;

    [SerializeField] private float m_JumpHeight = 20f;
    [SerializeField] private float m_JumpDuration = 0.3f;
    [SerializeField] private float m_JumpIntervalMin = 1f;
    [SerializeField] private float m_JumpIntervalMax = 3f;

    private Vector2[] m_OriginalAnchoredPositions;
    private Tween m_JumpTween;
    private Tween m_ScheduleTween;
    private int m_JumpingSlotIndex = -1;

    public void Apply(HouseRecord _record)
    {
        if (m_SlotImages == null)
            return;

        ResetJump();

        List<Sprite> picked = HouseSpriteLoader.LoadRandom(_record, m_SlotImages.Length);

        for (int i = 0; i < m_SlotImages.Length; ++i)
        {
            if (m_SlotImages[i] == null)
                continue;

            // 아직 말 그림이 없는 종족(마작 등)은 슬롯을 비운다
            if (i >= picked.Count)
            {
                m_SlotImages[i].gameObject.SetActive(false);
                continue;
            }

            Sprite sprite = picked[i];
            m_SlotImages[i].gameObject.SetActive(true);
            m_SlotImages[i].sprite = sprite;

            // 말마다 원본 높이가 달라서(29~32px) 크기를 매번 다시 잡아야 바닥이 맞는다
            RectTransform rectTransform = m_SlotImages[i].rectTransform;
            rectTransform.sizeDelta = new Vector2(sprite.rect.width * m_Scale, sprite.rect.height * m_Scale);
        }

        ScheduleNextJump();
    }

    private void OnEnable()
    {
        CacheOriginalPositions();
        ScheduleNextJump();
    }

    private void OnDisable()
    {
        ResetJump();
    }

    private void OnDestroy()
    {
        ResetJump();
    }

    private void CacheOriginalPositions()
    {
        if (m_SlotImages == null)
            return;

        if (m_OriginalAnchoredPositions != null)
            return;

        m_OriginalAnchoredPositions = new Vector2[m_SlotImages.Length];

        for (int i = 0; i < m_SlotImages.Length; ++i)
        {
            if (m_SlotImages[i] == null)
                continue;

            m_OriginalAnchoredPositions[i] = m_SlotImages[i].rectTransform.anchoredPosition;
        }
    }

    private void ResetJump()
    {
        if (m_ScheduleTween != null)
        {
            m_ScheduleTween.Kill();
            m_ScheduleTween = null;
        }

        if (m_JumpTween != null)
        {
            m_JumpTween.Kill();
            m_JumpTween = null;
        }

        if (m_JumpingSlotIndex >= 0 && m_OriginalAnchoredPositions != null && m_JumpingSlotIndex < m_SlotImages.Length && m_SlotImages[m_JumpingSlotIndex] != null)
        {
            m_SlotImages[m_JumpingSlotIndex].rectTransform.anchoredPosition = m_OriginalAnchoredPositions[m_JumpingSlotIndex];
        }

        m_JumpingSlotIndex = -1;
    }

    private void ScheduleNextJump()
    {
        float interval = Random.Range(m_JumpIntervalMin, m_JumpIntervalMax);

        m_ScheduleTween = TweenUtil.DelayedCall(interval, OnScheduledJump);
    }

    private void OnScheduledJump()
    {
        List<int> activeIndices = GetActiveSlotIndices();

        if (activeIndices.Count > 0)
        {
            int slotIndex = activeIndices[Random.Range(0, activeIndices.Count)];
            PlayJump(slotIndex);
        }

        ScheduleNextJump();
    }

    private void PlayJump(int _slotIndex)
    {
        RectTransform rectTransform = m_SlotImages[_slotIndex].rectTransform;
        Vector2 originalPosition = m_OriginalAnchoredPositions[_slotIndex];
        Vector2 upPosition = originalPosition + Vector2.up * m_JumpHeight;

        m_JumpingSlotIndex = _slotIndex;

        m_JumpTween = TweenSequenceBuilder.Create()
            .Append(TweenUtil.MoveAnchored(rectTransform, upPosition, m_JumpDuration * 0.5f).SetEase(Ease.OutQuad))
            .Append(TweenUtil.MoveAnchored(rectTransform, originalPosition, m_JumpDuration * 0.5f).SetEase(Ease.InQuad))
            .OnComplete(() => m_JumpingSlotIndex = -1)
            .Play();
    }

    private List<int> GetActiveSlotIndices()
    {
        List<int> activeIndices = new List<int>();

        for (int i = 0; i < m_SlotImages.Length; ++i)
        {
            if (m_SlotImages[i] == null)
                continue;

            if (m_SlotImages[i].gameObject.activeSelf == false)
                continue;

            activeIndices.Add(i);
        }

        return activeIndices;
    }
}
