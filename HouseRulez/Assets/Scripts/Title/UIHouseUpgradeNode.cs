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
    [SerializeField] private TextMeshProUGUI m_UpgradeButtonText;
    [SerializeField] private TextMeshProUGUI m_MaxText;

    private string m_NodeKey = string.Empty;
    private Action<string> m_OnClickUpgrade;

    public void SetData(string _nodeKey, string _displayName, string _description,
        string _levelLabel, string _costLabel, bool _isMaxLevel, bool _isAffordable,
        string _purchaseLabel, string _maxLabel, Action<string> _onClickUpgrade)
    {
        m_NodeKey = _nodeKey;
        m_OnClickUpgrade = _onClickUpgrade;

        if (m_NameText != null)
            m_NameText.text = _displayName;

        if (m_DescText != null)
            m_DescText.text = _description;

        if (m_LevelText != null)
            m_LevelText.text = _levelLabel;

        // 문구는 프리팹에 남은 값을 믿지 않고 매번 넣는다 — 템플릿을 복제해 만든 오브젝트라
        // 손대지 않으면 복제 원본의 텍스트("X" 등)가 그대로 화면에 남는다.
        if (m_MaxText != null)
        {
            m_MaxText.text = _maxLabel;
            m_MaxText.gameObject.SetActive(_isMaxLevel);
        }

        // 최대 레벨이어도 비용 칸을 끄지 않고 비워만 둔다. 오브젝트를 끄면 그 폭만큼
        // 오른쪽 칸이 당겨져, 줄마다 강화 버튼과 MAX LEVEL의 위치가 어긋난다.
        if (m_CostText != null)
        {
            m_CostText.gameObject.SetActive(true);
            m_CostText.text = (_isMaxLevel == true) ? string.Empty : _costLabel;
        }

        if (m_UpgradeButtonText != null)
            m_UpgradeButtonText.text = _purchaseLabel;

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
