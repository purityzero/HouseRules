using System;
using System.Collections.Generic;

// 프록시들을 타입으로 찾아 쓰는 목록. 매니저가 하나 들고 쓴다.
// 저장 매체는 여기서 한 번만 주입되고 각 프록시로 흘러간다 — 매체를 바꾸는 지점이 한 곳으로 모인다.
public class SaveDataRegistry
{
    private ISaveStorage m_Storage;
    private Dictionary<Type, ISaveDataProxy> m_DicProxy = new Dictionary<Type, ISaveDataProxy>();

    public ISaveStorage storage => m_Storage;

    public SaveDataRegistry(ISaveStorage _storage)
    {
        m_Storage = _storage;
    }

    public SaveDataProxy<T> Add<T>(string _key) where T : SaveData, new()
    {
        Type dataType = typeof(T);
        if (m_DicProxy.ContainsKey(dataType) == true)
        {
            Logger.Error($"[SaveDataRegistry] Add Failed! already added - {dataType}");
            return null;
        }

        SaveDataProxy<T> proxy = new SaveDataProxy<T>(_key, m_Storage);
        m_DicProxy.Add(dataType, proxy);
        return proxy;
    }

    public T Get<T>() where T : SaveData, new()
    {
        Type dataType = typeof(T);

        ISaveDataProxy proxy;
        if (m_DicProxy.TryGetValue(dataType, out proxy) == false)
        {
            Logger.Error($"[SaveDataRegistry] Get Failed! not added - {dataType}");
            return null;
        }

        SaveData saveData = proxy.GetSaveData();
        if (saveData is T == true)
        {
            var typedData = saveData as T;
            return typedData;
        }

        Logger.Error($"saveData is {dataType} convert failed!");
        return null;
    }

    public void LoadAll()
    {
        foreach (KeyValuePair<Type, ISaveDataProxy> pair in m_DicProxy)
        {
            pair.Value.Load();
        }
    }

    public void SaveAll()
    {
        foreach (KeyValuePair<Type, ISaveDataProxy> pair in m_DicProxy)
        {
            pair.Value.Save();
        }

        m_Storage.Flush();
    }

    // 매 프레임 호출. 바뀐 데이터만 저장하고, 실제로 쓴 게 있을 때만 Flush를 한 번 부른다 —
    // 한 프레임에 여러 값이 바뀌어도 매체 확정은 1회로 묶인다.
    public void UpdateLogic()
    {
        bool isSaved = false;
        foreach (KeyValuePair<Type, ISaveDataProxy> pair in m_DicProxy)
        {
            if (pair.Value.UpdateLogic() == true)
                isSaved = true;
        }

        if (isSaved == false)
            return;

        m_Storage.Flush();
    }

    public void Clear()
    {
        m_DicProxy.Clear();
    }
}
