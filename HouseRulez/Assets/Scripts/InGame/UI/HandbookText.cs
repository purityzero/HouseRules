using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 종족의 **족보**와 **유닛 특성**을 사람이 읽는 글로 만든다.
//
// 왜 별도 클래스인가 — 보여주는 자리는 바뀔 수 있다(지금은 접이식 패널이지만
// 도감 화면이나 툴팁이 될 수도 있다). 글을 만드는 규칙이 화면 클래스 안에 있으면
// 자리를 옮길 때마다 따라다니거나, 두 화면이 각자 다시 구현하게 된다.
//
// **테이블이 정본이다.** 계수는 JudgeTable, 스탯은 UnitTable, 문구는 StringTable 에서 온다.
// 여기에는 숫자도 문장도 박아 넣지 않는다.
public static class HandbookText
{
    // 슬롯은 같은 규칙이 심볼 수만큼 행으로 쪼개져 있다(SlotMatch3_0 ~ _5).
    // 그대로 늘어놓으면 12줄이 되어 읽히지 않으므로 접두사로 묶는다.
    private const string SLOT_MATCH3_PREFIX = "SlotMatch3_";
    private const string SLOT_MATCH2_PREFIX = "SlotMatch2_";
    private const string SLOT_MATCH3_KEY = "SlotMatch3";
    private const string SLOT_MATCH2_KEY = "SlotMatch2";

    private const string DESC_SUFFIX = "Desc";
    private const string NAME_SUFFIX = "Name";
    private const string PATTERN_PREFIX = "Pattern";

    // 족보. "무엇이 걸리면 얼마인가"를 배수 큰 것부터 보여준다 —
    // 노리고 싶은 것이 위에 오는 편이 읽기 좋다.
    public static string BuildPattern(string _houseKey)
    {
        JudgeTable judgeTable = TableManager.instance.GetTable<JudgeTable>();
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        if (judgeTable == null || stringTable == null)
        {
            Logger.Error("[HandbookText] BuildPattern Failed! JudgeTable 또는 StringTable not found (기대: TableManager에 등록됨)");
            return string.Empty;
        }

        // 묶어야 하는 슬롯 규칙은 대표 키 하나로 접고 배수는 범위로 보여준다.
        List<string> listKey = new List<string>();
        Dictionary<string, float> dicMin = new Dictionary<string, float>();
        Dictionary<string, float> dicMax = new Dictionary<string, float>();

        for (int i = 0; i < judgeTable.list.Count; ++i)
        {
            JudgeRecord record = judgeTable.list[i];
            if (record == null || record.HouseKey != _houseKey)
                continue;

            string key = FoldPatternKey(record.PatternKey);

            if (dicMin.ContainsKey(key) == false)
            {
                listKey.Add(key);
                dicMin.Add(key, record.Coef);
                dicMax.Add(key, record.Coef);
                continue;
            }

            if (record.Coef < dicMin[key])
                dicMin[key] = record.Coef;

            if (record.Coef > dicMax[key])
                dicMax[key] = record.Coef;
        }

        if (listKey.Count <= 0)
            return string.Empty;

        listKey.Sort((left, right) => dicMax[right].CompareTo(dicMax[left]));

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < listKey.Count; ++i)
        {
            string key = listKey[i];

            if (builder.Length > 0)
                builder.Append(System.Environment.NewLine);

            AppendPatternLine(builder, stringTable, key, dicMin[key], dicMax[key]);
        }

