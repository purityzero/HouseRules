using System.Collections.Generic;
using UnityEngine;

// 런 동안 유지되는 유닛 명부. 보관함(bench)과 전장(field) 두 곳을 한 주체가 소유한다.
//
// **왜 한 클래스가 둘 다 갖는가** — 승급은 보관함과 전장을 통틀어 센다. 소유자를 둘로 나누면
// "보관함에 2기, 전장에 1기"인 상태에서 어느 쪽도 3기를 못 세어 승급이 조용히 안 일어난다.
// 같은 상태를 두 시스템이 각자 들면 부정 경로에서만 어긋나는 그 함정이다.
//
// 기존 판정 소환(SummonSlot)과의 차이는 수명이다. 판정 결과는 스핀 1회짜리였지만
// 여기 담긴 유닛은 웨이브를 넘어 살아남고, 전투에서 죽어도 다음 웨이브에 다시 선다.
// 그래서 이 명부가 곧 런의 성장 축이다 — 연차가 오를수록 적이 강해지는 곡선을
// 따라갈 수단이 지금까지 없었다.
public class RunRoster
{
    // 전장 칸 수. 릴 3×3과 같은 9칸이라 JudgeResult.GRID_SIZE와 항상 같아야 한다.
    public const int FIELD_SIZE = JudgeResult.GRID_SIZE;

    // 승급에 필요한 같은 유닛 수(GDD UnitGrowth "동일 유닛 3기 흡수").
    public const int MERGE_COUNT = 3;

    public const int CELL_NONE = -1;

    private RunUnit[] m_ArrayField = new RunUnit[FIELD_SIZE];
    private List<RunUnit> m_ListBench = new List<RunUnit>();

    // UnitGradeTable이 정하는 상한. 여기 도달한 유닛은 더 흡수하지 않는다.
    private int m_MaxGrade = 1;

    public IReadOnlyList<RunUnit> listBench => m_ListBench;
    public int benchCount => m_ListBench.Count;
    public int maxGrade => m_MaxGrade;

    public void Init()
    {
        UnitGradeTable gradeTable = TableManager.instance.GetTable<UnitGradeTable>();
        if (gradeTable == null)
        {
            Logger.Error("[RunRoster] Init Failed! UnitGradeTable not found (기대: TableManager에 등록됨)");
            m_MaxGrade = 1;
        }
        else
        {
            m_MaxGrade = gradeTable.maxGrade;
        }

        for (int cell = 0; cell < FIELD_SIZE; ++cell)
        {
            m_ArrayField[cell] = null;
        }

        m_ListBench.Clear();
    }

    public RunUnit GetFieldUnit(int _cell)
    {
        if (IsCellValid(_cell) == false)
            return null;

        return m_ArrayField[_cell];
    }

    public RunUnit GetBenchUnit(int _index)
    {
        if (_index < 0 || _index >= m_ListBench.Count)
            return null;

        return m_ListBench[_index];
    }

    // 전장에 실제로 선 유닛 수. 전투에 나가는 머릿수다.
    public int fieldCount
    {
        get
        {
            int count = 0;
            for (int cell = 0; cell < FIELD_SIZE; ++cell)
            {
                if (m_ArrayField[cell] != null)
                    count += 1;
            }

            return count;
        }
    }

    public bool IsCellValid(int _cell)
    {
        return _cell >= 0 && _cell < FIELD_SIZE;
    }

    public bool IsFieldFull()
    {
        return fieldCount >= FIELD_SIZE;
    }

    // 판정으로 얻은 유닛을 보관함에 넣는다. 승급은 흡수로만 일어나므로 기본은 1성이다.
    // 넣은 직후 승급을 검사하므로 호출부가 따로 MergeAll()을 부를 필요가 없다.
    public RunUnit AddUnit(int _symbolType)
    {
        return AddUnit(_symbolType, 1);
    }

    // 성급을 지정해 넣는다. **윷 전용 통로다** — 윷은 업기(같은 칸 겹침)와 나기(완주)가
    // 등급을 직접 만드는 종족 규칙이라, 전력이 남아 올리는 자동 전환과 달리 그 등급이 판정 결과 자체다.
    // 다른 종족이 이 오버로드로 2성 이상을 넣기 시작하면 "승급은 3합으로만"이라는 규칙이 무너진다.
    public RunUnit AddUnit(int _symbolType, int _grade)
    {
        int grade = Mathf.Clamp(_grade, 1, m_MaxGrade);

        RunUnit unit = new RunUnit(_symbolType, grade);
        m_ListBench.Add(unit);

        MergeAll();

        return unit;
    }

    // 가능한 승급을 더 이상 일어나지 않을 때까지 수행하고 횟수를 돌려준다.
    //
    // 반복하는 이유는 연쇄 때문이다 — 1성 3기가 2성 1기가 되면서 기존 2성 2기와 합쳐져
    // 곧바로 3성이 되는 경우가 있다. 한 번만 돌리면 그 두 번째 승급이 다음 스핀까지 미뤄진다.
    public int MergeAll()
    {
        int mergeCount = 0;

        while (MergeOnce() == true)
        {
            mergeCount += 1;
        }

        return mergeCount;
    }

