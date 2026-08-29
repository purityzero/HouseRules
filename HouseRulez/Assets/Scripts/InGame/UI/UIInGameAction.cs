using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인게임 하단 액션 바. 기획서 §10 ScreenZones의 ACTION 영역(네이티브 144,250 - 492,284)을 x3한 1044x102다.
// 전투 시작은 이 클래스가 처리하지 않고 이벤트로 올린다 — 전투 단계가 아직 없어 받을 쪽이 정해지지 않았다.
public class UIInGameAction : MonoBehaviour
{
    [SerializeField] private Button m_BattleStartButton;

    [SerializeField] private RectTransform m_SwapPipRoot;
    [SerializeField] private Image m_SwapPipTemplate;
    [SerializeField] private TextMeshProUGUI m_SwapText;

    [SerializeField] private Button m_BattleSpeedButton;
    [SerializeField] private TextMeshProUGUI m_BattleSpeedText;

    [SerializeField] private float m_PipSpacing = 8f;
    [SerializeField] private Color m_PipFilledColor = new Color(0.9647059f, 0.9607843f, 0.9411765f, 1f);
    [SerializeField] private Color m_PipEmptyColor = new Color(0.22352941f, 0.24313726f, 0.30588236f, 1f);

    private List<Image> m_ListSwapPip = new List<Image>();
    private RunData m_RunData;

    public event Action OnBattleStart;
    public event Action OnBattleSpeed;

    private void Awake()
    {
        if (m_BattleStartButton != null)
            m_BattleStartButton.onClick.AddListener(OnClickBattleStartButton);

        if (m_BattleSpeedButton != null)
            m_BattleSpeedButton.onClick.AddListener(OnClickBattleSpeedButton);
    }

    public void Apply(RunData _runData)
    {
        if (_runData == null)
        {
            Logger.Error("[UIInGameAction] Apply Failed! runData == null");
            return;
        }

        m_RunData = _runData;

        BuildSwapPipList(_runData.swapCountMax);

        Refresh();
    }

    public void Refresh()
    {
        if (m_RunData == null)
            return;

        for (int i = 0; i < m_ListSwapPip.Count; ++i)
        {
            m_ListSwapPip[i].color = (i < m_RunData.swapCount) ? m_PipFilledColor : m_PipEmptyColor;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        SetText(m_SwapText, stringTable.GetString("ActionSwap", m_RunData.swapCount));
        SetText(m_BattleSpeedText, stringTable.GetString("ActionBattleSpeed", m_RunData.battleSpeed));
    }

    private void BuildSwapPipList(int _count)
    {
        if (m_SwapPipRoot == null)
        {
            Logger.Error("[UIInGameAction] BuildSwapPipList Failed! SwapPipRoot == null");
            return;
        }

        if (m_SwapPipTemplate == null)
        {
            Logger.Error("[UIInGameAction] BuildSwapPipList Failed! SwapPipTemplate == null");
            return;
        }

        m_SwapPipTemplate.gameObject.SetActive(false);

        for (int i = 0; i < m_ListSwapPip.Count; ++i)
        {
            Destroy(m_ListSwapPip[i].gameObject);
        }
        m_ListSwapPip.Clear();

        float pipWidth = m_SwapPipTemplate.rectTransform.sizeDelta.x;

        for (int i = 0; i < _count; ++i)
        {
            Image pip = Instantiate(m_SwapPipTemplate, m_SwapPipRoot);
            pip.gameObject.SetActive(true);

            RectTransform rectTransform = pip.rectTransform;
            rectTransform.anchoredPosition = new Vector2(i * (pipWidth + m_PipSpacing), 0f);

            m_ListSwapPip.Add(pip);
        }
    }

    public void OnClickBattleStartButton()
    {
        OnBattleStart?.Invoke();
    }

    public void OnClickBattleSpeedButton()
    {
        OnBattleSpeed?.Invoke();
    }

    private void SetText(TextMeshProUGUI _text, string _value)
    {
        if (_text == null)
            return;

        _text.text = _value;
    }
}
