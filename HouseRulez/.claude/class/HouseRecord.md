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

## 2026-08-27-1 — 슬롯 종족 추가 (`.claude/design/slot-house.html` 확정 스펙 반영)

### 개요
6번째 종족 슬롯을 `HouseTable.csv`에 추가하고, 홀드 상한 컬럼 `HoldMax`를 신설했다.
사용자 승인 3건: (1) 슬롯만 홀드 상한 2칸 (2) 슬롯을 해금 순서 1번(시작 종족)으로 (3) `AxisCeiling`=86 / `AxisLearning`=5.

### 수정 전 (`HouseTable.csv`, 헤더 13컬럼 · 5행)
```
Id,Key,NameKey,AccentColor,PoolCount,AxisPower,AxisVariance,AxisCeiling,AxisLearning,SpriteFolder,BackgroundPath,isUnlocked,isUseBet
1,chess,... / 2,janggi,... / 3,hwatu,... / 4,poker,... / 5,mahjong,...
```

### 수정 후 (헤더 14컬럼 · 6행 — `HoldMax` 추가, Id 재번호)
```
Id,Key,NameKey,AccentColor,PoolCount,AxisPower,AxisVariance,AxisCeiling,AxisLearning,SpriteFolder,BackgroundPath,isUnlocked,isUseBet,HoldMax
1,slot,HouseSlot,D45C9E,6,62,70,86,5,slot,Image/Title/bg_slot_neonstrip,1,0,2
2,chess,HouseChess,7FA7C9,6,61,38,45,10,chess,Image/Title/bg_chess_castle,1,0,4
3,janggi,HouseJanggi,57A277,7,63,40,65,35,janggi,Image/Title/bg_janggi_fortress,1,0,4
4,hwatu,HouseHwatu,D1554C,12,62,92,100,45,hwatu,Image/Title/bg_hwatu_moonfield,1,1,4
5,poker,HousePoker,D6A441,13,62,97,100,25,poker,Image/Title/bg_poker_frontier,1,0,4
6,mahjong,HouseMahjong,9B7FD4,9,62,62,0,0,mahjong,Image/Title/bg_mahjong_teahouse,1,0,4
```
전 행 필드 개수 14로 일치 확인(헤더 포함 7줄 × 14). 하나라도 비면 테이블 로드 전체 실패(2026-08-27-0 교훈).

### `HouseRecord.cs` 수정 전/후 (필드)
```csharp
// 전
public int isUseBet;
}

// 후
public int isUseBet;

// 홀드(재굴림 전 보류) 가능 칸 수 상한. 기본값 4, 슬롯만 2(GDD 밖 종족 전용 밸런스 조정, 2026-08-27 승인).
// 현재 이 값을 소비하는 홀드 로직은 없다(슬롯 판정기 미구현) — 컬럼/필드만 선반영.
public int HoldMax;
}
```
클래스 선언부 상단 주석의 해금 순서 나열도 `체스 -> 장기 -> ...`에서 `슬롯 -> 체스 -> 장기 -> ...`로 정정했다(`Assets/Scripts/Table/HouseRecord.cs:5`).

### 홀드 기능 구현 여부
**미구현.** 프로젝트 전체(`Assets/Scripts`)에 "hold" 관련 로직이 없다(grep 0건). `HoldMax` 컬럼·필드만 선반영했고, 소비하는 코드는 없다 — UNFINISHED.md 기준 판정기(`SlotJudge`) 자체가 아직 없다.

### Id 하드코딩 참조
`Assets/Scripts` 전체에서 `HouseRecord`/`HouseTable`을 참조하는 곳은 전부 `GetRecordByKey(문자열)` 경유였고, `.Id`를 직접 비교/참조하는 코드는 없었다(grep 확인). Id 재번호로 인한 영향 없음.

### `AxisCeiling`(마작, 미채움) — 계산만
스펙 §8.2 공식(`45 × (99%분위/평균 비 ÷ 3.25)`, 체스 기준 환산)을 마작에 적용하려면 마작의 1스핀 99%분위 값이 필요하다. `.claude/design/mahjong-house.html`에서 해당 수치를 찾지 못해 계산하지 못했다 — **채우지 않았고 채울 근거도 없다** (원 지시대로 승인 대상 아님, 손대지 않음).

### StringTable
`Assets/Resources/Table/StringTable.csv`에 Id 26 `HouseSlot`(슬롯/Slot/老虎机/スロット) 추가. 스펙 §12.2(배당표 UI용 27~36)는 문서 자체가 "배당표 UI가 실제로 만들어질 때 추가"라고 명시해 이번엔 제외.

### 검증
Unity MCP 연결 확인됨 — `refresh_unity(compile=request)` 후 `read_console(types=[error])` 0건. 컴파일 에러 없음.
