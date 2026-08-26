using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 종족 선택 화면. 타이틀 위에 얹는 패널이라 고르는 즉시 뒤 배경이 바뀌어 미리보기가 된다.
// 화면은 위에서부터 [말 종류] -> [능력치 막대] -> [종족 선택지 5개] 순으로 쌓인다.
public class UIHouseSelect : MonoBehaviour
{
    // 능력치 막대 한 줄. 라벨과 채워지는 이미지를 묶어둔다.
    [System.Serializable]
    public class StatBar
    {
        public TextMeshProUGUI NameText;
        public Image FillImage;
        public TextMeshProUGUI ValueText;
    }

    [SerializeField] private GameObject m_Root;
    [SerializeField] private TextMeshProUGUI m_TitleText;

    [SerializeField] private Transform m_UnitRoot;        // 말 종류가 깔리는 곳
    [SerializeField] private Image m_UnitSlotPrefabImage; // 슬롯 하나의 원본(비활성 상태로 씬에 둔다)

    [SerializeField] private StatBar[] m_StatBars;        // 평균 전력 / 분산 / 상한 / 학습 비용
    [SerializeField] private Transform m_HouseButtonRoot; // 선택지 5개가 가로로 놓이는 곳

    [SerializeField] private RawImage m_PreviewBackground; // 타이틀 배경(고르면 즉시 바뀐다)

    // 패널이 열린 동안 감출 타이틀 UI(로고·메뉴). 배경은 미리보기라 남겨둔다.
    [SerializeField] private GameObject[] m_HideOnOpen;

    // 타이틀에 서 있는 말 줄. 종족을 바꾸면 여기도 같이 갈린다.
    [SerializeField] private TitleUnitRow m_TitleUnitRow;

    // 0.45초 + OutCubic은 초반이 급해서 순간에 차버린다 — 차오르는 게 보이려면 이 정도는 필요하다
    [SerializeField] private float m_BarDuration = 0.9f;

    // 줄마다 늦게 시작해 위에서 아래로 순서가 읽히게 한다
    [SerializeField] private float m_BarStagger = 0.13f;
    [SerializeField] private int m_MaxUnitSlot = 8;

    private List<HouseRecord> m_ListHouse = new List<HouseRecord>();
    private List<Image> m_ListUnitSlot = new List<Image>();
    private List<UIHouseSelectButton> m_ListHouseButton = new List<UIHouseSelectButton>();
    private HouseRecord m_SelectedHouse;
    private Sequence m_BarSequence;

    public void Open()
    {
        if (m_Root == null)
            return;

        m_Root.SetActive(true);
        SetTitleUIVisible(false);

        if (m_ListHouse.Count <= 0)
            Build();

        // 저장된 선택을 되살린다. 없거나 그 종족이 아직 안 풀렸으면 첫 해금 종족으로 떨어진다.
        HouseRecord selected = PlayerManager.instance.GetSelectedHouseRecord();
        if (selected != null)
            Select(selected.Key);
    }

    public void Close()
    {
        KillBarSequence();
        SetTitleUIVisible(true);

        if (m_Root != null)
            m_Root.SetActive(false);
    }

    private void SetTitleUIVisible(bool _isVisible)
    {
        if (m_HideOnOpen == null)
            return;

        for (int i = 0; i < m_HideOnOpen.Length; ++i)
        {
            if (m_HideOnOpen[i] == null)
                continue;

            m_HideOnOpen[i].SetActive(_isVisible);
        }
    }

    public void OnClickCloseButton()
    {
        Close();
    }

    private void Build()
    {
        HouseTable houseTable = TableManager.instance.GetTable<HouseTable>();
        if (houseTable == null)
        {
            Logger.Error($"[UIHouseSelect] Build Failed! HouseTable not found");
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable != null && m_TitleText != null)
            m_TitleText.text = stringTable.GetString("HouseSelectTitle");

        m_ListHouse.Clear();
        m_ListHouse.AddRange(houseTable.list);

        BuildHouseButtons(stringTable);
        BuildStatBarLabels(stringTable);
    }

    private void BuildHouseButtons(StringTable _stringTable)
    {
        if (m_HouseButtonRoot == null)
            return;

        // 선택지는 씬에 미리 5개 놓아두고 여기서 데이터만 채운다 — 개수가 종족 수와 다르면 알려준다.
        m_ListHouseButton.Clear();
        m_HouseButtonRoot.GetComponentsInChildren<UIHouseSelectButton>(true, m_ListHouseButton);

        if (m_ListHouseButton.Count < m_ListHouse.Count)
        {
            Logger.Error($"[UIHouseSelect] 선택지 버튼이 모자란다 — 버튼 {m_ListHouseButton.Count} / 종족 {m_ListHouse.Count}");
        }

        for (int i = 0; i < m_ListHouseButton.Count; ++i)
        {
            if (i >= m_ListHouse.Count)
            {
                m_ListHouseButton[i].gameObject.SetActive(false);
                continue;
            }

            HouseRecord record = m_ListHouse[i];
            string displayName = record.NameKey;
            if (_stringTable != null)
                displayName = _stringTable.GetString(record.NameKey);

            // 해금 여부는 테이블만 보면 안 된다 — 플레이로 딴 해금은 PlayerData가 들고 있다.
            string lockedLabel = "";
            if (PlayerManager.instance.IsHouseUnlocked(record) == false && _stringTable != null)
                lockedLabel = _stringTable.GetString("HouseLocked");

            m_ListHouseButton[i].gameObject.SetActive(true);
            m_ListHouseButton[i].SetData(record, displayName, lockedLabel, OnClickHouse);
        }
    }

