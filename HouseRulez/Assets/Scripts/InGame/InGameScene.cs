using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TitleScene.cs와 동일한 이유로 DefaultExecutionOrder(-1000) 적용 —
// BaseScene.OnEnable()이 씬 내 다른 스크립트의 OnEnable()보다 먼저 실행되도록 강제한다.
[DefaultExecutionOrder(-1000)]
public class InGameScene : BaseScene
{
    [SerializeField] private UIHouseSlotMachine m_SlotMachine;
    [SerializeField] private Button m_SpinButton;
    [SerializeField] private TextMeshProUGUI m_SpinButtonText;
    [SerializeField] private RawImage m_BackgroundImage;
    [SerializeField] private UIInGameHud m_Hud;
    [SerializeField] private UIInGameAction m_Action;
    [SerializeField] private float m_SpinDuration = 1.5f; // 판정기가 아직 없어 임의로 굴리는 시간(전투/판정 붙으면 대체될 값)

    private Coroutine m_SpinRoutine;

    // 런 상태의 소유자는 이 씬이다. UI는 읽어서 그리기만 하고, 값을 바꾸는 건 전부 여기를 거친다.
    private RunData m_RunData = new RunData();

    protected override void OnSetup()
    {
        TableManager.instance.init();
        PlayerManager.instance.Load();

        if (m_SlotMachine == null)
        {
            Logger.Error($"[InGameScene] OnSetup Failed! UIHouseSlotMachine not linked");
            return;
        }

        HouseRecord record = PlayerManager.instance.GetSelectedHouseRecord();
        if (record == null)
        {
            Logger.Error($"[InGameScene] OnSetup Failed! GetSelectedHouseRecord == null");
            return;
        }

        m_RunData.Init();

        ApplyLocalizedText();

        m_SlotMachine.Apply(record);

        ApplyBackground(record);

        ApplyHud(record);
        ApplyAction();

        if (m_SpinButton != null)
            m_SpinButton.onClick.AddListener(OnClickSpinButton);
        else
            Logger.Error($"[InGameScene] OnSetup Failed! SpinButton not linked");
    }

    // HUD/ACTION 라벨은 각 UI가 스스로 채운다 — 여기서 다루는 건 어느 UI에도 안 속한 SPIN 버튼 하나다.
    private void ApplyLocalizedText()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
        {
            Logger.Error($"[InGameScene] ApplyLocalizedText Failed! StringTable not found");
            return;
        }

        SetText(m_SpinButtonText, stringTable.GetString("ReelSpin"));
    }

    private void SetText(TextMeshProUGUI _text, string _value)
    {
        if (_text == null)
            return;

        _text.text = _value;
    }

    private void ApplyHud(HouseRecord _record)
    {
        if (m_Hud == null)
        {
            Logger.Error($"[InGameScene] ApplyHud Failed! UIInGameHud not linked");
            return;
        }

        m_Hud.Apply(m_RunData, _record);
    }

    private void ApplyAction()
    {
        if (m_Action == null)
        {
            Logger.Error($"[InGameScene] ApplyAction Failed! UIInGameAction not linked");
            return;
        }

        m_Action.Apply(m_RunData);

        m_Action.OnBattleStart += OnBattleStart;
        m_Action.OnBattleSpeed += OnBattleSpeed;
    }

    public void OnClickSpinButton()
    {
        if (m_SlotMachine == null)
            return;

        // 스핀 1회에 코인 1개(GDD 03장). 코인이 떨어지면 굴리지 않는다 —
        // 추가 스핀 구매(골드 25)는 상점이 생긴 뒤에 여기서 갈라진다.
        if (m_RunData.SpendSpinCoin() == false)
            return;

        if (m_Hud != null)
            m_Hud.Refresh();

        if (m_SpinRoutine != null)
            StopCoroutine(m_SpinRoutine);

        m_SpinRoutine = StartCoroutine(CoSpinAndStop());
    }

    // TODO: 배치/전투 단계가 아직 없다. 단계가 생기면 여기서 넘긴다.
    private void OnBattleStart()
    {
        Logger.Log($"[InGameScene] OnBattleStart - 전투 단계 미구현");
    }

    private void OnBattleSpeed()
    {
        m_RunData.ToggleBattleSpeed();

        if (m_Action != null)
            m_Action.Refresh();
    }

    private IEnumerator CoSpinAndStop()
    {
        m_SlotMachine.Spin();

        yield return new WaitForSeconds(m_SpinDuration);

        m_SlotMachine.StopAll();
        m_SpinRoutine = null;
    }

    private void ApplyBackground(HouseRecord _record)
    {
        if (m_BackgroundImage == null)
            return;

        if (string.IsNullOrEmpty(_record.BackgroundPath) == true)
            return;

        Texture texture = ResUtil.Load<Texture>(_record.BackgroundPath);
        if (texture == null)
            return;

        m_BackgroundImage.texture = texture;
    }
}
