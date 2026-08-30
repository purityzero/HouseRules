using System.Collections.Generic;
using UnityEngine;

// 3×3 스핀 결과를 종족 문법으로 읽어 전력과 소환을 만든다.
//
// 판정 알고리즘은 GDD(reel_of_four_houses_GDD.html)의 judge()를 옮긴 것이고, 50만 회 몬테카를로로
// GDD 게시 수치(체스 6.11/0.64, 장기 6.27/0.67, 포커 6.23/1.62, 화투 6.20/1.53)를 재현하는 것을 확인했다.
//
// **다만 그대로 복사한 것은 아니다.** 두 곳이 의도적으로 다르다.
//  1. 화투 입력 — GDD는 `demoMonths[grid[i] % 6]`으로 6개월만 쓰는 데모용 축약이다.
//     여기서는 심볼 인덱스+1을 그대로 월로 읽어 12개월 풀 전체를 쓴다(실제 게임 조건).
//  2. 소환 환산 — GDD 데모는 전력÷3을 표시만 한다. 실제 규칙은 JudgeResult 주석 참고.
//
// 심볼 비교에 == 를 쓴다. CODE.MD의 "숫자 비교는 범위로"는 누적·감산되는 수치가 대상이고,
// 여기 값은 심볼의 신원(id)이라 정확 일치가 곧 계약이다.
public static class Judge
{
    // 8개 라인 — 가로 3 · 세로 3 · 대각 2
    private static readonly int[][] LINES =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 },
    };

    // 섯다는 세로 3열이 각각 한 손이다.
    private static readonly int[][] COLUMNS =
    {
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
    };

    // 장기 包의 심볼 인덱스. 스프라이트가 이름 오름차순으로 로드되므로
    // cha·jol·ma·po·sa·sang·wang 순서에서 3번이다(실제 폴더로 확인함).
    private const int JANGGI_PO_INDEX = 3;

    public static JudgeResult Evaluate(string _houseKey, int[] _grid)
    {
        JudgeResult result = new JudgeResult();

        if (_grid == null || _grid.Length < JudgeResult.GRID_SIZE)
        {
            Logger.Error($"[Judge] Evaluate Failed! grid 길이 부족 - {(_grid == null ? "null" : _grid.Length.ToString())} (기대: {JudgeResult.GRID_SIZE})");
            return result;
        }

        JudgeTable judgeTable = TableManager.instance.GetTable<JudgeTable>();
        if (judgeTable == null)
        {
            Logger.Error("[Judge] Evaluate Failed! JudgeTable not found (기대: TableManager에 등록됨)");
            return result;
        }

        switch (_houseKey)
        {
            case "chess":
                EvaluateChess(judgeTable, _grid, result);
                break;
            case "janggi":
                EvaluateJanggi(judgeTable, _grid, result);
                break;
            case "poker":
                EvaluatePoker(judgeTable, _grid, result);
                break;
            case "hwatu":
                EvaluateHwatu(judgeTable, _grid, result);
                break;
            default:
                Logger.Error($"[Judge] Evaluate Failed! 판정기 없는 종족 - {_houseKey} (기대: chess/janggi/poker/hwatu 중 하나)");
                return result;
        }

        BuildSummon(result);
        return result;
    }

    // 전력을 칸과 등급으로 나눈다.
    //
    // 1) 전력만큼 1성 유닛을 놓는다. 자리는 판정에 걸린 칸부터, 모자라면 남은 칸을 순서대로.
    //    (걸린 칸이 소환 수보다 적은 경우가 대부분이다 — 체스는 평균 0.65칸)
    // 2) 9칸을 다 쓰고도 전력이 남으면 **등급을 올려** 남은 전력을 흡수한다.
    //    승격 비용은 등급 배수의 차이다(1성→2성 = 2.6−1.0 = 1.6).
    //    가장 싼 승격부터 하므로 전장 전체가 고르게 올라간다.
    private static void BuildSummon(JudgeResult _result)
    {
        _result.ListSummon.Clear();
        _result.placedPower = 0f;

        if (_result.Power <= 0f)
            return;

        UnitGradeTable gradeTable = TableManager.instance.GetTable<UnitGradeTable>();
        if (gradeTable == null)
        {
            Logger.Error("[Judge] BuildSummon Failed! UnitGradeTable not found (기대: TableManager에 등록됨)");
            return;
        }

        int cellCount = Mathf.Clamp(Mathf.RoundToInt(_result.Power), 0, JudgeResult.MAX_SUMMON);
        List<int> listCell = new List<int>();

        for (int i = 0; i < _result.ListHitCell.Count; ++i)
        {
            if (listCell.Count >= cellCount)
                break;

            listCell.Add(_result.ListHitCell[i]);
        }

        for (int cell = 0; cell < JudgeResult.GRID_SIZE; ++cell)
        {
            if (listCell.Count >= cellCount)
                break;

            if (listCell.Contains(cell) == true)
                continue;

            listCell.Add(cell);
        }

        int[] grades = new int[listCell.Count];
        for (int i = 0; i < grades.Length; ++i)
        {
            grades[i] = 1;
        }

        float remain = _result.Power - listCell.Count * gradeTable.GetMultiplier(1);
        int maxGrade = gradeTable.maxGrade;

        // 남은 전력이 승격 비용의 절반 이상이면 올린다(반올림과 같은 기준).
        // 이 때문에 배치 전력이 Power보다 평균 0.05~0.19 높게 나온다 — 장기 6.46, 포커 6.39로
        // GDD 목표 6.1~6.3을 살짝 넘는다. 차이가 0.3 안쪽이라 전투가 붙어 실제 클리어율을
        // 볼 수 있을 때 조정하기로 했다(2026-08-30 판단). "비용 이상"으로 조이면 초과 지급은
        // 사라지지만 잔여분을 더 버리게 된다.
        bool promoted = true;
        while (promoted == true)
        {
            promoted = false;

            for (int i = 0; i < grades.Length; ++i)
            {
                if (grades[i] >= maxGrade)
                    continue;

                float cost = gradeTable.GetMultiplier(grades[i] + 1) - gradeTable.GetMultiplier(grades[i]);
                if (remain < cost * 0.5f)
                    continue;

                grades[i]++;
                remain -= cost;
                promoted = true;
                break;
            }
        }

        for (int i = 0; i < listCell.Count; ++i)
        {
            _result.ListSummon.Add(new SummonSlot(listCell[i], grades[i]));
            _result.placedPower += gradeTable.GetMultiplier(grades[i]);
        }
    }

    private static void AddHit(JudgeResult _result, int[] _line)
    {
        for (int i = 0; i < _line.Length; ++i)
        {
            if (_result.ListHitCell.Contains(_line[i]) == true)
                continue;

            _result.ListHitCell.Add(_line[i]);
        }
    }

    // 체스 — 동일성 판정. 라인 3개가 같으면 정렬, 2개만 같으면 반정렬.
    private static void EvaluateChess(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        int triple = 0;
        int pair = 0;

        for (int i = 0; i < LINES.Length; ++i)
        {
            int a = _grid[LINES[i][0]];
            int b = _grid[LINES[i][1]];
            int c = _grid[LINES[i][2]];

            if (a == b && b == c)
            {
                triple++;
                AddHit(_result, LINES[i]);
            }
            else if (a == b || b == c || a == c)
            {
                pair++;
            }
        }

        _result.Power = _table.GetCoef("chess", JudgeTable.CHESS_LINE_TRIPLE) * triple
                      + _table.GetCoef("chess", JudgeTable.CHESS_LINE_PAIR) * pair;
        _result.PatternName = (triple > 0) ? $"정렬 {triple}" : ((pair > 0) ? $"반정렬 {pair}" : "무판정");
    }

    // 장기 — 위치 관계 판정. A–B–A면 포가 넘고, 가운데가 包면 대포.
    private static void EvaluateJanggi(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        int jump = 0;
        int cannon = 0;
        int edge = 0;

        for (int i = 0; i < LINES.Length; ++i)
        {
            int a = _grid[LINES[i][0]];
            int b = _grid[LINES[i][1]];
            int c = _grid[LINES[i][2]];

            if (a == c && b != a)
            {
                jump++;
                if (b == JANGGI_PO_INDEX)
                    cannon++;

                AddHit(_result, LINES[i]);
            }
            else if (a == b || b == c)
            {
                edge++;
            }
        }

        _result.Power = _table.GetCoef("janggi", JudgeTable.JANGGI_JUMP) * jump
                      + _table.GetCoef("janggi", JudgeTable.JANGGI_CANNON) * cannon
                      + _table.GetCoef("janggi", JudgeTable.JANGGI_EDGE) * edge;
        _result.PatternName = (jump > 0)
            ? ((cannon > 0) ? $"포 넘기 {jump} · 대포 {cannon}" : $"포 넘기 {jump}")
            : ((edge > 0) ? $"진 {edge}" : "무판정");
    }

    // 포커 — 순서 판정. 유일하게 심볼에 순서가 있어 스트레이트가 성립한다.
    private static void EvaluatePoker(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        int triple = 0;
        int straight = 0;
        int pair = 0;
        int[] hand = new int[3];

        for (int i = 0; i < LINES.Length; ++i)
        {
            hand[0] = _grid[LINES[i][0]];
            hand[1] = _grid[LINES[i][1]];
            hand[2] = _grid[LINES[i][2]];
            System.Array.Sort(hand);

            if (hand[0] == hand[1] && hand[1] == hand[2])
            {
                triple++;
                AddHit(_result, LINES[i]);
            }
            else if (hand[1] == hand[0] + 1 && hand[2] == hand[1] + 1)
            {
                straight++;
                AddHit(_result, LINES[i]);
            }
            else if (hand[0] == hand[1] || hand[1] == hand[2])
            {
                pair++;
            }
        }

        _result.Power = _table.GetCoef("poker", JudgeTable.POKER_TRIPLE) * triple
                      + _table.GetCoef("poker", JudgeTable.POKER_STRAIGHT) * straight
                      + _table.GetCoef("poker", JudgeTable.POKER_PAIR) * pair;
        _result.PatternName = (triple > 0) ? $"트리플 {triple}"
            : ((straight > 0) ? $"스트레이트 {straight}" : ((pair > 0) ? $"페어 {pair}" : "하이카드"));
    }

    // ---------------- 화투(섯다) ----------------
    // 아래 족보 값은 섯다의 규칙 그 자체라 튜닝 대상이 아니다(땡 90을 조정하지 않는다).
    // 조정하는 건 JudgeTable의 HwatuScale 하나뿐이다.

    private static readonly HashSet<int> GWANG_MONTHS = new HashSet<int> { 1, 3, 8 };
    private static readonly HashSet<int> DEAD_MONTHS = new HashSet<int> { 11, 12 };

    private static readonly Dictionary<int, float> TTANG = new Dictionary<int, float>
    {
        { 10, 90f }, { 9, 70f }, { 8, 55f }, { 7, 45f }, { 6, 36f },
        { 5, 29f }, { 4, 23f }, { 3, 18f }, { 2, 14f }, { 1, 11f },
    };

    private static readonly float[] KKUT = { 0f, 0.3f, 0.5f, 0.8f, 1.1f, 1.5f, 1.9f, 2.4f, 3f, 4f };

    private const float SAMGWANG = 300f;

    private static float GetGwangPair(int _monthA, int _monthB)
    {
        int low = Mathf.Min(_monthA, _monthB);
        int high = Mathf.Max(_monthA, _monthB);

        if (low == 3 && high == 8)
            return 220f;
        if (low == 1 && high == 8)
            return 170f;
        if (low == 1 && high == 3)
            return 140f;

        return 0f;
    }

    private static float GetSpecial(int _monthA, int _monthB)
    {
        int low = Mathf.Min(_monthA, _monthB);
        int high = Mathf.Max(_monthA, _monthB);

        if (low == 1 && high == 2)
            return 9f;
        if (low == 1 && high == 4)
            return 8f;
        if (low == 1 && high == 9)
            return 7f;
        if (low == 1 && high == 10)
            return 6f;
        if (low == 4 && high == 6)
            return 5f;

        return 0f;
    }

    // 세 손 중 최고 하나만 채택한다 — 섯다는 누적이 아니라 승부다.
    private static void EvaluateHwatu(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        float bestValue = 0f;
        List<int> bestCells = new List<int>();
        int bestColumn = -1;
        string bestName = "망통";

        for (int ci = 0; ci < COLUMNS.Length; ++ci)
        {
            int[] column = COLUMNS[ci];
            int[] months = { _grid[column[0]] + 1, _grid[column[1]] + 1, _grid[column[2]] + 1 };

            bool allGwang = GWANG_MONTHS.Contains(months[0])
                         && GWANG_MONTHS.Contains(months[1])
                         && GWANG_MONTHS.Contains(months[2]);

            if (allGwang == true)
            {
                bool allDifferent = (months[0] != months[1]) && (months[1] != months[2]) && (months[0] != months[2]);
                if (allDifferent == true)
                {
                    if (SAMGWANG > bestValue)
                    {
                        bestValue = SAMGWANG;
                        bestName = "삼광";
                        bestCells = new List<int>(column);
                        bestColumn = ci;
                    }
                    continue;
                }

                float gwangPair = 0f;
                for (int a = 0; a < 3; ++a)
                {
                    for (int b = a + 1; b < 3; ++b)
                    {
                        float value = GetGwangPair(months[a], months[b]);
                        if (value > gwangPair)
                            gwangPair = value;
                    }
                }

                if (gwangPair > bestValue)
                {
                    bestValue = gwangPair;
                    bestName = "광땡";
                    bestCells = new List<int>(column);
                    bestColumn = ci;
                }

                if (gwangPair > 0f)
                    continue;
            }

            for (int a = 0; a < 3; ++a)
            {
                for (int b = a + 1; b < 3; ++b)
                {
                    int x = months[a];
                    int y = months[b];

                    if (DEAD_MONTHS.Contains(x) == true || DEAD_MONTHS.Contains(y) == true)
                        continue;

                    float value = 0f;
                    string name = "끗";

                    if (x == y)
                    {
                        value = TTANG[x];
                        name = "땡";
                    }
                    else if (GWANG_MONTHS.Contains(x) == true && GWANG_MONTHS.Contains(y) == true
                          && GetGwangPair(x, y) > 0f)
                    {
                        value = 0f;
                    }
                    else if (GetSpecial(x, y) > 0f)
                    {
                        value = GetSpecial(x, y);
                        name = "특수끗";
                    }
                    else
                    {
                        // 9월은 9와 10 둘 다로 읽어 더 높은 쪽을 쓴다(섯다의 국진 규칙).
                        int[] xs = (x == 9) ? new[] { 9, 10 } : new[] { x };
                        int[] ys = (y == 9) ? new[] { 9, 10 } : new[] { y };
                        for (int p = 0; p < xs.Length; ++p)
                        {
                            for (int q = 0; q < ys.Length; ++q)
                            {
                                float kkut = KKUT[(xs[p] + ys[q]) % 10];
                                if (kkut > value)
                                    value = kkut;
                            }
                        }
                    }

                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestName = name;
                        bestCells = new List<int> { column[a], column[b] };
                        bestColumn = ci;
                    }
                }
            }
        }

        for (int i = 0; i < bestCells.Count; ++i)
        {
            if (_result.ListHitCell.Contains(bestCells[i]) == true)
                continue;

            _result.ListHitCell.Add(bestCells[i]);
        }

        _result.Power = bestValue * _table.GetCoef("hwatu", JudgeTable.HWATU_SCALE);
        _result.PatternName = (bestColumn < 0) ? "망통" : $"{bestColumn + 1}열 · {bestName}";
    }
}
