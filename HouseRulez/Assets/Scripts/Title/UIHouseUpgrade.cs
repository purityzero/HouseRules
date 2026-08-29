using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHouseUpgrade : UIPopup
{
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private TextMeshProUGUI m_SelectedHouseNameText;
    [SerializeField] private TextMeshProUGUI m_EmptyText;
    [SerializeField] private Image m_AccentImage;
    [SerializeField] private Transform m_HouseButtonRoot;
    [SerializeField] private UIHouseUpgradeHouseButton m_HouseButtonTemplate;

    private List<HouseRecord> m_ListHouse = new List<HouseRecord>();
    private List<UIHouseUpgradeHouseButton> m_ListHouseButton = new List<UIHouseUpgradeHouseButton>();
    private HouseRecord m_SelectedHouse;

    public override void Show()
    {
        base.Show();

        RectTransform rectTransform = transform as RectTransform;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(1920f, 1080f);

        HouseTable houseTable = TableManager.instance.GetTable<HouseTable>();
        if (houseTable == null)
        {
            Logger.Error($"[UIHouseUpgrade] Show Failed! HouseTable not found");
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        m_ListHouse.Clear();
        m_ListHouse.AddRange(houseTable.list);

        ApplyLocalizedText(stringTable);
        BuildHouseButtons(stringTable);

        HouseRecord selectedHouse = PlayerManager.instance.GetSelectedHouseRecord();
        if (selectedHouse == null && m_ListHouse.Count > 0)
            selectedHouse = m_ListHouse[0];

        SelectHouse(selectedHouse);
    }

    public override void Close()
    {
        base.Close();
    }

    public void OnClickCloseButton()
    {
        Close();
    }

    private void ApplyLocalizedText(StringTable _stringTable)
    {
        string title = "HouseUpgradeTitle";
        string emptyMessage = "HouseUpgradeEmpty";

        if (_stringTable != null)
        {
            title = _stringTable.GetString("HouseUpgradeTitle");
            emptyMessage = _stringTable.GetString("HouseUpgradeEmpty");
        }

        if (m_TitleText != null)
            m_TitleText.text = title;

        if (m_EmptyText != null)
        {
            m_EmptyText.text = emptyMessage;
            m_EmptyText.gameObject.SetActive(true);
        }
    }

    private void BuildHouseButtons(StringTable _stringTable)
    {
        if (m_HouseButtonRoot == null || m_HouseButtonTemplate == null)
            return;

        m_ListHouseButton.Clear();
        m_HouseButtonRoot.GetComponentsInChildren(true, m_ListHouseButton);

        while (m_ListHouseButton.Count < m_ListHouse.Count)
        {
            UIHouseUpgradeHouseButton button = Instantiate(m_HouseButtonTemplate, m_HouseButtonRoot);
            m_ListHouseButton.Add(button);
        }

        string lockedLabel = (_stringTable != null) ? _stringTable.GetString("HouseLocked") : "HouseLocked";

        for (int i = 0; i < m_ListHouseButton.Count; ++i)
        {
            UIHouseUpgradeHouseButton button = m_ListHouseButton[i];
            button.transform.SetSiblingIndex(i);

            if (i >= m_ListHouse.Count)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            HouseRecord record = m_ListHouse[i];
            string displayName = (_stringTable != null) ? _stringTable.GetString(record.NameKey) : record.NameKey;
            bool isUnlocked = PlayerManager.instance.IsHouseUnlocked(record);

            button.gameObject.SetActive(true);
            button.SetData(record, displayName, lockedLabel, isUnlocked, SelectHouse);
        }
    }

    private void SelectHouse(HouseRecord _record)
    {
        if (_record == null)
            return;

        m_SelectedHouse = _record;

        for (int i = 0; i < m_ListHouseButton.Count; ++i)
        {
            bool isSelected = (m_ListHouseButton[i].houseKey == m_SelectedHouse.Key) ? true : false;
            m_ListHouseButton[i].SetSelected(isSelected);
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        string displayName = (stringTable != null) ? stringTable.GetString(m_SelectedHouse.NameKey) : m_SelectedHouse.NameKey;
        if (m_SelectedHouseNameText != null)
            m_SelectedHouseNameText.text = displayName;

        if (m_AccentImage != null)
            m_AccentImage.color = ToColor(m_SelectedHouse.AccentColor);
    }

    private Color ToColor(string _hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString($"#{_hex}", out color) == true)
            return color;

        Logger.Error($"[UIHouseUpgrade] AccentColor parse failed - {_hex}");
        return Color.white;
    }
}
