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

    // 번역이 비어 있으면 한국어로 떨어뜨린다.
    //
    // **왜 필요한가** — 키를 못 찾을 때는 위에서 로그를 남기지만, **찾았는데 그 열이 비면
    // 조용히 빈 문자열이 나간다.** 화면이 통째로 백지가 되는데 오류는 한 줄도 없다.
    // 2026-09-14에 족보·유닛 설명 105행을 한국어만 채우면서 이 구멍이 드러났다.
    //
    // 번역이 늦는 일은 앞으로도 있으므로 항목마다 막지 않고 여기 한 곳에서 막는다.
    private string FallbackToKorean(StringRecord _record, string _translated)
    {
        if (string.IsNullOrEmpty(_translated) == false)
            return _translated;

        return _record.Kr;
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
            case eLanguage.English:
                return FallbackToKorean(record, record.En);
            case eLanguage.Chinese:
                return FallbackToKorean(record, record.Cn);
            case eLanguage.Japanese:
                return FallbackToKorean(record, record.Jp);
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
