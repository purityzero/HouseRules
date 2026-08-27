# StringTable / StringRecord

연관: [[TableManager]], [[TitleScene]], [[GlobalEnum]]

다국어 문구 테이블(`Assets/Scripts/Table/StringRecord.cs`). 한 파일에 `StringRecord`(행)와 `StringTable`(조회 API)이 함께 들어 있다. CSV는 `Assets/Resources/Table/StringTable.csv`.

## 2026-08-25-1 — GeometryDefender에서 로컬라이제이션 이식

### 개요
타이틀 버튼 4개에 텍스트가 생긴 김에 로컬라이제이션을 도입(사용자 요청). 새로 설계하지 않고 **GeometryDefender의 것을 그대로 가져왔다** — `StringRecord.cs`는 무수정 복사, CSV만 HouseRulez용으로 새로 작성했다. GeometryDefender의 CSV에는 그 게임 고유 문구(Shard, MetaTree 등)가 들어 있어 가져오지 않았다.

### 파일
- `Assets/Scripts/Table/StringRecord.cs` (신규, GD에서 무수정 복사)
- `Assets/Resources/Table/StringTable.csv` (신규, HouseRulez용 4행)
- `Assets/Scripts/Glory/Table/TableManager.cs` (수정 — 등록 추가)
- `Assets/Scripts/Title/TitleScene.cs` (수정 — 적용)

### 스키마
```
Id,Key,Kr,En,Cn,Jp
1,TitlePlay,시작하기,Play,开始游戏,はじめる
2,TitleHouseSelect,종족선택하기,Select House,选择种族,種族選択
3,TitleUpgrade,업그레이드,Upgrade,升级,アップグレード
4,TitleSetting,설정,Setting,设置,設定
```

`eLanguage`(Korean/English/Chinese/Japanese)는 HouseRulez `GlobalEnum.cs`에 **이미 있었다** — 별도 추가 불필요.

### 조회 API
`StringTable.GetString(key)` 및 `string.Format` 인자 1~3개 오버로드. 키가 없으면 `Logger.Error`를 남기고 **키 문자열 자체를 반환**한다(화면이 비지 않아 누락을 눈으로 찾기 쉽다).

`StringTable.CurrentLanguage`는 static이며 초기값은 `GetDefaultLanguage()` — `Application.systemLanguage`로 자동 판별한다.

### TableManager 등록
`init()`의 기존 패턴 그대로 3줄 추가(Load → new → Add).

### 중요 — `TableManager.init()`을 부르는 곳이 없었다
이식 중 발견한 기존 문제다. GeometryDefender는 `GameManager`가 부팅 시 `init()`을 호출하는데, **HouseRulez에는 그 진입점이 아직 없어 어떤 테이블도 로드되지 않고 있었다.** 지금까지 BGM이 안 울린 것도 `SoundTable.csv`가 비어서가 아니라 애초에 테이블이 `null`이었기 때문이다.

임시 조치로 [[TitleScene]]의 `OnSetup()`에서 `TableManager.instance.init()`을 호출한다. `init()`은 자체 멱등 가드(`m_isInitialized`)가 있어 나중에 부팅 진입점이 생겨 양쪽에서 호출돼도 테이블이 중복 누적되지 않는다. **진입점을 만들면 이 줄을 그쪽으로 옮길 것.**

> 이 조치는 `TitleScene.cs`에 원래 있던 "이 메서드에 `TableManager.init()`을 넣지 말 것" 주석과 정면으로 어긋난다. 그 주석은 GeometryDefender에서 온 것으로, **부팅 진입점이 이미 있는 상황**을 전제한 경고였다. HouseRulez는 전제가 다르므로 주석도 현재 상황에 맞게 고쳤다.

### 적용 방식
GeometryDefender 관례를 따라 **UI 스크립트가 직접 `GetString`을 호출**한다(텍스트 컴포넌트에 키를 붙이는 자동 바인딩 컴포넌트는 GD에도 없다). [[TitleScene]]이 버튼 라벨 4개를 `[SerializeField] TextMeshProUGUI`로 들고 `OnSetup()`에서 `ApplyLocalizedText()`로 채운다.

### 검증 상태 — Play Mode 확인 완료
| 확인 항목 | 결과 |
|---|---|
| 컴파일 | 에러 0건 |
| `StringTable` 로드 | 4행 정상 |
| `CurrentLanguage` | `Korean` (시스템 언어 자동 판별) |
| 버튼 라벨 | 시작하기 / 종족선택하기 / 업그레이드 / 설정 — 화면 확인 |

### 다음에 키를 추가할 때
1. **같은 문구의 기존 키가 있는지 먼저 검색**한다(루트 CLAUDE.md 로컬라이제이션 규칙).
2. 런타임에 코드가 매번 값을 덮어쓰는 표시(점수, 수량 등)에는 키를 만들지 않는다 — 정적 라벨만 대상이다.
3. CSV는 UTF-8로 저장한다. 중국어·일본어 글리프는 `LiberationSans SDF`의 fallback 체인(DungGeunMo/PixelMplus/Vonwaon)이 받는데, **현재 확인된 것은 한글까지다.** 중/일 문구를 실제로 화면에 띄우려면 글리프 커버리지를 따로 확인해야 한다.

## 2026-08-27-0 — 인게임 HUD / ACTION 키 8개 추가

`StringTable.csv`에 Id 17~24 추가. 클래스 코드는 손대지 않았다.

| Key | Kr | 쓰는 곳 |
|---|---|---|
| `HudHome` | 본거지 | [[UIInGameHud]] |
| `HudYear` | `YEAR {0}/{1}` | 〃 |
| `HudGold` | 골드 | 〃 |
| `HudSpinCoin` | 스핀 코인 | 〃 |
| `HudBet` | `판돈 {0} ×{1}` | 〃 (화투 전용) |
| `ActionBattleStart` | 전투 시작 | [[UIInGameAction]] |
| `ActionSwap` | `스왑 {0}` | 〃 |
| `ActionBattleSpeed` | `배속 ×{0}` | 〃 |

포맷 인자가 있는 키는 `GetString(key, arg...)` 오버로드로 받는다. 값이 매번 바뀌는 표시(골드 액수 등)는
위 "다음에 키를 추가할 때" 2번대로 키를 만들지 않고 숫자만 직접 넣는다 — 라벨과 포맷 문자열만 키다.

### 검증 상태 — 미검증
중국어/일본어 글리프 커버리지는 이번에도 확인하지 않았다(위 3번 항목과 같은 상태).
