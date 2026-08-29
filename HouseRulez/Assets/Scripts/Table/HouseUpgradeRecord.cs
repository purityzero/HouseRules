using System.Collections.Generic;

public class HouseUpgradeRecord : Record
{
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
