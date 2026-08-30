using System.Collections.Generic;

// 적 1종의 스탯. `Power`가 밸런스의 기준 단위다 —
// 플레이어 1성 유닛 1기를 1로 보고, 이 적 한 마리가 그 몇 기에 해당하는지를 뜻한다.
// 웨이브가 몇 마리를 낼지는 이 값으로 역산하므로(WaveTable 참고) Hp/Atk을 고칠 때 Power도 함께 본다.
public class EnemyRecord : Record
{
    public string Key;
    public string NameKey;
    public string SpritePath;
    public int Hp;
    public int Atk;
    public float AtkSpeed;
    public int Range;
    public float MoveSpeed;
    public int Power;
}

public class EnemyTable : Table<EnemyRecord>
{
    public EnemyTable(List<EnemyRecord> _listRecord) : base(_listRecord) { }

    public EnemyRecord GetRecordByKey(string _key)
    {
        if (string.IsNullOrEmpty(_key) == true)
            return null;

        return list.Find(record => record != null && record.Key == _key);
    }
}
