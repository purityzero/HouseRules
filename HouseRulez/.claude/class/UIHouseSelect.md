# UIHouseSelect / UIHouseSelectButton

연관: [[TitleScene]], [[HouseRecord]], [[StringTable]], [[TableManager]], [[TitleBackgroundScroller]]

종족 선택 화면(`Assets/Scripts/Title/UIHouseSelect.cs`, `UIHouseSelectButton.cs`). 타이틀 위에 얹는 패널이라 고르는 즉시 뒤 배경이 바뀌어 미리보기가 된다.

## 2026-08-26-0 — 신규 작성

### 개요
사용자 요청: "밑에 선택지 5개를 가로로 놓고 그 위에 능력치를 바 형태로 애니메이션, 위에는 말들의 종류를 보여줌". Unity MCP로 씬까지 조립했다.

별도 씬이 아니라 **타이틀 위 패널**로 만들었다 — 타이틀 화면 기획서(`Assets/Design/title_screen_design.html`) §4의 권장안이고, 고르는 즉시 배경이 바뀌는 미리보기가 성립한다.

### 화면 구성 (위 → 아래)
```
Canvas/HouseSelectPanel        ← 평소 비활성, siblingIndex 마지막(항상 맨 앞)
├─ (Image)                      딤 #1E2129 alpha 0.97
├─ TitleText                    "종족 선택"
├─ UnitRoot                     ← 위: 그 종족의 말 종류 (HorizontalLayoutGroup)
│   └─ UnitSlot                 원본 1개(비활성). 코드가 필요한 만큼 복제
├─ StatBars                     ← 중간: 능력치 4줄
│   ├─ Bar0_Power   평균 전력    Name / Track / Track.Fill / Value
│   ├─ Bar1_Variance 분산
│   ├─ Bar2_Ceiling  상한
│   └─ Bar3_Learning 학습 비용
├─ HouseButtonRoot              ← 아래: 선택지 5개 가로 (간격 214)
│   └─ HouseButton0~4           Accent(색 띠) / Name / Locked
└─ CloseButton                  X
```

### 데이터 — 코드에 안 박고 테이블로
CODE.md의 "튜닝값/관계 데이터는 하드코딩 금지" 규칙에 따라 종족 수치를 CSV로 뺐다.

`Assets/Resources/Table/HouseTable.csv` (신규):
```
Id,Key,NameKey,AccentColor,PoolCount,AxisPower,AxisVariance,AxisCeiling,AxisLearning,SpriteFolder,BackgroundPath,isUnlocked
1,chess,  HouseChess,  7FA7C9, 6, 61,38, 45,10, chess,   Image/Title/bg_chess_castle,     1
2,janggi, HouseJanggi, 57A277, 7, 63,40, 65,35, janggi,  Image/Title/bg_janggi_fortress,  1
3,hwatu,  HouseHwatu,  D1554C,12, 62,92,100,45, hwatu,   Image/Title/bg_hwatu_moonfield,  1
4,poker,  HousePoker,  D6A441,13, 62,97,100,25, poker,   Image/Title/bg_poker_frontier,   1
5,mahjong,HouseMahjong,9B7FD4, 9, 62,62,  0, 0, mahjong,                        ,          0
```
- **Id 순서가 곧 해금 순서이자 화면 표시 순서**다(체스 → 장기 → 화투 → 포커 → 마작). 2026-08-25 사용자 결정으로 마작이 마지막이며, 규칙이 "풀 크기 순"에서 **"학습 난이도 순"**으로 재정의됐다.
- 축 값은 GDD 04장의 막대값 그대로. **마작의 `AxisCeiling`/`AxisLearning`은 아직 0** — [[mahjong-house]] §9-10에서 "아트/편집 판단이라 비워둔다"로 남은 항목이다.
- 마작은 `isUnlocked=0`. 유닛 스프라이트도 아직 없어서 말 줄이 비고, 배경 경로도 비어 있다.

