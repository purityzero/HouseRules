# UnitRecord / UnitBattleStat / UnitTable

**연관 스크립트**: [[UnitGradeRecord]] · [[RunUnit]] · [[RunRoster]] · [[BattleUnit]] · [[UIInGameBattle]] · [[RunData]] · [[TableManager]] · [[EnemyRecord]]

심볼 하나의 성능을 담는 테이블. **성급 × 심볼을 합친 최종 스탯 계산이 여기 모여 있다.**

---

## 2026-09-13-0 — 신규 (유닛별 테이블 1단계)

출처 기획: `.claude/design/unit-spec.html` · 설계: `.claude/architecture/unit-table.md`

### 증상

유닛 스탯의 유일한 출처가 `UnitGradeTable`이고 **성급만 봤다.** 즉 체스의
king·queen·rook·knight·bishop·pawn이 전부 같은 성능이고 그림만 달랐다(7종족 60여 종 전부).

그래서 [[RunRoster]]에 유닛이 쌓여도 **"무엇을 모을까"라는 선택이 생기지 않았고**, 3합 승급은
숫자 채우기였고, 드래그 배치는 어느 칸에 무엇을 놓든 결과가 같았다.

### 수정

`Assets/Resources/Table/UnitTable.csv` 신규 **59행**(7종족 전 심볼).

```
Id,HouseKey,SymbolIndex,NameKey,Role,HpRate,AtkRate,AtkSpeedRate,RangeBonus,MoveRate,TraitKey
1,slot,0,UnitSlotCherry,Front,1.25,0.75,0.95,0,0.95,TraitPack
```

`HouseKey` + `SymbolIndex`가 키다. `SymbolIndex`는 스프라이트 인덱스와 1:1이고
`RunUnit.SymbolType`이 그 값이다.

**최종 스탯 = `UnitGradeTable`(성급) × `UnitTable`(심볼 배율)**

| 항목 | 계산 |
|---|---|
| `Hp` | `Max(1, RoundToInt(grade.Hp * HpRate))` |
| `Atk` | `Max(ATK_MIN, grade.Atk * AtkRate)` — **float** |
| `AtkSpeed` | `grade.AtkSpeed * AtkSpeedRate` |
| `Range` | `Clamp(grade.Range + RangeBonus, 1, RANGE_MAX)` |
| `MoveSpeed` | `grade.MoveSpeed * MoveRate` |

### ★ 계산을 `UIInGameBattle` 안에 두지 않았다

같은 값을 보여줘야 하는 소비자가 최소 셋이다 — 전투 유닛 생성, 보관함 툴팁(미구현),
전장 슬롯 표시. 계산이 전투 화면에 있으면 나머지가 각자 다시 구현하게 되고,
그게 CLAUDE.md의 「같은 상태를 두 시스템이 각자 든다」의 전형이다.

그래서 `UnitTable.GetBattleStat(houseKey, symbolIndex, grade)` 하나로 모았고,
반환은 익명 튜플이 아니라 `UnitBattleStat` struct다(CODE.MD 「튜플 대신 struct」).

**배치 후에만 알 수 있는 조건부 Trait(전열·중열·후열)은 여기 넣지 않았다.** 배치가 정해진 뒤
한 번 더 통과시켜야 하며 아직 미구현이다.

### ★ `ATK_MIN`은 선택이 아니라 필수다

`Atk`가 0이 되면 **전투가 영원히 끝나지 않는다.** `BattleUnit.TakeDamage(0)`은 HP를 깎지 않고,
`UIInGameBattle`에 전투 시간 상한이 없어 화면이 그대로 멈춘다.
`Hp`는 `Setup`이 `Max(1, _hp)`로, `AtkSpeed`는 `Max(0.1f)`로 이미 막고 있었는데 **`Atk`만 무방비였다.**

실측으로는 `AtkRate` 최소가 0.55라 1성 `Atk 2`에 곱해도 1.10이어서 0이 될 일은 없다.
그래도 하한을 둔 것은 **나중에 배율을 더 낮추거나 디버프가 붙었을 때** 조용히 멈추는 것을 막기 위해서다.

### `RANGE_MAX = 4`

아군 출발선 `x=0`, 적 등장선 `x=900`, 한 칸 108px이므로 4칸(432px)이면 전장 절반을 덮는다.
그 위는 전투가 아니라 일방 포격이다. `RangeBonus` 위에 각인 「사거리」와 조건부 Trait이
겹쳐 쌓이므로(상한이 없으면 장기 포가 1+2+1+1 = 5칸) 상한이 필요하다.

**지금은 `RangeBonus` 최대가 2라 상한에 안 걸린다** — 각인·Trait이 붙으면 걸린다.
GDD에 사거리 상한 서술은 없고 4는 기획 스펙이 제안한 값이다.

---

## 2026-09-13-1 — `Atk` float 승격 (같은 작업)

### 왜

1성 `Atk`가 정수 2라서 심볼 배율 0.55~1.5를 곱하면 **1, 2, 3 세 단으로 뭉개진다.**
포커는 13단 사다리인데 런 초반은 전부 1성이라, **"유닛이 다르다"가 가장 잘 보여야 할 구간에서
안 보였다.** 사용자 결정(2026-09-12, 기획 스펙 Q4).

### 함께 바꿔야 했던 것 — 눈금이 하나다

