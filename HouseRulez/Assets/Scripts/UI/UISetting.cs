using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISetting : UIPopup
{
    private static readonly eLanguage[] Languages =
    {
        eLanguage.Korean,
        eLanguage.English,
        eLanguage.Chinese,
        eLanguage.Japanese,
    };

    private static readonly eFpsOption[] FpsOptions =
    {
        eFpsOption.Adaptive,
        eFpsOption.Fps30,
        eFpsOption.Fps60,
    };

    [SerializeField] private UIButton m_LanguageButton;
    [SerializeField] private TextMeshProUGUI m_LanguageValueText;
    [SerializeField] private UIButton m_FpsButton;
    [SerializeField] private TextMeshProUGUI m_FpsValueText;
    [SerializeField] private Slider m_BgmSlider;
    [SerializeField] private Slider m_SfxSlider;

    private bool m_areListenersBound;

    public override void Show()
    {
        base.Show();

        // 현재 TitleScene의 UIManager에는 PopupCanvas 자식이 없어 UIManager 루트(크기 0) 아래에 생성된다.
        // 이 프리팹은 자체 Overlay Canvas를 가지므로 기준 해상도를 명시해 부모 Rect 크기에 의존하지 않게 한다.
        RectTransform rectTransform = transform as RectTransform;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(1920f, 1080f);

        OptionData option = PlayerManager.instance.optionData;
        RefreshOptionValueText();

        m_BgmSlider.SetValueWithoutNotify(option.bgmVolume);
        m_SfxSlider.SetValueWithoutNotify(option.sfxVolume);

        if (m_areListenersBound == false)
        {
            m_areListenersBound = true;
            m_LanguageButton.onClick.AddListener(OnClickLanguageButton);
            m_FpsButton.onClick.AddListener(OnClickFpsButton);
            m_BgmSlider.onValueChanged.AddListener(PlayerManager.instance.SetBgmVolume);
            m_SfxSlider.onValueChanged.AddListener(PlayerManager.instance.SetSfxVolume);
        }
    }

    public void OnClickCloseButton()
    {
        Close();
    }

    private void OnClickLanguageButton()
    {
        int index = Array.IndexOf(Languages, PlayerManager.instance.optionData.language);
        PlayerManager.instance.SetLanguage(Languages[(index + 1) % Languages.Length]);
        RefreshOptionValueText();
    }

    private void OnClickFpsButton()
    {
        int index = Array.IndexOf(FpsOptions, PlayerManager.instance.optionData.fpsOption);
        PlayerManager.instance.SetFpsOption(FpsOptions[(index + 1) % FpsOptions.Length]);
        RefreshOptionValueText();
    }

    private void RefreshOptionValueText()
    {
        StringTable table = TableManager.instance.GetTable<StringTable>();
        if (table == null)
            return;

        string[] languageNames = { "한국어", "English", "中文", "日本語" };
        string[] fpsNames =
        {
            table.GetString("SettingsFpsAdaptive"),
            table.GetString("SettingsFps30"),
            table.GetString("SettingsFps60"),
        };

        m_LanguageValueText.text = languageNames[Mathf.Max(0, Array.IndexOf(Languages, PlayerManager.instance.optionData.language))];
        m_FpsValueText.text = fpsNames[Mathf.Max(0, Array.IndexOf(FpsOptions, PlayerManager.instance.optionData.fpsOption))];
    }
}