    // 승급 1회. 재료를 찾으면 수행하고 true.
    //
    // 재료 중 전장에 있던 것이 하나라도 있으면 승급체가 그 칸을 잇는다 —
    // 안 그러면 배치해둔 유닛이 승급할 때마다 보관함으로 빠져 플레이어가 매번 다시 놓아야 한다.
    private bool MergeOnce()
    {
        List<RunUnit> listMaterial = new List<RunUnit>();

        for (int cell = 0; cell < FIELD_SIZE; ++cell)
        {
            RunUnit unit = m_ArrayField[cell];
            if (unit == null)
                continue;

            if (IsMergeable(unit) == false)
                continue;

            CollectSameKind(unit, listMaterial);
            if (listMaterial.Count < MERGE_COUNT)
                continue;

            Promote(listMaterial, cell);
            return true;
        }

        for (int i = 0; i < m_ListBench.Count; ++i)
        {
            RunUnit unit = m_ListBench[i];
            if (IsMergeable(unit) == false)
                continue;

            CollectSameKind(unit, listMaterial);
            if (listMaterial.Count < MERGE_COUNT)
                continue;

            Promote(listMaterial, CELL_NONE);
            return true;
        }

        return false;
    }

    // 승급체를 놓을 자리를 정하고 재료를 걷어낸다.
    // _keepCell이 유효하면 그 칸의 유닛이 살아남고, 아니면 보관함의 첫 재료가 살아남는다.
    private void Promote(List<RunUnit> _listMaterial, int _keepCell)
    {
        RunUnit survivor = null;

        if (IsCellValid(_keepCell) == true)
            survivor = m_ArrayField[_keepCell];

        if (survivor == null)
            survivor = _listMaterial[0];

        int removed = 0;
        for (int i = 0; i < _listMaterial.Count; ++i)
        {
            if (removed >= MERGE_COUNT - 1)
                break;

            RunUnit material = _listMaterial[i];
            if (ReferenceEquals(material, survivor) == true)
                continue;

            RemoveUnit(material);
            removed += 1;
        }

        survivor.Grade += 1;
    }

    // 같은 종류·같은 성급을 전장 먼저, 보관함 나중 순으로 모은다.
    // 전장을 먼저 넣어야 승급체가 전장 자리를 잇는 규칙이 자연스럽게 성립한다.
    private void CollectSameKind(RunUnit _sample, List<RunUnit> _listResult)
    {
        _listResult.Clear();

        for (int cell = 0; cell < FIELD_SIZE; ++cell)
        {
            RunUnit unit = m_ArrayField[cell];
            if (unit == null)
                continue;

            if (IsSameKind(unit, _sample) == false)
                continue;

            _listResult.Add(unit);
        }

        for (int i = 0; i < m_ListBench.Count; ++i)
        {
            if (IsSameKind(m_ListBench[i], _sample) == false)
                continue;

            _listResult.Add(m_ListBench[i]);
        }
    }

    private bool IsSameKind(RunUnit _left, RunUnit _right)
    {
        if (_left == null || _right == null)
            return false;

        if (_left.SymbolType < _right.SymbolType || _left.SymbolType > _right.SymbolType)
            return false;

        return _left.Grade >= _right.Grade && _left.Grade <= _right.Grade;
    }

    private bool IsMergeable(RunUnit _unit)
    {
        if (_unit == null)
            return false;

        return _unit.Grade < m_MaxGrade;
    }

    private void RemoveUnit(RunUnit _unit)
    {
        for (int cell = 0; cell < FIELD_SIZE; ++cell)
        {
            if (ReferenceEquals(m_ArrayField[cell], _unit) == false)
                continue;

            m_ArrayField[cell] = null;
            return;
        }

        m_ListBench.Remove(_unit);
    }

    // 보관함 유닛을 전장 칸으로 옮긴다. 칸이 차 있으면 자리를 맞바꾼다 —
    // 드래그로 점유된 칸에 떨어뜨렸을 때 조용히 실패하면 조작이 먹히지 않은 것처럼 보인다.
    public bool MoveBenchToField(int _benchIndex, int _cell)
    {
        if (IsCellValid(_cell) == false)
            return false;

        RunUnit unit = GetBenchUnit(_benchIndex);
        if (unit == null)
            return false;

        RunUnit occupant = m_ArrayField[_cell];

        m_ListBench.RemoveAt(_benchIndex);
        m_ArrayField[_cell] = unit;

        if (occupant != null)
            m_ListBench.Insert(_benchIndex, occupant);

        MergeAll();
        return true;
    }

    public bool MoveFieldToBench(int _cell)
    {
        if (IsCellValid(_cell) == false)
            return false;

        RunUnit unit = m_ArrayField[_cell];
        if (unit == null)
            return false;

        m_ArrayField[_cell] = null;
        m_ListBench.Add(unit);

        return true;
    }

