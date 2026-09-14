using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 종족의 **족보**와 **유닛 특성**을 보는 팝업. 인게임과 종족 선택 두 자리에서 같은 것을 연다.
//
// 글은 만들지 않는다 — [[HandbookText]] 가 만든 것을 받아 한 덩어리로 붙인다.
// 보여주는 자리가 바뀌어도(도감 화면, 툴팁) 그쪽은 그대로 쓰이도록 나눠 뒀다.
//
// 사용자 요구(2026-09-14): "아예 팝업창에서 스크롤뷰 내려서 전체를 확인할 수 있었으면 한데, 자세하게"
public class UIHandbook : UIPopup
{
    // 프리팹 계층의 이름으로 찾는다. 직렬화 연결을 쓰지 않는 이유는 이 팝업이
    // UIManager.Get<T>() 로 런타임 생성되기 때문이다 — 씬에 인스턴스가 없어 인스펙터에서 이을 자리가 없다.
    private const string PATH_TITLE = "Panel/TitleText";
    private const string PATH_BODY = "Panel/ScrollView/Viewport/Content/BodyText";
    private const string PATH_SCROLL = "Panel/ScrollView";
    private const string PATH_CLOSE = "Panel/CloseButton";
    private const string PATH_HOUSE_TAB_ROOT = "Panel/HouseTabRoot";
    private const string PATH_PATTERN_TAB = "Panel/ModeTabRoot/PatternTabButton";
    private const string PATH_UNIT_TAB = "Panel/ModeTabRoot/UnitTabButton";

    // 종족 탭 오브젝트 이름은 `HouseTab_{key}` 다. 키가 곧 이름의 꼬리라 테이블과 1:1로 맞는다.
    private const string HOUSE_TAB_PREFIX = "HouseTab_";

    // 선택된 탭을 색으로 나눈다. 종족 강조색(AccentColor)은 Accent 자식이 따로 들고 있어
    // 배경은 밝기만 건드린다 — 종족색을 덮으면 어느 종족인지 알아볼 수 없다.
    private static readonly Color TAB_SELECTED = new Color(1f, 1f, 1f, 1f);
    private static readonly Color TAB_NORMAL = new Color(0.62f, 0.62f, 0.62f, 1f);

    // **라벨도 함께 뒤집는다.** 배경만 흰색으로 올리면 흰 글자가 그 위에서 사라진다
    // (2026-09-14 QA: "선택한 종족과 선택한 모드의 글자가 빈 칸처럼 보인다").
    // 어두운 쪽은 GDD §10의 최암부 #363B4A 다 — 팔레트 밖의 검정을 새로 들이지 않는다.
    private static readonly Color LABEL_SELECTED = new Color(0.212f, 0.231f, 0.290f, 1f);
    private static readonly Color LABEL_NORMAL = new Color(1f, 1f, 1f, 1f);

    private TextMeshProUGUI m_TitleText;
    private TextMeshProUGUI m_BodyText;
    private ScrollRect m_ScrollRect;

    // 종족 키 -> 그 탭 버튼. 테이블 순서를 그대로 따르므로 목록으로 든다.
    private List<string> m_ListHouseKey = new List<string>();
    private Dictionary<string, UIButton> m_DicHouseTab = new Dictionary<string, UIButton>();

    private UIButton m_PatternTabButton;
    private UIButton m_UnitTabButton;

    private string m_HouseKey = string.Empty;
    private eSummaryTab m_Tab = eSummaryTab.Pattern;

    // 유닛 탭이 쓸 명부. **전투 중에는 UIInGameField 가 자기 참조를 비우므로**
    // 여는 쪽(InGameScene)이 소유한 것을 직접 받는다. 종족 선택 화면에서 열 때는 null 이다.
    private RunRoster m_Roster;

    private bool m_isBuilt;

    // 인게임에서 연다 — 지금 종족과 보유 명부를 함께 넘긴다.
    public void Open(string _houseKey, RunRoster _roster)
    {
        m_Roster = _roster;
        OpenInternal(_houseKey);
    }

