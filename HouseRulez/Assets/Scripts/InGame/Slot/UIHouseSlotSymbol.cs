using System.Collections.Generic;
using UnityEngine;

// 심볼 한 칸이 가질 수 있는 표시용 스프라이트 쌍. 블러 스프라이트가 없는 경우도 있어 별도 필드로 둔다.
public struct HouseSlotSymbolSprite
{
    public Sprite NormalSprite;
    public Sprite BlurSprite;
}

// Glory UISlotMachineSymbol의 프로젝트 파생. symbolType(정수)을 "현재 종족 스프라이트 풀의 인덱스"로 해석해
// 블러 여부에 따라 원본/블러 스프라이트를 교체한다. 풀은 이 클래스가 직접 조회하지 않고
// 소유자(UIHouseSlotReel)가 SetSpritePool()로 주입한다 — 칸마다 테이블/리소스를 중복 조회하지 않기 위함.
public class UIHouseSlotSymbol : UISlotMachineSymbol
{
    private IReadOnlyList<HouseSlotSymbolSprite> m_SpritePool;

    public void SetSpritePool(IReadOnlyList<HouseSlotSymbolSprite> _spritePool)
    {
        m_SpritePool = _spritePool;
    }

    protected override void SetBlur(bool _isBlur)
    {
        if (m_SpritePool == null || m_SpritePool.Count <= 0)
            return;

        if (iconImage == null)
            return;

        int index = Mathf.Clamp(symbolType, 0, m_SpritePool.Count - 1);
        HouseSlotSymbolSprite spriteSet = m_SpritePool[index];

        // 블러 스프라이트가 없는 종족/파일이 있을 수 있다 — 없으면 조용히 원본으로 대체(에러 로그 없음).
        Sprite targetSprite = spriteSet.NormalSprite;
        if (_isBlur == true && spriteSet.BlurSprite != null)
            targetSprite = spriteSet.BlurSprite;

        iconImage.sprite = targetSprite;
    }
}
