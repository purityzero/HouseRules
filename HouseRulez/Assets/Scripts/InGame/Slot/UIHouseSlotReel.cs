using System.Collections.Generic;
using UnityEngine;

// Glory UISlotMachineReel의 프로젝트 파생 릴 1줄. Glory가 이미 하는 것(가감속/순환 버퍼/FSM 전이)은
// 다시 만들지 않고, 이 프로젝트에만 필요한 것 두 가지만 얹는다 — 칸 오브젝트 런타임 생성 + 심볼 공급.
public class UIHouseSlotReel : UISlotMachineReel
{
    // 심볼 원본(비활성) 1개를 필요한 칸 수만큼 복제해 베이스의 symbolList를 채운다.
    public void BuildSymbols(UIHouseSlotSymbol _symbolTemplate, int _symbolCount)
    {
        if (_symbolTemplate == null)
        {
            Logger.Error("[UIHouseSlotReel] BuildSymbols Failed! symbolTemplate == null");
            return;
        }

        var newSymbolList = new List<UISlotMachineSymbol>();
        for (int index = 0; index < _symbolCount; ++index)
        {
            UIHouseSlotSymbol symbolInstance = Instantiate(_symbolTemplate, transform);
            symbolInstance.gameObject.SetActive(true);
            newSymbolList.Add(symbolInstance);
        }

        SetSymbols(newSymbolList);
    }

    // 이 릴에 속한 모든 칸에 스프라이트 풀을 물리고, 스핀 중 굴러갈 다음 심볼을 그 풀 범위에서 무작위로 공급한다.
    public void ApplySpritePool(IReadOnlyList<HouseSlotSymbolSprite> _spritePool)
    {
        if (_spritePool == null || _spritePool.Count <= 0)
        {
            Logger.Error("[UIHouseSlotReel] ApplySpritePool Failed! spritePool == null or empty");
            return;
        }

        for (int index = 0; index < symbolList.Count; ++index)
        {
            if (symbolList[index] is UIHouseSlotSymbol == true)
            {
                var houseSymbol = symbolList[index] as UIHouseSlotSymbol;
                houseSymbol.SetSpritePool(_spritePool);
            }
            else
            {
                Logger.Error($"[UIHouseSlotReel] ApplySpritePool Failed! symbolList[{index}] is not UIHouseSlotSymbol");
            }
        }

        int poolCount = _spritePool.Count;
        OnRequestSymbol = () => Random.Range(0, poolCount);
    }
}
