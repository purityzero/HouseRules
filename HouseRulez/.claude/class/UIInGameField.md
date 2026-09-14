# UIInGameField

연관: [[InGameScene]], [[UIInGameFieldSlot]], [[UIInGameSummary]], [[UIHouseSlotMachine]], `JudgeResult`, `Judge`

## 2026-08-30-0 — 신설 (전장 3×3 + 깊이 배치)

### 개요
판정으로 소환된 아군을 **게임 배경의 바닥 위**에 세우는 표시 전용 UI. 릴 3×3과 칸이 1:1로 대응한다
(칸 인덱스 0~8, 행 우선 — `Judge`·`UIHouseSlotMachine`과 같은 좌표계).

파일: `Assets/Scripts/InGame/UI/UIInGameField.cs`

### 계층 구조 (InGameScene.unity)
```
SafeRoot
└─ Field                RectTransform 360x260, anchoredPos (520, 20)   ← UIInGameField (rt 508569371)
   └─ SlotRoot          RectTransform 360x200                          (rt 1437846840)
      └─ SlotTemplate   RectTransform 100x100  ★ 비활성 원본, 코드가 8개 더 복제한다
         ├─ Symbol      Image (stretch)
         └─ Grade       TMP 60x28, 우하단
```
`SlotTemplate`의 씬 크기는 100×100이지만 **코드가 `sizeDelta`를 96×96으로 덮어쓴다**(아래 참고).

### 배치 규칙 — 그리드가 아니라 바닥
`GridLayoutGroup`을 쓰지 않고 칸마다 `anchoredPosition`을 직접 계산한다. LayoutGroup은 균일 격자만
만들 수 있어 레인별 x 밀기(원근)를 표현하지 못한다.

| 상수 | 값 | 의미 |
|---|---|---|
| `COLUMN_SPACING` | 108 | 릴의 **열** = 전장의 깊이. 1열=후열(원거리) · 3열=전열(탱커). 적이 우측에서 오므로 전열이 오른쪽 끝 |
| `LANE_STEP_Y` | 52 | 릴의 **행** = 3개 레인. 뒤 레인일수록 위로 |
| `LANE_STEP_X` | 30 | 뒤 레인일수록 오른쪽으로 — 바닥이 비스듬히 깔린 것처럼 보인다 |
| `SLOT_SIZE` | 96 | 심볼이 32px 픽셀아트라 ×3. 정수 배율만 허용 |

`laneFromFront = 2 - row` → row 0이 가장 뒤 레인이라 화면 위쪽으로 간다.
`SetSiblingIndex(cell + 1)`로 앞 레인이 뒤 레인을 가리도록 그리기 순서를 맞춘다(0번은 템플릿 자리).

### ★ 레이아웃 상수를 직렬화하지 않는 이유
처음엔 `[SerializeField] private float m_ColumnSpacing = 108f;` 형태였다. 그런데 컴포넌트가 **씬에 이미
저장된 뒤에** 필드를 추가하면 역직렬화 시 그 값이 `0`으로 들어온다 — 인스펙터 기본값이 아니라 0이다.
그 결과 **9칸이 전부 같은 자리에 겹쳤고**, 씬에 남아 있던 100×100이 96×96을 덮었다.
그래서 배치의 소유자를 코드 한 곳으로 고정했다(`const` + 코드가 `sizeDelta`까지 대입).

> 교훈: 이미 씬/프리팹에 저장된 컴포넌트에 `[SerializeField]`를 나중에 추가하면 기본값이 아니라 0이 들어온다.

### 크기를 레인마다 줄이지 않는 이유
원근이라면 뒤 레인을 작게 하는 게 자연스럽지만, 심볼이 32px 픽셀아트라 정수 배율(×3=96) 외의 값을
쓰면 픽셀이 뭉개진다. 깊이는 **위치 오프셋만으로** 표현한다.

### 공개 API
| 메서드 | 하는 일 |
|---|---|
| `Apply()` | 템플릿을 9칸으로 늘리고(`BuildSlots`) 배치한 뒤(`LayoutSlots`) 비운다. `InGameScene.OnSetup()`에서 1회 |
| `ShowSummon(JudgeResult, int[] grid, spritePool)` | 소환 슬롯을 칸에 세우고 요약 패널 문구를 채운다 |
| `Clear()` | 전 칸 비우기 + 요약 문구 비우기 |

