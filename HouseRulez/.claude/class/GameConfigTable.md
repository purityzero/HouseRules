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
