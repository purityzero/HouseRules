# UIInGameBattle

연관: [[BattleUnit]], [[InGameScene]], [[UIInGameField]], `JudgeResult`, `WaveRecord`, `EnemyRecord`, `UnitGradeRecord`, `GameConfigTable`

## 2026-08-30-0 — 신설 (웨이브 전투)

### 개요
웨이브 한 판. 아군은 판정 소환 결과에서, 적은 `WaveTable`에서 만들어 서로 진격시킨다.
**최소 수직 슬라이스**다 — 레인(릴의 행) 안에서만 교전하고 레인 간 간섭은 없다.
전열/중열/후열의 역할 차이(GDD §FieldLayout)는 전투가 실제로 도는 걸 본 뒤에 얹는다.

파일: `Assets/Scripts/InGame/Battle/UIInGameBattle.cs`

### 계층 구조 (InGameScene.unity)
```
SafeRoot
└─ Battle             RectTransform 1400x320, anchoredPos (520, 20)   ← UIInGameBattle (GO 1703635131)
   └─ UnitRoot        RectTransform 1400x320                          (rt 1680718474)
      └─ UnitTemplate ★ 비활성 원본 → [[BattleUnit]]
```
`Field`와 같은 x(520)에 둔다 — 소환 표시가 그대로 전투 유닛으로 이어지는 것처럼 보여야 한다.

### x 좌표 배치
| 필드 | 값 | 의미 |
|---|---|---|
| `m_AllyStartX` | 0 | 아군 출발선 |
| `m_EnemySpawnX` | 900 | 적 등장선 |
| `m_HomeLineX` | -60 | 적이 이 왼쪽을 넘으면 본거지가 맞는다 |

레인 오프셋은 `UIInGameField`와 같은 규칙(`LANE_STEP_Y = 52`, `LANE_STEP_X = 30`) — 소환 화면에서
전투 화면으로 넘어갈 때 유닛이 튀지 않도록 두 곳의 값을 맞춰뒀다.
★ 두 클래스에 같은 숫자가 있다. 한쪽만 고치면 어긋나므로, 배치 규칙을 다시 손댈 땐 반드시 함께 본다.

### 적 마릿수는 CSV에 없다 — 역산한다
`WaveTable.GetSpawnCount(wave, enemy, basePower)`가
`목표전력 / 적1기전력`으로 계산한다(최소 1). 마릿수를 CSV에 따로 적으면 `PowerCoef`와 이중 소스가 되어
한쪽만 고쳤을 때 밸런스가 조용히 어긋난다.

### 적 아트가 아직 없다
`Setup(..., sprite: null, ...)`로 세워 실루엣만 보인다. 아트가 생기면 `SpawnEnemies`의 그 자리에 물린다.
파일명 규칙은 `Image/InGame/Enemy/enemy_*`.

### 승패 판정
| 조건 | 결과 |
|---|---|
| 살아있는 적 0 | `Victory` |
| 살아있는 아군 0 | `Defeat` — 남은 적이 그대로 본거지로 가므로 그 웨이브는 진 것으로 끝낸다 |
| 그 외 | `Running` |

적이 `m_HomeLineX`를 넘으면 그 적은 사라지고 `m_HomeHit`이 1 오른다.
★ 아직 `homeHit`을 `RunData.TakeHomeDamage()`로 넘기는 호출부가 없다 — 웨이브 종료 처리가 붙을 때 연결한다.

### 공개 API
| 멤버 | 설명 |
|---|---|
| `Begin(judgeResult, grid, spritePool, wave)` | 양쪽 유닛 세우기 |
| `Tick(deltaTime)` | 전 유닛 진행 + 본거지 선 검사 + 승패 검사 |
| `Clear()` | 유닛 파괴 및 상태 초기화 |
| `result` / `homeHit` / `isRunning` | 읽기 |

### 검증 상태 — Codex QA 통과 A~G (2026-08-30)
`BattleStartButton.onClick.Invoke()`(프로덕션 진입점)로 시작해 확인:
아군 우측·적 좌측 이동 / HP 감소와 사망 비활성화 / 6.00s에 `Running → Defeat` /
적 마릿수 `0.85 × 6 ÷ 1 = 5.1 → 5` 일치 / 레인 교차 교전 0건.

### 남은 위험
- 36웨이브 테이블이 승인될 때 12연차 플레이어 전력을 ×18.0(프리즘)으로 잡았으나, 실제 판정 소환은
  3성 ×7.0에서 막힌다. 밸런스 재산정 필요(`UNFINISHED.md` 기록).
