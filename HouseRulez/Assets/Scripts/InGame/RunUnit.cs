using UnityEngine;

// 런 한 판 동안 살아남는 유닛 하나.
//
// 기존 SummonSlot과 다른 점은 **수명**이다. SummonSlot은 스핀 1회의 결과라 전투가 끝나면
// 사라지지만, 이쪽은 런이 끝날 때까지 보관함과 전장을 오가며 계속 존재한다.
// 그래서 값 복사가 아니라 참조로 다룬다 — 드래그로 "이 유닛"을 집어 옮기는 조작에서
// 개체 동일성이 유지되어야 하고, 승급 재료로 소모된 개체를 정확히 지워야 한다.
public class RunUnit
{
    // 유닛 종류. 스핀 심볼의 종류를 그대로 쓴다 — 같은 종류 3기가 모이면 승급한다.
    //
    // 판정이 만든 고유 유닛은 심볼에서 나오지 않으므로 이 값이 SYMBOL_NONE 이고,
    // 대신 PatternKey 로 자기가 무엇인지 말한다.
    public int SymbolType;

    // 심볼이 아니라 **판정**에서 나온 유닛이면 그 판정 키가 들어간다(`PokerStraight` 등).
    // 보통 유닛은 비어 있다.
    public string PatternKey;

    public bool isPatternUnit => string.IsNullOrEmpty(PatternKey) == false;

    // 심볼 유닛이 아니라는 뜻. 릴 심볼 인덱스와 겹치지 않는 값이어야 한다.
    public const int SYMBOL_NONE = -1;

    // 1성부터. 상한은 UnitGradeTable이 정한다(현재 3성).
    public int Grade;

    // 전장에서 서 있는 자리. **보관함에 있을 때는 의미가 없다.**
    //
    // 2026-09-14 자유 배치 이전에는 전장 배열의 인덱스가 곧 자리였다. 이제 인덱스는
    // "몇 번째 슬롯인가"일 뿐이고 실제 위치는 이 값이다 — 플레이어가 드래그로 정하고,
    // 안 정했으면 자동 배치가 격자 기본 좌표를 넣는다.
    public Vector2 FieldPosition;

    public RunUnit(int _symbolType, int _grade, string _patternKey = null)
    {
        SymbolType = _symbolType;
        Grade = _grade;
        PatternKey = _patternKey;
    }
}
