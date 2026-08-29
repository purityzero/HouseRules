using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHouseUpgradeHouseButton : MonoBehaviour
{
    [SerializeField] private UIButton m_Button;
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private Image m_AccentImage;
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_LockedText;

    [SerializeField] private Color m_NormalColor = new Color(0.12f, 0.15f, 0.21f, 1f);
    [SerializeField] private Color m_SelectedColor = new Color(0.25f, 0.29f, 0.38f, 1f);

    private HouseRecord m_Record;
    private Action<HouseRecord> m_OnClick;

    public string houseKey
    {
        get
        {
            if (m_Record == null)
                return string.Empty;

            return m_Record.Key;
        }
    }

    public void SetData(HouseRecord _record, string _displayName, string _lockedLabel,
        bool _isUnlocked, Action<HouseRecord> _onClick)
    {
        m_Record = _record;
        m_OnClick = _onClick;

        if (m_NameText != null)
            m_NameText.text = _displayName;

        if (m_LockedText != null)
        {
            m_LockedText.gameObject.SetActive(_isUnlocked == false);
            m_LockedText.text = _lockedLabel;
        }

        if (m_AccentImage != null)
        {
            Color accent;
            if (ColorUtility.TryParseHtmlString($"#{_record.AccentColor}", out accent) == true)
            {
                if (_isUnlocked == false)
                    accent = new Color(accent.r, accent.g, accent.b, 0.35f);

                m_AccentImage.color = accent;
            }
        }

        if (m_Button != null)
        {
            m_Button.onClick.RemoveListener(OnClickSelf);
            m_Button.onClick.AddListener(OnClickSelf);
        }
    }

    public void SetSelected(bool _isSelected)
    {
        if (m_BackgroundImage == null)
            return;

        m_BackgroundImage.color = (_isSelected == true) ? m_SelectedColor : m_NormalColor;
    }

    private void OnClickSelf()
    {
        if (m_OnClick == null || m_Record == null)
            return;

        m_OnClick(m_Record);
    }
}