    // 종족 선택 화면에서 연다. 아직 런이 없어 명부가 없다.
    public void Open(string _houseKey)
    {
        m_Roster = null;
        OpenInternal(_houseKey);
    }

    private void OpenInternal(string _houseKey)
    {
        Build();

        m_HouseKey = _houseKey;

        // 명부가 없으면 유닛 탭에 보여줄 것이 없다. 족보부터 연다.
        m_Tab = eSummaryTab.Pattern;

        Show();
        Refresh();
    }

    // 계층을 한 번만 훑는다. 팝업은 UIManager 가 캐시해 재사용하므로 두 번째 열기부터는 건너뛴다.
    private void Build()
    {
        if (m_isBuilt == true)
            return;

        m_isBuilt = true;

        m_TitleText = FindText(PATH_TITLE);
        m_BodyText = FindText(PATH_BODY);

        Transform scroll = transform.Find(PATH_SCROLL);
        if (scroll != null)
            m_ScrollRect = scroll.GetComponent<ScrollRect>();

        if (m_ScrollRect == null)
            Logger.Error($"[UIHandbook] Build Failed! ScrollRect 없음 - {PATH_SCROLL} (기대: 프리팹에 ScrollRect)");

        BindButton(PATH_CLOSE, OnClickCloseButton);

        m_PatternTabButton = BindButton(PATH_PATTERN_TAB, OnClickPatternTab);
        m_UnitTabButton = BindButton(PATH_UNIT_TAB, OnClickUnitTab);

        BuildHouseTabs();
    }

