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

// 전력이 어디서 나왔는지 한 항목. "진 2줄 × 0.95 = 1.9"의 재료다.
//
// 결과만 보여주면 종족 규칙을 배울 수가 없다 — 무엇이 몇 개 성립해서 얼마가 됐는지
// 식으로 보여줘야 "아, 저 배치가 저 값이구나"가 쌓인다.
public struct JudgeTerm
{
    public string Label;

    // 대부분은 "몇 개 성립했나"(정렬 2줄)지만 화투만 **족보값**이다(끗 2.4 등).
    // 그래서 int가 아니라 float다 — 정수로 깎으면 화면의 식과 실제 전력이 어긋난다.
    // (실제로 int로 뒀다가 화투 20,000회 중 1,252회가 어긋났다. 2026-08-31 QA)
    public float Value;
    public float Coef;

    public JudgeTerm(string _label, float _value, float _coef)
    {
        Label = _label;
        Value = _value;
        Coef = _coef;
    }

    public float total => Value * Coef;
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

    // 절반만 성립한 칸. 전력은 주지만 주판정은 아닌 것들 —
    // 체스 반정렬, 장기 진, 포커 페어, 슬롯 2매치가 여기 들어간다.
    //
    // 소환 위치에는 쓰지 않는다. 오직 **화면에서 약하게 강조**하기 위한 값이다.
    // 이게 없으면 "전력이 나왔는데 릴은 아무것도 안 반짝인다"가 되어
    // 플레이어가 규칙을 유추할 단서를 잃는다.
    public List<int> ListPartialCell = new List<int>();

    // 전력의 내역. 화면에 계산 과정을 보여주는 데만 쓴다 — 밸런스 계산은 Power가 정본이다.
    public List<JudgeTerm> ListTerm = new List<JudgeTerm>();

    // 소환 결과. 칸마다 등급이 다를 수 있다.
    public List<SummonSlot> ListSummon = new List<SummonSlot>();

    public int summonCount => ListSummon.Count;

    // 실제로 배치된 전력의 합. Power와 거의 같아야 하며, 차이는 등급 승격 단위로
    // 나누어떨어지지 않고 남은 잔여분뿐이다.
    public float placedPower;

    // 이 판정이 돌려주는 무료 스핀 횟수. 지금은 윷만 쓴다(윷·모가 나오면 한 번 더 던지는 규칙).
    //
    // 판정기는 값만 내고 코인을 직접 건드리지 않는다 — 스핀 경제의 소유자는 RunData이고,
    // 판정기가 상태를 바꾸기 시작하면 같은 grid를 두 번 평가하는 것만으로 코인이 늘어난다.
    public int bonusSpin;
}
