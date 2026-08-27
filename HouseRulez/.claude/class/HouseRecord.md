# HouseRecord

연관: `HouseTable`/`HouseSpriteLoader`(같은 파일, `Assets/Scripts/Table/HouseRecord.cs`), `PlayerManager`, `UIHouseSelect`, `UIHouseSlotMachine`, [[UIInGameHud]], [[SutdaBetTable]]

## 2026-08-27-0 — `isUseBet` 컬럼 추가

### 개요
인게임 HUD에 판돈("판돈 N ×M.M")을 표시하는데, 판돈은 GDD 05장 기준 **화투(섯다)만 쓰는 개념**이다.
어느 종족이 판돈을 쓰는지를 코드에서 종족 키로 비교하지 않기 위해 레코드 속성으로 옮겼다.

### 수정 전
```csharp
public string SpriteFolder;
public string BackgroundPath;
public int isUnlocked;
```

### 수정 후
```csharp
public string SpriteFolder;
public string BackgroundPath;
public int isUnlocked;

// 판돈(배팅)을 쓰는 종족인가. GDD 05장 기준 화투(섯다)만 해당한다 —
// 종족 키를 코드에서 직접 비교하지 않으려고 레코드 속성으로 둔다.
public int isUseBet;
```

### HouseTable.csv
`isUnlocked` 뒤에 `isUseBet` 컬럼을 추가하고 화투(`Id 3`)만 `1`, 나머지 넷은 `0`.

파서가 헤더 개수만큼 `values[j]`를 그대로 집기 때문에 **모든 행에 값을 채워야 한다** —
한 행이라도 비면 인덱스 초과로 예외가 나고 테이블 전체 로드가 통째로 실패한다.

### 왜 `bool`이 아니라 `int`인가
CSV 파서가 `Convert.ChangeType(value, field.FieldType)`을 쓰는데 `"1"` → `bool` 변환은 실패한다.
기존 `isUnlocked`도 같은 이유로 `int`다 — 그 선례를 따랐다.
