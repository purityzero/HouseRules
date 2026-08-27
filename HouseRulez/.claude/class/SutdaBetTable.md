# SutdaBetTable

연관: `SutdaBetRecord`(같은 파일, `Assets/Scripts/Table/SutdaBetRecord.cs`), `TableManager`(등록처), [[UIInGameHud]](표시), `HouseRecord`(`isUseBet` 컬럼으로 대상 종족을 가린다)

## 2026-08-27-0 — 신설 (화투 판돈 단계)

### 개요
HUD 우측의 "판돈 N ×M.M" 표시에 필요한 레벨→배수 매핑. GDD 05장 SutdaBet 표에서 가져왔다.

### 파일
- `Assets/Scripts/Table/SutdaBetRecord.cs`
- `Assets/Resources/Table/SutdaBetTable.csv`

### 스키마와 현재 행
```
Id,Level,Multiplier
1,0,1.0
2,1,1.9
3,2,3.2
```
GDD 기준: 판돈0 ×1.0(몰수선 없음) / 판돈1 ×1.9(끗 이하 몰수) / 판돈2 ×3.2(특수끗 이하 몰수).
**몰수선 컬럼은 넣지 않았다** — 판정기(족보 판정)가 아직 없어 어떤 형태로 표현할지가 정해지지 않았다.
추측해서 컬럼을 만들면 판정기가 생길 때 어차피 갈아엎게 된다.

### 조회를 인덱스로 하는 이유
`GetRecordByLevel(_level)`은 `list[_level]`로 바로 집는다. `HouseTable`과 같이 **Id 순서가 곧 단계 순서**인
테이블이라 선형 탐색으로 `Level`을 비교할 이유가 없고, `CODE.MD`의 "정수 `==` 비교 금지"에도 걸리지 않는다.
범위를 벗어나면 `null`을 돌려준다.

### 왜 별도 테이블인가
판돈은 "레벨 × 속성"의 격자형 데이터라 `GameConfigTable`(키-값 스칼라)에도, `HouseTable`(종족 1행)에도
안 맞는다. `CODE.MD`의 "격자형 데이터가 기존 스키마에 안 맞으면 전용 테이블을 새로 만든다" 그대로다.

### 화투 전용이라는 걸 어떻게 아는가
종족 키를 코드에서 `== "hwatu"`로 비교하지 않는다. `HouseTable.csv`에 `isUseBet` 컬럼을 추가해
레코드가 직접 들고 있고, `UIInGameHud`는 그 값만 본다.
