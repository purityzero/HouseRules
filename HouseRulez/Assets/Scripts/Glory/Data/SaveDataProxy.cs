using System;
using UnityEngine;

public interface ISaveDataProxy
{
    void Load();
    void Save();

    // 더티 플래그가 서 있으면 저장한다. 실제로 저장했으면 true — 레지스트리가 Flush를 한 번만 부르도록.
    bool UpdateLogic();

    SaveData GetSaveData();
    Type GetDataType();
}

// 데이터 한 덩어리의 대리인. 호출부는 데이터를 직접 들고 있지 않고 이 프록시를 거치며,
// 프록시가 저장 매체·직렬화·저장 시점을 대신 책임진다.
public class SaveDataProxy<T> : ISaveDataProxy where T : SaveData, new()
{
    private string m_Key;
    private ISaveStorage m_Storage;
    private T m_Data;

    public T data => m_Data;

    public SaveDataProxy(string _key, ISaveStorage _storage)
    {
        m_Key = _key;
        m_Storage = _storage;

        m_Data = new T();
        m_Data.Init();
    }

    public SaveData GetSaveData()
    {
        return m_Data;
    }

    public Type GetDataType()
    {
        return typeof(T);
    }

    public void Load()
    {
        if (m_Storage.Has(m_Key) == false)
            return;

        string json = m_Storage.Load(m_Key);
        if (string.IsNullOrEmpty(json) == true)
            return;

        // FromJson이 아니라 FromJsonOverwrite — 인스턴스를 새로 만들면 SaveData.OnChanged 구독자가 전부 끊긴다.
        // 로드가 씬 진입보다 늦어도 이미 구독한 UI가 그대로 살아있게 하려면 인스턴스 동일성을 유지해야 한다.
        try
        {
            JsonUtility.FromJsonOverwrite(json, m_Data);
        }
        catch (Exception exception)
        {
            // 저장본이 깨졌을 때 — 기본값으로 되돌리고 계속 굴린다. 여기서 멈추면 앱이 아예 못 뜬다.
            Logger.Error($"[SaveDataProxy] Load Failed! {m_Key} / {exception.Message}");
            m_Data.Init();
        }

        m_Data.SetNeedSave(false);
    }

    public void Save()
    {
        m_Storage.Save(m_Key, JsonUtility.ToJson(m_Data));
        m_Data.SetNeedSave(false);
    }

    public bool UpdateLogic()
    {
        if (m_Data.isNeedSave == false)
            return false;

        Save();
        return true;
    }
}
