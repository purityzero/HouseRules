using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 진행도. GDD 06장 "메타 진행은 종족 해금 하나로 시작한다 / 영구 강화 트리는 초기 빌드에 넣지 않는다"가
// 이 클래스가 들 수 있는 필드의 상한선이다 — 코어·각인·재화는 런(run) 안에서만 사는 값이라 여기 들어오지 않는다.
[Serializable]
public class PlayerData : SaveData
{
    [SerializeField] private string m_SelectedHouseKey = string.Empty;

    // 테이블의 기본 개방분은 여기 담지 않는다. 플레이 중 새로 딴 것만 쌓인다.
    [SerializeField] private List<string> m_ListUnlockedHouseKey = new List<string>();

    [SerializeField] private string m_LastPlayedAt = string.Empty;

    public string selectedHouseKey => m_SelectedHouseKey;
    public List<string> listUnlockedHouseKey => m_ListUnlockedHouseKey;
    public string lastPlayedAt => m_LastPlayedAt;

    public override void Init()
    {
        m_SelectedHouseKey = string.Empty;
        m_ListUnlockedHouseKey.Clear();
        m_LastPlayedAt = string.Empty;
    }

    public void SetSelectedHouseKey(string _houseKey)
    {
        if (m_SelectedHouseKey == _houseKey)
            return;

        m_SelectedHouseKey = _houseKey;
        SetChanged();
    }

    public void AddUnlockedHouseKey(string _houseKey)
    {
        if (string.IsNullOrEmpty(_houseKey) == true)
            return;

        if (m_ListUnlockedHouseKey.Contains(_houseKey) == true)
            return;

        m_ListUnlockedHouseKey.Add(_houseKey);
        SetChanged();
    }

    public bool IsUnlockedHouseKey(string _houseKey)
    {
        return m_ListUnlockedHouseKey.Contains(_houseKey);
    }

    public void SetLastPlayedAt(string _lastPlayedAt)
    {
        if (m_LastPlayedAt == _lastPlayedAt)
            return;

        m_LastPlayedAt = _lastPlayedAt;
        SetChanged();
    }
}

// 설정값. 진행도와 저장 키를 나눠 둔다 — 진행도를 초기화해도 옵션은 남아야 하고, 그 반대도 마찬가지다.
[Serializable]
public class OptionData : SaveData
{
    [SerializeField] private float m_BgmVolume = 1f;
    [SerializeField] private float m_SfxVolume = 1f;
    [SerializeField] private eFpsOption m_FpsOption = eFpsOption.Fps60;
    [SerializeField] private eLanguage m_Language = eLanguage.Korean;

    public float bgmVolume => m_BgmVolume;
    public float sfxVolume => m_SfxVolume;
    public eFpsOption fpsOption => m_FpsOption;
    public eLanguage language => m_Language;

    public override void Init()
    {
        m_BgmVolume = 1f;
        m_SfxVolume = 1f;
        m_FpsOption = eFpsOption.Fps60;
        m_Language = eLanguage.Korean;
    }

    public void SetBgmVolume(float _volume)
    {
        if (Mathf.Approximately(m_BgmVolume, _volume) == true)
            return;

        m_BgmVolume = _volume;
        SetChanged();
    }

    public void SetSfxVolume(float _volume)
    {
        if (Mathf.Approximately(m_SfxVolume, _volume) == true)
            return;

        m_SfxVolume = _volume;
        SetChanged();
    }

    public void SetFpsOption(eFpsOption _fpsOption)
    {
        if (m_FpsOption == _fpsOption)
            return;

        m_FpsOption = _fpsOption;
        SetChanged();
    }

    public void SetLanguage(eLanguage _language)
    {
        if (m_Language == _language)
            return;

        m_Language = _language;
        SetChanged();
    }
}
