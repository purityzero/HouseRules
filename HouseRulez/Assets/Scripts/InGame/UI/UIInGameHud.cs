using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인게임 상단 HUD. 기획서 §10 ScreenZones의 HUD 영역(네이티브 0,0 - 640,20)을 x3한 1920x60 띠다.
// 표시만 한다 — 값을 바꾸는 건 런을 소유한 InGameScene이고, 이 클래스는 Refresh()로 다시 그리기만 한다.
public class UIInGameHud : MonoBehaviour
{
    [SerializeField] private RectTransform m_HomeHpPipRoot;
    [SerializeField] private Image m_HomeHpPipTemplate;

    [SerializeField] private TextMeshProUGUI m_YearText;

    [SerializeField] private TextMeshProUGUI m_GoldValueText;

    [SerializeField] private RectTransform m_SpinCoinPipRoot;
    [SerializeField] private Image m_SpinCoinPipTemplate;

    [SerializeField] private TextMeshProUGUI m_BetText;

    [SerializeField] private float m_PipSpacing = 6f;
    [SerializeField] private Color m_PipFilledColor = new Color(0.81960785f, 0.33333334f, 0.29803923f, 1f);
    [SerializeField] private Color m_PipEmptyColor = new Color(0.22352941f, 0.24313726f, 0.30588236f, 1f);
    [SerializeField] private Color m_SpinCoinFilledColor = new Color(0.9647059f, 0.9607843f, 0.9411765f, 1f);

    private List<Image> m_ListHomeHpPip = new List<Image>();
    private List<Image> m_ListSpinCoinPip = new List<Image>();
    private RunData m_RunData;
    private HouseRecord m_HouseRecord;

    // 인게임 진입 시 한 번. 핍 개수가 테이블 값이라 씬에 박지 않고 여기서 템플릿을 복제해 만든다.
    public void Apply(RunData _runData, HouseRecord _houseRecord)
    {
        if (_runData == null)
        {
            Logger.Error("[UIInGameHud] Apply Failed! runData == null");
            return;
        }

        m_RunData = _runData;
        m_HouseRecord = _houseRecord;


        BuildPipList(m_HomeHpPipRoot, m_HomeHpPipTemplate, _runData.homeHpMax, m_ListHomeHpPip);
        BuildPipList(m_SpinCoinPipRoot, m_SpinCoinPipTemplate, _runData.spinCoinMax, m_ListSpinCoinPip);

        Refresh();
    }

    public void Refresh()
    {
        if (m_RunData == null)
            return;

        RefreshPipList(m_ListHomeHpPip, m_RunData.homeHp, m_PipFilledColor);
        RefreshPipList(m_ListSpinCoinPip, m_RunData.spinCoin, m_SpinCoinFilledColor);

        RefreshYear();
        RefreshGold();
        RefreshBet();
    }

    private void BuildPipList(RectTransform _root, Image _template, int _count, List<Image> _listPip)
    {
        if (_root == null)
        {
            Logger.Error("[UIInGameHud] BuildPipList Failed! root == null");
            return;
        }

        if (_template == null)
        {
            Logger.Error("[UIInGameHud] BuildPipList Failed! template == null");
            return;
        }

        _template.gameObject.SetActive(false);

        // 종족을 바꿔 다시 들어와도 핍이 겹쳐 쌓이지 않게 이전 것을 먼저 지운다.
        for (int i = 0; i < _listPip.Count; ++i)
        {
            Destroy(_listPip[i].gameObject);
        }
        _listPip.Clear();

        // 간격 계산의 기준 폭은 템플릿의 실제 크기에서 읽는다 — 코드에 또 적으면 씬 값과 어긋난다.
        float pipWidth = _template.rectTransform.sizeDelta.x;

        for (int i = 0; i < _count; ++i)
        {
            Image pip = Instantiate(_template, _root);
            pip.gameObject.SetActive(true);

            RectTransform rectTransform = pip.rectTransform;
            rectTransform.anchoredPosition = new Vector2(i * (pipWidth + m_PipSpacing), 0f);

            _listPip.Add(pip);
        }
    }

    private void RefreshPipList(List<Image> _listPip, int _filledCount, Color _filledColor)
    {
        for (int i = 0; i < _listPip.Count; ++i)
        {
            _listPip[i].color = (i < _filledCount) ? _filledColor : m_PipEmptyColor;
        }
    }

    private void RefreshYear()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        SetText(m_YearText, stringTable.GetString("HudYear", m_RunData.year.ToString("00"), m_RunData.yearMax.ToString("00")));
    }

    private void RefreshGold()
    {
        SetText(m_GoldValueText, m_RunData.gold.ToString());
    }

    // 판돈은 화투(섯다)만 쓰는 개념이라, 쓰지 않는 종족에서는 자리 자체를 감춘다.
    private void RefreshBet()
    {
        if (m_BetText == null)
            return;

        if (m_HouseRecord == null || m_HouseRecord.isUseBet <= 0)
        {
            m_BetText.gameObject.SetActive(false);
            return;
        }

        SutdaBetTable betTable = TableManager.instance.GetTable<SutdaBetTable>();
        if (betTable == null)
        {
            Logger.Error("[UIInGameHud] RefreshBet Failed! SutdaBetTable not found");
            m_BetText.gameObject.SetActive(false);
            return;
        }

        SutdaBetRecord record = betTable.GetRecordByLevel(m_RunData.betLevel);
        if (record == null)
        {
            Logger.Error($"[UIInGameHud] RefreshBet Failed! bet level not found - {m_RunData.betLevel}");
            m_BetText.gameObject.SetActive(false);
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        m_BetText.gameObject.SetActive(true);
        SetText(m_BetText, stringTable.GetString("HudBet", record.Level, record.Multiplier.ToString("0.0")));
    }

    private void SetText(TextMeshProUGUI _text, string _value)
    {
        if (_text == null)
            return;

        _text.text = _value;
    }
}
