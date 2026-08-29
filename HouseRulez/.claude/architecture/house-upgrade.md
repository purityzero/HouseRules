# 종족별 영구 메타 업그레이드 기술 설계

작성일: 2026-08-28
대상 브랜치: `work/2026-08-28-house-upgrade`
기획 정본: `<ORCA>/Unity/HouseRulez/.claude/design/upgrade-system.html`

## 0. 결론

이번 구현은 **종족별 영구 메타 업그레이드의 읽기 전용 최소 수직 슬라이스**로 제한한다.

- 타이틀의 `UpgradeButton`이 `UIManager.Get<UIHouseUpgrade>()`로 공용 팝업을 연다.
- 팝업은 `HouseTable` 순서대로 종족 탭을 만들고, 현재 선택 종족을 기본 탭으로 표시한다.
- 탭 전환은 업그레이드 화면의 열람 대상만 바꾸며 `PlayerManager.SetSelectedHouse()`를 호출하지 않는다.
- `HouseUpgradeTable`과 `PlayerData`의 `HouseKey` 기반 진행 저장 골격을 추가한다.
- 아직 승인된 노드가 없으므로 `HouseUpgradeTable.csv`는 헤더만 둔다. 화면은 해당 종족의 빈 상태를 표시한다.
- 구매 버튼, 재화 표시, 효과 적용, 런 시작 modifier 스냅샷은 만들지 않는다.

이 범위는 실제 구매 가능한 업그레이드 시스템이 아니다. 다만 타이틀 버튼 → UITable → 공용 팝업 → 종족별 데이터 경계 → 영구 저장 스키마까지 한 번에 연결하여, M-2~M-6이 결정된 뒤 기존 경계를 깨지 않고 내용을 채울 수 있게 한다.

## 1. 근거와 현재 구조

확정된 요구:

1. 업그레이드는 다음 런에도 유지되는 영구 메타 진행이다.
2. 진행도는 `HouseKey`별로 독립한다.
3. 공용 데이터·공용 저장 모델·공용 UI를 쓰며 종족별 클래스/프리팹을 복제하지 않는다.
4. 런 내부 흡수 승급, 각인, 코어는 이 시스템과 별도다.
5. 구체 재화·비용·노드 효과는 정해지지 않았으므로 만들지 않는다.

재사용할 기존 관례:

- `TitleScene.OnClickHouseSelectButton()` / `OnClickSettingButton()`의 `UIManager.Get<T>()` 진입 방식
- `UITable.csv`의 `UIName,UIType,PrefabPath` 등록과 `UIType=Popup`
- `UIPopup`의 Show/Close/뒤로가기 스택
- `UIHouseSelect`의 `HouseTable` 기반 동적 종족 버튼 생성과 현재 선택 종족 fallback
- `PlayerData`의 `[SerializeField] private` 저장 필드와 `PlayerManager` 단일 창구
- `TableManager.init()`의 Load → Table 생성 → Dictionary 등록 3단계
- 신규 버튼은 표준 `Button`이 아니라 `UIButton`
- UI 루트는 기존 타이틀 팝업과 같은 1920×1080 기준, 중앙 피벗을 사용한다.

현재 프로젝트에는 영구 재화가 없고 `eCurrencyType`도 `None/Max`뿐이다. `UIAssetBox`는 이번 범위에서 사용하지 않는다.

## 2. 책임 분리

### 2.1 정적 기획 데이터

`HouseUpgradeTable`은 CSV에서 읽은 종족별 업그레이드 행만 소유한다.

- `HouseKey`: `HouseTable.Key`와 조인
- `Key`: 한 종족 안에서 유지되는 노드 키
- `Level`: 그 노드의 단계
- 행 식별 불변식: `(HouseKey, Key, Level)` 조합이 유일
- `PrerequisiteKey`: 같은 종족 안의 노드 `Key`를 가리킴

