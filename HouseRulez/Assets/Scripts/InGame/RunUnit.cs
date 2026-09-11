// 런 한 판 동안 살아남는 유닛 하나.
//
// 기존 SummonSlot과 다른 점은 **수명**이다. SummonSlot은 스핀 1회의 결과라 전투가 끝나면
// 사라지지만, 이쪽은 런이 끝날 때까지 보관함과 전장을 오가며 계속 존재한다.
// 그래서 값 복사가 아니라 참조로 다룬다 — 드래그로 "이 유닛"을 집어 옮기는 조작에서
// 개체 동일성이 유지되어야 하고, 승급 재료로 소모된 개체를 정확히 지워야 한다.
public class RunUnit
{
    // 유닛 종류. 스핀 심볼의 종류를 그대로 쓴다 — 같은 종류 3기가 모이면 승급한다.
    public int SymbolType;

    // 1성부터. 상한은 UnitGradeTable이 정한다(현재 3성).
    public int Grade;

    public RunUnit(int _symbolType, int _grade)
    {
        SymbolType = _symbolType;
        Grade = _grade;
    }
}
