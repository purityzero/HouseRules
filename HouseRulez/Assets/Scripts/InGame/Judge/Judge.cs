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

    // 슬롯의 페이라인은 `SlotLineTable`(CSV)이 정한다 — 코드에 박지 않는다.
    // 그 테이블이 "라인을 늘리거나 빼는 건 밸런싱이라 코드 수정 없이 조정할 수 있어야 한다"고
    // 명시하고 있고, 실제 값도 가로 3 · 대각 2로 슬롯 문법과 같다.
    // (세로는 없다. 릴이 세로로 도는 기계라 세로줄은 "한 릴 안의 연속된 칸"이지 라인이 아니다)
    //
    // 재사용 버퍼다. 20만 회 몬테카를로에서 라인마다 배열을 새로 만들면 그만큼 GC가 돈다.
    private static readonly int[] SLOT_LINE_BUFFER = new int[COLUMN_COUNT];

    // 섯다는 세로 3열이 각각 한 손이다.
    private static readonly int[][] COLUMNS =
    {
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
    };

    // 장기 包의 심볼 인덱스. 스프라이트가 이름 오름차순으로 로드되므로
    // cha·jol·ma·po·sa·sang·wang 순서에서 3번이다(실제 폴더로 확인함).
    private const int JANGGI_PO_INDEX = 3;

    // 마작은 통수패 1~9의 9종 풀이고, 9칸이 정확히 3면자로 떨어진다.
    // 머리(자또)를 쓰지 않는 이유가 여기 있다 — 실제 마작의 14장 = 4면자 + 머리와 달리
    // 릴 9칸은 3면자로 딱 나뉜다.
    private const int MAHJONG_RANK_COUNT = 9;
    private const int MAHJONG_MELD_MAX = 3;

    // 윷 — 심볼 인덱스 순서가 곧 이동값이다(파일명 yut_00_backdo ~ yut_05_mo, 이름 오름차순 로드).
    // 빽도만 음수다. 단순 실패 심볼이 아니라 합계를 뒤로 당기는 조절기다.
    private static readonly int[] YUT_MOVE = { -1, 1, 2, 3, 4, 5 };
    private const int YUT_BACKDO_INDEX = 0;

    // 3×3을 윷판 한 바퀴로 쓴다.
    private const int YUT_TRACK_SIZE = 9;

    // 이동값이 이 값 이상이면 윷(4) 또는 모(5)다 — 한 번 더 던지는 눈.
    private const int YUT_BONUS_MIN_MOVE = 4;

    private const int COLUMN_COUNT = 3;

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
            case "slot":
                EvaluateSlot(judgeTable, _grid, result);
                break;
            case "mahjong":
                EvaluateMahjong(judgeTable, _grid, result);
                break;
            case "yut":
                EvaluateYut(judgeTable, _grid, result);
                break;
            default:
                Logger.Error($"[Judge] Evaluate Failed! 판정기 없는 종족 - {_houseKey} (기대: chess/janggi/poker/hwatu/slot/mahjong/yut 중 하나)");
                return result;
        }

        // 윷은 배치를 자기가 만든다 — 심볼·칸·등급이 전부 줄 단위 규칙에서 나와서
        // "전력만큼 빈 칸을 채운다"는 공통 규칙으로는 셋 다 복원할 수 없다.
        // 이미 채워져 있으면 덮어쓰지 않는다.
        if (result.ListSummon.Count <= 0)
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

    // 슬롯 — PAYLINE_RANK. 5개 페이라인에서 3칸이 같으면 그 심볼의 3매치 배당,
    // 정확히 2칸이 같으면 그 심볼의 2매치 배당. 전부 다르면 0.
    //
    // 다른 다섯 종족에 없는 축이 여기 있다: **어느 심볼이 맞았는지가 배당을 정한다.**
    // 체스는 어느 말이든 정렬 8.0으로 같지만, 슬롯은 체리 3매치 3.0과 세븐 3매치 33.0이 11배 갈린다.
    // 그래서 계수 조회가 심볼 인덱스별로 나뉜다.
    //
    // 1매치 배당은 두지 않는다 — 넣으면 무소환률이 4.96%에서 0.6%로 무너져 체스와 같아진다.
    private static void EvaluateSlot(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        int match3 = 0;
        int match2 = 0;
        float power = 0f;
        float power3 = 0f;
        float power2 = 0f;
        int bestSymbol = -1;

        SlotLineTable lineTable = TableManager.instance.GetTable<SlotLineTable>();
        if (lineTable == null)
        {
            Logger.Error("[Judge] EvaluateSlot Failed! SlotLineTable not found (기대: TableManager에 등록됨)");
            return;
        }

        for (int i = 0; i < lineTable.list.Count; ++i)
        {
            SlotLineRecord line = lineTable.list[i];
            if (line == null)
                continue;

            // 라인은 릴별 "몇 번째 행"으로 적혀 있다. 칸 인덱스로 바꾼다 — cell = row * 릴수 + 릴.
            for (int reelIndex = 0; reelIndex < COLUMN_COUNT; ++reelIndex)
            {
                SLOT_LINE_BUFFER[reelIndex] = line.GetRow(reelIndex) * COLUMN_COUNT + reelIndex;
            }

            int a = _grid[SLOT_LINE_BUFFER[0]];
            int b = _grid[SLOT_LINE_BUFFER[1]];
            int c = _grid[SLOT_LINE_BUFFER[2]];

            if (a == b && b == c)
            {
                match3++;
                float coef3 = _table.GetCoef("slot", JudgeTable.SLOT_MATCH3_PREFIX + a);
                power += coef3;
                power3 += coef3;
                AddHit(_result, SLOT_LINE_BUFFER);

                if (a > bestSymbol)
                    bestSymbol = a;

                continue;
            }

            // 정확히 2개가 같은 경우. 어느 심볼이 짝인지에 따라 배당이 다르므로 그 심볼을 집어낸다.
            int paired = -1;
            if (a == b)
                paired = a;
            else if (b == c)
                paired = b;
            else if (a == c)
                paired = a;

            if (paired < 0)
                continue;

            match2++;
            float coef2 = _table.GetCoef("slot", JudgeTable.SLOT_MATCH2_PREFIX + paired);
            power += coef2;
            power2 += coef2;
            AddPartialPair(_result, _grid, SLOT_LINE_BUFFER);
        }

        _result.Power = power;

        // 슬롯은 심볼마다 배당이 달라 "N개 × 계수" 꼴로 못 묶는다.
        // 개수만 세고 계수 자리에는 그 줄들의 평균 배당을 넣는다(합계는 Power와 일치한다).
        AddTerm(_result, "3매치", match3, (match3 > 0) ? power3 / match3 : 0f);
        AddTerm(_result, "2매치", match2, (match2 > 0) ? power2 / match2 : 0f);

        _result.PatternName = (match3 > 0)
            ? $"{GetSlotSymbolName(bestSymbol)} {match3}줄"
            : ((match2 > 0) ? $"짝 {match2}줄" : "무판정");
    }

    // 배당 등급 순 = 스프라이트 로드 순서(slot_01_cherry ~ slot_06_seven)라 인덱스가 곧 등급이다.
    private static string GetSlotSymbolName(int _symbolIndex)
    {
        switch (_symbolIndex)
        {
            case 0: return "체리";
            case 1: return "레몬";
            case 2: return "벨";
            case 3: return "BAR";
            case 4: return "2BAR";
            case 5: return "세븐";
        }

        return "무판정";
    }

    // 마작 — 면자 완전분할(MeldPartition). 9칸을 3장씩 세 묶음으로 나눠
    // 같은 숫자 3장(커쯔) 또는 연속 세 숫자(슌쯔)면 면자로 친다.
    //
    // **위치를 보지 않는 유일한 종족이다.** 9칸이 숫자별 개수 배열로 환원되므로 라인 순회가 아예 없다.
    private static void EvaluateMahjong(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        int[] counts = new int[MAHJONG_RANK_COUNT];
        for (int i = 0; i < _grid.Length; ++i)
        {
            if (_grid[i] < 0 || _grid[i] >= MAHJONG_RANK_COUNT)
                continue;

            counts[_grid[i]]++;
        }

        int meld = 0;
        int kotsu = 0;
        Decompose(counts, ref meld, ref kotsu);

        float meldCoef = _table.GetCoef("mahjong", JudgeTable.MAHJONG_MELD);
        float power = meldCoef * meld;
        bool isWin = (meld >= MAHJONG_MELD_MAX);
        bool isIkkitsukan = false;

        AddTerm(_result, "면자", meld, meldCoef);

        if (isWin == true)
        {
            isIkkitsukan = IsIkkitsukan(counts);

            float winCoef = _table.GetCoef("mahjong", JudgeTable.MAHJONG_WIN);
            power += winCoef;
            AddTerm(_result, "화료", 1, winCoef);

            // 커쯔 프리미엄은 화료라는 문턱을 넘은 뒤에만 열린다.
            // 부분 성립에도 주면 마작이 "같은 것 3개 모으기"가 되어 체스 정렬·포커 트리플과 체감이 겹친다.
            float kotsuCoef = _table.GetCoef("mahjong", JudgeTable.MAHJONG_KOTSU);
            power += kotsuCoef * kotsu;
            AddTerm(_result, "커쯔", kotsu, kotsuCoef);

            if (isIkkitsukan == true)
            {
                float ikkiCoef = _table.GetCoef("mahjong", JudgeTable.MAHJONG_IKKITSUKAN);
                power += ikkiCoef;
                AddTerm(_result, "일기통관", 1, ikkiCoef);
            }

            // 화료는 9칸을 전부 쓴 것이라 판정에 걸린 칸도 9칸 전체다.
            for (int i = 0; i < _grid.Length; ++i)
            {
                if (_result.ListHitCell.Contains(i) == true)
                    continue;

                _result.ListHitCell.Add(i);
            }
        }
        else if (IsTenpai(counts) == true)
        {
            // 텐파이 보너스는 화료를 못 한 상태에서만 붙는다(유국텐파이료).
            float tenpaiCoef = _table.GetCoef("mahjong", JudgeTable.MAHJONG_TENPAI);
            power += tenpaiCoef;
            AddTerm(_result, "텐파이", 1, tenpaiCoef);
        }

        _result.Power = power;

        if (isWin == true)
        {
            _result.PatternName = (isIkkitsukan == true)
                ? "일기통관"
                : ((kotsu > 0) ? $"화료 · 커쯔 {kotsu}" : "화료");
        }
        else
        {
            _result.PatternName = (meld > 0) ? $"{meld}면자" : "무판정";
        }
    }

    // 개수 배열을 재귀 분해해 (최대 면자 수, 그 최대를 낼 때의 최대 커쯔 수)를 구한다.
    // 비교는 (면자 수, 커쯔 수) 사전식 — 면자를 먼저 최대화하고 동률이면 커쯔를 최대화한다(커쯔가 점수가 높다).
    //
    // 브리프가 제안한 "280가지 3분할 완전탐색"은 불필요하다. 위치 무관이라 개수 배열로 환원되고,
    // 최대 깊이 9 · 분기 3이라 메모이제이션 없이도 스핀당 마이크로초 단위다.
    private static void Decompose(int[] _counts, ref int _meld, ref int _kotsu)
    {
        int low = -1;
        for (int i = 0; i < _counts.Length; ++i)
        {
            if (_counts[i] <= 0)
                continue;

            low = i;
            break;
        }

        if (low < 0)
        {
            _meld = 0;
            _kotsu = 0;
            return;
        }

        int bestMeld = 0;
        int bestKotsu = 0;

        // 1) 커쯔로 뗀다
        if (_counts[low] >= 3)
        {
            _counts[low] -= 3;
            int subMeld = 0;
            int subKotsu = 0;
            Decompose(_counts, ref subMeld, ref subKotsu);
            _counts[low] += 3;

            TakeBetter(subMeld + 1, subKotsu + 1, ref bestMeld, ref bestKotsu);
        }

        // 2) 슌쯔로 뗀다
        if (low + 2 < _counts.Length && _counts[low + 1] > 0 && _counts[low + 2] > 0)
        {
            _counts[low]--;
            _counts[low + 1]--;
            _counts[low + 2]--;
            int subMeld = 0;
            int subKotsu = 0;
            Decompose(_counts, ref subMeld, ref subKotsu);
            _counts[low]++;
            _counts[low + 1]++;
            _counts[low + 2]++;

            TakeBetter(subMeld + 1, subKotsu, ref bestMeld, ref bestKotsu);
        }

        // 3) 이 패 1장을 버린다. 면자를 못 만드는 패가 섞여 있어도 나머지로 분해가 이어져야 한다.
        _counts[low]--;
        int dropMeld = 0;
        int dropKotsu = 0;
        Decompose(_counts, ref dropMeld, ref dropKotsu);
        _counts[low]++;

        TakeBetter(dropMeld, dropKotsu, ref bestMeld, ref bestKotsu);

        _meld = bestMeld;
        _kotsu = bestKotsu;
    }

    private static void TakeBetter(int _meld, int _kotsu, ref int _bestMeld, ref int _bestKotsu)
    {
        if (_meld < _bestMeld)
            return;

        if (_meld <= _bestMeld && _kotsu <= _bestKotsu)
            return;

        _bestMeld = _meld;
        _bestKotsu = _kotsu;
    }

    // 한 장을 더 받으면 3면자가 되는 상태. 화료가 아닐 때만 조회한다.
    private static bool IsTenpai(int[] _counts)
    {
        for (int rank = 0; rank < _counts.Length; ++rank)
        {
            _counts[rank]++;
            int meld = 0;
            int kotsu = 0;
            Decompose(_counts, ref meld, ref kotsu);
            _counts[rank]--;

            if (meld >= MAHJONG_MELD_MAX)
                return true;
        }

        return false;
    }

    // 1~9가 각 1장씩. 9칸뿐이라 이 조건은 곧 123·456·789 세 슌쯔다.
    private static bool IsIkkitsukan(int[] _counts)
    {
        for (int i = 0; i < _counts.Length; ++i)
        {
            if (_counts[i] < 1 || _counts[i] > 1)
                return false;
        }

        return true;
    }

    // 윷 — 가로줄 3개를 각각 말 하나로 읽는다. 이동값의 합(도착점)이 그 말의 모든 것을 정한다.
    //
    // 다른 여섯 종족과 구조가 다르다: 전력을 먼저 구하고 칸을 채우는 게 아니라,
    // 줄마다 칸·심볼·등급이 먼저 정해지고 전력은 그 결과다. 그래서 BuildSummon을 타지 않는다.
    private static void EvaluateYut(JudgeTable _table, int[] _grid, JudgeResult _result)
    {
        UnitGradeTable gradeTable = TableManager.instance.GetTable<UnitGradeTable>();
        if (gradeTable == null)
        {
            Logger.Error("[Judge] EvaluateYut Failed! UnitGradeTable not found (기대: TableManager에 등록됨)");
            return;
        }

        int grade2Landing = Mathf.RoundToInt(_table.GetCoef("yut", JudgeTable.YUT_GRADE2_LANDING));

        // 줄마다 도착점과 말의 종류를 먼저 구한다.
        int[] landings = new int[COLUMN_COUNT];
        int[] symbols = new int[COLUMN_COUNT];
        for (int row = 0; row < COLUMN_COUNT; ++row)
        {
            landings[row] = GetYutLanding(_grid, row);
            symbols[row] = GetYutSymbol(_grid, row);
        }

        int summonCount = 0;
        int backdoCount = 0;
        int lapCount = 0;

        // 윷·모가 나오면 한 번 더 던진다는 윷놀이 규칙. 한 줄이 통째로 윷/모일 때만 발동한다.
        // 칸마다 주면 스핀당 기대 3회가 되어 스핀 경제가 무너진다 —
        // 줄 단위면 줄당 1/27, 세 줄 중 최소 하나가 10.70%다.
        // 스핀당 최대 1회로 묶는다. 세 줄이 다 걸리면 3회가 나와 상한이 없으면 같은 문제가 생긴다.
        for (int row = 0; row < COLUMN_COUNT; ++row)
        {
            if (IsYutBonusRow(_grid, row) == false)
                continue;

            _result.bonusSpin = 1;
            break;
        }

        for (int row = 0; row < COLUMN_COUNT; ++row)
        {
            int landing = landings[row];

            // 음수는 뒤로 밀려 판을 벗어난 것이다 — 그 줄은 소환하지 않는다.
            if (landing < 0)
                continue;

            int cell = GetYutCell(landing);

            // 업기 — 같은 칸에 도착한 줄은 겹쳐서 한 말이 되고 등급이 오른다.
            // 윷놀이의 업기(말을 포개 하나로 움직이는 것)를 그대로 등급으로 옮긴 것이다.
            //
            // 판정 기준을 도착점이 아니라 **칸**으로 잡는다. 트랙이 9칸에서 감기므로
            // 도착점 1과 10처럼 서로 다른 값도 같은 칸이 된다 — 도착점으로 비교하면
            // 그 경우를 못 잡아 한 칸에 두 기가 겹쳐 선다.
            if (_result.ListHitCell.Contains(cell) == true)
            {
                RaiseYutStack(_result, cell, gradeTable.maxGrade);
                continue;
            }

            int grade = 1;
            if (landing >= grade2Landing)
                grade++;

            // 완주 — 트랙 한 바퀴(9칸)를 넘겼다. 윷놀이의 "나기"에 해당해 한 단계 더 올린다.
            bool isLap = (landing > YUT_TRACK_SIZE);
            if (isLap == true)
            {
                grade++;
                lapCount++;
            }

            if (landing <= 0)
                backdoCount++;

            grade = Mathf.Clamp(grade, 1, gradeTable.maxGrade);

            _result.ListSummon.Add(new SummonSlot(cell, grade, symbols[row]));
            if (_result.ListHitCell.Contains(cell) == false)
                _result.ListHitCell.Add(cell);

            summonCount++;
        }

        // 전력은 배치 결과에서 나온다. 다른 종족은 전력이 배치를 정하지만 윷은 반대다.
        float power = 0f;
        for (int i = 0; i < _result.ListSummon.Count; ++i)
        {
            power += gradeTable.GetMultiplier(_result.ListSummon[i].Grade);
        }

        _result.Power = power;
        _result.placedPower = power;

        // 윷은 계수를 곱해 전력을 만드는 게 아니라 배치 결과가 곧 전력이다.
        // 그래서 내역도 "말 N기 × 등급 배수"가 아니라 등급별 기수로 보여준다.
        UnitGradeTable termTable = gradeTable;
        for (int grade = 1; grade <= termTable.maxGrade; ++grade)
        {
            int count = 0;
            for (int i = 0; i < _result.ListSummon.Count; ++i)
            {
                if (_result.ListSummon[i].Grade < grade || _result.ListSummon[i].Grade > grade)
                    continue;

                count++;
            }

            AddTerm(_result, $"{grade}성 말", count, termTable.GetMultiplier(grade));
        }

        if (summonCount <= 0)
            _result.PatternName = "낙";
        else if (lapCount > 0)
            _result.PatternName = $"완주 {lapCount}";
        else if (backdoCount > 0)
            _result.PatternName = $"빽도 {backdoCount}";
        else
            _result.PatternName = $"말 {summonCount}";
    }

    // 이미 같은 칸에 선 말의 등급을 한 단계 올린다(업기).
    private static void RaiseYutStack(JudgeResult _result, int _cell, int _maxGrade)
    {
        for (int i = 0; i < _result.ListSummon.Count; ++i)
        {
            SummonSlot slot = _result.ListSummon[i];
            if (slot.Cell < _cell || slot.Cell > _cell)
                continue;

            slot.Grade = Mathf.Clamp(slot.Grade + 1, 1, _maxGrade);
            _result.ListSummon[i] = slot;
            return;
        }
    }

    // 가로줄이 통째로 윷 또는 모인가. 이동값으로 판단한다 —
    // 심볼 인덱스를 직접 비교하면 나중에 풀 순서가 바뀔 때 조용히 어긋난다.
    private static bool IsYutBonusRow(int[] _grid, int _row)
    {
        for (int column = 0; column < COLUMN_COUNT; ++column)
        {
            int symbol = _grid[_row * COLUMN_COUNT + column];
            if (symbol < 0 || symbol >= YUT_MOVE.Length)
                return false;

            if (YUT_MOVE[symbol] < YUT_BONUS_MIN_MOVE)
                return false;
        }

        return true;
    }

    private static int GetYutLanding(int[] _grid, int _row)
    {
        int landing = 0;
        for (int column = 0; column < COLUMN_COUNT; ++column)
        {
            int symbol = _grid[_row * COLUMN_COUNT + column];
            if (symbol < 0 || symbol >= YUT_MOVE.Length)
                continue;

            landing += YUT_MOVE[symbol];
        }

        return landing;
    }

    // 말의 종류는 가로줄을 **오른쪽부터** 훑어 처음 만나는 빽도가 아닌 심볼이다.
    // 마지막 윷 결과가 말의 형태를 정한다는 직관이고, 빽도가 독립 유닛으로 과다 소환되는 것도 막는다.
    // 전부 빽도면 어쩔 수 없이 빽도가 말이 된다(합계 -3이라 실제로는 소환되지 않는다).
    private static int GetYutSymbol(int[] _grid, int _row)
    {
        for (int column = COLUMN_COUNT - 1; column >= 0; --column)
        {
            int symbol = _grid[_row * COLUMN_COUNT + column];
            if (symbol <= YUT_BACKDO_INDEX)
                continue;

            return symbol;
        }

        return YUT_BACKDO_INDEX;
    }

    // 3×3을 윷판 한 바퀴(9칸)로 쓴다. 트랙 순서는 후열 → 중열 → 전열이라
    // 멀리 간 말일수록 적 쪽으로 나가 선다. 한 바퀴를 넘기면 다시 후열부터 돈다.
    private static int GetYutCell(int _landing)
    {
        // 도착점 0(빽도 전용 유닛)은 출발점에 세운다.
        if (_landing <= 0)
            return 0;

        int track = (_landing - 1) % YUT_TRACK_SIZE;
        return (track % COLUMN_COUNT) * COLUMN_COUNT + (track / COLUMN_COUNT);
    }

    // 전력 내역 한 줄. 0개면 안 담는다 — 화면에 "정렬 0 × 8.0 = 0"이 뜨면 잡음이다.
    private static void AddTerm(JudgeResult _result, string _label, float _value, float _coef)
    {
        if (_value <= 0f)
            return;

        _result.ListTerm.Add(new JudgeTerm(_label, _value, _coef));
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

    // 절반만 성립한 칸. 주판정 칸과 겹치면 넣지 않는다 — 겹치면 약한 강조가 강한 강조를 덮는다.
    private static void AddPartial(JudgeResult _result, int[] _line)
    {
        for (int i = 0; i < _line.Length; ++i)
        {
            if (_result.ListPartialCell.Contains(_line[i]) == true)
                continue;

            _result.ListPartialCell.Add(_line[i]);
        }
    }

    // 같은 심볼이 2개인 라인에서 그 2칸만 골라낸다.
    // 라인 전체를 약하게 칠하면 관계없는 세 번째 칸까지 반짝여 규칙을 더 헷갈리게 만든다.
    private static void AddPartialPair(JudgeResult _result, int[] _grid, int[] _line)
    {
        for (int i = 0; i < _line.Length; ++i)
        {
            for (int j = i + 1; j < _line.Length; ++j)
            {
                if (_grid[_line[i]] != _grid[_line[j]])
                    continue;

                if (_result.ListPartialCell.Contains(_line[i]) == false)
                    _result.ListPartialCell.Add(_line[i]);

                if (_result.ListPartialCell.Contains(_line[j]) == false)
                    _result.ListPartialCell.Add(_line[j]);

                return;
            }
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
                AddPartialPair(_result, _grid, LINES[i]);
            }
        }

        float tripleCoef = _table.GetCoef("chess", JudgeTable.CHESS_LINE_TRIPLE);
        float pairCoef = _table.GetCoef("chess", JudgeTable.CHESS_LINE_PAIR);
        _result.Power = tripleCoef * triple + pairCoef * pair;

        AddTerm(_result, "정렬", triple, tripleCoef);
        AddTerm(_result, "반정렬", pair, pairCoef);
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
                AddPartialPair(_result, _grid, LINES[i]);
            }
        }

        float jumpCoef = _table.GetCoef("janggi", JudgeTable.JANGGI_JUMP);
        float cannonCoef = _table.GetCoef("janggi", JudgeTable.JANGGI_CANNON);
        float edgeCoef = _table.GetCoef("janggi", JudgeTable.JANGGI_EDGE);
        _result.Power = jumpCoef * jump + cannonCoef * cannon + edgeCoef * edge;

        AddTerm(_result, "포 넘기", jump, jumpCoef);
        AddTerm(_result, "대포", cannon, cannonCoef);
        AddTerm(_result, "진", edge, edgeCoef);
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
                AddPartialPair(_result, _grid, LINES[i]);
            }
        }

        float tripleCoef = _table.GetCoef("poker", JudgeTable.POKER_TRIPLE);
        float straightCoef = _table.GetCoef("poker", JudgeTable.POKER_STRAIGHT);
        float pairCoef = _table.GetCoef("poker", JudgeTable.POKER_PAIR);
        _result.Power = tripleCoef * triple + straightCoef * straight + pairCoef * pair;

        AddTerm(_result, "트리플", triple, tripleCoef);
        AddTerm(_result, "스트레이트", straight, straightCoef);
        AddTerm(_result, "페어", pair, pairCoef);
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

        float scale = _table.GetCoef("hwatu", JudgeTable.HWATU_SCALE);
        _result.Power = bestValue * scale;

        // 화투는 족보 값 자체가 크고(삼광 300, 땡 90) 스케일로 눌러 맞춘다.
        // 그래서 "족보값 × 스케일"이 그대로 계산 과정이다.
        // 족보값을 정수로 깎지 않는다. 끗은 2.4 같은 실수라 반올림하면 식과 전력이 어긋난다.
        AddTerm(_result, bestName, bestValue, scale);

        _result.PatternName = (bestColumn < 0) ? "망통" : $"{bestColumn + 1}열 · {bestName}";
    }
}
