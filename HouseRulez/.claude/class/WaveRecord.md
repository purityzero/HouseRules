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