기획 문서의 “Key는 노드/레벨 유일 키”와 `PlayerData.nodeKey`는 그대로 두면 서로 충돌한다. 저장 진행도는 레벨이 바뀌어도 같은 키를 써야 하므로, 구현에서는 `Key`를 **안정적인 노드 키**로 해석하고 행 유일성은 위 복합 키로 보장한다. 실제 데이터 행을 넣기 전에 이 해석을 기획 정본에도 반영해야 한다.

`EffectType`과 `CostType`은 아직 승인된 enum이 없으므로 이번에는 `string` 필드로만 둔다. 빈 CSV이므로 처리 분기나 기본 효과를 만들지 않는다.

### 2.2 영구 진행 저장

`PlayerData`는 계정의 종족별 노드 레벨만 저장한다.

```text
PlayerData
└─ List<HouseUpgradeProgressData>
   ├─ houseKey
   └─ List<HouseUpgradeNodeProgressData>
      ├─ nodeKey
      └─ level
```

`JsonUtility`의 일반 `Dictionary` 직렬화 제약 때문에 리스트를 쓴다. 기존 저장 JSON에 새 필드가 없을 수 있으므로 모든 조회는 리스트 null을 안전하게 0레벨로 처리한다. 단순 선택 필드 추가라 현재 `SaveData.version`은 올리지 않는다.

이번 슬라이스에는 구매가 없으므로 외부 변경 API를 열지 않는다. 읽기 API만 추가한다. 미래의 `TryPurchaseHouseUpgrade()`가 생길 때만 `PlayerData` 내부 변경 메서드와 `SetChanged()`를 함께 추가한다. 임시 `SetHouseUpgradeLevel()`을 public으로 열면 비용 검증을 우회하는 영구 저장 경로가 되므로 금지한다.

### 2.3 UI

`UIHouseUpgrade`는 화면 상태만 소유한다.

- `HouseTable`에서 탭 목록을 얻는다.
- 기본 탭은 `PlayerManager.GetSelectedHouseRecord()`의 결과다. raw `selectedHouseKey`를 직접 쓰지 않는다.
- 탭 전환은 로컬 `m_SelectedHouse`만 바꾼다.
- `HouseTable.NameKey`, `AccentColor`, `PlayerManager.IsHouseUnlocked()`를 표시용으로 재사용한다.
- 현재 CSV가 헤더뿐이므로 본문에는 로컬라이즈된 빈 상태만 표시한다.
- 구매 버튼과 재화 위젯은 프리팹에 만들지 않는다.

종족별 `UIHouseUpgradeChess`, `UIHouseUpgradeSlot` 같은 클래스/프리팹은 만들지 않는다.

### 2.4 런 적용

이번 범위에서 `RunData`, `InGameScene`, `HouseUpgradeModifierSet`은 수정/생성하지 않는다.

노드 효과와 적용 순서가 정해지면 별도 단계에서 선택 종족의 진행도를 한 번 계산하여 `RunData`에 스냅샷한다. 매 프레임 `PlayerManager`를 조회하거나, 진행 중인 런에 타이틀 저장 변경을 소급 적용하지 않는다.

## 3. 최소 수직 슬라이스의 정확한 파일과 API

### 3.1 신규 데이터 파일

#### `Assets/Scripts/Table/HouseUpgradeRecord.cs`

```csharp
public class HouseUpgradeRecord : Record
{
    public string Key;
    public string HouseKey;
    public string NameKey;
    public string DescKey;
    public int Level;
    public string EffectType;
    public string TargetKey;
    public float Value;
    public string CostType;
    public int CostValue;
    public string PrerequisiteKey;
    public int SortOrder;
    public string IconPath;
}

public class HouseUpgradeTable : Table<HouseUpgradeRecord>
{
    public HouseUpgradeTable(List<HouseUpgradeRecord> _listRecord);
    public List<HouseUpgradeRecord> GetListByHouseKey(string _houseKey);
}
```

`GetListByHouseKey()`는 원본 `list`를 노출하지 않고 새 리스트를 반환하며 `SortOrder → Key → Level` 순으로 결정적으로 정렬한다. 빈/알 수 없는 `HouseKey`에는 빈 리스트를 반환한다.

