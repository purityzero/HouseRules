using System.Collections.Generic;

// 단일 스칼라 튜닝값을 담는 키-값 테이블. 밸런스 수치를 코드 상수로 박지 않기 위한 통로다.
// 레코드 1개에 자연스럽게 붙는 값(종족별 속성 등)은 여기가 아니라 해당 테이블의 컬럼으로 간다.
public class GameConfigRecord : Record
{
    public string Key;
    public int Value;
}

public class GameConfigTable : Table<GameConfigRecord>
{
    // 런(run) 상태 초기값 키. 문자열을 호출부마다 다시 적으면 오타가 조용히 기본값으로 흘러간다.
    public const string KEY_HOME_HP_MAX = "HomeHpMax";
    public const string KEY_RUN_YEAR_MAX = "RunYearMax";
    public const string KEY_RUN_START_GOLD = "RunStartGold";
    public const string KEY_SPIN_COIN_PER_YEAR = "SpinCoinPerYear";
    public const string KEY_SWAP_COUNT_PER_YEAR = "SwapCountPerYear";
    public const string KEY_BATTLE_SPEED_FAST = "BattleSpeedFast";

    // 영구 재화(옥새) 지급 규칙. 런 종료 시 도달 연차에 비례하고, 완주하면 보너스가 붙는다.
    public const string KEY_ROYAL_PER_YEAR = "RoyalPerYear";
    public const string KEY_ROYAL_CLEAR_BONUS = "RoyalClearBonus";

    // 웨이브 적 전력의 기준값. 플레이어가 스핀 1회로 소환하는 평균 유닛 수(6기)에 맞춰 두었다 —
    // 이 값이 1연차 1웨이브의 난이도를 정하고, 나머지는 WaveTable의 계수가 배수로 올린다.
    public const string KEY_WAVE_BASE_POWER = "WaveBasePower";

    public GameConfigTable(List<GameConfigRecord> _listRecord) : base(_listRecord) { }

    // 키가 없으면 기본값으로 조용히 흘러가지 않도록 에러를 남긴다 —
    // CSV 헤더와 코드가 어긋나면 값이 항상 기본값이 되는데, 로그가 없으면 계산 로직부터 의심하게 된다.
    public int GetValue(string _key, int _defaultValue)
    {
        GameConfigRecord record = list.Find(config => config.Key == _key);
        if (record == null)
        {
            Logger.Error($"[GameConfigTable] GetValue Failed! key not found - {_key}");
            return _defaultValue;
        }

        return record.Value;
    }
}
