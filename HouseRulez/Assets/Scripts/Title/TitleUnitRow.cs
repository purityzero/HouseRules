using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면 아래에 서 있는 말 몇 개. 현재 종족의 말 중에서 매번 다르게 뽑는다.
// 종족 선택 화면에서 종족을 바꾸면 여기도 같이 갈린다.
public class TitleUnitRow : MonoBehaviour
{
    [SerializeField] private Image[] m_SlotImages;

    // 스프라이트 원본이 32px 기준이라 정수 배율로만 키운다
    [SerializeField] private float m_Scale = 3f;

    public void Apply(HouseRecord _record)
    {
        if (m_SlotImages == null)
            return;

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
    }
}
