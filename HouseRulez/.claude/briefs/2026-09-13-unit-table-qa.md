# 유닛 테이블 1단계 검증 — 컴파일 위험이 특히 큰 변경

## 무엇을 했나

심볼마다 성능을 다르게 주는 테이블을 넣었다. 그 전까지는 유닛 스탯의 출처가 `UnitGradeTable`
하나였고 **성급만 봐서**, 체스의 king·queen·rook·pawn이 전부 같은 성능이었다(7종족 60여 종 전부).

| 파일 | 변경 |
|---|---|
| `Assets/Resources/Table/UnitTable.csv` | **신규 59행** — 7종족 전 심볼의 배율·Role·RangeBonus·TraitKey |
| `Assets/Scripts/Table/UnitRecord.cs` | **신규** — `UnitRecord`, `UnitBattleStat` struct, `UnitTable` |
| `Glory/Table/TableManager.cs` | `UnitTable` 등록 |
| `Table/UnitGradeRecord.cs` · `Table/EnemyRecord.cs` | **`Atk` int → float** |
| `InGame/Battle/BattleUnit.cs` | `m_Atk`·`Setup(_atk)`·`TakeDamage` **float**, `m_Hp`·`m_MaxHp`도 **float** |
| `InGame/Battle/UIInGameBattle.cs` | `SpawnAllies`가 `GetBattleStat` 사용, `Begin`에 `houseKey` 인자 추가 |
| `InGame/InGameScene.cs` · `InGame/RunData.cs` | 종족 키 전달(`RunData.houseKey` 신규) |

**최종 스탯 = `UnitGradeTable`(성급) × `UnitTable`(심볼 배율)**이고 계산은 `UnitTable.GetBattleStat()`
한 곳에만 있다(전투·툴팁·전장 표시가 같은 값을 써야 하므로).

## 1순위 — 컴파일 (이번엔 위험이 크다)

**`Atk`·`Hp` 타입 변경이 7개 파일에 걸쳐 있고 재컴파일을 한 번도 못 했다.** 제 세션에 Unity MCP가
안 붙어서(세션 시작 시 브릿지 없음) 확인할 수단이 없었다.

- 컴파일 에러·경고
- 신규 `UnitRecord.cs`와 `UnitTable.csv`의 `.meta`가 생성됐는지
- `int`/`float` 암묵 변환이 남긴 경고가 있는지 — 특히 `BattleUnit`의 HP 비교·연산

## 2순위 — 테이블이 실제로 로드되고 값이 다른가

1. **`UnitTable`이 59행으로 로드되는지.** `TableManager`의 파서가 `float` 필드를 지원한다고
   코드상 확인했지만(`TableManager.cs:141`) 실제 로드는 못 봤다
2. **같은 성급의 다른 심볼이 다른 스탯을 내는지** — 이게 이 작업의 목적이다.
   예를 들어 slot 1성 cherry(HpRate 1.25 / AtkRate 0.75)와 seven(0.70 / 1.45)을 비교해
   실제 `BattleUnit`의 HP·ATK가 갈리는지 본다. 안 갈리면 배율이 적용되지 않은 것이다
3. **`Atk`가 소수로 들어가는지** — 1성 `Atk` 2에 배율을 곱하면 1.1~3.0이 나와야 한다.
   전부 정수로 보이면 어딘가에서 반올림되고 있다(그럼 이 변경의 목적이 사라진다)

## 3순위 — ★ 전투가 끝나는가 (부정 경로)

기획 스펙이 경고한 지점이다. **`Atk`가 0이면 `TakeDamage(0)`이 HP를 깎지 않고, 전투에 시간 상한이
없어 화면이 그대로 멈춘다.** `UnitTable.ATK_MIN = 1f`로 하한을 뒀지만 실제로 막히는지 봐야 한다.

- 전투가 정상적으로 승패까지 가는지 (여러 웨이브)
- **HP를 float로 바꾼 뒤 소수 피해가 누적돼 결국 0에 도달하는지** — 정수였다면 0.4가 0으로
  깎여 안 끝났을 자리다
- 체력 바(`m_Hp / m_MaxHp`)가 정상 표시되는지 — 캐스팅을 없앴다

## 4순위 (여유 있으면) — 없는 심볼 조회

`GetBattleStat`은 심볼 행이 없으면 **배율 없이 성급 값만** 쓰고 `GetRecord`가 에러를 남긴다
(유닛이 아예 안 서는 것보다 낫다는 판단). 일부러 없는 심볼 인덱스로 불러 그 폴백이 도는지,
에러 로그가 한 번만 남는지 확인.

## 확인된 사실 (참고)

CSV를 만든 뒤 제가 직접 검산했다 — 7종족 모두 `Hp`·`Atk`·`Spd`·`Mv` 배율 평균이 정확히 **1.000**,
`Id` 1~59 중복 없음. 판정이 "전력 1 = 1성 1기"로 유닛 수를 정하므로 평균이 1을 넘으면 그 종족만 강해진다.

다만 **이 평균은 심볼 균등 가중**이다. `Judge.BuildSummon`이 판정에 걸린 심볼을 과대 대표하므로
실제 획득 분포로 가중하면 1.0이 아닐 수 있다. 그 측정은 별건이다.

## 제약

- **프로젝트 파일을 수정하지 말 것.** 산출물은 `Temp/` 아래에만
- 폰트 아틀라스 4개는 건드리지도 되돌리지도 말 것
- 이 QA가 도는 동안 저는 HouseRulez 워킹트리를 건드리지 않습니다

## 보고

`D:/Orca/reports/unit-table-qa-2026-09-13.md`

1순위~4순위 각각 통과/실패와 실측 근거. 실패가 있으면 재현 조건과 관련 파일·줄.
