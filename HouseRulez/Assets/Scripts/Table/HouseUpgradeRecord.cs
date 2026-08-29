using System.Collections.Generic;

// Value는 "그 레벨에서의 총 증가량"이다. 레벨당 증가분이 아니므로 호출부가 Lv1..N을 합산하지 않는다 —
// 합산 경로를 두면 중복 호출 한 번에 값이 배수로 튄다.
public class HouseUpgradeRecord : Record
{
    // EffectType. 지금은 런 시작값 가산 한 종류뿐이라 상수 하나로 둔다.
    public const string EFFECT_RUN_CONFIG_ADD = "RunConfigAdd";

    public string Key;
    public string HouseKey;
    public string NameKey;
    public string DescKey;
    public int Level;
    public string EffectType;
    public string TargetKey;
    public float Value;
    public string CostType;
    public int CostValue;
    public string PrerequisiteKey;
    public int SortOrder;
    public string IconPath;
}

public class HouseUpgradeTable : Table<HouseUpgradeRecord>
{
    public HouseUpgradeTable(List<HouseUpgradeRecord> _listRecord) : base(_listRecord) { }

    public List<HouseUpgradeRecord> GetListByHouseKey(string _houseKey)
    {
        List<HouseUpgradeRecord> listResult = new List<HouseUpgradeRecord>();
        if (string.IsNullOrEmpty(_houseKey) == true)
            return listResult;

        for (int i = 0; i < list.Count; ++i)
        {
            HouseUpgradeRecord record = list[i];
            if (record == null)
                continue;

            if (record.HouseKey != _houseKey)
                continue;

            listResult.Add(record);
        }

        listResult.Sort(CompareRecord);
        return listResult;
    }

    // 종족이 가진 노드 키를 SortOrder 순으로, 중복 없이 돌려준다. 화면의 노드 줄 하나가 이 키 하나다.
    public List<string> GetNodeKeyList(string _houseKey)
    {
        List<string> listNodeKey = new List<string>();
        List<HouseUpgradeRecord> listRecord = GetListByHouseKey(_houseKey);

        for (int i = 0; i < listRecord.Count; ++i)
        {
            string nodeKey = listRecord[i].Key;
            if (listNodeKey.Contains(nodeKey) == true)
                continue;

            listNodeKey.Add(nodeKey);
        }

        return listNodeKey;
    }

    public HouseUpgradeRecord GetRecord(string _houseKey, string _nodeKey, int _level)
    {
        if (string.IsNullOrEmpty(_houseKey) == true)
            return null;

        if (string.IsNullOrEmpty(_nodeKey) == true)
            return null;

        return list.Find(record => record != null
            && record.HouseKey == _houseKey
            && record.Key == _nodeKey
            && record.Level >= _level
            && record.Level <= _level);
    }

    public int GetMaxLevel(string _houseKey, string _nodeKey)
    {
        int maxLevel = 0;

        for (int i = 0; i < list.Count; ++i)
        {
            HouseUpgradeRecord record = list[i];
            if (record == null)
                continue;

            if (record.HouseKey != _houseKey)
                continue;

            if (record.Key != _nodeKey)
                continue;

            if (record.Level > maxLevel)
                maxLevel = record.Level;
        }

        return maxLevel;
    }

    private static int CompareRecord(HouseUpgradeRecord _left, HouseUpgradeRecord _right)
    {
        int sortOrderComparison = _left.SortOrder.CompareTo(_right.SortOrder);
        if (sortOrderComparison < 0 || sortOrderComparison > 0)
            return sortOrderComparison;

        int keyComparison = string.CompareOrdinal(_left.Key, _right.Key);
        if (keyComparison < 0 || keyComparison > 0)
            return keyComparison;

        return _left.Level.CompareTo(_right.Level);
    }
}
