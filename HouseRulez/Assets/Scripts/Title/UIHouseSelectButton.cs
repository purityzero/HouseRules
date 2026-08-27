using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 종족 선택지 하나. 화면 아래에 가로로 5개가 놓인다.
public class UIHouseSelectButton : MonoBehaviour
{
    [SerializeField] private Button m_Button;
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private Image m_AccentImage;      // 종족 색 띠
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_LockedText;

    [SerializeField] private Color m_NormalColor = new Color(0.35f, 0.38f, 0.46f, 1f);
    [SerializeField] private Color m_SelectedColor = new Color(0.51f, 0.55f, 0.65f, 1f);

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

    public void SetData(HouseRecord _record, string _displayName, string _lockedLabel, Action<HouseRecord> _onClick)
    {
        m_Record = _record;
        m_OnClick = _onClick;

        if (m_NameText != null)
            m_NameText.text = _displayName;

        bool isLocked = (_record.isUnlocked <= 0);

        if (m_LockedText != null)
        {
            m_LockedText.gameObject.SetActive(isLocked);
            m_LockedText.text = _lockedLabel;
        }

        if (m_AccentImage != null)
        {
            Color accent;
            if (ColorUtility.TryParseHtmlString($"#{_record.AccentColor}", out accent) == true)
            {
                // 잠긴 종족은 색을 죽여서 한눈에 구분되게 한다
                if (isLocked == true)
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

        if (_isSelected == true)
            m_BackgroundImage.color = m_SelectedColor;
        else
            m_BackgroundImage.color = m_NormalColor;
    }

    private void OnClickSelf()
    {
        if (m_OnClick == null)
            return;

        m_OnClick(m_Record);
    }
}
