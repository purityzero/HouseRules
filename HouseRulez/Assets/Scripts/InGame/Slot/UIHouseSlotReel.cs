using System.Collections.Generic;
using UnityEngine;

// Glory UISlotMachineReel의 프로젝트 파생 릴 1줄. Glory가 이미 하는 것(가감속/순환 버퍼/FSM 전이)은
// 다시 만들지 않고, 이 프로젝트에만 필요한 것 세 가지만 얹는다 — 칸 오브젝트 런타임 생성 + 심볼 공급 + 보이는 칸 조회.
public class UIHouseSlotReel : UISlotMachineReel
{
    private int m_VisibleStartIndex;
    private int m_SettleStepCount;

    // 심볼 원본(비활성) 1개를 필요한 칸 수만큼 복제해 베이스의 symbolList를 채우고, 칸 간격으로 세로 배치한다.
    // _settleStepCount는 스핀이 끝날 때 릴이 마지막으로 더 내려가 멈추는 칸 수다(감속해서 정착하는 구간).
    public void BuildSymbols(UIHouseSlotSymbol _symbolTemplate, int _symbolCount, int _visibleSymbolCount, int _settleStepCount)
    {
        if (_symbolTemplate == null)
        {
            Logger.Error("[UIHouseSlotReel] BuildSymbols Failed! symbolTemplate == null");
            return;
        }

        // 베이스의 GetVisibleStartIndex()와 같은 식(버퍼를 위아래로 절반씩 나눈 가운데가 보이는 창).
        // 베이스 쪽이 private이라 여기서 다시 구한다 — 두 값이 어긋나면 결과가 들어가는 칸과 화면에 보이는 칸이
        // 서로 다른 칸이 되어 "결과가 안 맞는다"는 증상으로만 드러난다.
        m_VisibleStartIndex = (_symbolCount - _visibleSymbolCount) / 2;
        m_SettleStepCount = _settleStepCount;

        // 릴은 기준 위치(y=0)와 정착 위치(y=AnswerPosY) 양쪽에서 창이 꽉 차 있어야 한다.
        // 정착 위치에서 보이는 맨 아래 칸이 버퍼를 넘어가면 그 자리가 빈 칸으로 보인다.
        if (m_VisibleStartIndex + _settleStepCount + _visibleSymbolCount > _symbolCount)
        {
            Logger.Error($"[UIHouseSlotReel] BuildSymbols Failed! symbolCount({_symbolCount}) is too small for visible({_visibleSymbolCount}) + settle({_settleStepCount})");
            return;
        }

        RectTransform reelRectTransform = transform as RectTransform;
        float cellHeight = reelRectTransform.rect.height / _symbolCount;
        float cellWidth = reelRectTransform.rect.width;

        var newSymbolList = new List<UISlotMachineSymbol>();
        for (int index = 0; index < _symbolCount; ++index)
        {
            UIHouseSlotSymbol symbolInstance = Instantiate(_symbolTemplate, transform);
            symbolInstance.gameObject.SetActive(true);

            RectTransform symbolRectTransform = symbolInstance.transform as RectTransform;

            // 피벗을 칸 한가운데로 옮긴다 — 당첨 연출이 이 트랜스폼을 회전시키는데,
            // 피벗이 좌상단이면 칸이 제자리에서 기우는 게 아니라 모서리를 축으로 휘둘린다.
            symbolRectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 릴이 정착 위치(아래로 _settleStepCount칸)에 멈췄을 때 m_VisibleStartIndex 칸이 창 맨 위에 오도록
            // 그만큼 위로 올려 배치한다. 결과는 그 칸부터 채워지므로(ApplyResultToVisibleSymbols)
            // 이 오프셋이 없으면 결과가 화면 밖에 그려진다. 배치 자체를 빠뜨리면 칸이 전부 (0,0)에 겹친다.
            // 릴 앵커가 좌상단이라 피벗을 옮긴 만큼(반 칸) 오른쪽·아래로 더 밀어야 칸 위치가 그대로 유지된다.
            float positionX = cellWidth * 0.5f;
            float positionY = -cellHeight * (index - m_VisibleStartIndex) + cellHeight * _settleStepCount - cellHeight * 0.5f;
            symbolRectTransform.anchoredPosition = new Vector2(positionX, positionY);

            newSymbolList.Add(symbolInstance);
        }

        SetSymbols(newSymbolList);
    }

