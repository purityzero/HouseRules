using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인게임 하단 액션 바. 기획서 §10 ScreenZones의 ACTION 영역(네이티브 144,250 - 492,284)을 x3한 1044x102다.
// 전투 시작은 이 클래스가 처리하지 않고 이벤트로 올린다 — 전투 단계가 아직 없어 받을 쪽이 정해지지 않았다.
//
// 2026-09-13에 스왑 표시(핍 · 라벨 · 폭 보정)를 걷어냈다. 스왑은 **끝내 구현되지 않은 기능**이고
// (RunData 에 카운터가, 여기에 핍이 있었을 뿐 소비 경로가 없었다) 드래그 배치가 그 역할을 대신한다.
// 함께 사라진 것: 칸 수에 맞춰 루트 폭과 라벨 위치를 다시 계산하던 코드와 그 폭 여유 결함.
public class UIInGameAction : MonoBehaviour
{
    [SerializeField] private Button m_BattleStartButton;

    [SerializeField] private Button m_BattleSpeedButton;
    [SerializeField] private TextMeshProUGUI m_BattleSpeedText;

    // 추가 스핀 구매(GDD 03장 — 골드로 +1, 연차당 상한까지, 가격 고정).
    // 상한은 업그레이드로 올라간다(2026-09-13, 스왑 노드를 이 효과로 전용).
    [SerializeField] private Button m_ExtraSpinButton;
    [SerializeField] private TextMeshProUGUI m_ExtraSpinText;

    private RunData m_RunData;

    public event Action OnBattleStart;
    public event Action OnBattleSpeed;
    public event Action OnBuyExtraSpin;

    private void Awake()
    {
        if (m_BattleStartButton != null)
            m_BattleStartButton.onClick.AddListener(OnClickBattleStartButton);

        if (m_BattleSpeedButton != null)
            m_BattleSpeedButton.onClick.AddListener(OnClickBattleSpeedButton);

        if (m_ExtraSpinButton != null)
            m_ExtraSpinButton.onClick.AddListener(OnClickExtraSpinButton);
    }

    public void Apply(RunData _runData)
    {
        if (_runData == null)
        {
            Logger.Error("[UIInGameAction] Apply Failed! runData == null");
            return;
        }

        m_RunData = _runData;

        Refresh();
    }

    public void Refresh()
    {
        if (m_RunData == null)
            return;

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        SetText(m_BattleSpeedText, stringTable.GetString("ActionBattleSpeed", m_RunData.battleSpeed));

        RefreshExtraSpin(stringTable);
    }

    // 남은 구매 횟수와 가격을 같이 보여준다 — 가격만 있으면 "몇 번 더 살 수 있는지"를 알 수 없다.
    // 살 수 없는 상태(횟수 소진 / 골드 부족)는 버튼을 꺼서 알린다.
    private void RefreshExtraSpin(StringTable _stringTable)
    {
        int remain = m_RunData.extraSpinMax - m_RunData.extraSpinBought;
        if (remain < 0)
            remain = 0;

        SetText(m_ExtraSpinText, _stringTable.GetString("ActionExtraSpin", m_RunData.extraSpinGoldCost, remain));

        if (m_ExtraSpinButton != null)
            m_ExtraSpinButton.interactable = m_RunData.IsExtraSpinBuyable();
    }

    public void OnClickBattleStartButton()
    {
        OnBattleStart?.Invoke();
    }

    public void OnClickExtraSpinButton()
    {
        OnBuyExtraSpin?.Invoke();
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