`StringTable.csv`에 12개 키 추가 — 종족 이름 5개(`HouseChess`~`HouseMahjong`), 축 이름 4개(`AxisPower`~`AxisLearning`), `HouseSelectTitle`/`HouseLocked`/`HouseSelectConfirm`.

`TableManager.init()`에 `HouseTable` 등록(기존 패턴대로 3줄).

### 구현 요점
- **능력치 막대는 `Image.type = Filled` + `fillMethod = Horizontal`, 그리고 `sprite`가 반드시 있어야 한다.**
  - `sprite`가 `null`인 Image는 **`type = Filled`를 무시하고 그냥 사각형으로 그린다.** `fillAmount` 값 자체는 정상적으로 바뀌지만 화면에는 전혀 반영되지 않아, 모든 막대가 값과 무관하게 꽉 찬 채로 보인다(축이 0인 마작조차 가득 찼다). 에러도 경고도 없다.
  - 그래서 `Assets/Resources/Image/UI/ui_white.png`(4×4 순백, Point·무압축·Sprite)를 만들어 Fill과 Track 양쪽에 물렸다. 빌트인 `UI/Skin/UISprite.psd`는 9-slice라 Filled 막대에는 가장자리가 지저분해진다.
- 막대는 0에서 목표치까지 `Ease.OutCubic`으로 차오르고, **줄마다 0.07초씩 늦게 시작**해 위에서 아래로 읽히게 했다. 숫자도 `DOVirtual.Int`로 함께 카운트업된다.
- 막대 색은 선택한 종족의 `AccentColor`를 그대로 쓴다 — 종족이 바뀌면 색까지 갈린다.
- `SetUpdate(true)`로 `Time.timeScale`과 무관하게 돈다(일시정지 중 UI가 멈추지 않게).
- **말 슬롯은 고정 개수로 못 만든다.** 종족마다 6/7/12/13종으로 달라서, 원본 슬롯 하나를 씬에 비활성으로 두고 코드가 `Instantiate`로 늘린다(`m_MaxUnitSlot` 8개까지).
- `Resources.LoadAll`은 폴더 전부를 가져오므로 **미리 확대해둔 `_x8` 사본을 이름으로 걸러낸다.**
- 잠긴 종족도 눌러서 볼 수는 있게 두고, 확정만 막는다. 액센트 색은 alpha 0.35로 죽여 한눈에 구분되게 했다.

### 패널을 열면 타이틀 UI를 감춘다
처음엔 딤만 깔았더니 **뒤의 로고·메뉴가 그대로 비쳐 말과 겹쳐 읽혔다.** `m_HideOnOpen` 배열에 `Title`/`Menu`/`UnitRow`를 넣어 열 때 끄고 닫을 때 되돌린다. `Background`는 미리보기 대상이라 일부러 남긴다.

### 검증 상태 — Play Mode 확인 완료
| 확인 항목 | 결과 |
|---|---|
| 컴파일 | 에러 0건 |
| `HouseTable` 로드 | 5행 |
| 체스 선택 | 말 6개 / 바 61·38·45·10 / 색 #7FA7C9 |
| 장기 전환 | 말 7개 / 바 63·40·65·35 / 색 녹색 / **배경이 기와 성문으로 교체** |
| 선택지 | 체스·장기·화투·포커 + 마작(잠금) |
| 잠금 표시 | 마작만 "미해금" |

### 남은 것
- **마작 축 2개(`AxisCeiling`/`AxisLearning`)가 0이라 막대가 빈다.** 값이 정해지면 CSV만 고치면 된다.
- ~~마작 유닛 스프라이트 없음 — `Image/InGame/Actor/mahjong/` 폴더 자체가 없어 말 줄이 빈다.~~ → **2026-08-26에 해소.** 현재 폴더에 마작 9장(각 `_blur` 사본 포함 18장)이 존재한다.
- ~~선택 결과를 저장하지 않는다.~~ → **2026-08-26-1에서 해결.** 아래 참고.
- `Play` 버튼과의 연결 없음 — 고른 종족으로 인게임에 들어가는 경로가 아직 없다.

---

## 2026-08-26-1 — 선택 결과 저장 연동

