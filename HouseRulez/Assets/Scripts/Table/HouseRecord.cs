using System.Collections.Generic;
using UnityEngine;

// 종족(House) 한 줄. 수치는 전부 GDD에서 온 값이라 코드에 박지 않고 이 테이블이 들고 있는다.
// Id 순서가 곧 해금 순서이자 화면 표시 순서다(체스 -> 장기 -> 화투 -> 포커 -> 마작, 학습 난이도 순).
public class HouseRecord : Record
{
    public string Key;
    public string NameKey;
    public string AccentColor;
    public int PoolCount;

    // 종족 선택 화면의 능력치 막대(0~100). GDD 04장의 축 값 그대로.
    public int AxisPower;
    public int AxisVariance;
    public int AxisCeiling;
    public int AxisLearning;

    public string SpriteFolder;
    public string BackgroundPath;
    public int isUnlocked;
}

public class HouseTable : Table<HouseRecord>
{
    public HouseTable(List<HouseRecord> _listRecord) : base(_listRecord) { }

    public HouseRecord GetRecordByKey(string _key)
    {
        return list.Find(record => record.Key == _key);
    }
}

// 종족 말 스프라이트 로딩. 종족 선택 화면과 타이틀 유닛 줄이 같은 규칙으로 읽어야 해서 한곳에 모은다.
public static class HouseSpriteLoader
{
    // 로드할 때 제외할 스프라이트 이름 접미사. _x8(미리 확대해둔 사본), _blur(슬롯 릴 회전용)는 "말 종류"가 아니다.
    private static readonly string[] EXCLUDE_SUFFIXES = { "_x8", "_blur" };

    public static List<Sprite> Load(HouseRecord _record)
    {
        List<Sprite> listSprite = new List<Sprite>();
        if (_record == null)
            return listSprite;

        if (string.IsNullOrEmpty(_record.SpriteFolder) == true)
            return listSprite;

        Sprite[] loaded = Resources.LoadAll<Sprite>($"Image/InGame/Actor/{_record.SpriteFolder}");
        for (int i = 0; i < loaded.Length; ++i)
        {
            bool shouldExclude = false;
            for (int j = 0; j < EXCLUDE_SUFFIXES.Length; ++j)
            {
                if (loaded[i].name.Contains(EXCLUDE_SUFFIXES[j]) == true)
                {
                    shouldExclude = true;
                    break;
                }
            }

            if (shouldExclude == true)
                continue;

            listSprite.Add(loaded[i]);
        }

        return listSprite;
    }

    // 중복 없이 _count개를 뽑는다. 풀이 모자라면 있는 만큼만 준다.
    public static List<Sprite> LoadRandom(HouseRecord _record, int _count)
    {
        List<Sprite> pool = Load(_record);
        List<Sprite> picked = new List<Sprite>();

        while (picked.Count < _count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked;
    }
}