#### `Assets/Resources/Table/HouseUpgradeTable.csv`

이번 구현의 정확한 내용은 헤더 한 줄뿐이다.

```csv
Id,Key,HouseKey,NameKey,DescKey,Level,EffectType,TargetKey,Value,CostType,CostValue,PrerequisiteKey,SortOrder,IconPath
```

행을 임의로 추가하지 않는다. 특히 공격력/체력/스핀/홀드 노드, 비용, 테스트용 무료 노드를 만들지 않는다.

### 3.2 수정 데이터/저장 파일

#### `Assets/Scripts/Glory/Table/TableManager.cs`

`init()`에 기존 관례 그대로 다음 세 단계를 추가한다.

```csharp
List<HouseUpgradeRecord> houseUpgradeRecords =
    LoadCsvTable<HouseUpgradeRecord>("Table/HouseUpgradeTable");
HouseUpgradeTable houseUpgradeTable = new HouseUpgradeTable(houseUpgradeRecords);
m_TableDictionary.Add(typeof(HouseUpgradeTable), houseUpgradeTable);
```

Glory의 프로젝트 비의존 원칙에는 이미 프로젝트 테이블 등록이 이 메서드에 모여 있는 예외가 존재하므로, 새 별도 로더/레지스트리를 만들지 않는다.

#### `Assets/Scripts/Player/PlayerData.cs`

신규 직렬화 DTO:

```text
[Serializable] HouseUpgradeNodeProgressData
  private string m_NodeKey
  private int m_Level
  public string nodeKey
  public int level

[Serializable] HouseUpgradeProgressData
  private string m_HouseKey
  private List<HouseUpgradeNodeProgressData> m_ListNodeProgress
  public string houseKey
  public int GetLevel(string _nodeKey)
```

`PlayerData` 변경:

```text
private List<HouseUpgradeProgressData> m_ListHouseUpgradeProgress
public int GetHouseUpgradeLevel(string _houseKey, string _nodeKey)
```

`Init()`은 이 리스트를 비운다. null인 구버전 저장도 조회 시 0을 반환한다. 리스트 자체를 public property로 노출하지 않는다.

파일 상단의 기존 주석은 GDD의 “영구 강화 트리 제외”를 저장 필드의 상한으로 선언하고 있어 최신 사용자 결정과 충돌한다. 이 주석을 “종족별 영구 메타 진행은 저장하고, 런 내부 승급·각인·코어는 저장하지 않는다”로 함께 고친다. 코드만 추가하고 이 제한 주석을 남겨두면 안 된다.

#### `Assets/Scripts/Player/PlayerManager.cs`

읽기 전용 API 하나만 추가한다.

```csharp
public int GetHouseUpgradeLevel(string _houseKey, string _nodeKey)
```

빈 키는 0을 반환하고, 그 외에는 `m_PlayerData.GetHouseUpgradeLevel()`로 위임한다.

다음 API는 이번에 만들지 않는다.

```text
CanPurchaseHouseUpgrade
TryPurchaseHouseUpgrade
SetHouseUpgradeLevel
GetCurrencyAmount / SpendCurrency
BuildHouseUpgradeModifierSet
```

### 3.3 신규 UI 코드

#### `Assets/Scripts/Title/UIHouseUpgrade.cs`

`UIPopup` 파생 공용 화면.

직렬화 필드:

```text
TextMeshProUGUI m_TitleText
TextMeshProUGUI m_SelectedHouseNameText
TextMeshProUGUI m_EmptyText
Image m_AccentImage
Transform m_HouseButtonRoot
UIHouseUpgradeHouseButton m_HouseButtonTemplate
```

주요 API/흐름:

```text
public override void Show()
  base.Show()
  1920×1080 중앙 기준 보정
  HouseTable/StringTable 조회
  종족 탭 부족분만 복제
  현재 언어로 제목/이름/빈 상태 갱신
  PlayerManager.GetSelectedHouseRecord()를 기본 탭으로 SelectHouse

public override void Close()
  base.Close()

public void OnClickCloseButton()
  Close()

private void SelectHouse(HouseRecord _record)
  m_SelectedHouse만 교체
  탭 selected 상태/이름/accent/empty state 갱신
  PlayerManager.SetSelectedHouse 호출 금지
```

