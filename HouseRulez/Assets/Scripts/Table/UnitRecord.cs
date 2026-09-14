using System.Collections.Generic;
using UnityEngine;

// 심볼 하나의 성능. 종족 × 심볼 인덱스가 키다.
//
// 예전엔 유닛 스탯의 출처가 UnitGradeTable 하나였고 **성급만 봤다** —
// 체스의 king·queen·rook·pawn이 전부 같은 성능이고 그림만 달랐다(7종족 60여 종 전부).
// 그러면 명부에 유닛이 쌓여도 "무엇을 모을까"라는 선택이 생기지 않는다.
//
// 여기 값은 **배율**이다. 성급이 전체 크기를 정하고 심볼이 역할을 나눈다.
// 종족 안에서 HpRate·AtkRate·AtkSpeedRate·MoveRate의 평균이 1.0이어야 한다 —
// 판정이 "전력 1 = 1성 1기"로 유닛 수를 정하므로, 평균이 1을 넘으면 그 종족만 강해진다.
public class UnitRecord : Record
{
    public string HouseKey;
    public int SymbolIndex;
    public string NameKey;

    // Front / Mid / Back. GDD FieldLayout의 전열·중열·후열이며 조건부 Trait의 조건이 된다.
    // 열별 역할 자체는 아직 미구현이라 지금은 표시·기획 의도 기록용이다.
    public string Role;

    public float HpRate;
    public float AtkRate;
    public float AtkSpeedRate;

    // 사거리는 정수 칸이라 배율이 아니라 가산이다.
    public int RangeBonus;

    public float MoveRate;

    // 이 심볼의 고정 특성. 비어 있을 수 있다. 실제 효과는 아직 미구현이다.
    public string TraitKey;

    // 릴에 이 심볼이 나올 **상대 가중치**. 종족 안에서만 의미가 있고 1.0이 기준이다.
    //
    // 2026-09-14 도입. 그 전에는 릴이 `Random.Range(0, poolCount)` 로 균등하게 뽑았고,
    // 그래서 "심볼마다 얼마나 자주 나오는가"가 코드에 박혀 조정할 수 없었다
    // (CODE.MD 「튜닝값은 하드코딩 금지, CSV로」).
    //
    // **배율과 같은 행에 둔 이유** — "이 심볼은 이만큼 나오고 이만큼 강하다"가 한 줄에 모여야
    // 밸런싱할 때 양쪽을 함께 본다. 획득 분포와 배율이 따로 놀면 종족 평균이 조용히 어긋난다
    // (실제로 화투에서 그랬다: 강한 패가 0.62배로 덜 나와 가중 평균이 -5.2%였다).
    public float SpawnWeight;
}

// 성급 × 심볼을 합친 최종 전투 스탯.
//
// 익명 튜플로 넘기지 않는다(CODE.MD 「튜플 대신 struct」) — 호출부가 늘어날수록
// "몇 번째 값이 뭐였는지"를 되짚어야 하기 때문이다.
public struct UnitBattleStat
{
    public int Hp;
    public float Atk;
    public float AtkSpeed;
    public int Range;
    public float MoveSpeed;
}

public class UnitTable : Table<UnitRecord>
{
    // 사거리 상한. 아군 출발선 x=0, 적 등장선 x=900에 한 칸이 108px이므로
    // 4칸(432px)이면 전장 절반을 덮는다. 그 위는 전투가 아니라 일방 포격이 된다.
    // RangeBonus 위에 각인 「사거리」와 조건부 Trait이 겹쳐 쌓이므로 상한이 필요하다
    // (상한이 없으면 장기 포가 1+2+1+1 = 5칸에 도달한다).
    public const int RANGE_MAX = 4;

    // 공격력의 하한. **0이 되면 전투가 영원히 끝나지 않는다** —
    // BattleUnit.TakeDamage(0)은 HP를 깎지 않고, 전투에 시간 상한이 없어 화면이 그대로 멈춘다.
    // Hp는 BattleUnit.Setup이 Mathf.Max(1, _hp)로, AtkSpeed는 Mathf.Max(0.1f)로 이미 막고 있는데
    // Atk만 무방비였다.
    public const float ATK_MIN = 1f;

    public UnitTable(List<UnitRecord> _listRecord) : base(_listRecord) { }

    // 종족별 누적 가중치 캐시. 릴 생성이 스핀마다 9번 부르므로 매번 테이블을 훑으면
    // 그만큼 선형 탐색이 반복된다. 테이블은 런타임에 바뀌지 않아 무효화가 필요 없다.
    private Dictionary<string, float[]> m_HashCumulativeWeight = new Dictionary<string, float[]>();

