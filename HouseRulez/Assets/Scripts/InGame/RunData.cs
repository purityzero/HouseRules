// 런(run) 한 판 동안만 사는 상태. 저장하지 않는다 —
// PlayerData가 못 박아둔 대로 재화/코어/각인은 런 밖으로 나가지 않으므로 저장 계층에 넣지 않는다.
// 초기값은 전부 GameConfigTable에서 온다. 여기에 숫자를 박으면 CSV만으로 튜닝할 수 없게 된다.
public class RunData
{
    public const int WAVE_PER_YEAR = 3;

    private int m_HomeHp;
    private int m_HomeHpMax;
    private int m_Year;
    private int m_WaveIndex;
    private int m_YearMax;
    private int m_Gold;
    private int m_SpinCoin;
    private int m_SpinCoinMax;
    private int m_SwapCount;
    private int m_SwapCountMax;
    private int m_BetLevel;
    private int m_BattleSpeed;
    private int m_BattleSpeedFast;

    public int homeHp => m_HomeHp;
    public int homeHpMax => m_HomeHpMax;
    public int year => m_Year;

    // 연차당 웨이브 3개. 스핀 코인 1개가 웨이브 1개다.
    public int waveIndex => m_WaveIndex;
    public int yearMax => m_YearMax;
    public int gold => m_Gold;
    public int spinCoin => m_SpinCoin;
    public int spinCoinMax => m_SpinCoinMax;
    public int swapCount => m_SwapCount;
    public int swapCountMax => m_SwapCountMax;
    public int betLevel => m_BetLevel;
    public int battleSpeed => m_BattleSpeed;

    public void Init()
    {
        GameConfigTable configTable = TableManager.instance.GetTable<GameConfigTable>();
        if (configTable == null)
        {
            Logger.Error("[RunData] Init Failed! GameConfigTable not found");
            return;
        }

        // 영구 메타는 런이 시작될 때 한 번만 스냅샷으로 반영한다. 런 도중 타이틀에서 산 것이
        // 진행 중인 런에 소급되지 않도록, 여기 말고 다른 곳에서 다시 더하지 않는다.
        HouseRecord selectedHouse = PlayerManager.instance.GetSelectedHouseRecord();
        string houseKey = (selectedHouse != null) ? selectedHouse.Key : string.Empty;

        m_HomeHpMax = configTable.GetValue(GameConfigTable.KEY_HOME_HP_MAX, 8)
            + PlayerManager.instance.GetRunConfigBonus(houseKey, GameConfigTable.KEY_HOME_HP_MAX);
        m_YearMax = configTable.GetValue(GameConfigTable.KEY_RUN_YEAR_MAX, 12);
        m_SpinCoinMax = configTable.GetValue(GameConfigTable.KEY_SPIN_COIN_PER_YEAR, 3)
            + PlayerManager.instance.GetRunConfigBonus(houseKey, GameConfigTable.KEY_SPIN_COIN_PER_YEAR);
        m_SwapCountMax = configTable.GetValue(GameConfigTable.KEY_SWAP_COUNT_PER_YEAR, 2)
            + PlayerManager.instance.GetRunConfigBonus(houseKey, GameConfigTable.KEY_SWAP_COUNT_PER_YEAR);
        m_BattleSpeedFast = configTable.GetValue(GameConfigTable.KEY_BATTLE_SPEED_FAST, 2);

        m_HomeHp = m_HomeHpMax;
        m_Year = 1;
        m_WaveIndex = 1;
        m_Gold = configTable.GetValue(GameConfigTable.KEY_RUN_START_GOLD, 0)
            + PlayerManager.instance.GetRunConfigBonus(houseKey, GameConfigTable.KEY_RUN_START_GOLD);
        m_SpinCoin = m_SpinCoinMax;
        m_SwapCount = m_SwapCountMax;
        m_BetLevel = 0;
        m_BattleSpeed = 1;
    }

    // 스핀 1회에 코인 1개. 판돈으로 코인을 쓰는 규칙(GDD 05장)은 판정기가 붙은 뒤에 이어진다.
    public bool SpendSpinCoin()
    {
        if (m_SpinCoin <= 0)
            return false;

        m_SpinCoin -= 1;
        return true;
    }

    // 웨이브를 하나 끝냈다. 3개를 다 치르면 다음 연차로 넘어간다.
    // TODO: 연차 전환 시 스핀 코인·스왑 회복과 상점/외교 단계는 그 단계가 생길 때 여기에 붙인다.
    public void AdvanceWave()
    {
        m_WaveIndex++;
        if (m_WaveIndex <= WAVE_PER_YEAR)
            return;

        m_WaveIndex = 1;
        m_Year++;
    }

    // 본거지가 맞았다. 0이 되면 런이 끝난다(종료 처리는 아직 없다).
    public void TakeHomeDamage(int _amount)
    {
        if (_amount <= 0)
            return;

        m_HomeHp -= _amount;
        if (m_HomeHp < 0)
            m_HomeHp = 0;
    }

    // 런 종료 시 줄 옥새. 패배해도 도달 연차만큼은 지급하고, 완주하면 보너스가 붙는다.
    // TODO: 런 종료 단계가 아직 없어 호출부가 없다. 종료 처리가 생기면 여기 결과를 PlayerManager.AddRoyal()로 넘긴다.
    public int GetRoyalReward()
    {
        GameConfigTable configTable = TableManager.instance.GetTable<GameConfigTable>();
        if (configTable == null)
        {
            Logger.Error("[RunData] GetRoyalReward Failed! GameConfigTable not found");
            return 0;
        }

        int reward = m_Year * configTable.GetValue(GameConfigTable.KEY_ROYAL_PER_YEAR, 1);

        if (m_Year >= m_YearMax)
            reward += configTable.GetValue(GameConfigTable.KEY_ROYAL_CLEAR_BONUS, 6);

        return reward;
    }

    // 배속은 ×1과 설정된 빠른 배속 두 단계만 오간다.
    public void ToggleBattleSpeed()
    {
        if (m_BattleSpeed >= m_BattleSpeedFast)
        {
            m_BattleSpeed = 1;
            return;
        }

        m_BattleSpeed = m_BattleSpeedFast;
    }
}