현재 모든 종족이 `HouseTable.csv`에서 기본 해금 상태이므로 잠금 정책은 화면에 영향을 주지 않는다. 향후 잠긴 종족이 생기면 기존 `UIHouseSelect`와 동일하게 읽기 전용 미리보기까지만 허용하고 구매는 별도 기획 결정 전까지 불가로 둔다.

#### `Assets/Scripts/Title/UIHouseUpgradeHouseButton.cs`

종족 탭 하나를 담당한다.

```text
UIButton m_Button
Image m_BackgroundImage
Image m_AccentImage
TextMeshProUGUI m_NameText
TextMeshProUGUI m_LockedText

string houseKey { get; }
void SetData(HouseRecord _record, string _displayName, string _lockedLabel,
             bool _isUnlocked, Action<HouseRecord> _onClick)
void SetSelected(bool _isSelected)
```

해금 판단은 `HouseRecord.isUnlocked` 직접 비교가 아니라 호출자가 전달하는 `PlayerManager.IsHouseUnlocked(record)` 결과를 쓴다. 버튼은 `UIButton`이어야 한다.

### 3.4 신규 UI 프리팹

#### `Assets/Resources/Prefabs/UI/UIHouseUpgrade.prefab`

기존 `UISetting`/`UIHouseSelect`의 TMP 폰트·기본 Image·팝업 계층을 재사용한다. 새 이미지나 오디오를 가져오지 않는다.

권장 계층:

```text
UIHouseUpgrade                         UIHouseUpgrade
├─ DimmedBackground                   Image, raycast on
└─ Panel                              Image, centered
   ├─ TitleText                       TMP
   ├─ CloseButton                     UIButton
   │  └─ Label                        TMP, X
   ├─ HouseButtonRoot                 HorizontalLayoutGroup
   │  └─ HouseButtonTemplate          UIHouseUpgradeHouseButton, inactive template
   │     ├─ Accent                    Image
   │     ├─ Name                      TMP
   │     └─ Locked                    TMP
   ├─ SelectedHouseNameText           TMP
   ├─ Accent                          Image
   └─ EmptyText                       TMP
```

- 루트는 1920×1080 full stretch, Panel은 약 1440×880 중앙 배치로 기존 팝업보다 본문 폭을 충분히 확보한다.
- 탭은 6개가 한 줄에 들어오도록 `HorizontalLayoutGroup`으로 배치한다. 코드에 종족별 x좌표 배열을 하드코딩하지 않는다.
- `HouseButtonTemplate`만 프리팹에 두고 런타임에 종족 수만큼 복제한다.
- 구매 버튼, 비용 영역, 재화 아이콘, 노드 트리 선/좌표는 두지 않는다.
- 구조적 프리팹 편집은 MCP 연결을 확인한 뒤 프리팹 스테이지에서 하고, 저장 후 YAML의 스크립트 GUID와 직렬화 참조를 대조한다.

### 3.5 수정 UI 연결 파일

#### `Assets/Resources/Table/UITable.csv`

```csv
UIHouseUpgrade,Popup,Prefabs/UI/UIHouseUpgrade
```

#### `Assets/Resources/Table/StringTable.csv`

기존 마지막 Id 뒤에 다음 표시 문자열만 추가한다. 이 값은 밸런스/노드 기획이 아니라 화면 골격의 상태 문구다.

```text
HouseUpgradeTitle = 종족 업그레이드
HouseUpgradeEmpty = 업그레이드 노드 기획 중
```

Kr/En/Cn/Jp 네 컬럼을 모두 채운다. 종족명과 잠금 문구는 기존 `HouseRecord.NameKey`와 `HouseLocked`를 재사용한다.

#### `Assets/Scripts/Title/TitleScene.cs`

