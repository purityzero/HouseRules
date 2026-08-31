# UIInGameAction

연관: `InGameScene`(`Assets/Scripts/InGame/InGameScene.cs`), [[RunData]], [[UIInGameHud]](같은 관례로 만든 형제), `StringTable`

## 2026-08-27-0 — 신설 (인게임 하단 ACTION 바)

### 개요
기획서 §10 ScreenZones의 ACTION 영역(네이티브 `144,250 – 492,284`)을 ×3한 **1044×102**.
전투 시작 · 스왑 잔여 · 배속을 담는다. 기획서 비고의 "우하단 엄지 사정권" 그대로 화면 하단 우측.

파일: `Assets/Scripts/InGame/UI/UIInGameAction.cs`

### 계층 구조 (InGameScene.unity)
```
SafeRoot
└─ Action                    RectTransform 1044x102, SafeRoot 좌상단 (432,-750)   ← UIInGameAction
   ├─ BattleStartButton      Image #D1554C + Button, x=24 size 288x72
   │  └─ Label               TMP, stretch, 가운데정렬 fontSize 30 #F6F5F0
   ├─ SwapPipRoot            RectTransform, x=336 w=72 (2칸 × 32 + 간격 8)
   │  └─ PipTemplate         Image #F6F5F0 32x32  ★ 비활성 원본, 코드가 복제한다
   ├─ SwapText               TMP, x=424 w=160 좌측정렬 #ADACA4
   └─ BattleSpeedButton      Image #454B5E + Button, x=756 size 264x72
      └─ Label               TMP, stretch, 가운데정렬 fontSize 30 #F6F5F0
```
Action 루트에는 배경 패널을 두지 않았다 — 목업에서도 하단은 버튼만 떠 있고 띠가 없다.

### 직렬화 필드 ↔ 씬 fileID
| 필드 | fileID | 타입 |
|---|---|---|
| `m_BattleStartButton` | 900200304 | Button |
| `m_BattleStartText` | 900200312 | TextMeshProUGUI |
| `m_SwapPipRoot` | 900200321 | RectTransform |
| `m_SwapPipTemplate` | 900200332 | Image |
| `m_SwapText` | 900200342 | TextMeshProUGUI |
| `m_BattleSpeedButton` | 900200354 | Button |
| `m_BattleSpeedText` | 900200362 | TextMeshProUGUI |

두 버튼의 `m_TargetGraphic`은 각자의 `Image`(900200302 / 900200352)로 채웠고 그 Image들만
`m_RaycastTarget: 1`이다 — `PREFAB.MD`가 적어둔 "클릭 안 되는 버그의 상당수"가 이 두 가지 누락이다.

### 클릭 연결 — 코드에서
`Awake()`에서 `onClick.AddListener()`로 붙인다. 씬 YAML에 UnityEvent Persistent Call을 손으로 쓰지
않는다(`PREFAB.MD` 지침, `InGameScene`의 SPIN 버튼과 같은 방식).

### 전투 시작을 이 클래스가 처리하지 않는 이유
배치/전투 단계가 아직 없어 **받을 쪽이 정해지지 않았다.** 그래서 `event Action OnBattleStart`로 올리기만
하고, 지금은 `InGameScene`이 받아 로그만 찍는다(`TODO`). 단계가 생기면 그 자리에서 이어붙인다.

배속은 반대로 런 상태(`RunData.m_BattleSpeed`) 변경이라 `OnBattleSpeed` 이벤트 → `InGameScene`이
`ToggleBattleSpeed()` 호출 → `Refresh()`로 되돌아온다. **UI가 직접 `RunData`를 고치지 않는다** —
고치는 경로가 둘로 갈라지면 서로 덮어쓴다.

### StringTable 키 (이번에 추가)
`ActionBattleStart` / `ActionSwap`(`스왑 {0}`) / `ActionBattleSpeed`(`배속 ×{0}`)