[[UnitGradeRecord]] md의 경고가 그대로 적용됐다: *"`Multiplier`만 고치고 Hp/Atk을 안 고치면
판정 전력과 실제 전투력이 갈라진다."* 아군 `Atk`와 적 `Atk`는 **같은 눈금**이다.

| 파일 | 전 | 후 |
|---|---|---|
| `UnitGradeRecord.Atk` | `int` | `float` |
| `EnemyRecord.Atk` | `int` | `float` |
| `BattleUnit.m_Atk` · `Setup(_atk)` · `TakeDamage(_amount)` ×2 | `int` | `float` |
| `BattleUnit.m_Hp` · `m_MaxHp` | `int` | **`float`** |

**HP까지 float으로 올린 이유**: 피해가 소수로 들어오는데 HP가 정수면 반올림해야 하고,
0.4 같은 피해가 0으로 깎여 위의 "전투가 안 끝난다"가 다시 생긴다.
체력 바는 `m_Hp / m_MaxHp`로 캐스팅이 오히려 사라졌다.

CSV는 안 고쳤다 — `TableManager`의 파서가 `float` 필드를 지원하므로(`TableManager.cs:141`)
`2`·`5`·`14`가 그대로 `float`로 읽힌다.

---

## 검산 — 평균 1.0 제약

기획 스펙이 "종족 안에서 배율 평균이 1.0"을 보장한다고 했고, **CSV를 만든 뒤 직접 계산해 확인했다.**
판정이 "전력 1 = 1성 1기"로 유닛 수를 정하므로, 평균이 1을 넘으면 그 종족만 강해진다.

| 종족 | 심볼 | Hp | Atk | Spd | Mv | RangeBonus 합 |
|---|---:|---:|---:|---:|---:|---:|
| slot | 6 | 1.000 | 1.000 | 1.000 | 1.000 | 1 |
| chess | 6 | 1.000 | 1.000 | 1.000 | 1.000 | 2 |
| janggi | 7 | 1.000 | 1.000 | 1.000 | 1.000 | 3 |
| hwatu | 12 | 1.000 | 1.000 | 1.000 | 1.000 | 1 |
| poker | 13 | 1.000 | 1.000 | 1.000 | 1.000 | 0 |
| mahjong | 9 | 1.000 | 1.000 | 1.000 | 1.000 | 0 |
| yut | 6 | 1.000 | 1.000 | 1.000 | 1.000 | 1 |

`Id` 1~59 중복 없음.

**⚠️ 이 평균은 심볼 균등 가중이다.** 기획 스펙 Q8이 짚었듯 `Judge.BuildSummon`은 판정에 걸린 심볼을
과대 대표하므로, **실제 획득 분포로 가중하면 1.0이 아닐 수 있다.** 특히 판정이 심볼을 이미 차등하는
slot·hwatu·yut은 방향만 확신하고 크기는 확신할 수 없다. 실측이 필요하다.

---

## 2026-09-13-2 — Play Mode 검증 통과

컴파일은 Claude Code가 브릿지 없이 확정했고(`Assembly-CSharp.dll` 11:31:40 빌드,
`error CS` 0건, DLL에 새 타입 4개 존재 — 방법은 `D:/Orca/runbook/unity-bridge.md`),
런타임은 Codex가 Play Mode에서 확인했다(`D:/Orca/reports/unit-table-qa-2026-09-13.md`).

| 확인 | 결과 |
|---|---|
| 심볼별 차이가 `BattleUnit`까지 가는가 | slot 1성 cherry `HP12/ATK1.5`, seven `HP7/ATK2.9` |
| 전투가 끝나는가 | poker 13종 실제 풀로 1-1~1-3 Victory(4.14 / 4.68 / 4.90초), 타임아웃 0 |
| `ATK_MIN` 이 실제로 필요한가 | 합성 `AtkRate=0`에서도 ATK 1 유지 — 무한 교전 차단 확인 |
| float 승격이 목적을 달성했나 | 0.4 피해 3회에 `1 → 0.6 → 0.200000018 → 0`, 사망까지 도달 |
| HP바 표시 | `10/12 = 0.8333333` 이 `fillAmount` 와 일치 |
| 없는 심볼 폴백 | `slot/999` → 성급 원값, 오류 로그 호출당 정확히 1건 |

**`Atk`/`Hp` float 승격이 실제로 무엇을 막았는지 증거가 나왔다.** 정수 HP였다면
0.4 피해가 0으로 깎여 위의 「전투가 영원히 안 끝난다」가 그대로 발생했을 값이다.

수치는 CSV와 대조해 재확인했다 — cherry `1.25 x 10 = 12.5 -> 12` · `0.75 x 2 = 1.5`,
seven `0.70 x 10 = 7` · `1.45 x 2 = 2.9`.

---

## ⬜ 아직 안 한 것

- **`StringTable`에 `NameKey` 59행을 안 넣었다.** 유닛 이름을 표시하는 UI가 아직 없어
  지금은 무해하지만, 툴팁이나 보관함이 생기면 빈 이름이 뜬다.
  기획 스펙 §4.4에 한국어 59행이 있으니 그때 옮긴다
- **`Role`은 저장만 하고 전투가 읽지 않는다.** 열별 역할은 2단계
- **`TraitKey`도 저장만 한다.** 효과 구현은 3·5단계
- `UnitSpawnTable`(소환형) 미착수 — 5단계
