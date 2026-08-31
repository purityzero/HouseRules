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
    [SerializeField] private RawImage m_BackgroundImage;
    [SerializeField] private UIInGameHud m_Hud;
    [SerializeField] private UIInGameAction m_Action;
    [SerializeField] private float m_SpinDuration = 1.5f; // 판정기가 아직 없어 임의로 굴리는 시간(전투/판정 붙으면 대체될 값)

    [SerializeField] private UIInGameField m_Field;
    [SerializeField] private UIInGameBattle m_Battle;

    // 릴 3개가 순차 정지를 끝낼 때까지 기다렸다가 소환을 띄운다.
    [SerializeField] private float m_SummonDelay = 0.9f;

    [SerializeField] private UIInGameBanner m_Banner;

    private const string STRING_KEY_BONUS_SPIN = "InGameBonusSpin";

    private Coroutine m_SpinRoutine;

    // 전투 시작이 마지막 스핀 결과를 쓴다. 스핀을 안 돌렸으면 전투가 성립하지 않는다.
    private JudgeResult m_LastJudgeResult;
    private int[] m_LastGrid;

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

        // 정적 라벨은 UIText가 스스로 채운다. 최초 1회만 여기서 돌린다.
        UIText.RefreshAll();

        m_SlotMachine.Apply(record);

        if (m_Field != null)
            m_Field.Apply();

        ApplyBackground(record);

        ApplyHud(record);
        ApplyAction();

        if (m_SpinButton != null)
            m_SpinButton.onClick.AddListener(OnClickSpinButton);
        else
            Logger.Error($"[InGameScene] OnSetup Failed! SpinButton not linked");
    }

    // HUD/ACTION 라벨은 각 UI가 스스로 채운다 — 여기서 다루는 건 어느 UI에도 안 속한 SPIN 버튼 하나다.
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

    // 전투 시작. 마지막 스핀의 판정 결과를 아군으로, 현재 연차·웨이브의 적을 상대로 세운다.
    private void OnBattleStart()
    {
        if (m_Battle == null)
        {
            Logger.Error("[InGameScene] OnBattleStart Failed! UIInGameBattle 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        if (m_LastJudgeResult == null || m_LastGrid == null)
        {
            Logger.Log("[InGameScene] OnBattleStart - 스핀 결과가 없어 전투를 시작하지 않는다 (기대: SPIN 선행)");
            return;
        }

        WaveTable waveTable = TableManager.instance.GetTable<WaveTable>();
        if (waveTable == null)
        {
            Logger.Error("[InGameScene] OnBattleStart Failed! WaveTable not found");
            return;
        }

        WaveRecord wave = waveTable.GetRecord(m_RunData.year, m_RunData.waveIndex);
        if (wave == null)
        {
            Logger.Error($"[InGameScene] OnBattleStart Failed! 웨이브 없음 - 연차 {m_RunData.year} 웨이브 {m_RunData.waveIndex} (기대: WaveTable.csv에 해당 행)");
            return;
        }

        // 소환 표시는 전투 유닛이 대신하므로 겹쳐 보이지 않게 지운다.
        if (m_Field != null)
            m_Field.Clear();

        m_Battle.Begin(m_LastJudgeResult, m_LastGrid, m_SlotMachine.spritePool, wave);
    }

    private void Update()
    {
        if (m_Battle == null || m_Battle.isRunning == false)
            return;

        m_Battle.Tick(Time.deltaTime * m_RunData.battleSpeed);
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

        if (m_Field != null)
            m_Field.Clear();

        // 결과를 먼저 만들고 그 값 하나로 판정과 릴을 모두 돌린다.
        // 릴이 스스로 무작위를 굴리게 두면 화면에 보이는 3×3과 판정한 3×3이 달라진다.
        HouseRecord record = PlayerManager.instance.GetSelectedHouseRecord();
        int[] grid = m_SlotMachine.CreateRandomGrid();
        m_SlotMachine.SetResultByGrid(grid);

        JudgeResult judgeResult = (record != null) ? Judge.Evaluate(record.Key, grid) : null;

        yield return new WaitForSeconds(m_SpinDuration);

        m_SlotMachine.StopAll();

        // 릴이 순차 정지를 마치는 동안 기다렸다가 소환을 보여준다 —
        // 정지 전에 띄우면 아직 돌고 있는 릴의 결과를 미리 알려주는 꼴이 된다.
        yield return new WaitForSeconds(m_SummonDelay);

        m_LastJudgeResult = judgeResult;
        m_LastGrid = grid;

        // 당첨 배당. 소환과 별개로 골드가 나온다 — 전력이 소환 1기에 못 미치는 스핀도 빈손이 아니다.
        if (judgeResult != null)
        {
            m_RunData.AwardGoldByPower(judgeResult.Power);

            // 무료 스핀(윷·모). 코인을 먼저 돌려주고 그다음에 화면을 그린다 —
            // 순서가 뒤집히면 방금 돌아온 칸이 아직 비어 있는 상태로 강조된다.
            int bonusSpin = m_RunData.AddSpinCoin(judgeResult.bonusSpin);

            if (m_Hud != null)
                m_Hud.Refresh();

            if (bonusSpin > 0)
                ShowBonusSpin();
        }

        if (m_Field != null && judgeResult != null)
            m_Field.ShowSummon(judgeResult, grid, m_SlotMachine.spritePool);

        m_SpinRoutine = null;
    }

    // 무료 스핀을 화면에 알린다. 코인 칸이 하나 돌아오는 게 전부라 그냥 두면 눈에 안 띈다.
    // 두 겹으로 알린다 — 화면 중앙 배너가 말해주고, 돌아온 칸이 튀어 시선을 HUD로 끈다.
    //
    // Glory의 UIManager.ShowToast를 쓰려 했으나 못 쓴다:
    // 로드 대상 프리팹 Resources/Prefabs/UI/UIToastMessage가 프로젝트에 없어 Pop()이 null을 돌려주고
    // 조용히 반환한다(2026-08-31 QA에서 활성 토스트 0개로 확인). UNFINISHED에 별도 항목으로 남겼다.
    private void ShowBonusSpin()
    {
        if (m_Hud != null)
            m_Hud.PlaySpinCoinBonus();

        if (m_Banner == null)
        {
            Logger.Error("[InGameScene] ShowBonusSpin Failed! UIInGameBanner 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
        {
            Logger.Error("[InGameScene] ShowBonusSpin Failed! StringTable not found (기대: TableManager에 등록됨)");
            return;
        }

        m_Banner.Show(stringTable.GetString(STRING_KEY_BONUS_SPIN));
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
