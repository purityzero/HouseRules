# EnemyRecord / EnemyTable

연관: [[WaveRecord]], [[UIInGameBattle]], [[BattleUnit]], [[UnitGradeRecord]]

## 2026-08-30-0 — 신설 문서 (구현은 커밋 8efc247)

### 개요
적 1종의 스탯. 7종(`grunt` / `shield` / `archer` / `brute` / `king_first` / `king_second` / `king_final`).

파일: `Assets/Scripts/Table/EnemyRecord.cs` / `Assets/Resources/Table/EnemyTable.csv`

### `Power`가 밸런스의 기준 단위다
**플레이어 1성 유닛 1기 = 1.** 이 적 한 마리가 그 몇 기에 해당하는지를 뜻한다.
웨이브가 몇 마리를 낼지는 이 값으로 역산하므로([[WaveRecord]]), `Hp`/`Atk`을 고칠 때 `Power`도 함께 본다.
★ `Power`만 고치고 스탯을 안 고치면 마릿수만 변하고 실제 체감 난이도는 그대로다 — 반대도 마찬가지.

| Key | Hp | Atk | AtkSpeed | Range | MoveSpeed | Power |
|---|---|---|---|---|---|---|
| grunt | 10 | 2 | 1.0 | 1 | 1.0 | 1 |
| shield | 26 | 3 | 0.8 | 1 | 0.8 | 2 |
| archer | 14 | 5 | 1.0 | 3 | 0.9 | 2 |
| brute | 52 | 8 | 0.7 | 1 | 0.7 | 4 |
| king_first | 240 | 18 | 0.9 | 2 | 0.6 | 17 |
| king_second | 600 | 34 | 0.9 | 2 | 0.6 | 41 |
| king_final | 1650 | 78 | 1.0 | 2 | 0.6 | 110 |

`grunt`가 1성 유닛(Hp 10 / Atk 2)과 정확히 같은 눈금이다 — 이게 `Power = 1`의 정의다.

### 아트가 아직 없다
`SpritePath`는 `Image/InGame/Enemy/enemy_*` 규칙만 정해뒀고 **파일은 없다.**
[[UIInGameBattle]]이 스프라이트 없이 세워 실루엣만 보인다. 아트가 생기면 그 자리에 물린다.
