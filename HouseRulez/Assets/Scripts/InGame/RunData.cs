// 런(run) 한 판 동안만 사는 상태. 저장하지 않는다 —
// PlayerData가 못 박아둔 대로 재화/코어/각인은 런 밖으로 나가지 않으므로 저장 계층에 넣지 않는다.
// 초기값은 전부 GameConfigTable에서 온다. 여기에 숫자를 박으면 CSV만으로 튜닝할 수 없게 된다.
public class RunData
{
    private int m_HomeHp;
    private int m_HomeHpMax;
    private int m_Year;
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

        m_HomeHpMax = configTable.GetValue(GameConfigTable.KEY_HOME_HP_MAX, 8);
        m_YearMax = configTable.GetValue(GameConfigTable.KEY_RUN_YEAR_MAX, 12);
        m_SpinCoinMax = configTable.GetValue(GameConfigTable.KEY_SPIN_COIN_PER_YEAR, 3);
        m_SwapCountMax = configTable.GetValue(GameConfigTable.KEY_SWAP_COUNT_PER_YEAR, 2);
        m_BattleSpeedFast = configTable.GetValue(GameConfigTable.KEY_BATTLE_SPEED_FAST, 2);

        m_HomeHp = m_HomeHpMax;
        m_Year = 1;
        m_Gold = configTable.GetValue(GameConfigTable.KEY_RUN_START_GOLD, 0);
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
