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