    // 전장 두 칸을 맞바꾼다. 한쪽이 비어 있어도 성립한다(빈 칸으로 옮기는 것과 같다).
    public bool SwapField(int _cellFrom, int _cellTo)
    {
        if (IsCellValid(_cellFrom) == false || IsCellValid(_cellTo) == false)
            return false;

        if (_cellFrom >= _cellTo && _cellFrom <= _cellTo)
            return false;

        RunUnit from = m_ArrayField[_cellFrom];
        RunUnit to = m_ArrayField[_cellTo];

        if (from == null && to == null)
            return false;

        m_ArrayField[_cellFrom] = to;
        m_ArrayField[_cellTo] = from;

        return true;
    }

    // 전장에 선 유닛의 전력 합. 적 전력과 같은 눈금(1성 = 1.0)이라 밸런스 비교에 그대로 쓴다.
    public float GetFieldPower()
    {
        UnitGradeTable gradeTable = TableManager.instance.GetTable<UnitGradeTable>();
        if (gradeTable == null)
        {
            Logger.Error("[RunRoster] GetFieldPower Failed! UnitGradeTable not found (기대: TableManager에 등록됨)");
            return 0f;
        }

        float power = 0f;
        for (int cell = 0; cell < FIELD_SIZE; ++cell)
        {
            if (m_ArrayField[cell] == null)
                continue;

            power += gradeTable.GetMultiplier(m_ArrayField[cell].Grade);
        }

        return power;
    }

    // 전장을 성급 높은 순으로 자동 배치한다. 플레이어가 배치를 안 해도 전투가 성립하게 하는 안전망이다.
    // 반환값은 자리가 실제로 바뀐 칸 수(연출 판단용).
    //
    // **왜 성급 순인가** — 예전엔 빈 칸을 보관함 앞쪽부터 채웠다. 그러면 3성이 보관함에 남고
    // 1성이 전장에 서는 일이 생긴다. 전장이 9칸뿐이라 그 손해가 그대로 전투력이 된다.
    // 시뮬레이션에서 같은 보유량인데 배치만 바꿔도 6연차 시점 전장 전력이
    // 심볼 13종 기준 25 → 49로 거의 두 배였다(`scratchpad/merge_rate.py`, 심볼 균등 가정).
    // 실측 Poker 6-3의 전장 전력 30.6도 최선 배치보다 그 낮은 쪽에 가까웠다.
    //
    // ★ **드래그 배치가 붙으면 이 함수를 자동으로 부르지 않는다.**
    //   전장 전체를 다시 세우므로 플레이어가 직접 놓은 배치를 덮어쓴다. 지금은 배치 수단이
    //   아예 없어서 순이득이지만, 드래그가 생기면 "빈 칸만 채우는" 쪽으로 좁혀야 한다.
    //   (제거 조건: `UIInGameFieldSlot`에 드래그 핸들러가 붙는 시점)
    public int ArrangeFieldByGrade()
    {
        List<RunUnit> listAll = new List<RunUnit>();

        for (int cell = 0; cell < FIELD_SIZE; ++cell)
        {
            if (m_ArrayField[cell] != null)
                listAll.Add(m_ArrayField[cell]);
        }

        for (int i = 0; i < m_ListBench.Count; ++i)
        {
            listAll.Add(m_ListBench[i]);
        }

        // 성급이 높은 것부터 담는다. List.Sort는 불안정 정렬이라 같은 성급의 순서가 매번
        // 뒤바뀔 수 있고, 그러면 스핀마다 화면의 유닛이 이유 없이 자리를 옮긴다.
        // 성급별로 훑어 담으면 같은 성급 안에서 원래 순서가 유지된다.
        List<RunUnit> listSorted = new List<RunUnit>();
        for (int grade = m_MaxGrade; grade >= 1; --grade)
        {
            for (int i = 0; i < listAll.Count; ++i)
            {
                if (listAll[i].Grade < grade || listAll[i].Grade > grade)
                    continue;

                listSorted.Add(listAll[i]);
            }
        }

        int changed = 0;
        m_ListBench.Clear();

        for (int i = 0; i < listSorted.Count; ++i)
        {
            if (i >= FIELD_SIZE)
            {
                m_ListBench.Add(listSorted[i]);
                continue;
            }

            if (ReferenceEquals(m_ArrayField[i], listSorted[i]) == false)
                changed += 1;

            m_ArrayField[i] = listSorted[i];
        }

        // 유닛이 9칸보다 적으면 뒤쪽 칸을 비운다. 안 비우면 옮겨간 유닛이 원래 칸에도 남는다.
        for (int cell = listSorted.Count; cell < FIELD_SIZE; ++cell)
        {
            if (m_ArrayField[cell] != null)
                changed += 1;

            m_ArrayField[cell] = null;
        }

        return changed;
    }
}
