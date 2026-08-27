using TMPro;
using UnityEngine;

// BaseScene의 OnEnable()이 씬 내 다른 스크립트의 OnEnable()보다 먼저 실행되도록 강제 —
// Unity는 "모든 Awake가 끝난 뒤에 Start가 불린다"는 보장은 하지만 OnEnable 순서에는 이 보장이 없다.
// UpdatableBehaviour.OnEnable()이 BaseScene.OnEnable()보다 먼저 돌면 BaseScene.Current가 아직 null이라
// Register(this)가 널 조건 연산자에 걸려 조용히 건너뛰어지고(에러 로그조차 없음) 그 오브젝트의 UpdateLogic()이 영영 안 불린다.
[DefaultExecutionOrder(-1000)]
public class TitleScene : BaseScene
{
    private const string INGAME_SCENE_NAME = "InGameScene";

    [SerializeField] private TextMeshProUGUI m_PlayButtonText;
    [SerializeField] private TextMeshProUGUI m_HouseSelectButtonText;
    [SerializeField] private TextMeshProUGUI m_UpgradeButtonText;
    [SerializeField] private TextMeshProUGUI m_SettingButtonText;

    [SerializeField] private UIHouseSelect m_HouseSelect;

    protected override void OnSetup()
    {
        // 이 프로젝트엔 아직 부팅 진입점(GameManager 등)이 없어 테이블을 로드하는 곳이 여기뿐이다.
        // init()은 자체 멱등 가드(m_isInitialized)를 갖고 있어, 나중에 부팅 진입점이 생겨
        // 양쪽에서 호출돼도 테이블이 중복 누적되지 않는다. 진입점이 생기면 이 줄을 그쪽으로 옮긴다.
        TableManager.instance.init();

        // 저장본을 여기서 깨운다(MonoSingleton이 없으면 만들면서 Awake -> Load까지 돈다).
        // ApplyLocalizedText()보다 반드시 먼저여야 한다 — 저장된 언어가 StringTable.CurrentLanguage에 반영된 뒤 텍스트를 뽑아야 한다.
        PlayerManager.instance.Load();

        ApplyLocalizedText();
        ApplyHouseTheme();
        PlayBgm();
    }

    // 저장된 종족을 타이틀 화면에 입힌다. 이게 없으면 씬에 박아둔 기본 그림으로만 떠서,
    // 종족을 바꿔둔 채 게임을 다시 켜면 타이틀만 이전 종족으로 남는다.
    private void ApplyHouseTheme()
    {
        if (m_HouseSelect == null)
        {
            Logger.Error($"[TitleScene] ApplyHouseTheme Failed! UIHouseSelect not linked");
            return;
        }

        HouseRecord record = PlayerManager.instance.GetSelectedHouseRecord();
        if (record == null)
        {
            Logger.Error($"[TitleScene] ApplyHouseTheme Failed! GetSelectedHouseRecord == null");
            return;
        }

        m_HouseSelect.ApplyTitleTheme(record);
    }

    private void ApplyLocalizedText()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
        {
            Logger.Error($"[TitleScene] ApplyLocalizedText Failed! StringTable not found");
            return;
        }

        SetText(m_PlayButtonText, stringTable.GetString("TitlePlay"));
        SetText(m_HouseSelectButtonText, stringTable.GetString("TitleHouseSelect"));
        SetText(m_UpgradeButtonText, stringTable.GetString("TitleUpgrade"));
        SetText(m_SettingButtonText, stringTable.GetString("TitleSetting"));
    }

    private void SetText(TextMeshProUGUI _text, string _value)
    {
        if (_text == null)
            return;

        _text.text = _value;
    }

    private void PlayBgm()
    {
        SoundTable soundTable = TableManager.instance.GetTable<SoundTable>();
        SoundRecord record = soundTable?.GetRecordByKey("TitleTheme");
        if (record == null)
        {
            Logger.Error($"[TitleScene] PlayBgm Failed! SoundRecord not found - TitleTheme");
            return;
        }

        AudioClip clip = ResUtil.Load<AudioClip>(record.ClipPath);
        if (clip == null)
            return;

        SoundManager.instance.PlayBgm(clip);
    }

    public void OnClickPlayButton()
    {
        // 전환 중 연타 방지 — NextScene()은 부를 때마다 같은 커맨드 묶음을 큐에 더 쌓기만 해서,
        // 두 번 눌리면 이미 언로드된 씬을 다시 언로드하려 들며 페이드가 두 번 겹친다.
        if (SceneManager.instance.IsSceneTransitioning == true)
            return;

        SceneManager.instance.NextScene(INGAME_SCENE_NAME);
    }

    public void OnClickHouseSelectButton()
    {
        if (m_HouseSelect == null)
        {
            Logger.Error($"[TitleScene] OnClickHouseSelectButton Failed! UIHouseSelect not linked");
            return;
        }

        m_HouseSelect.Open();
    }

    public void OnClickUpgradeButton()
    {
        // TODO: 업그레이드 화면
    }

    public void OnClickSettingButton()
    {
        // TODO: 설정 화면
    }
}
