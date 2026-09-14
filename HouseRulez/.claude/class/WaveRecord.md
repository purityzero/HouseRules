# WaveRecord / WaveTable

연관: [[EnemyRecord]], [[UIInGameBattle]], [[RunData]], [[InGameScene]], `GameConfigTable`

## 2026-08-30-0 — 신설 문서 (구현은 커밋 8efc247)

### 개요
12연차 × 3웨이브 = **36행**. 한 웨이브가 곧 스핀 1회다(연차당 스핀 코인 3개).

파일: `Assets/Scripts/Table/WaveRecord.cs` / `Assets/Resources/Table/WaveTable.csv`

### 컬럼
| 컬럼 | 의미 |
|---|---|
| `Year` / `WaveIndex` | 1~12 / 1~3. 둘이 합쳐 키가 된다 |
| `WaveType` | `Normal` / `Judgement` / `Final` |
| `PowerCoef` | 기준 전력에 곱할 계수. **밸런스는 이 값 하나만 만진다** |
| `EnemyKey` | `EnemyTable.Key` |

### ★ 적 마릿수를 CSV에 적지 않는다
```
목표전력 = PowerCoef × GameConfigTable[WaveBasePower]
마릿수   = max(1, round(목표전력 / EnemyRecord.Power))
```
마릿수와 계수를 둘 다 적으면 한쪽만 고쳤을 때 어느 게 맞는지 알 수 없다.
계수가 아무리 작아도 웨이브에 적이 0마리면 전투가 성립하지 않으므로 하한이 1이다.

### 조회
`GetRecord(year, waveIndex)` / `GetListByYear(year)`(WaveIndex 오름차순 정렬).
비교는 전부 `>=` + `<=` 범위 비교다(CODE.MD 「숫자 비교」).

### 남은 위험 — 밸런스 재산정 필요
36행이 승인될 때 **12연차 플레이어 전력을 ×18.0(프리즘)으로 잡았다.** 그런데 실제 판정 소환은
**3성 ×7.0**에서 막힌다(등급 상한). 후반 연차가 계산보다 훨씬 어렵다.
`UNFINISHED.md`에 재산정 항목으로 올려뒀다.

## 2026-08-31-0 — `EnemyHouse` 컬럼 추가

적이 몬스터가 아니라 **다른 종족**이 됐다(GDD 06장). 스탯은 `EnemyKey`가 계속 담당하고
이 컬럼은 **겉모습만** 정한다 — 상세는 [[EnemyHouseResolver]].

| 연차 | 상대 |
|---|---|
| 1 | `neutral` (아직 어느 종족과도 전쟁 전 — 종족색 없는 무리) |
| 2~12 | chess · janggi · poker · hwatu · mahjong · slot · yut · chess · janggi · poker · hwatu |

**연차 안에서는 세 웨이브가 같은 종족이다.** 침공 예고가 "올해 상대는 누구"를 보여주는 화면이라
연차 도중에 바뀌면 예고가 성립하지 않는다.

표에 적힌 종족이 내 종족이면 런타임에 다음 종족으로 민다([[EnemyHouseResolver]]`.Resolve`).

### 검증 — Codex QA 통과 (2026-08-31)
36/36행 채워짐, 1연차 3행 `neutral`, 7개 아군 종족 × 전 비중립 웨이브에서 **자기 종족 반환 0건**.

---

## 2026-09-14 — 12-2 `PowerCoef` 16.0 → 14.0

후반 곡선 완화의 일부(상세는 [[EnemyRecord]] 2026-09-14). brute 24 → **21마리**.

실측에서 12-1(20마리)은 넘고 **12-2(24마리)에서 막히고 있었다.** 그 사이로 낮췄다.
12-1(13.6)은 건드리지 않았다.

### ★ `PowerCoef`는 보스 웨이브에 효과가 없다

`GetSpawnCount`가 `Mathf.Max(1, count)`로 최소 1마리를 보장하므로,
**12-3처럼 적 1기(`king_final`, Power 110)인 웨이브는 계수를 낮춰도 같은 보스가 나온다.**
그 웨이브의 난이도는 전적으로 `EnemyTable`의 스탯이 정한다.

후반 완화를 계수로만 시도하면 **마지막 웨이브만 그대로 남는다** — 실제로 1차 조정에서
12-2는 통과하기 시작했는데 12-3에서 전멸했다.
