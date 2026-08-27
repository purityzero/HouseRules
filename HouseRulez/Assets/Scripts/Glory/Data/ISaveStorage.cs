using UnityEngine;

// 저장 매체 교체 지점. 이 인터페이스가 프록시 구조를 쓰는 실질적인 이유다 —
// 로컬 저장에서 서버 저장으로 갈아탈 때 구현체 하나만 갈면 SaveDataProxy 위쪽(데이터 클래스, 호출부)은 그대로 둔다.
public interface ISaveStorage
{
    bool Has(string _key);
    string Load(string _key);
    void Save(string _key, string _json);
    void Delete(string _key);

    // 매체가 쓰기를 모아뒀다가 한 번에 내보내는 경우의 확정 시점(PlayerPrefs.Save 등).
    void Flush();
}

public class PlayerPrefsSaveStorage : ISaveStorage
{
    public bool Has(string _key)
    {
        return PlayerPrefs.HasKey(_key);
    }

    public string Load(string _key)
    {
        return PlayerPrefs.GetString(_key, string.Empty);
    }

    public void Save(string _key, string _json)
    {
        PlayerPrefs.SetString(_key, _json);
    }

    public void Delete(string _key)
    {
        PlayerPrefs.DeleteKey(_key);
    }

    public void Flush()
    {
        PlayerPrefs.Save();
    }
}
