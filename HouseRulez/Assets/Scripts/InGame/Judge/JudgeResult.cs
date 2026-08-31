using System.Collections.Generic;

// 소환 한 칸. 어느 칸에 몇 성 유닛이 서는지를 함께 들고 다닌다.
public struct SummonSlot
{
    // 이 칸이 어느 심볼로 보일지 판정기가 직접 정하지 않았다는 뜻.
    // 이 경우 표시하는 쪽이 스핀 결과 grid[Cell]의 심볼을 쓴다(여섯 종족 공통).
    public const int SYMBOL_FROM_GRID = -1;

    public int Cell;
    public int Grade;

    // 윷만 이 값을 쓴다. 윷은 "가로줄을 오른쪽부터 훑어 처음 만나는 빽도 아닌 심볼"이 말의 종류가 되는데,
    // 그 심볼이 서는 칸(도착점 트랙)과 아무 관계가 없어서 grid[Cell]로는 복원할 수 없다.
    public int SymbolType;

    public SummonSlot(int _cell, int _grade)
    {
        Cell = _cell;
        Grade = _grade;
        SymbolType = SYMBOL_FROM_GRID;
    }

    public SummonSlot(int _cell, int _grade, int _symbolType)
    {
        Cell = _cell;
        Grade = _grade;
        SymbolType = _symbolType;
    }
}

// 스핀 1회의 판정 결과.
//
// 전력을 유닛으로 바꾸는 규칙(2026-08-30 확정):
//   전력 1 = 1성 유닛 1기. 전장이 9칸뿐이라 9를 넘는 전력은 칸을 늘리지 못하는데,
//   그때 남는 전력을 **등급**으로 돌린다(1성 → 2성 → 3성).
//
//   상한에서 그냥 잘라내면 분산이 큰 종족(포커·화투)만 손해를 본다 —
//   실측에서 평균 소환이 3.61 vs 5.71로 58% 벌어졌고, 이는 GDD의
//   "평균은 붙이고 분산만 벌린다"는 설계와 정반대였다.
//   등급 전환으로 종족 간 평균 전력 차이가 2.09 → 0.29로 줄었다(교차검증 실측).
//
// **전력이 항상 보존되지는 않는다.** 9칸 전부 3성이 상한이라 배치 가능한 최대 전력은
//   9 × 7.0 = 63이다. 그 위는 버려진다 — 포커 트리플 4개(Power 164)에서 101이 사라진다.
//   30만 회 중 오차 10 초과는 326회(0.1%)로 드물지만, 하필 가장 큰 판정에서 깎인다.
//   프리즘(×18.0)을 상한으로 올리면 해소되나 그건 심판 웨이브 조건과 2칸 점유가 붙은
//   별개 기능이라 판정 소환에 끌어오지 않았다.
public class JudgeResult
{
    public const int GRID_SIZE = 9;
    public const int MAX_SUMMON = GRID_SIZE;

    // 판정 이름. 화면 표시용이며 밸런스에는 쓰지 않는다.
    public string PatternName = string.Empty;

    // 판정이 만들어낸 전력. GDD의 종족별 계수를 그대로 적용한 값이다.
    public float Power;

    // 판정에 실제로 걸린 칸(0~8). 소환 위치를 정할 때 이 칸을 먼저 채운다.
    public List<int> ListHitCell = new List<int>();

    // 소환 결과. 칸마다 등급이 다를 수 있다.
    public List<SummonSlot> ListSummon = new List<SummonSlot>();

    public int summonCount => ListSummon.Count;

    // 실제로 배치된 전력의 합. Power와 거의 같아야 하며, 차이는 등급 승격 단위로
    // 나누어떨어지지 않고 남은 잔여분뿐이다.
    public float placedPower;
}
