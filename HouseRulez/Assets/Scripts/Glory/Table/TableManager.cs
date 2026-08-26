using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Threading.Tasks;

public class TableManager : MonoSingleton<TableManager>
{
    public Dictionary<System.Type, ITable> m_TableDictionary = new Dictionary<System.Type, ITable>();

    private FlowCommand m_FlowCommand = new FlowCommand();

    // 핫 리로드 시 m_TableDictionary(Dictionary)는 리셋되지만 이 bool은 값이 보존되어
    // "초기화된 것처럼 보이는데 테이블은 비어있는" 상태가 됨 — SpawnManager/MonsterManager와 동일 이유로 보존 대상에서 제외
    [System.NonSerialized] private bool m_isInitialized;

    public void init()
    {
        // 중복 호출 방지 — GameManager(MonoSingleton) 중복 인스턴스의 Awake()도 무조건 이 메서드를 부르므로
        // 씬을 오가며 GameManager가 다시 생성/파괴될 때마다 재실행되면 m_TableDictionary.Add에서 예외가 난다
        if (m_isInitialized == true)
            return;

        m_isInitialized = true;

        // UITable/SoundTable/ToggleListTable/ToggleMenuTable은 Glory의 UIManager.Get<T>()/BaseScene.PlaySfx()/
        // ToggleButtonList가 직접 참조하는 최소 인프라 테이블이라 기본 등록해둔다(Assets/Scripts/Table/*.cs).
        // 그 외 게임 고유 데이터 테이블은 Glory 프로젝트 비의존 원칙(.claude/rules/glory.md)에 따라 여기
        // 하드코딩하지 않는다 — 프로젝트에서 CSV 테이블을 추가할 때마다 아래 패턴으로 이어서 등록한다:
        //   List<{Record}> {name}Records = LoadCsvTable<{Record}>("Table/{TableName}");
        //   {Table} {name}Table = new {Table}({name}Records);
        //   m_TableDictionary.Add(typeof({Table}), {name}Table);

        List<UIRecord> uiRecords = LoadCsvTable<UIRecord>("Table/UITable");
        List<SoundRecord> soundRecords = LoadCsvTable<SoundRecord>("Table/SoundTable");
        List<ToggleListRecord> toggleListRecords = LoadCsvTable<ToggleListRecord>("Table/ToggleListTable");
        List<ToggleMenuRecord> toggleMenuRecords = LoadCsvTable<ToggleMenuRecord>("Table/ToggleMenuTable");
        List<StringRecord> stringRecords = LoadCsvTable<StringRecord>("Table/StringTable");
        List<HouseRecord> houseRecords = LoadCsvTable<HouseRecord>("Table/HouseTable");
        List<SlotLineRecord> slotLineRecords = LoadCsvTable<SlotLineRecord>("Table/SlotLineTable");

        UITable uiTable = new UITable(uiRecords);
        SoundTable soundTable = new SoundTable(soundRecords);
        ToggleListTable toggleListTable = new ToggleListTable(toggleListRecords);
        ToggleMenuTable toggleMenuTable = new ToggleMenuTable(toggleMenuRecords);
        StringTable stringTable = new StringTable(stringRecords);
        HouseTable houseTable = new HouseTable(houseRecords);
        SlotLineTable slotLineTable = new SlotLineTable(slotLineRecords);

        m_TableDictionary.Add(typeof(UITable), uiTable);
        m_TableDictionary.Add(typeof(SoundTable), soundTable);
        m_TableDictionary.Add(typeof(ToggleListTable), toggleListTable);
        m_TableDictionary.Add(typeof(ToggleMenuTable), toggleMenuTable);
        m_TableDictionary.Add(typeof(StringTable), stringTable);
        m_TableDictionary.Add(typeof(HouseTable), houseTable);
        m_TableDictionary.Add(typeof(SlotLineTable), slotLineTable);
    }

    public async Task<List<T>> LoadCsvTableToAddressable<T>(string key) where T : new()
    {

        m_FlowCommand.Add(new Command_CheckAsset(key, (exists)=> {
            if( exists == true )
            {

            }
            else
            {

            }
        }));


        return null;
    }

    public List<T> LoadCsvTable<T>(string resourcePath) where T : new()
    {
        List<T> result = new List<T>();

        try
        {
            // Resources 폴더에서 CSV 파일 읽기
            TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);
            if (csvFile == null)
            {
                Debug.LogWarning($"CSV 파일을 찾을 수 없습니다: {resourcePath}");
                return result;
            }

            // 파일 내용 읽기
            string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // 첫 번째 줄은 헤더
            string[] headers = lines[0].Split(',');

            // 데이터 라인
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');

                // 반사(reflection)를 사용하여 클래스 객체 생성
                T obj = new T();
                for (int j = 0; j < headers.Length; j++)
                {
                    string propertyName = headers[j];
                    string value = values[j];

                    // 속성 찾기 (대소문자 구분 무시)
                    var field = typeof(T).GetField(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (field != null)
                    {
                        object convertedValue;

                        // Enum 처리
                        if (field.FieldType.IsEnum)
                        {
                            convertedValue = Enum.Parse(field.FieldType, value, ignoreCase: true); // Enum 값 변환
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            // Float 변환
                            convertedValue = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(value, field.FieldType);
                        }

                        field.SetValue(obj, convertedValue);
                    }
                    else
                    {
                        Logger.Error($"Property or Field '{propertyName}' not found in type {typeof(T).Name}");
                    }
                }
                result.Add(obj);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CSV 파일 처리 중 오류 발생: {ex.Message}");
        }

        return result;
    }

    public T GetTable<T>() where T : class, ITable
    {
        if (m_isInitialized == false)
            init();

        System.Type _type = typeof(T);

        ITable _find = null;
        if(m_TableDictionary.TryGetValue(_type, out _find) == false )
        {
            Logger.Error($"{this.ToString()} ::GetTable() { _type.ToString()}");
            return null;
        }

        return _find as T;
    }

    private void Update()
    {
        m_FlowCommand.Update();
    }

}
