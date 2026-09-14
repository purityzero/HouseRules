# UIHandbook

연관: [[HandbookText]], `UIPopup`, `UIManager`, [[UIInGameAction]], `UIHouseSelect`, [[RunRoster]]

## 2026-09-14 — 신설 (족보 팝업)

### 개요
종족의 **족보**와 **유닛 특성**을 보는 팝업. 인게임과 종족 선택 **두 자리에서 같은 것**을 연다.

사용자 요구: *"족보버튼은 따로 있었으면 좋겠어. 모든 당첨이 나오는 족보. 스크롤 뷰여도되."*
*"아예 팝업창에서 스크롤뷰 내려서 전체를 확인할 수 있었으면 한데, 자세하게"*

파일: `Assets/Scripts/InGame/UI/UIHandbook.cs`
프리팹: `Assets/Resources/Prefabs/UI/UIHandbook.prefab` (Codex 작업)

### 계층과 찾는 경로
```
UIHandbook
├ Dim
└ Panel (1500x900)
  ├ TitleText            ← "{종족} · {탭}"
  ├ CloseButton
  ├ HouseTabRoot
  │ └ HouseTab_{key} ×7  ← 이름의 꼬리가 곧 HouseTable.Key
  ├ ModeTabRoot
  │ ├ PatternTabButton
  │ └ UnitTabButton
  ├ ScrollView / Viewport / Content / BodyText
  └ AxisPlaceholder      ← 4축 막대용 예약(비활성)
```

**직렬화 연결을 쓰지 않고 이름으로 찾는다.** 이 팝업은 `UIManager.Get<T>()` 가 런타임에
생성하므로 씬에 인스턴스가 없어 인스펙터에서 이을 자리가 자체가 없다.

⚠️ 이름 기반이라 **오타 하나면 조용히 실패한다.** 도입 시 경로 7개와 종족 탭 7개가
프리팹에 실제로 있는지 기계로 대조했다(전부 통과).

### 종족 탭은 테이블 순서를 따른다
프리팹의 오브젝트 순서가 아니라 `HouseTable` 을 순회해 `HouseTab_{Key}` 를 찾는다.
종족이 늘거나 순서가 바뀌면 테이블만 고치면 되고, 프리팹에 탭이 없으면 로그를 남기고 그 종족만 빠진다.

```csharp
string houseKey = record.Key;   // 캡처가 루프 변수를 물지 않게 지역으로 받는다
button.onClick.AddListener(() => OnClickHouseTab(houseKey));
```
루프 변수를 그대로 캡처하면 **탭 7개가 전부 마지막 종족으로 열린다.**

### 런타임 배선
프리팹의 버튼 12개는 persistent UnityEvent 호출이 **0개**다(Codex 확인).
`UIInGameSummary` 가 *"복제가 남의 UnityEvent 영구 호출을 물고 오는 사고를 하루에 세 번"*
겪고 남긴 기록이 있어, 종족 탭처럼 복제로 만드는 구조에서는 런타임 등록이 안전하다.

### 명부를 여는 쪽이 넘긴다
```csharp
handbook.Open(m_RunData.houseKey, m_RunData.roster);   // 인게임
handbook.Open(m_SelectedHouse.Key);                     // 종족 선택 (명부 없음)
```

**`UIInGameField` 에서 받지 않는다** — 전투가 시작되면 `Clear()` 가 `m_Roster = null` 로 놓기 때문에
유닛 탭이 빈 채로 열린다. 전장에는 유닛이 서 있는데도 그렇다. 런 상태의 소유자는 `InGameScene` 이다.

명부가 없는 종족 선택 화면에서는 유닛 탭이 그 사실을 글로 알린다(`HandbookUnitEmpty`).

### 탭을 바꾸면 스크롤을 맨 위로
내용이 통째로 바뀌는데 스크롤이 중간에 남아 있으면 바뀐 것을 못 알아챈다.

### 진입 버튼
| 자리 | 경로 |
|---|---|
| 인게임 | `InGameScene/Action/HandbookButton` → `UIInGameAction.OnOpenHandbook` |
| 종족 선택 | `UIHouseSelect` 루트 `/HandbookButton` |

둘 다 **직렬화 필드를 두되 연결 전까지 이름으로 찾는 폴백**을 넣었다 —
씬 편집은 Codex 몫이라 코드가 먼저 들어가는 순서였기 때문이다.
인스펙터 연결이 끝나면 폴백은 안 타지만 남겨 둔다(연결이 풀렸을 때의 안전망).

### ⚠️ 화면 검증이 남았다
열기·닫기, 탭 전환, 긴 본문 스크롤, 1500x900 레이아웃의 여백과 가독성은 Play Mode 로 봐야 한다.
구조 대조(경로 7개 · 종족 탭 7/7 · ScrollRect 참조)와 컴파일까지만 확인했다.

## 2026-09-14-1 — 선택 탭 글자가 사라지던 것 (QA 발견)

### 증상
선택한 종족과 선택한 모드의 **글자가 빈 칸처럼 보였다.**
실측: 선택된 버튼 배경 RGBA(1,1,1,1), 라벨 RGBA(1,1,1,1).

### 원인
`TAB_SELECTED` 를 순백으로 두고 **배경만** 바꿨다. 라벨이 원래 흰색이라 그 위에서 사라졌다.
주석에 "배경은 밝기만 건드린다"고 적어놓고 그 밝기가 글자를 지웠다.

### 고친 방식
배경과 라벨을 **함께** 뒤집는다. 배경을 진하게 바꾸는 안은 쓰지 않았다 —
`Accent` 자식이 종족색을 들고 있어 그쪽과 부딪힌다.

```csharp
private static readonly Color LABEL_SELECTED = new Color(0.212f, 0.231f, 0.290f, 1f); // #363B4A
private static readonly Color LABEL_NORMAL = new Color(1f, 1f, 1f, 1f);
```

어두운 쪽은 **GDD §10의 최암부 `#363B4A`** 다. 팔레트 밖의 검정을 새로 들이지 않는다.

`SetModeTabColor` 를 지우고 종족 탭과 모드 탭이 **같은 `SetTabColor`** 를 쓰게 했다 —
색을 뒤집는 자리가 둘이면 한쪽만 고쳐진다(오늘 이미 같은 종류의 사고를 겪었다).
