using System.Collections.Generic;
using UnityEngine;

// 종족(House) 한 줄. 수치는 전부 GDD에서 온 값이라 코드에 박지 않고 이 테이블이 들고 있는다.
// Id 순서가 곧 해금 순서이자 화면 표시 순서다(슬롯 -> 체스 -> 장기 -> 화투 -> 포커 -> 마작, 학습 난이도 순).
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

    // 판돈(배팅)을 쓰는 종족인가. GDD 05장 기준 화투(섯다)만 해당한다 —
    // 종족 키를 코드에서 직접 비교하지 않으려고 레코드 속성으로 둔다.
    public int isUseBet;

    // 홀드(재굴림 전 보류) 가능 칸 수 상한. 기본값 4, 슬롯만 2(GDD 밖 종족 전용 밸런스 조정, 2026-08-27 승인).
    // 현재 이 값을 소비하는 홀드 로직은 없다(슬롯 판정기 미구현) — 컬럼/필드만 선반영.
    public int HoldMax;

    // 이 종족이 적으로 나올 때 보스가 쓸 심볼의 풀 인덱스(이름 오름차순 로드 순서).
    // 각 종족의 최상위 말이다 — 체스 king, 장기 wang, 포커 K, 화투 비광, 마작 9통, 슬롯 세븐, 윷 모.
    public int BossSymbolIndex;
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
    public const string BLUR_SUFFIX = "_blur";

    // 로드할 때 제외할 스프라이트 이름 접미사. _x8(미리 확대해둔 사본), _blur(슬롯 릴 회전용)는 "말 종류"가 아니다.
    private static readonly string[] EXCLUDE_SUFFIXES = { "_x8", BLUR_SUFFIX };

    public static List<Sprite> Load(HouseRecord _record)
    {
        if (_record == null)
            return new List<Sprite>();

        return LoadFolder($"Image/InGame/Actor/{_record.SpriteFolder}");
    }

    // 적으로 나올 때 쓸 사본. 같은 그림을 적 팔레트로 치환한 것이라(GDD 10장 "검정은 적군 전용")
    // 폴더 구조와 파일 이름이 Actor와 같고, 그래서 **인덱스가 1:1로 대응한다.**
    // 아군 3번 말과 적 3번 말이 같은 그림이라는 뜻이다.
    public static List<Sprite> LoadEnemy(HouseRecord _record)
    {
        if (_record == null)
            return new List<Sprite>();

        return LoadFolder($"Image/InGame/Enemy/{_record.SpriteFolder}");
    }

    private static List<Sprite> LoadFolder(string _path)
    {
        List<Sprite> listSprite = new List<Sprite>();
        if (string.IsNullOrEmpty(_path) == true)
            return listSprite;

        Sprite[] loaded = Resources.LoadAll<Sprite>(_path);
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

    // 블러 스프라이트를 "원본 이름 -> 블러 스프라이트" 사전으로 돌려준다.
    // 짝짓기는 이름으로 한다 — 블러 이름에서 "_blur"만 빼면 원본 이름과 같아진다
    // (원본 mahjong_06_pin_0 / 블러 mahjong_06_pin_blur_0).
    // 파일명으로 경로를 직접 조합하면 안 된다: 스프라이트 이름 끝의 _0은 Unity가 시트를 자르며 붙이는
    // 인덱스라 파일명에 없고, "{스프라이트이름}_blur"로 만들면 mahjong_06_pin_0_blur가 되어 항상 빗나간다.
    public static Dictionary<string, Sprite> LoadBlurDictionary(HouseRecord _record)
    {
        Dictionary<string, Sprite> dicBlur = new Dictionary<string, Sprite>();
        if (_record == null)
            return dicBlur;

        if (string.IsNullOrEmpty(_record.SpriteFolder) == true)
            return dicBlur;

        Sprite[] loaded = Resources.LoadAll<Sprite>($"Image/InGame/Actor/{_record.SpriteFolder}");
        for (int i = 0; i < loaded.Length; ++i)
        {
            if (loaded[i].name.Contains(BLUR_SUFFIX) == false)
                continue;

            string normalName = loaded[i].name.Replace(BLUR_SUFFIX, string.Empty);
            dicBlur[normalName] = loaded[i];
        }

        return dicBlur;
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
