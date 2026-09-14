# 2026-09-14 — 판정 고유 유닛 + 족보 패널 (진행 중)

브랜치 `work/2026-09-14-pattern-unit` (`da7e788` 위). **두 주제가 섞여 있어 커밋을 나눠야 한다.**

## 왜 시작했나

사용자 지적: **"당첨됐다고 숫자가 다 나오니까 게임이 지저분해"**

측정으로 확인했다(7종족 × 20,000판):

| 종족 | 평균 | 최대 | 9칸 초과 |
|---|---:|---:|---:|
| 포커 | 6.3기 | **84기** | **25.4%** |
| 장기 | 6.4기 | 36기 | 21.8% |
| 화투 | 6.1기 | 62기 | 19.0% |
| 체스 | 6.1기 | 44기 | 17.6% |
| 슬롯 | 6.2기 | 77기 | 17.0% |
| 마작 | 5.9기 | **87기** | 2.7% |
| **윷** | **2.6기** | **3기** | **0.0%** |

**윷만 멀쩡한 이유가 답이었다** — 윷은 판정 자체가 유닛이 된다(가로 3줄 = 말 3기).
나머지 여섯이 `BuildSummon`(전력 → 1성 N기)을 지나며 숫자를 쏟는다.
즉 새 발상이 아니라 **프로젝트 안에 이미 있던 구조를 옮기는 일**이다.

## 주제 A — 판정 고유 유닛 (포커만)

### 한 일

| 파일 | 내용 |
|---|---|
| `UnitTable.csv` | `PatternKey`·`SpriteName` 컬럼 + 고유 유닛 2행(Id 60·61) |
| `Assets/Scripts/Table/UnitRecord.cs` | 두 필드, `FindPatternUnit()`, `GetBattleStat` PatternKey 오버로드, `BuildStat` 공통화 |
| `Assets/Scripts/InGame/Judge/JudgeResult.cs` | `SummonSlot.PatternKey`+`isPatternUnit`, `JudgeTerm.PatternKey` |
| `Assets/Scripts/InGame/Judge/Judge.cs` | `BuildPatternUnits()`, `AddTerm` 확장, 전력 차감 |
| `Assets/Scripts/InGame/RunUnit.cs` | `PatternKey`·`isPatternUnit`·`SYMBOL_NONE` |
| `Assets/Scripts/InGame/RunRoster.cs` | `AddUnit` 오버로드, `IsSameKind` 에 고유 유닛 비교 |
| `Assets/Scripts/InGame/InGameScene.cs` | 소비 루프 분기, `Show()` 에 houseKey |
| `Assets/Scripts/InGame/UI/UIInGameField.cs` | `FindUnitSprite()`, 패턴 스프라이트 캐시 |
| `Assets/Scripts/InGame/Battle/UIInGameBattle.cs` | `GetBattleStat` 에 PatternKey 전달 |
| `Assets/Scripts/Table/HouseRecord.cs` | `LoadPatternDictionary()` |
| `Image/InGame/Pattern/poker/` | **새로 그린 픽셀아트 2종** + x8 |

### 결과

| 판정 | 전 | 후 |
|---|---:|---|
| 트리플 1줄 | 40기 | 1기 (Hp 400 / Atk 80) |
| 스트레이트 2줄 | 33기 | 3기 → 3합 승급 시 2성 1기 |
| 페어 | 1기 | 그대로 |

**전력이 보존된다** — `HpRate`/`AtkRate` 를 JudgeTable 계수와 같은 값으로 두어
계수 11 = 1성 11기분(Hp 110 / Atk 22)이 된다. 걸린 횟수만큼 나오므로 총량이 같다.

### 막은 함정 셋

1. **릴 오염** — 심볼 폴더에 이미지를 넣으면 `LoadFolder` 가 통째로 읽어 릴에 굴러다니고,
   이름 정렬에 끼어들어 **심볼 인덱스가 통째로 밀린다.** 별도 폴더로 분리했다(릴 13종 유지 확인)
2. **고유 유닛끼리 합쳐짐** — 전부 `SymbolType = -1` 이라 스트레이트와 트리플이 같은 종류로 묶였다
3. **소비자 미연결** — 처음엔 `InGameScene` 이 고유 유닛을 **그대로 버렸다**(3기 전부 사라짐).
   만드는 쪽만 고치고 받는 쪽을 안 고치면 이렇게 된다

### 검증한 것 / 못 한 것

기계로 확인: 컴파일, `verify-tables` 15개, 스탯 1성 11기분·40기분 일치,
스프라이트 로드 2/2, 릴 풀 13종 유지, 승급 규칙.

**화면은 한 번도 못 봤다.** Codex QA 를 인계했으나(브리프 `2026-09-14-pattern-unit-qa.md`)
토큰 한도로 2개 명령 실행 후 중단됐다. 재개 필요.

### 스킬은 비어 있다
`TraitKey` 9종이 테이블에 있지만 **전투 코드에서 읽는 곳이 하나도 없다.**
고유 유닛의 `TraitKey` 도 비워 뒀다 — 구조 먼저, 스킬은 그 위에(2026-09-14 사용자 지시).

## 주제 B — 족보·유닛 패널 (데이터만 됨)

사용자 요청: "각 종족별 당첨규칙(족보), 그 유닛의 특성을 알 수 있는 방법이 있어야겠어"

`StringTable.csv` 에 **105행 추가**(72 → 177행). `verify-tables` 통과.
족보 설명 18 · 역할 3 · 특성 18 · 패널 라벨 8 · **심볼 이름 59**.
`Kr` 만 채우고 `En`·`Cn`·`Jp` 는 비워 뒀다.

심볼 이름 59개는 `UnitTable.NameKey` 와 1:1 대조를 코드로 강제했다(누락 0 / 잉여 0).

**화면 작업은 미착수.** `UIInGameSummary` 접이식 패널에 「결과 / 족보 / 유닛」 탭을
얹기로 정했으나 구현 전이다.

### 이어서 할 때 알아둘 것
- 족보 **이름**은 `Judge.cs` 에 한국어로 하드코딩돼 있다(`AddTerm(_result, "포 넘기", ...)`).
  StringTable 에는 **설명만** 넣었다. 이름까지 옮기면 로컬라이제이션이 완성되지만 Judge 를 다시 건드려야 한다
- 슬롯의 `SlotMatch3_0~5`·`SlotMatch2_0~5` 는 12개 키지만 규칙은 2가지다. 묶어서 보여줘야 읽힌다
- 고유 유닛이 자리를 잡으면 패널에 "이 판정은 이 유닛을 준다"가 들어가야 한다 — 그래서 A 가 먼저다

## 반드시 남은 것

**밸런스 재측정.** 포커가 구조적으로 다른 종족이 됐다.
오늘 잰 사거리 수치(보스 도달 75% / 클리어 25%)도 이 위에서 다시 봐야 한다.
