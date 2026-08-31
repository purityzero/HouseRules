# EnemyHouseResolver

연관: [[WaveRecord]], [[HouseRecord]], [[UIInGameBattle]], [[EnemyRecord]], `PlayerManager`, `HouseSpriteLoader`

## 2026-08-31-0 — 신설 (적 = 다른 종족)

### 개요
이번 웨이브의 적을 **어느 종족 심볼로 그릴지** 정한다.
파일: `Assets/Scripts/InGame/Battle/EnemyHouseResolver.cs`

GDD 06장이 "고유 덱을 가진 복수의 세력이 서로를 침공"이라 적었고,
10장이 "적 유닛은 같은 스프라이트를 검정 계열로 치환한 것"이라 못 박았다.
적은 몬스터가 아니라 **다른 종족**이다.

### ★ 스탯은 여기서 정하지 않는다
`EnemyTable`이 계속 담당한다. GDD가 **"종족 = 유닛 성능이 아니라 3×3을 읽는 문법"**이라 했으므로
적 종족도 성능이 아니라 겉모습과 서사만 바꾼다.

```
적 1기 = [ EnemyTable 스탯 ]  +  [ 이 클래스가 고른 종족 심볼 ]
           ↑ 밸런스              ↑ 겉모습
```

여기에 성능을 얹으면 `WaveTable`의 `PowerCoef → 마릿수` 역산 계약이 깨진다.

### 공개 API
| 메서드 | 하는 일 |
|---|---|
| `Resolve(WaveRecord)` | 이 웨이브가 실제로 쓸 종족. 중립이면 `null` |
| `LoadPool(WaveRecord, EnemyRecord)` | 세울 스프라이트 목록. 중립이면 `EnemyRecord.SpritePath` 한 장 |
| `GetBossSymbolIndex(WaveRecord)` | 보스 웨이브면 최상위 말 인덱스, 아니면 `-1`(무작위) |
| `GetYearEnemyHouse(int year)` | **침공 예고용.** 그 연차 상대 종족 |

### 내 종족은 적으로 안 나온다
표에 적힌 종족이 내 종족이면 `HouseTable` 순서대로 다음 종족으로 민다(`MAX_SHIFT = 8`).
내 말이 적으로 나오면 "검정이면 적"이라는 판독 규칙 위에서도 적아 구분이 헷갈린다.

### 중립 웨이브
1연차는 `WaveRecord.HOUSE_NEUTRAL`("neutral")이다. 아직 어느 종족과도 전쟁하지 않은 시점이라
종족색 없는 무리(`Image/InGame/Enemy/enemy_*` 7종)가 나온다 — 튜토리얼 구간이 종족 색에 안 물든다.

### 연차 안에서는 상대가 안 바뀐다
`WaveTable.csv`의 세 웨이브가 같은 종족이다. **침공 예고가 "올해 상대는 누구"를 보여주는 화면**이라
연차 도중에 바뀌면 예고가 성립하지 않는다.

### 보스 심볼
`WaveType`이 `Judgement`/`Final`이면 `HouseRecord.BossSymbolIndex`를 쓴다 — 그 종족의 최상위 말이다.
일반 웨이브는 풀에서 무작위로 섞는다. 한 종류로만 줄 세우면 "종족이 쳐들어왔다"가 아니라 "복제"로 보인다.

| 종족 | 보스 | 인덱스 |
|---|---|---|
| chess | king | 1 |
| janggi | wang | 6 |
| poker | 13_K | 12 |
| hwatu | 12_rain(비광) | 11 |
| mahjong | 09_pin | 8 |
| slot | 06_seven | 5 |
| yut | 05_mo | 5 |

### 검증 — Codex QA 통과 (2026-08-31, Play Mode)
- `EnemyHouse` 36/36행 존재, 1연차 3행 모두 `neutral`
- **7개 아군 종족 × 전 비중립 웨이브: 자기 종족 반환 0건**, `Resolve()`가 null인 경우 0건
- 2연차(chess) — 아군 chess면 적 janggi, 나머지면 적 chess
- 실제 전투: 적 7기 전부 `Symbol Image.enabled=true` / `sprite!=null`,
  이름이 `chess_pawn_0` `chess_knight_0` `chess_rook_0` `chess_queen_0`
- 4연차 3웨이브 `Judgement` → poker, 실제 `poker_13_K_0`
- 12연차 3웨이브 `Final` → hwatu, 실제 `hwatu_12_rain_0`
- 1연차 → 적 5기 전부 `enemy_grunt`