    // 종족 탭은 테이블 순서를 따른다 — 프리팹의 오브젝트 순서가 아니라.
    // 종족이 늘거나 순서가 바뀌면 테이블만 고치면 되고, 프리팹에 탭이 없으면 그 종족은 조용히 빠진다.
    private void BuildHouseTabs()
    {
        Transform root = transform.Find(PATH_HOUSE_TAB_ROOT);
        if (root == null)
        {
            Logger.Error($"[UIHandbook] BuildHouseTabs Failed! 탭 루트 없음 - {PATH_HOUSE_TAB_ROOT} (기대: 프리팹에 HouseTabRoot)");
            return;
        }

        HouseTable houseTable = TableManager.instance.GetTable<HouseTable>();
        if (houseTable == null)
        {
            Logger.Error("[UIHandbook] BuildHouseTabs Failed! HouseTable not found (기대: TableManager에 등록됨)");
            return;
        }

        for (int i = 0; i < houseTable.list.Count; ++i)
        {
            HouseRecord record = houseTable.list[i];
            if (record == null || string.IsNullOrEmpty(record.Key) == true)
                continue;

            Transform tab = root.Find(HOUSE_TAB_PREFIX + record.Key);
            if (tab == null)
            {
                Logger.Error($"[UIHandbook] BuildHouseTabs Failed! 탭 없음 - {HOUSE_TAB_PREFIX}{record.Key} (기대: HouseTabRoot 아래에 존재)");
                continue;
            }

            UIButton button = tab.GetComponent<UIButton>();
            if (button == null)
                continue;

            // 캡처가 루프 변수를 물지 않게 지역으로 받는다 — 전부 마지막 종족으로 열리는 고전적 사고다.
            string houseKey = record.Key;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClickHouseTab(houseKey));

            m_ListHouseKey.Add(houseKey);
            m_DicHouseTab[houseKey] = button;

            SetTabLabel(tab, stringKey: record.NameKey);
        }
    }

    private void SetTabLabel(Transform _tab, string stringKey)
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        Transform label = _tab.Find("Label");
        if (label == null)
            return;

        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = stringTable.GetString(stringKey);
    }

    private TextMeshProUGUI FindText(string _path)
    {
        Transform found = transform.Find(_path);
        if (found == null)
        {
            Logger.Error($"[UIHandbook] FindText Failed! 경로 없음 - {_path} (기대: 프리팹 계층에 존재)");
            return null;
        }

        return found.GetComponent<TextMeshProUGUI>();
    }

    // **런타임으로 배선한다.** 프리팹에 영구 호출을 두면 복제가 남의 이벤트를 물고 온다 —
    // UIInGameSummary 가 같은 사고를 하루에 세 번 겪고 남긴 기록이 있다.
    private UIButton BindButton(string _path, UnityEngine.Events.UnityAction _action)
    {
        Transform found = transform.Find(_path);
        if (found == null)
        {
            Logger.Error($"[UIHandbook] BindButton Failed! 경로 없음 - {_path} (기대: 프리팹 계층에 존재)");
            return null;
        }

        UIButton button = found.GetComponent<UIButton>();
        if (button == null)
        {
            Logger.Error($"[UIHandbook] BindButton Failed! UIButton 없음 - {_path} (기대: UIButton 컴포넌트)");
            return null;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(_action);
        return button;
    }

    private void Refresh()
    {
        RefreshTitle();
        RefreshBody();
        RefreshTabColor();

        // 종족이나 탭을 바꾸면 맨 위부터 읽어야 한다. 스크롤이 중간에 남아 있으면
        // 내용이 바뀐 것을 못 알아챈다.
        if (m_ScrollRect != null)
            m_ScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshTitle()
    {
        if (m_TitleText == null)
            return;

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        HouseTable houseTable = TableManager.instance.GetTable<HouseTable>();
        if (stringTable == null || houseTable == null)
            return;

        HouseRecord record = houseTable.list.Find(house => house != null && house.Key == m_HouseKey);
        string houseName = (record != null) ? stringTable.GetString(record.NameKey) : m_HouseKey;
        string tabName = stringTable.GetString(
            (m_Tab == eSummaryTab.Unit) ? "SummaryTabUnit" : "SummaryTabPattern");

        m_TitleText.text = $"{houseName} · {tabName}";
    }

    private void RefreshBody()
    {
        if (m_BodyText == null)
            return;

        if (m_Tab == eSummaryTab.Unit)
        {
            // 명부가 없으면(종족 선택 화면) 보여줄 유닛이 없다. 빈 화면 대신 이유를 적는다.
            m_BodyText.text = (m_Roster != null)
                ? HandbookText.BuildUnit(m_HouseKey, m_Roster)
                : TableManager.instance.GetTable<StringTable>().GetString("HandbookUnitEmpty");
            return;
        }

        m_BodyText.text = HandbookText.BuildPattern(m_HouseKey);
    }

    private void RefreshTabColor()
    {
        for (int i = 0; i < m_ListHouseKey.Count; ++i)
        {
            string key = m_ListHouseKey[i];
            UIButton button = m_DicHouseTab[key];
            if (button == null)
                continue;

            SetTabColor(button, key == m_HouseKey);
        }

        SetTabColor(m_PatternTabButton, m_Tab == eSummaryTab.Pattern);
        SetTabColor(m_UnitTabButton, m_Tab == eSummaryTab.Unit);
    }

    // 배경과 라벨을 **함께** 뒤집는다. 한쪽만 바꾸면 글자가 배경에 묻힌다 —
    // 종족 탭과 모드 탭이 같은 함수를 쓰므로 한쪽만 고쳐지는 일도 없다.
    private void SetTabColor(UIButton _button, bool _isSelected)
    {
        if (_button == null)
            return;

        Image image = _button.GetComponent<Image>();
        if (image != null)
            image.color = (_isSelected == true) ? TAB_SELECTED : TAB_NORMAL;

        Transform label = _button.transform.Find("Label");
        if (label == null)
            return;

        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.color = (_isSelected == true) ? LABEL_SELECTED : LABEL_NORMAL;
    }

    private void OnClickHouseTab(string _houseKey)
    {
        if (m_HouseKey == _houseKey)
            return;

        m_HouseKey = _houseKey;
        Refresh();
    }

    private void OnClickPatternTab()
    {
        if (m_Tab == eSummaryTab.Pattern)
            return;

        m_Tab = eSummaryTab.Pattern;
        Refresh();
    }

    private void OnClickUnitTab()
    {
        if (m_Tab == eSummaryTab.Unit)
            return;

        m_Tab = eSummaryTab.Unit;
        Refresh();
    }

    private void OnClickCloseButton()
    {
        Close();
    }
}
