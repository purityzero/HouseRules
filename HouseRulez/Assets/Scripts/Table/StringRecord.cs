using System.Collections.Generic;
using UnityEngine;

public class StringRecord : Record
{
    public string Key;
    public string Kr;
    public string En;
    public string Cn;
    public string Jp;
}

public class StringTable : Table<StringRecord>
{
    // 언어는 관찰 가능한 값이다. 열려 있는 화면이 여럿일 때 바꾼 쪽이 남의 화면까지
    // 챙겨 갱신해줄 수는 없으므로, 각 화면이 스스로 구독해서 다시 그린다.
    // RegisterObserver는 등록 즉시 현재 값으로 한 번 호출하므로 최초 적용도 겸한다.
    public static readonly ObservableVariable<eLanguage> LANGUAGE = new ObservableVariable<eLanguage>(GetDefaultLanguage());

    // 기존 호출부를 그대로 두기 위한 통로. 읽고 쓰는 건 전부 위 관찰 값이다.
    public static eLanguage CurrentLanguage
    {
        get { return LANGUAGE.Value; }
        set { LANGUAGE.Value = value; }
    }

    public StringTable(List<StringRecord> _listRecord) : base(_listRecord) { }

    public string GetString(string _key)
    {
        return GetTemplate(_key);
    }

    public string GetString(string _key, object _arg1)
    {
        return string.Format(GetTemplate(_key), _arg1);
    }

    public string GetString(string _key, object _arg1, object _arg2)
    {
        return string.Format(GetTemplate(_key), _arg1, _arg2);
    }

    public string GetString(string _key, object _arg1, object _arg2, object _arg3)
    {
        return string.Format(GetTemplate(_key), _arg1, _arg2, _arg3);
    }

    private string GetTemplate(string _key)
    {
        StringRecord record = list.Find(record => record.Key == _key);
        if (record == null)
        {
            Logger.Error($"[StringTable] GetString Failed! key not found - {_key}");
            return _key;
        }

        switch (CurrentLanguage)
        {
            case eLanguage.Korean:
                return record.Kr;
            case eLanguage.English:
                return record.En;
            case eLanguage.Chinese:
                return record.Cn;
            case eLanguage.Japanese:
                return record.Jp;
            default:
                return record.Kr;
        }
    }

    public static eLanguage GetDefaultLanguage()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean:
                return eLanguage.Korean;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
                return eLanguage.Chinese;
            case SystemLanguage.Japanese:
                return eLanguage.Japanese;
            default:
                return eLanguage.English;
        }
    }
}