    // 가중치에 따라 심볼 하나를 고른다. **가중치가 전부 같으면 균등 무작위와 수학적으로 동일하다** —
    // 그래서 전 종족을 1.0으로 두면 도입 전과 결과가 달라지지 않는다.
    //
    // 테이블에 행이 없거나 합이 0이면 균등으로 떨어진다. 릴은 매 스핀 도는 경로라
    // 여기서 멈추거나 오류를 쏟으면 게임이 못 돈다.
    public int PickWeightedSymbol(string _houseKey, int _symbolCount)
    {
        if (_symbolCount <= 0)
            return 0;

        float[] cumulative = GetCumulativeWeight(_houseKey, _symbolCount);
        if (cumulative == null)
            return Random.Range(0, _symbolCount);

        float total = cumulative[cumulative.Length - 1];
        if (total <= 0f)
            return Random.Range(0, _symbolCount);

        float pick = Random.Range(0f, total);
        for (int i = 0; i < cumulative.Length; ++i)
        {
            if (pick < cumulative[i])
                return i;
        }

        // 부동소수점 끝자락. 마지막 심볼로 떨어뜨린다.
        return cumulative.Length - 1;
    }

    private float[] GetCumulativeWeight(string _houseKey, int _symbolCount)
    {
        float[] cached = null;
        if (m_HashCumulativeWeight.TryGetValue(_houseKey, out cached) == true)
        {
            if (cached != null && cached.Length >= _symbolCount)
                return cached;
        }

        float[] cumulative = new float[_symbolCount];
        float sum = 0f;

        for (int i = 0; i < _symbolCount; ++i)
        {
            // GetRecord 를 쓰지 않는다 — 그쪽은 못 찾으면 오류를 남기는데,
            // 스프라이트 풀이 테이블보다 클 수 있고 여기는 매 스핀 도는 경로다.
            UnitRecord record = FindRecordSilent(_houseKey, i);

            float weight = (record != null) ? record.SpawnWeight : 1f;
            if (weight < 0f)
                weight = 0f;

            sum += weight;
            cumulative[i] = sum;
        }

        m_HashCumulativeWeight[_houseKey] = cumulative;
        return cumulative;
    }

    private UnitRecord FindRecordSilent(string _houseKey, int _symbolIndex)
    {
        return list.Find(unit => unit != null
            && unit.HouseKey == _houseKey
            && unit.SymbolIndex >= _symbolIndex && unit.SymbolIndex <= _symbolIndex);
    }

    public UnitRecord GetRecord(string _houseKey, int _symbolIndex)
    {
        UnitRecord record = list.Find(unit => unit != null
            && unit.HouseKey == _houseKey
            && unit.SymbolIndex >= _symbolIndex && unit.SymbolIndex <= _symbolIndex);

        if (record == null)
            Logger.Error($"[UnitTable] GetRecord Failed! 심볼 없음 - {_houseKey}/{_symbolIndex} (기대: UnitTable.csv에 해당 행 존재)");

        return record;
    }

    // 성급과 심볼을 합친 최종 스탯. **전투 생성·보관함 툴팁·전장 표시가 전부 이 하나를 부른다.**
    // 계산을 전투 화면 안에 두면 나머지 소비자가 각자 다시 구현하게 되고,
    // 그게 CLAUDE.md가 말하는 "같은 상태를 두 시스템이 각자 든다"의 전형이다.
    //
    // 배치가 정해진 뒤에만 알 수 있는 조건부 Trait(전열·중열·후열, 전장 구성)은 여기 넣지 않는다.
    // 그건 배치 후에 한 번 더 통과시켜야 한다(아직 미구현).
    public UnitBattleStat GetBattleStat(string _houseKey, int _symbolIndex, int _grade)
    {
        UnitBattleStat stat = new UnitBattleStat();

        UnitGradeTable gradeTable = TableManager.instance.GetTable<UnitGradeTable>();
        if (gradeTable == null)
        {
            Logger.Error("[UnitTable] GetBattleStat Failed! UnitGradeTable not found (기대: TableManager에 등록됨)");
            return stat;
        }

        UnitGradeRecord grade = gradeTable.GetRecord(_grade);
        if (grade == null)
            return stat;

        UnitRecord unit = GetRecord(_houseKey, _symbolIndex);

        // 심볼 행이 없으면 배율 없이 성급 값만 쓴다 — 유닛이 아예 안 서는 것보다 낫다.
        // GetRecord가 이미 에러를 남겼으므로 여기서 또 남기지 않는다.
        if (unit == null)
        {
            stat.Hp = Mathf.Max(1, grade.Hp);
            stat.Atk = Mathf.Max(ATK_MIN, grade.Atk);
            stat.AtkSpeed = grade.AtkSpeed;
            stat.Range = Mathf.Clamp(grade.Range, 1, RANGE_MAX);
            stat.MoveSpeed = grade.MoveSpeed;
            return stat;
        }

        stat.Hp = Mathf.Max(1, Mathf.RoundToInt(grade.Hp * unit.HpRate));
        stat.Atk = Mathf.Max(ATK_MIN, grade.Atk * unit.AtkRate);
        stat.AtkSpeed = grade.AtkSpeed * unit.AtkSpeedRate;
        stat.Range = Mathf.Clamp(grade.Range + unit.RangeBonus, 1, RANGE_MAX);
        stat.MoveSpeed = grade.MoveSpeed * unit.MoveRate;

        return stat;
    }
}