### 개요
[[PlayerManager]] / [[SaveData]](Glory 저장 프레임워크) 신설에 맞춰, 고른 종족이 저장되고 다시 열 때 되살아나도록 연결했다.

기획서 `title_screen_design.html` §04 **Q1 "현재 종족을 어디에 저장하나 — PlayerPrefs 종족 Id 하나 vs 세이브 데이터 클래스"** 가 이 작업으로 **세이브 데이터 클래스**로 결론났다. Id(int)가 아니라 `Key`(문자열)를 저장한다 — CSV의 Id는 표시 순서 겸 해금 순서라 나중에 종족이 끼어들면 값이 밀리지만, `Key`("chess"/"mahjong" 등)는 안 밀린다.

### 수정 — 함수 단위 전/후

**`Open()`** — 매번 첫 해금 종족을 강제 선택하던 것을 저장된 선택 복원으로 교체
```csharp
// 전
HouseRecord first = m_ListHouse.Find(record => record.isUnlocked > 0);
if (first != null)
    Select(first.Key);

// 후
HouseRecord selected = PlayerManager.instance.GetSelectedHouseRecord();
if (selected != null)
    Select(selected.Key);
```
`GetSelectedHouseRecord()`가 "저장된 종족 → 없거나 잠겼으면 첫 해금 종족" 폴백까지 안에서 처리한다.

**`BuildHouseButtons()`** — 잠금 라벨 판정을 테이블 직접 참조에서 PlayerManager 경유로
```csharp
// 전
if (record.isUnlocked <= 0 && _stringTable != null)

// 후
if (PlayerManager.instance.IsHouseUnlocked(record) == false && _stringTable != null)
```
해금은 이제 테이블(기본 개방)과 PlayerData(플레이로 딴 해금) 둘이 나눠 들기 때문에, 테이블만 보면 안 된다.

**`Select()`** — 선택 즉시 저장하는 3줄 추가 (`m_SelectedHouse = record;` 바로 아래)
```csharp
if (PlayerManager.instance.IsHouseUnlocked(record) == true)
    PlayerManager.instance.SetSelectedHouse(record.Key);
```

### 확정 버튼을 안 만든 이유
`HouseSelectConfirm` 문자열 키는 이전 리비전에서 만들어져 있지만 버튼은 안 만들었다. 이 패널은 **고르는 즉시 배경·말 줄이 갈리는 미리보기 화면**이라 이미 "고른 상태"가 화면에 반영된다 — 여기에 확정 버튼을 더하면 "보이는 것"과 "저장된 것"이 어긋나는 상태가 생긴다. 잠긴 종족만 저장에서 제외하면 충분하다.
- 나중에 종족 변경에 비용(재화 등)이 붙으면 그때 확정 버튼이 필요해진다. 키는 그대로 남겨둔다.

### 검증 상태 — **미검증**
Unity MCP 미연결 세션이라 컴파일/Play Mode 확인을 못 했다. 이전 리비전(2026-08-26-0)의 Play Mode 통과 결과는 이 변경 **이전** 것이다.

---

## 2026-08-26-2 — 블러 이미지 필터 추가

### 문제
2026-08-26에 슬롯 릴 회전용 블러 스프라이트 47장(`{원본이름}_blur.png`)을 추가했는데, `HouseSpriteLoader.Load()`의 필터링에 `_blur`가 없어서 블러 이미지와 원본 이미지가 **둘 다** 말 목록에 들어갔다. 종족 선택 화면 위쪽 말 줄과 타이틀 말 줄에 블러 이미지가 섞여 보였고, 표시 개수도 두 배가 됐다.

### 수정 — [[HouseRecord.cs]]
**`HouseSpriteLoader` 클래스**에 제외 접미사 배열을 추가하고 필터링 로직을 변경:
- `EXCLUDE_SUFFIXES` static readonly 배열에 `_x8`, `_blur` 저장 (이후 추가 접미사 발생 시 목록에만 추가하면 됨)
- 필터 루프를 배열 순회로 변경하여 각 접미사를 검사