```csharp
public void OnClickUpgradeButton()
{
    UIManager.instance.Get<UIHouseUpgrade>();
}
```

그 외 타이틀 초기화/버튼은 수정하지 않는다.

## 4. 지금 구현하면 안 되는 부분

다음은 값만 미정인 것이 아니라 동작 계약 자체가 미정이므로 스텁도 만들지 않는다.

1. M-2 트리 형태: 선형/트리/상호 배타 분기, 좌표 컬럼, 노드 아이템 UI
2. M-3 재화: 이름, 공용/종족별 여부, 획득 경로, 패배 보상, `eCurrencyType`
3. M-4 노드 효과/수치: 공격력·체력·스핀·홀드 등 모든 실제 행과 효과 처리기
4. M-5 재분배/최대 진행: 환불, 초기화, 완료까지 필요한 런 수
5. M-6 혼혈 유닛 적용 규칙
6. 구매 원자성: 재화 차감과 진행 저장을 한 저장 단위/트랜잭션으로 묶는 방식
7. 효과 적용 순서: 등급 배수/각인/메타 배율의 순서
8. `HouseUpgradeModifierSet`과 `RunData` 스냅샷
9. 흡수 승급, 각인, 코어와 관련된 모든 코드/CSV/UI

특히 “테스트를 위해 무료 노드 하나”를 넣거나, `TryPurchaseHouseUpgrade()`가 재화 없이 성공하게 만드는 구현은 저장 데이터를 오염시키므로 금지한다.

## 5. 두 code-writer 파일 소유권 분할

텍스트 편집은 두 레인으로 병렬 가능하다. 동일 파일 소유가 없으며 Unity 에디터/MCP는 UI 레인만 사용한다. 단, Unity 최종 컴파일은 두 레인이 모두 끝난 뒤 감독 세션이 한 번 실행한다.

### 레인 A — 데이터/저장

소유 파일:

- `Assets/Scripts/Table/HouseUpgradeRecord.cs` + `.meta`
- `Assets/Resources/Table/HouseUpgradeTable.csv` + `.meta`
- `Assets/Scripts/Glory/Table/TableManager.cs`
- `Assets/Scripts/Player/PlayerData.cs`
- `Assets/Scripts/Player/PlayerManager.cs`

금지:

- `TitleScene.cs`, `UITable.csv`, `StringTable.csv`, UI 스크립트/프리팹
- 재화/구매/효과/RunData 구현
- Unity MCP 사용

### 레인 B — 타이틀 UI/프리팹

소유 파일:

- `Assets/Scripts/Title/UIHouseUpgrade.cs` + `.meta`
- `Assets/Scripts/Title/UIHouseUpgradeHouseButton.cs` + `.meta`
- `Assets/Resources/Prefabs/UI/UIHouseUpgrade.prefab` + `.meta`
- `Assets/Resources/Table/UITable.csv`
- `Assets/Resources/Table/StringTable.csv`
- `Assets/Scripts/Title/TitleScene.cs`

금지:

- `TableManager.cs`, `PlayerData.cs`, `PlayerManager.cs`, `HouseUpgradeTable.csv`
- 노드 아이템/구매 버튼/재화 위젯
- 기존 `UISetting.prefab`/`UIHouseSelect.prefab` 수정

### 병렬 실행 주의

- 레인 B가 레인 A의 신규 타입을 직접 참조하지 않도록 이번 읽기 전용 UI는 `HouseTable`만 사용한다. 그래서 어느 레인이 먼저 끝나도 코드 참조가 깨지지 않는다.
- 프리팹 생성은 레인 B만 수행한다.
- `.meta` 생성은 각 소유 레인이 자기 신규 에셋과 함께 책임진다.
- 두 레인은 commit/merge/push하지 않는다.
- 프로젝트 `.claude`는 스냅샷이므로 code-writer가 class/prefab 기록을 동시에 고치지 않는다. 채택 후 감독 세션이 `<ORCA>/Unity/HouseRulez/.claude/class|prefab` 정본을 한 번 갱신한다.

