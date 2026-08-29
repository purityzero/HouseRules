using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHouseUpgrade : UIPopup
{
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private TextMeshProUGUI m_SelectedHouseNameText;
    [SerializeField] private TextMeshProUGUI m_EmptyText;

    // 빈 상태 텍스트는 배경 패널 안에 들어 있다. 텍스트만 끄면 빈 배경 상자가 그대로 남는다.
    [SerializeField] private GameObject m_EmptyStatePanel;
    [SerializeField] private Image m_AccentImage;
    [SerializeField] private Transform m_HouseButtonRoot;
    [SerializeField] private UIHouseUpgradeHouseButton m_HouseButtonTemplate;
    [SerializeField] private TextMeshProUGUI m_RoyalText;
    [SerializeField] private Transform m_NodeRoot;
    [SerializeField] private UIHouseUpgradeNode m_NodeTemplate;

    private List<HouseRecord> m_ListHouse = new List<HouseRecord>();
    private List<UIHouseUpgradeHouseButton> m_ListHouseButton = new List<UIHouseUpgradeHouseButton>();
    private List<UIHouseUpgradeNode> m_ListNode = new List<UIHouseUpgradeNode>();
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

        // 켜고 끄는 건 노드를 실제로 만들어 본 BuildNodes가 정한다 — 여기서 켜두면 노드가 있어도 같이 뜬다.
        if (m_EmptyText != null)
            m_EmptyText.text = emptyMessage;
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

        RefreshRoyal(stringTable);
        BuildNodes(stringTable);
    }

    private void RefreshRoyal(StringTable _stringTable)
    {
        if (m_RoyalText == null)
            return;

        string royalName = (_stringTable != null) ? _stringTable.GetString("HouseUpgradeRoyal") : "HouseUpgradeRoyal";
        m_RoyalText.text = $"{royalName} {PlayerManager.instance.royal}";
    }

    // 항목 수가 몇 개뿐이라 전부 파괴/재생성하지 않고 초과분만 끄고 부족분만 늘린다.
    private void BuildNodes(StringTable _stringTable)
    {
        if (m_NodeRoot == null || m_NodeTemplate == null)
            return;

        HouseUpgradeTable upgradeTable = TableManager.instance.GetTable<HouseUpgradeTable>();
        if (upgradeTable == null)
        {
            Logger.Error($"[UIHouseUpgrade] BuildNodes Failed! HouseUpgradeTable not found");
            return;
        }

        List<string> listNodeKey = upgradeTable.GetNodeKeyList(m_SelectedHouse.Key);

        bool isEmpty = (listNodeKey.Count <= 0);
        if (m_EmptyStatePanel != null)
            m_EmptyStatePanel.SetActive(isEmpty);
        else if (m_EmptyText != null)
            m_EmptyText.gameObject.SetActive(isEmpty);

        m_ListNode.Clear();
        m_NodeRoot.GetComponentsInChildren(true, m_ListNode);

        while (m_ListNode.Count < listNodeKey.Count)
        {
            UIHouseUpgradeNode node = Instantiate(m_NodeTemplate, m_NodeRoot);
            m_ListNode.Add(node);
        }

        int royal = PlayerManager.instance.royal;

        for (int i = 0; i < m_ListNode.Count; ++i)
        {
            UIHouseUpgradeNode node = m_ListNode[i];
            node.transform.SetSiblingIndex(i);

            if (i >= listNodeKey.Count)
            {
                node.gameObject.SetActive(false);
                continue;
            }

            string nodeKey = listNodeKey[i];
            int currentLevel = PlayerManager.instance.GetHouseUpgradeLevel(m_SelectedHouse.Key, nodeKey);
            int maxLevel = upgradeTable.GetMaxLevel(m_SelectedHouse.Key, nodeKey);
            HouseUpgradeRecord nextRecord = PlayerManager.instance.GetNextUpgradeRecord(m_SelectedHouse.Key, nodeKey);

            // 이름/설명은 레벨과 무관하므로 현재 레벨 행이 없는 0레벨에서도 읽히도록 1레벨 행에서 가져온다.
            HouseUpgradeRecord labelRecord = upgradeTable.GetRecord(m_SelectedHouse.Key, nodeKey, 1);
            string displayName = nodeKey;
            string description = string.Empty;
            if (labelRecord != null && _stringTable != null)
            {
                displayName = _stringTable.GetString(labelRecord.NameKey);
                description = _stringTable.GetString(labelRecord.DescKey);
            }

            string levelLabel = (_stringTable != null)
                ? _stringTable.GetString("HouseUpgradeLevelFormat", currentLevel, maxLevel)
                : $"Lv {currentLevel}/{maxLevel}";

            bool isMaxLevel = (nextRecord == null);
            int cost = (isMaxLevel == true) ? 0 : nextRecord.CostValue;
            bool isAffordable = (isMaxLevel == false && royal >= cost);

            node.gameObject.SetActive(true);
            node.SetData(nodeKey, displayName, description, levelLabel, cost, isMaxLevel, isAffordable, OnClickUpgradeNode);
        }
    }

    private void OnClickUpgradeNode(string _nodeKey)
    {
        if (m_SelectedHouse == null)
            return;

        if (PlayerManager.instance.TryPurchaseHouseUpgrade(m_SelectedHouse.Key, _nodeKey) == false)
            return;

        // 재화가 줄고 레벨이 올랐으니 보유량과 노드 줄을 같이 다시 그린다.
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        RefreshRoyal(stringTable);
        BuildNodes(stringTable);
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
