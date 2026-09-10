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

    [SerializeField] private UIInGameBanner m_Banner;

    private const string STRING_KEY_BONUS_SPIN = "InGameBonusSpin";
    private const string STRING_KEY_BATTLE_VICTORY = "InGameBattleVictory";
    private const string STRING_KEY_BATTLE_DEFEAT = "InGameBattleDefeat";
    private const string STRING_KEY_YEAR_START = "InGameYearStart";

    // 스핀 흐름은 커맨드 큐로 돈다. 진행 여부는 이 큐가 스스로 답한다 —
    // 코루틴 핸들처럼 따로 들고 비워줄 상태가 없다.
    private FlowCommand m_SpinFlow = new FlowCommand();

    private bool m_isSpinning
    {
        get { return m_SpinFlow.IsFinished() == false; }
    }

    // 전투가 실제로 시작됐는지. UIInGameBattle.result는 초기값이 Running이라
    // 이 플래그 없이 isRunning만 보면 전투 전에도 Tick이 돌고,
    // CheckResult가 "적이 하나도 없다"를 승리로 읽어 유령 승리가 난다.
    private bool m_isBattleActive;

    // 런이 닫혔는가. 옥새 이중 지급과 종료 후 조작을 함께 막는다.
    private bool m_isRunEnded;

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
        m_Action.OnBuyExtraSpin += OnBuyExtraSpin;
    }

    public void OnClickSpinButton()
    {
        if (m_SlotMachine == null)
            return;

        if (m_isRunEnded == true)
            return;

        // 이미 돌고 있거나 전투 중이면 무시한다. 가드가 없던 동안엔 연타할 때마다
        // 코인이 깎이고(SpendSpinCoin이 먼저 돌았다) 돌던 릴이 리셋됐다.
        // 가드는 코인 차감보다 앞에 있어야 한다 — 뒤에 두면 무시된 입력에도 코인이 샌다.
        if (m_isSpinning == true)
            return;

        if (m_isBattleActive == true)
            return;

        // 스핀 1회에 코인 1개(GDD 03장). 코인이 떨어지면 굴리지 않는다 —
        // 추가 스핀 구매(골드 25)는 상점이 생긴 뒤에 여기서 갈라진다.
        if (m_RunData.SpendSpinCoin() == false)
            return;

        RefreshRunUI();

        StartSpinFlow();
    }

    // 전투 시작. 마지막 스핀의 판정 결과를 아군으로, 현재 연차·웨이브의 적을 상대로 세운다.
    private void OnBattleStart()
    {
        if (m_Battle == null)
        {
            Logger.Error("[InGameScene] OnBattleStart Failed! UIInGameBattle 미연결 (기대: 씬에서 직렬화 연결)");
            return;
        }

        if (m_isRunEnded == true)
            return;

        if (m_isBattleActive == true)
        {
            Logger.Log("[InGameScene] OnBattleStart - 이미 전투 중이라 무시한다 (기대: 전투 종료 후 재시작)");
            return;
        }

        if (m_isSpinning == true)
        {
            Logger.Log("[InGameScene] OnBattleStart - 릴이 도는 중이라 무시한다 (기대: 스핀 완료 후 전투)");
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
        m_isBattleActive = true;
    }

    private void Update()
    {
        // 스핀 흐름은 전투 여부와 무관하게 매 프레임 돌아야 한다.
        m_SpinFlow.Update();

        if (m_isBattleActive == false)
            return;

        m_Battle.Tick(Time.deltaTime * m_RunData.battleSpeed);

        if (m_Battle.isRunning == true)
            return;

        // 결과가 뒤집힌 그 프레임에 한 번만 마무리한다.
        m_isBattleActive = false;
        OnBattleFinished();
    }

    // 전투가 끝난 프레임에 한 번 불린다.
    //
    // 웨이브 진행 규칙은 「패배하면 같은 웨이브를 다시 한다」(기획 결정 2026-09-10, run-end-flow.html Q3-B).
    // 재도전에는 스핀 코인이 든다 — 그래서 코인이 곧 연차당 실패 허용 횟수이고,
    // 코인이 떨어지면 그 자리에서 런이 닫힌다. 안 그러면 진행도 종료도 안 되는 교착이 된다.
    private void OnBattleFinished()
    {
        eBattleResult result = m_Battle.result;
        int leakCount = m_Battle.homeHit;

        // 이 판정 결과는 소진됐다. 안 비우면 같은 병력으로 전투를 다시 걸 수 있다.
        m_LastJudgeResult = null;
        m_LastGrid = null;

        m_RunData.TakeHomeDamage(GetHomeDamage(result, leakCount));

        ShowBannerByKey((result == eBattleResult.Victory) ? STRING_KEY_BATTLE_VICTORY : STRING_KEY_BATTLE_DEFEAT);

        RefreshRunUI();

        if (m_RunData.homeHp <= 0)
        {
            EndRun(eRunEndReason.HomeFallen);
            return;
        }

        // 최종 연차의 마지막 웨이브는 넘어갈 다음 웨이브가 없다 — 승패와 무관하게 런이 닫힌다.
        bool isFinalWave = (m_RunData.year >= m_RunData.yearMax && m_RunData.waveIndex >= RunData.WAVE_PER_YEAR);
        if (isFinalWave == true && result == eBattleResult.Victory)
        {
            EndRun(eRunEndReason.Cleared);
            return;
        }

        if (result == eBattleResult.Victory)
            AdvanceToNextWave();

        // 코인이 떨어져도 골드로 살 수 있으면 아직 끝이 아니다 —
        // 살 수단까지 없어야 비로소 더 진행할 방법이 없는 것이다.
        if (m_RunData.spinCoin <= 0 && m_RunData.IsExtraSpinBuyable() == false)
            EndRun(eRunEndReason.OutOfSpinCoin);
    }

    // 패배의 대가 = 성문을 넘은 적 수 × PerLeak + (패배면) PerDefeat.
    // 고정분이 있어야 적이 한 마리도 안 넘고 아군만 전멸한 판도 대가를 치른다.
    private int GetHomeDamage(eBattleResult _result, int _leakCount)
    {
        GameConfigTable configTable = TableManager.instance.GetTable<GameConfigTable>();
        if (configTable == null)
        {
            Logger.Error("[InGameScene] GetHomeDamage Failed! GameConfigTable not found (기대: TableManager에 등록됨)");
            return _leakCount;
        }

        int damage = _leakCount * configTable.GetValue(GameConfigTable.KEY_HOME_DAMAGE_PER_LEAK, 1);

        if (_result == eBattleResult.Defeat)
            damage += configTable.GetValue(GameConfigTable.KEY_HOME_DAMAGE_PER_DEFEAT, 1);

        return damage;
    }

    private void AdvanceToNextWave()
    {
        bool isYearAdvanced = m_RunData.AdvanceWave();
        if (isYearAdvanced == false)
            return;

        // 연차가 넘어가며 스핀 코인·스왑이 회복됐다. 화면에 안 알리면
        // 코인이 왜 늘었는지 알 수 없다 — HUD 핍만으로는 놓치기 쉽다.
        RefreshRunUI();

        ShowYearStartBanner(m_RunData.year);

        // ▼ 외교 단계는 여기 들어온다(GDD 02장 STEP 05 / 09장 "3연차부터 개방").
        //   기획 스펙이 자리만 정해두고 구현은 별도 작업으로 남겼다.
    }

    // 런 종료. 옥새는 여기서 한 번만 지급된다 — 멱등 가드가 이중 지급을 막는다.
    private void EndRun(eRunEndReason _reason)
    {
        if (m_isRunEnded == true)
            return;

        m_isRunEnded = true;

        int royal = m_RunData.GetRoyalReward();
        PlayerManager.instance.AddRoyal(royal);

        Logger.Log("InGameScene", $"런 종료 - 사유 {_reason} / 도달 {m_RunData.year}/{m_RunData.yearMax}연차 / 옥새 +{royal}", Logger.eColor.Green);

        UIRunResult popup = UIManager.instance.Get<UIRunResult>();
        if (popup == null)
        {
            Logger.Error("[InGameScene] EndRun Failed! UIRunResult 생성 실패 (기대: UITable.csv에 행 + Resources/Prefabs/UI/UIRunResult 프리팹)");
            return;
        }

        popup.Apply(_reason, m_RunData.year, m_RunData.yearMax, royal);
    }

    // 연차 문구만 포맷 인자가 있다("{0}연차 시작").
    private void ShowYearStartBanner(int _year)
    {
        if (m_Banner == null)
            return;

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
        {
            Logger.Error($"[InGameScene] ShowYearStartBanner Failed! StringTable not found - {STRING_KEY_YEAR_START} (기대: TableManager에 등록됨)");
            return;
        }

        m_Banner.Show(stringTable.GetString(STRING_KEY_YEAR_START, _year));
    }

    // 런 상태를 바꾼 뒤엔 HUD와 ACTION을 항상 함께 갱신한다.
    // 골드는 HUD에만 뜨는 값이 아니다 — ACTION의 추가 스핀 버튼도 골드를 보고 활성/비활성을 정한다.
    // 예전엔 스핀 뒤 HUD만 갱신해서, 골드가 쌓여도 버튼이 계속 비활성으로 남아
    // 코인 0인데 살 수도 없는 교착이 났다(2026-09-10 QA).
    private void RefreshRunUI()
    {
        if (m_Hud != null)
            m_Hud.Refresh();

        if (m_Action != null)
            m_Action.Refresh();
    }

    private void ShowBannerByKey(string _key)
    {
        if (m_Banner == null)
            return;

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
        {
            Logger.Error($"[InGameScene] ShowBannerByKey Failed! StringTable not found - {_key} (기대: TableManager에 등록됨)");
            return;
        }

        m_Banner.Show(stringTable.GetString(_key));
    }

    // 골드로 스핀 코인을 산다(GDD 03장). 재도전 비용이 곧 이 골드다.
    private void OnBuyExtraSpin()
    {
        if (m_isRunEnded == true)
            return;

        if (m_RunData.BuyExtraSpin() == false)
            return;

        RefreshRunUI();
    }

    private void OnBattleSpeed()
    {
        m_RunData.ToggleBattleSpeed();

        RefreshRunUI();
    }

    // 스핀 흐름. 이 프로젝트의 관례대로 FlowCommand로 조립한다 —
    // SceneManager·TableManager·UIManager·릴 정착 트윈이 전부 같은 방식이다.
    //
    // 코루틴 대신 커맨드를 쓰는 이유:
    //  1. `Cancel()`이 인터페이스에 있어 중단 처리가 일원화된다.
    //     코루틴은 핸들을 들고 StopCoroutine을 부르고 그 핸들을 다시 비우는 걸 손으로 챙겨야 한다.
    //  2. 오브젝트가 꺼져도 조용히 죽지 않는다. 코루틴은 유니티가 강제로 멈추는데,
    //     그때 핸들이 남아 있으면 "아직 돌고 있다"고 오판하게 된다(실제로 겪은 결함이다).
    private void StartSpinFlow()
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

        // 릴이 멈춘 뒤 "판정에 실제로 걸린 칸"을 반짝이게 하려고 결과를 먼저 넘겨둔다.
        // 예전엔 슬롯머신이 자기 규칙("같은 심볼 3개")으로 반짝여서 7종족 중 6종족에서 거짓 신호였다.
        m_SlotMachine.SetJudgeResult(judgeResult);

        m_SpinFlow.Clear();

        // ① 굴리는 시간이 지나면 정지 신호를 보낸다.
        m_SpinFlow.Add(new Command_DeltaTime(m_SpinDuration, m_SlotMachine.StopAll));

        // ② 릴이 **실제로** 다 멈출 때까지 기다린다.
        //    ★ 고정 시간으로 기다리면 안 된다. 릴 감속은 프레임당 이동량 기반이라
        //      (`UISlotMachineReel.GetStopSpeed`) 정지 소요가 프레임레이트를 탄다 —
        //      52fps에서 1.00초, 23fps에서 1.69초로 측정됐다. 예전엔 여기가 고정 0.9초라
        //      두 경우 모두 릴보다 결과가 먼저 떴다(2026-09-10 QA 계측).
        m_SpinFlow.Add(new Command_WaitUntil(() => m_SlotMachine.isAllReelIdle));

        // ③ 멈춘 뒤에 결과를 드러낸다.
        m_SpinFlow.Add(new Command_Delegate(() => OnSpinSettled(judgeResult, grid)));
    }

    // 릴이 완전히 멈춘 프레임에 한 번 불린다.
    private void OnSpinSettled(JudgeResult _judgeResult, int[] _grid)
    {
        // 무엇이 성립했는지 크게 한 번 알린다. 요약 패널은 접혀 있어서 안 열면 안 보이는데,
        // 그러면 "왜 이만큼 소환됐지"를 배울 기회가 매 스핀 그냥 지나간다.
        // 무판정일 때는 띄우지 않는다 — 전력이 0인 스핀이 화면을 덮을 이유가 없다.
        if (_judgeResult != null && _judgeResult.Power > 0f && m_Banner != null)
            m_Banner.Show(_judgeResult.PatternName);

        m_LastJudgeResult = _judgeResult;
        m_LastGrid = _grid;

        // 당첨 배당. 소환과 별개로 골드가 나온다 — 전력이 소환 1기에 못 미치는 스핀도 빈손이 아니다.
        if (_judgeResult != null)
        {
            m_RunData.AwardGoldByPower(_judgeResult.Power);

            // 무료 스핀(윷·모). 코인을 먼저 돌려주고 그다음에 화면을 그린다 —
            // 순서가 뒤집히면 방금 돌아온 칸이 아직 비어 있는 상태로 강조된다.
            int bonusSpin = m_RunData.AddSpinCoin(_judgeResult.bonusSpin);

            RefreshRunUI();

            if (bonusSpin > 0)
                ShowBonusSpin();
        }

        if (m_Field != null && _judgeResult != null)
            m_Field.ShowSummon(_judgeResult, _grid, m_SlotMachine.spritePool);
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