### 아직 안 되는 것
- 전투 시작 버튼은 로그만 찍는다(전투 단계 없음)
- 스왑 잔여는 항상 최대치로 뜬다 — 스왑을 소모하는 배치 단계가 없다
- 배속 토글은 표시만 바뀐다 — 배속을 실제로 적용할 전투가 없다

### 검증 상태 — 미검증
Unity MCP 미연결 세션이라 씬 YAML 직접 작성. 파일 정합성만 스크립트로 대조했고 컴파일/플레이는 못 했다.

## 2026-08-30-0 — ACTION 바를 화면 위로 올림

### 배경
유닛을 배경의 바닥에 세우면서(→ [[UIInGameField]]) 하단에 있던 ACTION 바가 유닛과 겹쳤다.

### 수정
`Action` RectTransform: 앵커 상단(`m_AnchorMin (0,1)`), `anchoredPosition (432, -84)`, 크기 1044×102.
슬롯머신 오른쪽·HUD 아래에 붙는 위치다. 하단은 전장(바닥)이 통째로 쓴다.

### 검증 상태 — Codex QA 통과 (2026-08-30)

## 2026-08-30-1 — 스왑 칸이 늘면 라벨을 덮던 문제

### 증상
스왑 최대치가 3칸 이상인 세이브에서 `스왑 3` 라벨이 칸 위에 겹쳐 찍혔다.

### 원인
`BuildSwapPipList`가 칸만 만들고 **`SwapPipRoot`의 폭을 안 고쳤다.**
씬에 2칸 기준 폭 72(32×2 + 간격 8)가 박혀 있어, 3칸(112)이 되면 루트 밖으로 40px 뻗는다.
`SwapText`는 x=424 고정이고 칸은 336에서 시작하니 336+112=448 > 424 → 24px 겹침.

★ **[[UIInGameHud]]에서 똑같은 결함을 이미 고쳤는데 여기를 빠뜨렸다.**
그때 "루트 폭을 코드가 소유한다"로 고쳤지만 그 판단을 HUD에만 적용했다 —
CLAUDE.md 「한 사례만 보고 끝내지 않는다」를 그대로 밟았다.

### HUD와 달리 폭만 고쳐선 안 됐다
HUD는 `HorizontalLayoutGroup`이 있어 폭이 늘면 뒤 항목이 알아서 밀린다.
**ACTION 바는 레이아웃 그룹이 없고 자식이 전부 절대 좌표다** — 폭만 늘리면 아무도 안 밀린다.

### 수정
1. `BuildSwapPipList`가 실제 칸 수로 `m_SwapPipRoot.sizeDelta`를 갱신
2. `LayoutSwapText(rootWidth)`가 라벨을 `칸 오른쪽 끝 + 간격` 위치로 이동
3. 그 간격은 `CacheSwapTextGap()`이 **첫 빌드 전에 씬 값에서 읽어 캐시**한다 —
   코드에 16을 적으면 씬과 이중 소스가 되어 한쪽만 바뀔 때 어긋난다

레이아웃 그룹을 새로 붙이지 않은 이유: ACTION 바 위치를 막 조정한 참이라
그룹을 넣으면 전투 시작·배속 버튼 배치가 함께 흔들린다. 코드 수정이 침습이 더 작다.

### 검증 (Codex, Play Mode · 화면 좌표 실측)
| 최대 칸 | 칸 x범위 | 라벨 x범위 | 간격 | 겹침 |
|---|---|---|---|---|
| 2 | 768–840 | 856–1016 | 16 | 0 |
| 3 | 768–880 | 896–1056 | 16 | 0 |
| 4 | 768–920 | 936–1096 | 16 | 0 |
| 5 | 768–960 | 976–1136 | 16 | 0 |

라벨↔배속 버튼도 전부 겹침 0. 실제 문자열 `스왑 2`~`스왑 5` 확인(키 이름 노출 없음).
HUD 회귀도 같이 확인 — HP 10칸(160–454) ↔ YEAR(470–690) 간격 16, 겹침 0.
