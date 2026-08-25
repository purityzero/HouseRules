using TMPro;
using UnityEngine;

// BaseScene의 OnEnable()이 씬 내 다른 스크립트의 OnEnable()보다 먼저 실행되도록 강제 —
// Unity는 "모든 Awake가 끝난 뒤에 Start가 불린다"는 보장은 하지만 OnEnable 순서에는 이 보장이 없다.
// UpdatableBehaviour.OnEnable()이 BaseScene.OnEnable()보다 먼저 돌면 BaseScene.Current가 아직 null이라
// Register(this)가 널 조건 연산자에 걸려 조용히 건너뛰어지고(에러 로그조차 없음) 그 오브젝트의 UpdateLogic()이 영영 안 불린다.
[DefaultExecutionOrder(-1000)]
public class TitleScene : BaseScene
{
    [SerializeField] private TextMeshProUGUI m_PlayButtonText;
    [SerializeField] private TextMeshProUGUI m_HouseSelectButtonText;
    [SerializeField] private TextMeshProUGUI m_UpgradeButtonText;
    [SerializeField] private TextMeshProUGUI m_SettingButtonText;

    protected override void OnSetup()
    {
        // 이 프로젝트엔 아직 부팅 진입점(GameManager 등)이 없어 테이블을 로드하는 곳이 여기뿐이다.
        // init()은 자체 멱등 가드(m_isInitialized)를 갖고 있어, 나중에 부팅 진입점이 생겨
        // 양쪽에서 호출돼도 테이블이 중복 누적되지 않는다. 진입점이 생기면 이 줄을 그쪽으로 옮긴다.
        TableManager.instance.init();

        ApplyLocalizedText();
        PlayBgm();
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
        // TODO: 인게임 씬 진입
    }

    public void OnClickHouseSelectButton()
    {
        // TODO: 종족 선택 화면 — 체스/장기/포커/화투
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
