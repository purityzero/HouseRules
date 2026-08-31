using System.Collections.Generic;
using UnityEngine;

// 이번 웨이브의 적을 "어느 종족 심볼로 그릴지" 정한다.
//
// GDD 06장: 고유 덱을 가진 복수의 세력이 서로를 침공한다. 그래서 적은 몬스터가 아니라 다른 종족이다.
// 다만 **스탯은 EnemyTable이 계속 담당한다** — GDD가 "종족 = 유닛 성능이 아니라 3×3을 읽는 문법"이라고
// 못 박았으므로, 적 종족도 성능이 아니라 겉모습과 서사만 바꾼다.
// 여기서 종족을 성능에 얹으면 WaveTable의 PowerCoef → 마릿수 역산 계약이 깨진다.
public static class EnemyHouseResolver
{
    // 내 종족이 적으로 나오지 않게 밀어낼 때 몇 번까지 시도할지.
    // 종족 수만큼 돌면 반드시 다른 종족을 찾는다(전 종족이 하나뿐인 경우는 없다).
    private const int MAX_SHIFT = 8;

    // 이 웨이브가 실제로 쓸 종족 레코드. 중립이면 null이다.
    // 표에 적힌 종족이 내 종족이면 다음 종족으로 민다 — 내 말이 적으로 나오면 적아 구분이 무너진다.
    public static HouseRecord Resolve(WaveRecord _wave)
    {
        if (_wave == null)
            return null;

        if (string.IsNullOrEmpty(_wave.EnemyHouse) == true)
            return null;

        if (_wave.EnemyHouse == WaveRecord.HOUSE_NEUTRAL)
            return null;

        HouseTable houseTable = TableManager.instance.GetTable<HouseTable>();
        if (houseTable == null)
        {
            Logger.Error("[EnemyHouseResolver] Resolve Failed! HouseTable not found (기대: TableManager에 등록됨)");
            return null;
        }

        int index = houseTable.list.FindIndex(house => house != null && house.Key == _wave.EnemyHouse);
        if (index < 0)
        {
            Logger.Error($"[EnemyHouseResolver] Resolve Failed! 종족 없음 - {_wave.EnemyHouse} (기대: HouseTable.csv에 해당 행)");
            return null;
        }

        HouseRecord selected = PlayerManager.instance.GetSelectedHouseRecord();
        string playerKey = (selected != null) ? selected.Key : string.Empty;

        for (int shift = 0; shift < MAX_SHIFT; ++shift)
        {
            HouseRecord candidate = houseTable.list[(index + shift) % houseTable.list.Count];
            if (candidate == null)
                continue;

            if (candidate.Key == playerKey)
                continue;

            return candidate;
        }

        Logger.Error($"[EnemyHouseResolver] Resolve Failed! 내 종족을 피할 상대를 못 찾았다 - 내 종족 {playerKey} (기대: 종족 2개 이상)");
        return null;
    }

    // 세울 적의 스프라이트 목록. 중립 웨이브는 종족색 없는 무리(Enemy/enemy_*)를 한 장만 쓴다.
    public static List<Sprite> LoadPool(WaveRecord _wave, EnemyRecord _enemy)
    {
        List<Sprite> listSprite = new List<Sprite>();

        HouseRecord house = Resolve(_wave);
        if (house != null)
            return HouseSpriteLoader.LoadEnemy(house);

        // 중립 — 1연차처럼 아직 어느 종족과도 전쟁하지 않은 구간이다.
        if (_enemy == null)
            return listSprite;

        Sprite sprite = ResUtil.Load<Sprite>(_enemy.SpritePath);
        if (sprite != null)
            listSprite.Add(sprite);

        return listSprite;
    }

    // 보스 웨이브면 그 종족의 최상위 말 인덱스, 아니면 -1(무작위로 섞으라는 뜻).
    public static int GetBossSymbolIndex(WaveRecord _wave)
    {
        if (_wave == null)
            return -1;

        if (_wave.WaveType != WaveRecord.TYPE_JUDGEMENT && _wave.WaveType != WaveRecord.TYPE_FINAL)
            return -1;

        HouseRecord house = Resolve(_wave);
        if (house == null)
            return -1;

        return house.BossSymbolIndex;
    }

    // 침공 예고용 — 그 연차의 상대 종족과 보스 심볼을 한 번에 알려준다.
    // 연차 안의 세 웨이브는 같은 종족이라 첫 웨이브만 봐도 된다.
    public static HouseRecord GetYearEnemyHouse(int _year)
    {
        WaveTable waveTable = TableManager.instance.GetTable<WaveTable>();
        if (waveTable == null)
        {
            Logger.Error("[EnemyHouseResolver] GetYearEnemyHouse Failed! WaveTable not found (기대: TableManager에 등록됨)");
            return null;
        }

        return Resolve(waveTable.GetRecord(_year, 1));
    }
}