`_spritePool`은 슬롯머신이 이미 만들어 둔 것을 넘겨받는다 — 여기서 다시 로드하면 같은 파일을 두 번 읽는다.

### 검증 상태 — Codex QA 통과 (2026-08-30)
Play Mode에서 9칸 좌표가 전부 다르고, 크기 96×96, 레인별 y 오프셋 52·x 오프셋 30이 적용됨을 확인.
Unity MCP 검증은 Codex가 수행했다(AGENT.MD 라우팅).


---

## 2026-09-13 — 드래그 중재와 `RefreshSlots()`

**연관**: [[UIInGameFieldSlot]] · [[RunRoster]]

### 수정

| 추가 | 하는 일 |
|---|---|
| `m_Roster` · `m_SpritePool` | 드래그 후 다시 그리려면 들고 있어야 한다. **소유자는 `RunData`** — 표시용 참조일 뿐 사본이 아니다 |
| `RefreshSlots()` | 칸만 다시 그린다. **판정 요약은 건드리지 않는다** |
| `IsDragAllowed()` | `m_Roster != null` |
| `OnSlotDragBegin` / `OnSlotDrop` / `OnSlotDragEnd` | 출발지·목적지를 모아 두고 끝날 때 한 번 처리 |

`BuildSlots()` 마지막 루프에서 각 칸에 `Setup(this, i)` 를 부른다.

### ★ 전투 중 드래그가 별도 플래그 없이 막힌다

`Clear()` 에서 `m_Roster` 를 놓는다. `Clear()` 는 `OnBattleStart` 에서 불리므로
**전투 중에는 `IsDragAllowed()` 가 자연히 false** 가 된다. 플래그를 따로 두면
그 플래그를 내리는 것을 잊는 경로가 생긴다.

### ★ 교환은 명부에 맡긴다

`OnSlotDragEnd()` 가 `RunRoster.SwapField(from, to)` 를 부른 **뒤에만** 다시 그린다.
화면만 바꾸면 전투가 옛 배치로 싸운다 — 전장의 정본은 `RunRoster` 다(2026-09-11).

`SwapField` 가 false 를 돌려주면(둘 다 빈 칸 등) 다시 그리지 않는다.
칸 밖에 놓았으면 목적지가 `CELL_NONE` 이라 아무 일도 하지 않는다.

### 호출 순서 (Unity)

`OnBeginDrag` -> `OnDrag` … -> **(대상 칸의)`OnDrop`** -> `OnEndDrag`
목적지가 `OnEndDrag` 시점에 이미 정해져 있어 한 번만 처리할 수 있다.

### ⬜ 아직 없는 것

- **보관함 UI** — 그래서 `RunRoster.MoveBenchToField()` · `MoveFieldToBench()` 는 호출부가 없다.
  이번에는 전장 칸끼리 교환만 붙였다
- **드래그 고스트** — 원본 심볼을 직접 옮기는 방식이라 별도 오브젝트가 없다. 씬 작업이 필요하면 그때 분리


---

## 2026-09-14 — 자유 배치: 좌표가 정본, 그리기 순서는 y 로

### 중재가 단순해졌다

격자 시절에는 "어느 칸에 놓였나"를 판정해야 해서 출발지·목적지를 모아 두고
`IDropHandler` 로 대상을 받았다. **자유 배치에서는 놓인 좌표를 그대로 쓰므로 그 과정이 사라졌다.**

| 제거 | 추가 |
|---|---|
| `OnSlotDragBegin` · `OnSlotDrop` · `OnSlotDragEnd` | `TryMoveUnit(cell, position)` |
| `m_DragFromCell` · `m_DragToCell` | `RefreshDrawOrder()` |

### ★ 그리기 순서를 y 로 다시 정한다

격자였을 때는 칸 번호가 곧 깊이라 `SetSiblingIndex(cell + 1)` 로 끝났다.
**자유 배치에서는 y 가 낮을수록 앞**이므로 좌표로 정렬해야 한다 — 안 그러면 뒤에 선 유닛이
앞 유닛을 덮는다.

y 내림차순(뒤에 있는 것을 먼저 그린다)으로 삽입 정렬한다. **같은 y 면 순서를 유지**하도록
안정 정렬을 썼다 — 불안정 정렬이면 같은 높이의 유닛들이 매 갱신마다 앞뒤로 바뀐다
(`ArrangeFieldByGrade` 에서 겪은 것과 같은 함정).