    // 베이스는 "버퍼 끝까지" 내려가는 값을 쓰는데, 그러면 버퍼가 짧을 때 정착 위치에서 창이 비어버린다.
    // 여기서는 배치와 짝을 이루는 칸 수만큼만 내려간다.
    public override float AnswerPosY()
    {
        return PosmaxDownY() * m_SettleStepCount;
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

    // 베이스의 Open()/ResetSymbol()은 모든 칸을 타입 0으로 되돌린다(Glory는 종족 풀을 모르니 그게 맞다).
    // 그대로 두면 첫 화면이 같은 말 9개가 되므로, 진입 직후 한 번 풀 범위에서 무작위로 채운다.
    public void FillRandomSymbols()
    {
        if (OnRequestSymbol == null)
        {
            Logger.Error("[UIHouseSlotReel] FillRandomSymbols Failed! OnRequestSymbol == null");
            return;
        }

        for (int index = 0; index < symbolList.Count; ++index)
        {
            symbolList[index].Open(OnRequestSymbol(), false);
        }
    }

    // 진입 직후에도 스핀이 끝난 뒤와 같은 상태(릴이 정착 위치)로 맞춘다.
    // 베이스 Open()은 릴을 기준 위치(0)에 두는데, 그 상태로 두면 창에 보이는 칸과
    // GetVisibleSymbol()/ApplyResultToVisibleSymbols()가 가리키는 칸이 정착 칸 수만큼 어긋난다.
    public void ResetToSettledPosition()
    {
        ResetPosition(-AnswerPosY());
    }

    // 정착 위치(AnswerPosY)에 멈춰 있던 릴을 기준 위치(0)로 되돌린다. 다음 스핀을 걸기 전에 부른다.
    // 위치만 되돌리면 창에 보이는 칸이 정착 칸 수만큼 건너뛰어 화면이 순간이동한다 —
    // 그래서 심볼 내용도 같은 칸 수만큼 뒤로 밀어 화면에 보이는 그림을 그대로 유지한다.
    // 위로 밀려 비는 칸은 새 심볼로 채운다(어차피 창 위쪽 화면 밖이다).
    public void ResetToBasePosition()
    {
        // 뒤에서부터 밀어야 아직 안 읽은 칸을 덮어쓰지 않는다.
        for (int index = symbolList.Count - 1; index >= 0; --index)
        {
            int sourceIndex = index - m_SettleStepCount;
            if (sourceIndex < 0)
            {
                int newSymbolType = (OnRequestSymbol != null) ? OnRequestSymbol() : 0;
                symbolList[index].Open(newSymbolType, false);
                continue;
            }

            symbolList[index].Open(symbolList[sourceIndex].symbolType, false);
        }

        // 칸을 전부 선명하게 다시 열었으니 블러 상태 캐시도 맞춰둔다.
        SetBlurState(false);

        ResetPosition(0f);
    }

    // 보이는 창의 _rowIndex번째(위에서부터 0) 칸. 당첨 판정과 당첨 연출이 같은 칸을 가리키게 하는 유일한 통로다.
    public UIHouseSlotSymbol GetVisibleSymbol(int _rowIndex)
    {
        int index = m_VisibleStartIndex + _rowIndex;
        if (_rowIndex < 0 || index >= symbolList.Count)
        {
            Logger.Error($"[UIHouseSlotReel] GetVisibleSymbol Failed! rowIndex out of range - {_rowIndex}");
            return null;
        }

        if (symbolList[index] is UIHouseSlotSymbol == true)
        {
            var houseSymbol = symbolList[index] as UIHouseSlotSymbol;
            return houseSymbol;
        }

        Logger.Log($"symbolList[{index}] is UIHouseSlotSymbol convert failed!");
        return null;
    }
}
