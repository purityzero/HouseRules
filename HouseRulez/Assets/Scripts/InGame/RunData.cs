using UnityEngine;

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

    // 추가 스핀 구매(GDD 03장). 연차당 횟수 제한이 있고 가격은 고정이다.
    private int m_ExtraSpinBought;
    private int m_ExtraSpinMax;
    private int m_ExtraSpinGoldCost;

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
    public int extraSpinBought => m_ExtraSpinBought;
    public int extraSpinMax => m_ExtraSpinMax;
    public int extraSpinGoldCost => m_ExtraSpinGoldCost;

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
        m_ExtraSpinMax = configTable.GetValue(GameConfigTable.KEY_EXTRA_SPIN_MAX_PER_YEAR, 2);
        m_ExtraSpinGoldCost = configTable.GetValue(GameConfigTable.KEY_EXTRA_SPIN_GOLD_COST, 25);

        m_HomeHp = m_HomeHpMax;
        m_Year = 1;
        m_WaveIndex = 1;
        m_Gold = configTable.GetValue(GameConfigTable.KEY_RUN_START_GOLD, 0)
            + PlayerManager.instance.GetRunConfigBonus(houseKey, GameConfigTable.KEY_RUN_START_GOLD);
        m_SpinCoin = m_SpinCoinMax;
        m_SwapCount = m_SwapCountMax;
        m_BetLevel = 0;
        m_BattleSpeed = 1;
        m_ExtraSpinBought = 0;
    }

    // 추가 스핀을 살 수 있는가. 연차당 횟수와 골드를 둘 다 본다.
    public bool IsExtraSpinBuyable()
    {
        if (m_ExtraSpinBought >= m_ExtraSpinMax)
            return false;

        return m_Gold >= m_ExtraSpinGoldCost;
    }

    // 골드로 스핀 코인 1개를 산다. 가격은 고정이다(GDD 03장).
    public bool BuyExtraSpin()
    {
        if (IsExtraSpinBuyable() == false)
            return false;

        m_Gold -= m_ExtraSpinGoldCost;
        m_SpinCoin++;
        m_ExtraSpinBought++;

        return true;
    }

    // 스핀 1회에 코인 1개. 판돈으로 코인을 쓰는 규칙(GDD 05장)은 판정기가 붙은 뒤에 이어진다.
    public bool SpendSpinCoin()
    {
        if (m_SpinCoin <= 0)
            return false;

        m_SpinCoin -= 1;
        return true;
    }

    // 무료 스핀. 실제로 채운 개수를 돌려준다(0이면 이미 가득 차 연출할 게 없다는 뜻).
    //
    // 최대치를 넘기지 않는다. 넘기면 HUD 핍이 spinCoinMax 개수만 만들어져 있어서
    // 초과분이 화면에서 그냥 사라진다 — 본거지 HP·스왑 칸에서 두 번 겪은 그 결함과 같은 함정이다.
    // 스핀은 항상 코인을 먼저 쓰므로 지급 시점엔 최대치 미만이라 실제로 상한이 걸릴 일은 드물다.
    public int AddSpinCoin(int _amount)
    {
        if (_amount <= 0)
            return 0;

        int before = m_SpinCoin;
        m_SpinCoin = Mathf.Min(m_SpinCoin + _amount, m_SpinCoinMax);
        return m_SpinCoin - before;
    }

    // 판정 전력에 비례한 배당을 골드로 지급하고, 지급액을 돌려준다(화면 연출용).
    // 전력이 소환 1기에 못 미쳐도 골드는 나온다 — 맞았는데 아무것도 안 남는 스핀을 없애는 통로다.
    public int AwardGoldByPower(float _power)
    {
        if (_power <= 0f)
            return 0;

        GameConfigTable configTable = TableManager.instance.GetTable<GameConfigTable>();
        if (configTable == null)
        {
            Logger.Error("[RunData] AwardGoldByPower Failed! GameConfigTable not found (기대: TableManager에 등록됨)");
            return 0;
        }

        int goldPerPower = configTable.GetValue(GameConfigTable.KEY_GOLD_PER_POWER, 2);
        int gold = Mathf.RoundToInt(_power * goldPerPower);

        // 전력이 조금이라도 있으면 최소 1골드는 준다. 반올림으로 0이 되면
        // "맞았는데 빈손"이 골드 쪽에서 다시 생긴다.
        if (gold <= 0)
            gold = 1;

        m_Gold += gold;
        return gold;
    }

    // 웨이브를 하나 끝냈다. 3개를 다 치르면 다음 연차로 넘어간다.
    // 반환값은 "연차가 넘어갔는가" — 호출부가 연차 전환 연출을 그 프레임에 띄우는 데 쓴다.
    public bool AdvanceWave()
    {
        m_WaveIndex++;
        if (m_WaveIndex <= WAVE_PER_YEAR)
            return false;

        m_WaveIndex = 1;
        m_Year++;

        // GDD 03장 "연차 시작 시 지급, 이월 없음" — 더하지 않고 최대치로 되돌린다.
        // 이 리셋이 없으면 1연차 스핀 코인을 다 쓴 채 2연차에 들어가 스핀 자체가 막힌다.
        m_SpinCoin = m_SpinCoinMax;
        m_SwapCount = m_SwapCountMax;

        // 추가 스핀 구매 횟수도 연차 단위다("연차당 2회까지").
        m_ExtraSpinBought = 0;

        return true;
    }

    // 본거지가 맞았다. 0이 되면 런이 끝난다(종료 판정은 InGameScene이 한다).
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