    private void BuildStatBarLabels(StringTable _stringTable)
    {
        if (m_StatBars == null)
            return;

        string[] axisKeys = new string[] { "AxisPower", "AxisVariance", "AxisCeiling", "AxisLearning" };
        for (int i = 0; i < m_StatBars.Length; ++i)
        {
            if (i >= axisKeys.Length)
                break;

            if (m_StatBars[i].NameText == null)
                continue;

            string label = axisKeys[i];
            if (_stringTable != null)
                label = _stringTable.GetString(axisKeys[i]);

            m_StatBars[i].NameText.text = label;
        }
    }

    private void OnClickHouse(HouseRecord _record)
    {
        if (_record == null)
            return;

        // 잠긴 종족도 눌러서 볼 수는 있게 둔다. 확정만 막으면 된다.
        Select(_record.Key);
    }

    private void Select(string _houseKey)
    {
        HouseRecord record = m_ListHouse.Find(house => house.Key == _houseKey);
        if (record == null)
        {
            Logger.Error($"[UIHouseSelect] Select Failed! HouseRecord not found - {_houseKey}");
            return;
        }

        m_SelectedHouse = record;

        // 확정 버튼이 없는 화면이라 고르는 즉시 저장한다. 잠긴 종족은 미리보기만 하고 저장하지 않는다.
        if (PlayerManager.instance.IsHouseUnlocked(record) == true)
            PlayerManager.instance.SetSelectedHouse(record.Key);

        for (int i = 0; i < m_ListHouseButton.Count; ++i)
        {
            m_ListHouseButton[i].SetSelected(m_ListHouseButton[i].houseKey == _houseKey);
        }

        ApplyUnits(record);
        ApplyBackground(record);
        PlayBarAnimation(record);

        if (m_TitleUnitRow != null)
            m_TitleUnitRow.Apply(record);
    }

    // 위쪽 — 그 종족의 말 종류를 깔아준다
    private void ApplyUnits(HouseRecord _record)
    {
        if (m_UnitRoot == null || m_UnitSlotPrefabImage == null)
            return;

        List<Sprite> listSprite = HouseSpriteLoader.Load(_record);

        int needCount = Mathf.Min(listSprite.Count, m_MaxUnitSlot);
        EnsureUnitSlot(needCount);

        for (int i = 0; i < m_ListUnitSlot.Count; ++i)
        {
            if (i >= needCount)
            {
                m_ListUnitSlot[i].gameObject.SetActive(false);
                continue;
            }

            m_ListUnitSlot[i].gameObject.SetActive(true);
            m_ListUnitSlot[i].sprite = listSprite[i];
            m_ListUnitSlot[i].SetNativeSize();
            m_ListUnitSlot[i].transform.SetSiblingIndex(i);
        }
    }

    private void EnsureUnitSlot(int _needCount)
    {
        while (m_ListUnitSlot.Count < _needCount)
        {
            Image slot = Instantiate(m_UnitSlotPrefabImage, m_UnitRoot);
            slot.gameObject.SetActive(true);
            m_ListUnitSlot.Add(slot);
        }
    }

    private void ApplyBackground(HouseRecord _record)
    {
        if (m_PreviewBackground == null)
            return;

        if (string.IsNullOrEmpty(_record.BackgroundPath) == true)
            return;

        Texture texture = ResUtil.Load<Texture>(_record.BackgroundPath);
        if (texture == null)
            return;

        m_PreviewBackground.texture = texture;
    }

    // 능력치 막대 — 0에서 목표치까지 차오른다. 줄마다 조금씩 늦게 시작해 순서가 읽히게 한다.
    private void PlayBarAnimation(HouseRecord _record)
    {
        if (m_StatBars == null)
            return;

        KillBarSequence();

        int[] values = new int[] { _record.AxisPower, _record.AxisVariance, _record.AxisCeiling, _record.AxisLearning };
        Color accent = ToColor(_record.AccentColor);

        m_BarSequence = DOTween.Sequence();
        m_BarSequence.SetUpdate(true);

        for (int i = 0; i < m_StatBars.Length; ++i)
        {
            if (i >= values.Length)
                break;

            StatBar bar = m_StatBars[i];
            if (bar.FillImage == null)
                continue;

            float target = Mathf.Clamp01(values[i] / 100f);
            bar.FillImage.color = accent;
            bar.FillImage.fillAmount = 0f;

            // OutQuart는 끝에서 부드럽게 멎어 "차오른다"가 눈에 남는다
            m_BarSequence.Insert(i * m_BarStagger, bar.FillImage.DOFillAmount(target, m_BarDuration).SetEase(Ease.OutQuart));

            if (bar.ValueText != null)
            {
                int shown = values[i];
                TextMeshProUGUI valueText = bar.ValueText;
                valueText.text = "0";

                // 숫자도 막대와 같은 이징으로 올려야 둘이 따로 노는 느낌이 안 난다
                m_BarSequence.Insert(i * m_BarStagger,
                    DOVirtual.Int(0, shown, m_BarDuration, value => valueText.text = value.ToString()).SetEase(Ease.OutQuart));
            }
        }
    }

    private void KillBarSequence()
    {
        if (m_BarSequence == null)
            return;

        m_BarSequence.Kill();
        m_BarSequence = null;
    }

    private Color ToColor(string _hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString($"#{_hex}", out color) == true)
            return color;

        Logger.Error($"[UIHouseSelect] AccentColor 파싱 실패 - {_hex}");
        return Color.white;
    }

    private void OnDestroy()
    {
        KillBarSequence();
    }
}
