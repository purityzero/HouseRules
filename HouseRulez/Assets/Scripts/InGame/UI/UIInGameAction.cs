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

    // 씬이 잡아둔 "칸 오른쪽 끝 ~ 라벨" 간격. 칸 수가 바뀌어도 이 간격은 유지한다.
    private float m_SwapTextGap;
    private bool m_isSwapTextGapCached;

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

        // 칸을 만들기 전에 씬이 잡아둔 "칸 오른쪽 끝 ~ 라벨" 간격을 한 번 기록해 둔다.
        // 칸 수가 늘면 라벨을 그만큼 밀어야 하는데, 간격을 코드에 숫자로 적으면 씬 값과 이중 소스가 된다.
        CacheSwapTextGap();

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

        // 루트 폭을 실제 개수에 맞춘다. 씬에 박아둔 폭(설계 당시 개수 기준)을 그대로 두면
        // 업그레이드로 스왑 최대치가 늘어난 순간 칸이 루트 밖으로 뻗는다.
        // UIInGameHud에서 같은 결함을 먼저 고쳤는데 여기를 빠뜨려 스왑 3칸에서 다시 났다.
        float rootWidth = (_count > 0) ? (_count * (pipWidth + m_PipSpacing) - m_PipSpacing) : 0f;
        m_SwapPipRoot.sizeDelta = new Vector2(rootWidth, m_SwapPipRoot.sizeDelta.y);

        // ACTION 바는 HUD와 달리 레이아웃 그룹이 없고 자식이 전부 절대 좌표다.
        // 그래서 폭만 늘리면 아무도 안 밀린다 — 라벨을 직접 옮겨야 한다.
        LayoutSwapText(rootWidth);
    }

    // 칸 오른쪽 끝에서 원래 간격만큼 떨어진 자리에 라벨을 놓는다.
    private void LayoutSwapText(float _rootWidth)
    {
        if (m_SwapText == null || m_SwapPipRoot == null)
            return;

        if (m_isSwapTextGapCached == false)
            return;

        RectTransform textRect = m_SwapText.rectTransform;
        float x = m_SwapPipRoot.anchoredPosition.x + _rootWidth + m_SwapTextGap;
        textRect.anchoredPosition = new Vector2(x, textRect.anchoredPosition.y);
    }

    private void CacheSwapTextGap()
    {
        if (m_isSwapTextGapCached == true)
            return;

        if (m_SwapText == null || m_SwapPipRoot == null)
            return;

        float pipRootRight = m_SwapPipRoot.anchoredPosition.x + m_SwapPipRoot.sizeDelta.x;
        m_SwapTextGap = m_SwapText.rectTransform.anchoredPosition.x - pipRootRight;
        m_isSwapTextGapCached = true;
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
