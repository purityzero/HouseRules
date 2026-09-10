# GameConfigTable

연관: `GameConfigRecord`(같은 파일, `Assets/Scripts/Table/GameConfigRecord.cs`), `TableManager`(등록처), [[RunData]](유일한 소비자)

## 2026-08-27-0 — 신설 (단일 스칼라 튜닝값 키-값 테이블)

### 개요
`CODE.MD`가 "단일 스칼라 튜닝값(배율, 임계값, 지속시간 등)은 `GameConfigTable`에 키-값 행으로 추가한다"고
정해 뒀는데 이 프로젝트에는 그 테이블이 아직 없었다. 인게임 HUD의 런 초기값(HP 8칸, 12연차 등)을 넣을
자리가 필요해 이번에 만들었다.

### 파일
- `Assets/Scripts/Table/GameConfigRecord.cs` — `GameConfigRecord` + `GameConfigTable`
- `Assets/Resources/Table/GameConfigTable.csv`

### 스키마
```
Id,Key,Value
```
`Value`는 `int`다. 소수가 필요한 값(판돈 배수 등)은 여기가 아니라 전용 테이블로 간다 —
한 컬럼에 정수와 소수를 섞으면 읽는 쪽이 매번 어느 쪽인지 알아야 한다.

### 현재 행
| Key | Value | 근거 |
|---|---|---|
| `HomeHpMax` | 8 | GDD §10 ScreenZones "HP는 핍 8칸" |
| `RunYearMax` | 12 | GDD §09 "1런 = 12연차" |
| `RunStartGold` | 0 | GDD에 시작 골드 명시 없음 — 상점이 생길 때 정한다 |
| `SpinCoinPerYear` | 3 | GDD §03 SpinEconomy "기본 스핀 3" |
| `SwapCountPerYear` | 2 | GDD §08 "배치 단계에서 무료 스왑 2회" |
| `BattleSpeedFast` | 2 | 목업 하단 "배속 ×2" |

### 키 상수
호출부가 문자열을 다시 적으면 오타가 조용히 기본값으로 흘러가므로(파서가 없는 키를 에러 없이 넘기는
구조와 같은 함정), 키를 `GameConfigTable`의 `const`로 두고 그것만 쓴다.

### GetValue가 로그를 남기는 이유
`CLAUDE.md`의 "데이터 파일 컬럼/키 이름 불일치 — 조용히 기본값으로 귀결" 항목 그대로다. 키가 없을 때
기본값만 돌려주고 조용하면, 값이 이상할 때 계산 로직부터 의심하게 된다. 그래서 `Logger.Error`를 남긴다.

---

## 2026-09-10-0 — 런 종료·추가 스핀 키 4개 추가

기획 스펙 `.claude/design/run-end-flow.html` 구현에 따라 추가했다.

| Key | 값 | 뜻 |
|---|---|---|
| `HomeDamagePerLeak` | 1 | 성문을 넘은 적 1마리당 본거지 피해 |
| `HomeDamagePerDefeat` | 1 | 패배 자체의 고정 피해 |
| `ExtraSpinGoldCost` | 25 | 추가 스핀 1개 가격(GDD 03장, 가격 고정) |
| `ExtraSpinMaxPerYear` | 2 | 연차당 구매 가능 횟수 |

### ★ `SpinCoinPerYear`를 3 → 5로 올렸다가 되돌렸다

「패배하면 같은 웨이브를 다시 한다」 규칙(Q3-B)을 붙이면서 재도전 비용이 필요해
2026-09-10에 임시로 5로 올렸다. **그러나 GDD 03장이 이미 그 여유를 정의하고 있었다** —
*"기본 스핀 3 / 추가 스핀 골드 25 → +1, 연차당 2회까지"*. 즉 **3 + 2 = 5**가 설계값이고,
그 2를 공짜로 주는 대신 **골드로 사게 하는 것**이 원래 의도였다.

→ 3으로 되돌리고 `ExtraSpin*` 키 2개를 추가했다.
**튜닝값을 바꾸기 전에 GDD에 이미 정의가 있는지 먼저 찾을 것** — 같은 숫자를 두 방식으로 만들 뻔했다.

### `HomeDamagePerDefeat`가 왜 따로 필요한가
적이 한 마리도 성문을 못 넘고 아군만 전멸하는 판이 있다(그때 leak = 0).
고정분이 없으면 **그 패배는 아무 대가 없이 지나간다.** 실제 피해는 두 값의 합이다.
