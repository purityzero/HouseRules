# PlayerManager / PlayerData / OptionData

연관: [[SaveData]], [[MonoSingleton]], [[UIHouseSelect]], [[HouseRecord]], [[StringTable]], [[SoundManager]], [[TitleScene]], [[TableManager]]

`Assets/Scripts/Player/PlayerManager.cs`, `Assets/Scripts/Player/PlayerData.cs` — 저장 데이터의 유일한 창구.

## 2026-08-26-0 — 신규 작성

### 개요
사용자 요청: "종족 선택하기에서 내가 선택한 번호는 저장해서 가지고 있어야 하니까 PlayerData, PlayerManager 만들자. PlayerManager는 MonoSingleton 사용. 기획문서 확인하고 필요한 건 미리 집어넣어도 됨."

저장 계층은 Glory의 공용 프레임워크([[SaveData]])가 맡고, 이 클래스들은 **무엇을 저장할지와 저장값을 실제 시스템에 어떻게 반영할지**만 든다.

### PlayerData 필드 범위 — GDD가 상한선
`Assets/Design/reel_of_four_houses_GDD.html` 541줄: **"메타 진행은 종족 해금 하나로 시작합니다. 영구 강화 트리는 초기 빌드에 넣지 않습니다 — 밸런싱 축이 하나 더 늘어나면 프로토타입 검증이 불가능해집니다."**

| 저장 키 | 클래스 | 필드 | 넣은 이유 |
|---|---|---|---|
| `PlayerData` | `PlayerData` | `m_SelectedHouseKey` | 이번 요청의 본체 |
| | | `m_ListUnlockedHouseKey` | GDD가 인정한 유일한 메타 진행 |
| | | `m_LastPlayedAt` | 복귀 보상/휴면 판정에 쓰이는 최소 정보 |
| `OptionData` | `OptionData` | `m_BgmVolume` / `m_SfxVolume` | `SoundManager`가 볼륨 저장을 의도적으로 안 갖는다(glory.md) — 프로젝트가 들어야 함 |
| | | `m_FpsOption` / `m_Language` | `GlobalEnum`에 enum이 이미 있고 `TitleScene.OnClickSettingButton`이 TODO로 열려 있음 |

**일부러 넣지 않은 것**
- **재화(`AssetData`)** — GeometryDefender엔 있지만 이 게임엔 근거가 없다. GDD의 코인/코어는 전부 런(run) 안에서만 사는 값이라 영구 저장 대상이 아니다. `GlobalEnum.eCurrencyType`이 `None/Max`뿐인 빈 스텁인 것도 같은 이유. `UIAssetBox.cs:23` 주석이 "재화 저장소(PlayerManager 등)"를 상정하지만, **영구 재화가 기획에 생긴 뒤에** 추가한다.
- **영구 강화 트리 / 각인 / 코어 보유** — 위 GDD 문장이 명시적으로 배제.
- **런 기록(RunRecord)** — GeometryDefender엔 있으나 이 게임엔 기록 화면 기획이 아직 없다.

### 해금 소유권 — 테이블과 PlayerData가 나눠 든다
`HouseRecord.isUnlocked`(CSV)는 **"기본 개방"**만 뜻하고, 플레이로 딴 해금은 `PlayerData.m_ListUnlockedHouseKey`가 든다. 판정은 `PlayerManager.IsHouseUnlocked(HouseRecord)` 한 곳으로 모았다.
- 테이블만 보면 플레이어별 진행도를 담을 수 없고, PlayerData만 보면 초기 개방 종족을 CSV로 튜닝할 수 없다. 둘 다 필요해서 OR로 합친다.
- 현재 CSV 기준 체스/장기/화투/포커 = `isUnlocked 1`, 마작 = `0`. 즉 마작만 `UnlockHouse("mahjong")`을 받아야 열린다.
- 마이그레이션 불필요 — 기존 저장본이 없다(이번이 저장 시스템 최초 도입).

### 초기화 순서
```
TitleScene.OnSetup()
  1. TableManager.instance.init()      테이블 먼저 (HouseTable/StringTable)
  2. PlayerManager.instance.Load()     저장본 로드 + 옵션 반영(언어/볼륨/FPS)
  3. ApplyLocalizedText()              저장된 언어가 반영된 뒤에 텍스트를 뽑아야 함
```
- `Load()`는 **`Awake()`에서도 불리고 `TitleScene.OnSetup()`에서도 불린다.** `m_isLoaded` 멱등 가드가 두 번째 호출을 막는다 — 가드가 없으면 두 번째 호출이 레지스트리를 새로 만들어 `OnChanged` 구독과 아직 저장 안 된 변경을 통째로 날린다(`TableManager.init()`이 같은 이유로 `m_isInitialized`를 갖고 있다).
- 부팅 진입점(GameManager 등)이 생기면 위 3줄을 통째로 그쪽으로 옮긴다.

### 저장 시점
- 평소: `Update()`에서 `Registry.UpdateLogic()`이 더티 플래그를 보고 자동 저장. 호출부는 `Save()`를 부를 필요가 없다.
- 앱이 내려갈 때: `OnApplicationPause(true)` / `OnApplicationQuit()`에서 `Save()` 강제 + `LastPlayedAt` 갱신.

### public API
```csharp
PlayerData playerData { get; }
OptionData optionData { get; }
string selectedHouseKey { get; }

void Load();                                  // 멱등
void Save();                                  // 즉시 저장(앱 종료용)

bool IsHouseUnlocked(HouseRecord _record);    // 테이블 기본개방 || PlayerData 추가해금
void UnlockHouse(string _houseKey);
void SetSelectedHouse(string _houseKey);
HouseRecord GetSelectedHouseRecord();         // 없거나 잠겼으면 첫 해금 종족으로 폴백

void SetBgmVolume(float _volume);             // 저장 + SoundManager 반영
void SetSfxVolume(float _volume);
void SetFpsOption(eFpsOption _fpsOption);     // 저장 + Application.targetFrameRate 반영
void SetLanguage(eLanguage _language);        // 저장 + StringTable.CurrentLanguage 반영
```

### 검증 상태 — **미검증**
Unity MCP 미연결 세션이라 컴파일/Play Mode 확인을 못 했다.

### 남은 것
- 옵션을 실제로 바꾸는 **설정 화면이 아직 없다**(`TitleScene.OnClickSettingButton`은 TODO). `OptionData`는 저장/반영 경로만 완성된 상태.
- 마작 해금을 실제로 주는 경로가 없다 — `UnlockHouse()`를 부를 곳(왕 처치 등)이 아직 구현 전.
