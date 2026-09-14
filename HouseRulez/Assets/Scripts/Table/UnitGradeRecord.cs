using System.Collections.Generic;

// 유닛 등급별 전력 배수. GDD의 흡수 승급 배수(2성 x2.6 / 3성 x7.0)를 그대로 쓴다.
// 프리즘(x18.0)은 심판 웨이브 클리어라는 별도 조건이 붙고 전장 2칸을 먹으므로 판정 소환에는 넣지 않는다.
public class UnitGradeRecord : Record
{
    public int Grade;
    public string NameKey;
    public float Multiplier;

    // 전투 스탯. Hp/Atk은 1성 기본값(10/2)에 Multiplier를 곱한 값이라
    // 전력 1 = 1성 1기라는 환산과 어긋나지 않는다. 적 grunt(Power 1, Hp 10, Atk 2)와도 같은 눈금이다.
    public int Hp;
    // float이다 — 1성 Atk가 2라서 정수로 두면 심볼 배율 0.55~1.5가 전부 1~3으로 뭉개진다.
    // 런 초반은 전부 1성이라, 유닛이 다르다는 것이 가장 잘 보여야 할 구간에서 안 보였다(2026-09-12 결정).
    // 적 Atk와 같은 눈금이므로 EnemyRecord.Atk도 함께 float다.
    public float Atk;
    public float AtkSpeed;
    public int Range;
    public float MoveSpeed;
}

public class UnitGradeTable : Table<UnitGradeRecord>
{
    public UnitGradeTable(List<UnitGradeRecord> _listRecord) : base(_listRecord) { }

    public UnitGradeRecord GetRecord(int _grade)
    {
        UnitGradeRecord record = list.Find(grade => grade != null && grade.Grade >= _grade && grade.Grade <= _grade);
        if (record == null)
            Logger.Error($"[UnitGradeTable] GetRecord Failed! 등급 없음 - {_grade} (기대: UnitGradeTable.csv에 해당 행 존재)");

        return record;
    }

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