## 6. 검증 계약

### 6.1 정적/CLI

두 레인 종료 후 감독 세션이 실행한다.

```powershell
git -C "D:\Unity\HouseRules" diff --check
unity command verify-compile --project-path "D:\Unity\HouseRules\HouseRulez" --format json
unity command verify-tables --project-path "D:\Unity\HouseRules\HouseRulez" --format json
```

통과 기준:

- `verify-compile` 종료 코드 0
- `verify-tables` 종료 코드 0, 기존보다 테이블 수 1 증가
- `HouseUpgradeTable.csv` 모든 헤더가 `HouseUpgradeRecord` public 필드와 대소문자까지 일치
- 신규 에셋마다 `.meta` 존재
- 기존 사용자 변경인 `Assets/font/DungGeunMo Bitmap.asset`, `ProjectSettings/ProjectSettings.asset`은 diff에서 그대로 보존

### 6.2 프리팹 구조

- 루트에 `UIHouseUpgrade` 컴포넌트가 있고 모든 직렬화 참조가 연결됨
- Close/종족 탭 버튼 컴포넌트는 `UIButton`
- UITable의 `UIName`, 컴포넌트명, 프리팹명이 모두 `UIHouseUpgrade`
- `UIType=Popup`
- 저장 후 YAML에서 missing script/GUID/dangling fileID가 없음
- 구매 버튼, 비용, 재화 오브젝트가 없음

### 6.3 Play Mode

1. TitleScene 진입 후 업그레이드 버튼을 누르면 `PopupCanvas/UIHouseUpgrade`가 열린다.
2. 팝업은 화면 중앙에 정상 크기로 표시되고 1920×1080/다른 Game View 비율에서 잘리지 않는다.
3. 종족 탭이 `HouseTable`의 6개 순서대로 보인다.
4. 최초 선택은 `PlayerManager.GetSelectedHouseRecord()`와 같다.
5. 다른 탭을 눌러 이름/액센트가 바뀌지만, 닫은 뒤 종족 선택값과 타이틀 배경은 바뀌지 않는다.
6. 본문은 “업그레이드 노드 기획 중” 빈 상태를 표시하고 구매 버튼/재화는 보이지 않는다.
7. 닫기 버튼과 Escape/모바일 뒤로가기 모두 최상단 팝업만 닫는다.
8. 설정에서 언어를 바꾼 뒤 다시 열면 제목/빈 상태/종족명이 현재 언어로 갱신된다.
9. UISetting/UIHouseSelect/Play 버튼의 기존 동작에 회귀가 없다.
10. Console에 compile error, missing reference, `UITable record not found`, `HouseTable not found`가 없다.

## 7. 다음 기획 결정 후 확장 순서

1. M-2와 `Key` 의미를 기획 정본에 확정
2. M-3~M-5 확정 후 실제 `HouseUpgradeTable.csv` 행과 노드 표시 UI 추가
3. 재화 저장 단위와 구매 원자성 설계 후 `TryPurchaseHouseUpgrade()` 한 경로만 추가
4. M-6과 효과 적용 순서 확정 후 `HouseUpgradeModifierSet` 작성
5. 런 시작 시 선택 종족 modifier를 `RunData`에 한 번 스냅샷
6. 메타 0/메타 적용 QA 프로필을 분리해 밸런스 검증

## 8. 남은 blocker

- 실제 업그레이드 콘텐츠를 만들 수 없는 직접 blocker: M-2, M-3, M-4, M-5, M-6
- 테이블 식별자 문구 불일치: `Key`를 행 유일 키로 볼지 노드 키로 볼지 기획 정본 정리 필요
- 구매 원자성: 재화와 진행도를 서로 다른 SaveData에 둘 경우 앱 종료 시 부분 저장 가능성이 있으므로 재화 설계와 함께 결정 필요
- 잠긴 종족의 업그레이드 열람 정책은 현재 데이터가 전부 해금이라 관찰되지 않는다. 실제 잠금 사용 전 확정 필요
