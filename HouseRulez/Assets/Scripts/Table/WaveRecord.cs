using System.Collections.Generic;
using UnityEngine;

// 12연차 × 3웨이브 = 36행. 한 웨이브가 곧 스핀 1회다(연차당 스핀 코인 3개).
//
// 적 마릿수는 CSV에 적지 않고 `PowerCoef`에서 역산한다 — 마릿수와 계수를 둘 다 적으면
// 한쪽만 고쳤을 때 어느 게 맞는지 알 수 없게 된다. 튜닝은 계수 하나만 만진다.
public class WaveRecord : Record
{
    public const string TYPE_NORMAL = "Normal";
    public const string TYPE_JUDGEMENT = "Judgement";
    public const string TYPE_FINAL = "Final";

    public int Year;
    public int WaveIndex;
    public string WaveType;
    public float PowerCoef;
    public string EnemyKey;
}

public class WaveTable : Table<WaveRecord>
{
    public WaveTable(List<WaveRecord> _listRecord) : base(_listRecord) { }

    public WaveRecord GetRecord(int _year, int _waveIndex)
    {
        return list.Find(record => record != null
            && record.Year >= _year && record.Year <= _year
            && record.WaveIndex >= _waveIndex && record.WaveIndex <= _waveIndex);
    }

    public List<WaveRecord> GetListByYear(int _year)
    {
        List<WaveRecord> listResult = new List<WaveRecord>();
        for (int i = 0; i < list.Count; ++i)
        {
            WaveRecord record = list[i];
            if (record == null)
                continue;

            if (record.Year < _year || record.Year > _year)
                continue;

            listResult.Add(record);
        }

        listResult.Sort((left, right) => left.WaveIndex.CompareTo(right.WaveIndex));
        return listResult;
    }

    // 이 웨이브가 낼 적의 목표 전력. 플레이어 1성 유닛 몇 기에 해당하는지를 뜻한다.
    public static float GetTargetPower(WaveRecord _record, int _basePower)
    {
        if (_record == null)
            return 0f;

        return _record.PowerCoef * _basePower;
    }

    // 목표 전력을 그 적의 Power로 나눈 마릿수. 계수가 아무리 작아도 웨이브에 적이 0마리면
    // 전투가 성립하지 않으므로 최소 1마리는 낸다.
    public static int GetSpawnCount(WaveRecord _record, EnemyRecord _enemy, int _basePower)
    {
        if (_record == null || _enemy == null)
            return 0;

        if (_enemy.Power <= 0)
        {
            Logger.Error($"[WaveTable] GetSpawnCount Failed! Power가 0 이하 - {_enemy.Key} (기대: 1 이상)");
            return 0;
        }

        int count = Mathf.RoundToInt(GetTargetPower(_record, _basePower) / _enemy.Power);
        return Mathf.Max(1, count);
    }
}
