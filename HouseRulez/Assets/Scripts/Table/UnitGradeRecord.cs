using System.Collections.Generic;

// 유닛 등급별 전력 배수. GDD의 흡수 승급 배수(2성 x2.6 / 3성 x7.0)를 그대로 쓴다.
// 프리즘(x18.0)은 심판 웨이브 클리어라는 별도 조건이 붙고 전장 2칸을 먹으므로 판정 소환에는 넣지 않는다.
public class UnitGradeRecord : Record
{
    public int Grade;
    public string NameKey;
    public float Multiplier;
}

public class UnitGradeTable : Table<UnitGradeRecord>
{
    public UnitGradeTable(List<UnitGradeRecord> _listRecord) : base(_listRecord) { }

    public float GetMultiplier(int _grade)
    {
        UnitGradeRecord record = list.Find(grade => grade != null && grade.Grade >= _grade && grade.Grade <= _grade);
        if (record == null)
        {
            Logger.Error($"[UnitGradeTable] GetMultiplier Failed! 등급 없음 - {_grade} (기대: UnitGradeTable.csv에 해당 행 존재)");
            return 1f;
        }

        return record.Multiplier;
    }

    public int maxGrade
    {
        get
        {
            int max = 1;
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] != null && list[i].Grade > max)
                    max = list[i].Grade;
            }
            return max;
        }
    }
}
