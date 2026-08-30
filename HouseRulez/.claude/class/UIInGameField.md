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
