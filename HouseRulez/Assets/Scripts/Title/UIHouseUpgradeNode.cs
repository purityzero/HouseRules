using System;
using TMPro;
using UnityEngine;

// 업그레이드 노드 한 줄. 이름/설명/현재 레벨/비용과 강화 버튼을 든다.
// 비활성 사유를 "버튼 회색" 하나로 뭉치지 않는다 — 최대 레벨과 옥새 부족은 표시가 갈려야 한다.
public class UIHouseUpgradeNode : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_DescText;
    [SerializeField] private TextMeshProUGUI m_LevelText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private UIButton m_UpgradeButton;
    [SerializeField] private GameObject m_MaxObject;

    private string m_NodeKey = string.Empty;
    private Action<string> m_OnClickUpgrade;

    public void SetData(string _nodeKey, string _displayName, string _description,
        string _levelLabel, int _cost, bool _isMaxLevel, bool _isAffordable, Action<string> _onClickUpgrade)
    {
        m_NodeKey = _nodeKey;
        m_OnClickUpgrade = _onClickUpgrade;

        if (m_NameText != null)
            m_NameText.text = _displayName;

        if (m_DescText != null)
            m_DescText.text = _description;

        if (m_LevelText != null)
            m_LevelText.text = _levelLabel;

        if (m_MaxObject != null)
            m_MaxObject.SetActive(_isMaxLevel);

        if (m_CostText != null)
        {
            m_CostText.gameObject.SetActive(_isMaxLevel == false);
            m_CostText.text = _cost.ToString();
        }

        if (m_UpgradeButton != null)
        {
            m_UpgradeButton.gameObject.SetActive(_isMaxLevel == false);
            m_UpgradeButton.interactable = _isAffordable;

            m_UpgradeButton.onClick.RemoveListener(OnClickUpgradeButton);
            m_UpgradeButton.onClick.AddListener(OnClickUpgradeButton);
        }
    }

    public void OnClickUpgradeButton()
    {
        if (m_OnClickUpgrade == null)
            return;

        if (string.IsNullOrEmpty(m_NodeKey) == true)
            return;

        m_OnClickUpgrade(m_NodeKey);
    }
}
