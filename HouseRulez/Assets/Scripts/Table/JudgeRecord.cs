using System.Collections.Generic;

// 종족별 판정 패턴의 계수. GDD가 50만 회 몬테카를로로 역산한 값이라 코드에 박지 않고 CSV로 둔다.
// 여기 숫자를 고치면 종족 밸런스가 직접 바뀐다 — 고친 뒤에는 반드시 시뮬레이션을 다시 돌린다.
public class JudgeRecord : Record
{
    public string HouseKey;
    public string PatternKey;
    public float Coef;
}

public class JudgeTable : Table<JudgeRecord>
{
    // 패턴 키. 문자열을 호출부마다 다시 적으면 오타가 조용히 계수 0으로 흘러간다.
    public const string CHESS_LINE_TRIPLE = "ChessLineTriple";
    public const string CHESS_LINE_PAIR = "ChessLinePair";
    public const string JANGGI_JUMP = "JanggiJump";
    public const string JANGGI_CANNON = "JanggiCannon";
    public const string JANGGI_EDGE = "JanggiEdge";
    public const string POKER_TRIPLE = "PokerTriple";
    public const string POKER_STRAIGHT = "PokerStraight";
    public const string POKER_PAIR = "PokerPair";
    public const string HWATU_SCALE = "HwatuScale";

    // 슬롯은 "어느 심볼이 맞았나"가 배당을 정한다(PAYLINE_RANK) — 다른 종족에 없는 축이라
    // 패턴 키가 심볼 인덱스별로 갈린다. 접두사 뒤에 인덱스를 붙여 만든다.
    public const string SLOT_MATCH3_PREFIX = "SlotMatch3_";
    public const string SLOT_MATCH2_PREFIX = "SlotMatch2_";

    public const string MAHJONG_MELD = "MahjongMeld";
    public const string MAHJONG_TENPAI = "MahjongTenpai";
    public const string MAHJONG_WIN = "MahjongWin";
    public const string MAHJONG_KOTSU = "MahjongKotsu";
    public const string MAHJONG_IKKITSUKAN = "MahjongIkkitsukan";

    // 윷은 계수가 아니라 **도착점 임계값**이다. 멀리 간 말일수록 등급이 높다(도착점 비례).
    // 이 숫자를 올리면 윷이 약해지고 내리면 세진다 — 윷의 유일한 밸런스 레버다.
    //
    // 3성 임계값을 따로 두지 않는 이유: 완주(트랙 한 바퀴)가 이미 +1을 주기 때문에
    // 어떤 값을 넣어도 발동하지 않는 죽은 손잡이가 된다. 3성은 완주가 담당한다.
    public const string YUT_GRADE2_LANDING = "YutGrade2Landing";

    public JudgeTable(List<JudgeRecord> _listRecord) : base(_listRecord) { }

    public float GetCoef(string _houseKey, string _patternKey)
    {
        JudgeRecord record = list.Find(judge => judge != null
            && judge.HouseKey == _houseKey
            && judge.PatternKey == _patternKey);

        if (record == null)
        {
            Logger.Error($"[JudgeTable] GetCoef Failed! 계수 없음 - {_houseKey}/{_patternKey} (기대: JudgeTable.csv에 해당 행 존재)");
            return 0f;
        }

        return record.Coef;
    }
}