### 좌표 규칙은 FieldLayout 이 소유한다

`COLUMN_SPACING` · `LANE_STEP_Y` · `LANE_STEP_X` 를 여기서 지웠다.
같은 상수를 `UIInGameBattle` 도 갖고 있어 **소유자가 둘**이었다.

`LayoutSlots` 는 이제 크기·기준점만 잡고 **자리를 정하지 않는다.** 위치의 정본은 명부이고
`RefreshSlots` 가 `runUnit.FieldPosition` 을 읽어 놓는다.

## 2026-09-14-1 — 배치 가능 영역 표시

### 개요
드래그하는 동안 **어디까지 둘 수 있는지**를 화면에 보여준다. 그전에는 놓아봐야 알았고,
튕겨 돌아와도 왜 거부됐는지 알 길이 없었다(2026-09-14 사용자 요청).

혼란의 원인이 둘이었다 —
- 영역 밖으로 끌면 `FieldLayout.Clamp`가 조용히 잘라서 **엉뚱한 자리에 붙는다**
- 영역 안인데도 다른 유닛과 54px 미만이면 `IsPositionFree`가 거부해 **원래 자리로 튕긴다**

### 추가한 것

| 멤버 | 하는 일 |
|---|---|
| `ShowPlacementArea(bool)` | 드래그 중에만 영역을 켠다. 첫 호출에 만들고 이후 재사용 |
| `IsPlacementValid(int, Vector2)` | 그 자리에 놓을 수 있는가. 영역 밖 + 간격 위반을 함께 본다 |
| `CreatePlacementArea()` | 영역 `Image`를 런타임에 만든다 |
| `m_PlacementArea` | 만든 `RectTransform`. 직렬화하지 않는다 |
| `PLACEMENT_AREA_COLOR` | `(1,1,1,0.12)` — 배경을 가리지 않을 만큼만 |

### 씬·프리팹을 고치지 않는다
영역을 런타임에 만든다. `UIInGameFieldSlot.Setup`이 이미 `AddComponent<Image>()`를 쓰는 선례가 있고,
무엇보다 **크기가 `FieldLayout` 상수에서 파생**되므로 인스펙터 값과 코드가 갈릴 여지가 없다.
직렬화 필드로 뒀다면 9칸이 0으로 겹쳤던 사고와 같은 종류의 위험을 다시 들이는 셈이다.

### 좌표 — 영역은 상수 범위보다 칸 한 변만큼 크다
칸의 `pivot`이 `(0,0)`이라 `anchoredPosition`은 **좌하단 모서리**다.
`FieldLayout.AREA_*`는 그 모서리의 허용 범위이므로, 유닛이 실제로 덮는 영역은
`(0,0) ~ (276+96, 104+96)` = `(0,0) ~ (372,200)`이다.

```csharp
areaRect.sizeDelta = new Vector2(
    FieldLayout.AREA_MAX_X - FieldLayout.AREA_MIN_X + SLOT_SIZE,
    FieldLayout.AREA_MAX_Y - FieldLayout.AREA_MIN_Y + SLOT_SIZE);
```

### 영역 밖도 무효로 친다
실제 거부는 간격 위반뿐이고 영역 밖은 `SetFieldPosition`이 잘라서 받는다.
그래도 **끄는 사람에게는 "의도한 자리에 못 놓는다"는 점이 같아서** 함께 빨강으로 알린다.
잘려서 엉뚱한 데 붙는 것을 놓기 전에 알 수 있다.

### 매 프레임 도는 경로다
`IsPlacementValid`는 드래그 중 매 프레임 불린다. 할당하지 않으며, 명부 쪽 비교는
유닛 9기 기준 최대 8회다. `raycastTarget = false`로 두어 포인터 판정도 가로채지 않는다.

## 2026-09-14-2 — 요약 패널에 재료를 넘긴다

`BuildSummaryText()` 를 [[UIInGameSummary]] 로 옮겼다. 패널이 탭 셋(결과·족보·유닛)을
보여주게 되면서 **보여주는 쪽이 셋을 다 만드는** 편이 자연스러워졌다.

```csharp
m_Summary.SetText(BuildSummaryText(_result));   // 전
m_Summary.Apply(_result, _roster, _houseKey);   // 후
```

`Show()` 가 `_houseKey` 를 받는 이유가 둘이 됐다 — 고유 유닛 스프라이트 찾기와 족보 조회.
