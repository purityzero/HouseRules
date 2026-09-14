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
