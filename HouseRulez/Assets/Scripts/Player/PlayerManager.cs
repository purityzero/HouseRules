using System;
using UnityEngine;

// 저장 데이터의 유일한 창구. 밖에서는 PlayerData/OptionData를 직접 고치지 않고 전부 이 매니저를 거친다.
// 실제 저장/직렬화/매체는 Glory의 SaveDataRegistry -> SaveDataProxy -> ISaveStorage 가 대신 맡는다.
public class PlayerManager : MonoSingleton<PlayerManager>
{
    private const string PLAYER_SAVE_KEY = "PlayerData";
    private const string OPTION_SAVE_KEY = "OptionData";

    private SaveDataRegistry m_Registry;
    private bool m_isLoaded;

    // 프록시가 FromJsonOverwrite로 같은 인스턴스에 덮어쓰므로, 로드 후에도 이 참조는 그대로 유효하다.
    private PlayerData m_PlayerData;
    private OptionData m_OptionData;

    public PlayerData playerData => m_PlayerData;
    public OptionData optionData => m_OptionData;

    public string selectedHouseKey => m_PlayerData.selectedHouseKey;

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    // 멱등 가드 — Awake와 씬 진입점(TitleScene.OnSetup) 양쪽에서 불린다.
    // 가드가 없으면 두 번째 호출이 레지스트리를 새로 만들어 OnChanged 구독과 아직 저장 안 된 변경을 통째로 날린다.
    public void Load()
    {
        if (m_isLoaded == true)
            return;

        m_isLoaded = true;

        m_Registry = new SaveDataRegistry(new PlayerPrefsSaveStorage());

        // 첫 실행 판단은 저장본이 만들어지기 전에 해야 한다 — LoadAll 뒤로 밀면 항상 false가 된다.
        bool isFirstLaunch = m_Registry.storage.Has(OPTION_SAVE_KEY) == false;

        m_PlayerData = m_Registry.Add<PlayerData>(PLAYER_SAVE_KEY).data;
        m_OptionData = m_Registry.Add<OptionData>(OPTION_SAVE_KEY).data;

        m_Registry.LoadAll();

        if (isFirstLaunch == true)
            m_OptionData.SetLanguage(StringTable.GetDefaultLanguage());

        ApplyOption();
    }

    // 저장본에서 읽은 옵션을 실제 시스템에 반영한다. 로드 직후와 옵션 변경 직후에 부른다.
    private void ApplyOption()
    {
        StringTable.CurrentLanguage = m_OptionData.language;

        SoundManager.instance.SetCategoryVolume(eSoundCategory.Bgm, m_OptionData.bgmVolume);
        SoundManager.instance.SetCategoryVolume(eSoundCategory.Sfx, m_OptionData.sfxVolume);

        ApplyFpsOption();
    }

    private void ApplyFpsOption()
    {
        switch (m_OptionData.fpsOption)
        {
            case eFpsOption.Fps30:
                Application.targetFrameRate = 30;
                break;
            case eFpsOption.Fps60:
                Application.targetFrameRate = 60;
                break;
            case eFpsOption.Adaptive:
            default:
                Application.targetFrameRate = -1;
                break;
        }
    }

    // 즉시 저장. 평소엔 Update()의 더티 검사가 알아서 저장하므로 앱이 내려가는 시점에만 쓴다.
    public void Save()
    {
        m_PlayerData.SetLastPlayedAt(DateTime.Now.ToString("o"));

        if (m_Registry == null)
            return;

        m_Registry.SaveAll();
    }

    private void Update()
    {
        if (m_Registry == null)
            return;

        m_Registry.UpdateLogic();
    }

    // ---------------- 종족 ----------------

    // 해금 소유권: 테이블의 isUnlocked는 "기본 개방"만 뜻하고, 플레이로 딴 해금은 PlayerData가 든다.
    // 테이블만 보면 플레이어별 진행도를 담을 수 없고, PlayerData만 보면 초기 개방 종족을 튜닝할 수 없다.
    public bool IsHouseUnlocked(HouseRecord _record)
    {
        if (_record == null)
            return false;

        if (_record.isUnlocked > 0)
            return true;

        return m_PlayerData.IsUnlockedHouseKey(_record.Key);
    }

    public void UnlockHouse(string _houseKey)
    {
        m_PlayerData.AddUnlockedHouseKey(_houseKey);
    }

    public void SetSelectedHouse(string _houseKey)
    {
        m_PlayerData.SetSelectedHouseKey(_houseKey);
    }

    // 저장된 선택이 없거나 그 종족이 사라졌으면 첫 해금 종족으로 떨어진다.
    public HouseRecord GetSelectedHouseRecord()
    {
        HouseTable houseTable = TableManager.instance.GetTable<HouseTable>();
        if (houseTable == null)
        {
            Logger.Error($"[PlayerManager] GetSelectedHouseRecord Failed! HouseTable not found");
            return null;
        }

        HouseRecord record = houseTable.GetRecordByKey(m_PlayerData.selectedHouseKey);
        if (record != null && IsHouseUnlocked(record) == true)
            return record;

        return houseTable.list.Find(house => IsHouseUnlocked(house) == true);
    }

    public int GetHouseUpgradeLevel(string _houseKey, string _nodeKey)
    {
        if (string.IsNullOrEmpty(_houseKey) == true)
            return 0;

        if (string.IsNullOrEmpty(_nodeKey) == true)
            return 0;

        return m_PlayerData.GetHouseUpgradeLevel(_houseKey, _nodeKey);
    }

    // ---------------- 옵션 ----------------

    public void SetBgmVolume(float _volume)
    {
        m_OptionData.SetBgmVolume(_volume);
        SoundManager.instance.SetCategoryVolume(eSoundCategory.Bgm, _volume);
    }

    public void SetSfxVolume(float _volume)
    {
        m_OptionData.SetSfxVolume(_volume);
        SoundManager.instance.SetCategoryVolume(eSoundCategory.Sfx, _volume);
    }

    public void SetFpsOption(eFpsOption _fpsOption)
    {
        m_OptionData.SetFpsOption(_fpsOption);
        ApplyFpsOption();
    }

    public void SetLanguage(eLanguage _language)
    {
        m_OptionData.SetLanguage(_language);
        StringTable.CurrentLanguage = _language;
    }

    private void OnApplicationPause(bool _isPaused)
    {
        if (_isPaused == false)
            return;

        Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
