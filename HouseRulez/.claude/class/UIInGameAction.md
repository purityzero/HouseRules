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