        return builder.ToString();
    }

    // 유닛. 지금 **전장에 서 있는 것**만 보여준다 —
    // 종족 전체를 늘어놓으면 13줄이 되고, 정작 궁금한 것은 내가 가진 말이다.
    public static string BuildUnit(string _houseKey, RunRoster _roster)
    {
        if (_roster == null)
            return string.Empty;

        UnitTable unitTable = TableManager.instance.GetTable<UnitTable>();
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        if (unitTable == null || stringTable == null)
        {
            Logger.Error("[HandbookText] BuildUnit Failed! UnitTable 또는 StringTable not found (기대: TableManager에 등록됨)");
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int cell = 0; cell < RunRoster.FIELD_SIZE; ++cell)
        {
            RunUnit unit = _roster.GetFieldUnit(cell);
            if (unit == null)
                continue;

            UnitRecord record = (unit.isPatternUnit == true)
                ? unitTable.FindPatternUnit(_houseKey, unit.PatternKey)
                : unitTable.GetRecord(_houseKey, unit.SymbolType);

            if (record == null)
                continue;

            if (builder.Length > 0)
                builder.Append(System.Environment.NewLine);

            AppendUnitLine(builder, stringTable, unitTable, record, unit, _houseKey);
        }

        if (builder.Length <= 0)
            return string.Empty;

        return builder.ToString();
    }

    private static void AppendUnitLine(StringBuilder _builder, StringTable _stringTable, UnitTable _unitTable,
        UnitRecord _record, RunUnit _unit, string _houseKey)
    {
        _builder.Append(_stringTable.GetString(_record.NameKey));

        if (_unit.Grade > 1)
        {
            _builder.Append(" ★");
            _builder.Append(_unit.Grade);
        }

        _builder.Append("  ");
        _builder.Append(_stringTable.GetString("Role" + _record.Role));

        // 배율이 아니라 **실제로 싸울 때의 값**을 보여준다. 0.74 같은 배율은
        // 그 자체로는 세다 약하다를 말해주지 않는다.
        UnitBattleStat stat = _unitTable.GetBattleStat(
            _houseKey, _record.SymbolIndex, _unit.Grade, _record.PatternKey);

        _builder.Append(System.Environment.NewLine);
        _builder.Append("   ");
        _builder.Append(_stringTable.GetString("SummaryStatHp"));
        _builder.Append(' ');
        _builder.Append(stat.Hp);
        _builder.Append("  ");
        _builder.Append(_stringTable.GetString("SummaryStatAtk"));
        _builder.Append(' ');
        _builder.Append(stat.Atk.ToString("0.#"));
        _builder.Append("  ");
        _builder.Append(_stringTable.GetString("SummaryStatRange"));
        _builder.Append(' ');
        _builder.Append(stat.Range);

        if (string.IsNullOrEmpty(_record.TraitKey) == true)
            return;

        _builder.Append(System.Environment.NewLine);
        _builder.Append("   [");
        _builder.Append(_stringTable.GetString(_record.TraitKey + NAME_SUFFIX));
        _builder.Append("] ");
        _builder.Append(_stringTable.GetString(_record.TraitKey + DESC_SUFFIX));
    }

    // **Coef 가 언제나 배수인 것은 아니다.**
    //  · 윷 `YutGrade2Landing` 11 은 "이동값 합이 11 이상이면 2성"이라는 **임계값**이다
    //  · 화투 `HwatuScale` 0.2055 는 섯다 족보값에 곱하는 **스케일**이라, ×0.21 만 보면 약해 보인다
    //    (실제로는 땡 90 × 0.2055 ≈ 18.5)
    //
    // 그래서 **설명 문구가 형식을 정한다** — `{0}` 자리표시자가 있으면 그 값을 문장에 녹이고
    // 배수 라벨은 붙이지 않는다. 어떤 판정이 그런지를 코드가 목록으로 들고 있으면
    // 종족이 늘 때마다 그 목록을 고쳐야 하고, 빠뜨리면 조용히 틀린 숫자가 표시된다.
    // (2026-09-14 사용자 지적으로 발견 — 윷을 ×11 로 찍고 있었다)
    private static void AppendPatternLine(StringBuilder _builder, StringTable _stringTable,
        string _key, float _min, float _max)
    {
        string descKey = PATTERN_PREFIX + _key + DESC_SUFFIX;
        string desc = _stringTable.GetString(descKey);

        if (desc.Contains("{0}") == true)
        {
            _builder.Append(_stringTable.GetString(descKey, _max.ToString("0.####")));
            return;
        }

        _builder.Append(BuildCoefLabel(_min, _max));
        _builder.Append("  ");
        _builder.Append(desc);
    }

    // 슬롯의 심볼별 행을 대표 키로 접는다. 다른 종족은 그대로 돌려준다.
    private static string FoldPatternKey(string _patternKey)
    {
        if (string.IsNullOrEmpty(_patternKey) == true)
            return string.Empty;

        if (_patternKey.StartsWith(SLOT_MATCH3_PREFIX) == true)
            return SLOT_MATCH3_KEY;

        if (_patternKey.StartsWith(SLOT_MATCH2_PREFIX) == true)
            return SLOT_MATCH2_KEY;

        return _patternKey;
    }

    // 접힌 규칙은 배수가 여럿이라 범위로 적는다(슬롯 3매치는 심볼마다 다르다).
    private static string BuildCoefLabel(float _min, float _max)
    {
        if (_max - _min > 0.001f)
            return $"×{_min:0.##}~{_max:0.##}";

        return $"×{_max:0.##}";
    }
}
