using System.Collections.Generic;

// 화투(섯다)의 판돈 단계. GDD 05장 SutdaBet 표의 배수를 그대로 들고 있다.
// 몰수선(끗 이하 / 특수끗 이하)은 판정기가 생긴 뒤에 컬럼으로 붙인다 — 지금 추측해서 넣지 않는다.
public class SutdaBetRecord : Record
{
    public int Level;
    public float Multiplier;
}

public class SutdaBetTable : Table<SutdaBetRecord>
{
    public SutdaBetTable(List<SutdaBetRecord> _listRecord) : base(_listRecord) { }

    // Id 순서가 곧 판돈 단계 순서다(HouseTable과 같은 관례) — 그래서 인덱스로 바로 집는다.
    public SutdaBetRecord GetRecordByLevel(int _level)
    {
        if (_level < 0)
            return null;

        if (_level >= list.Count)
            return null;

        return list[_level];
    }
}
