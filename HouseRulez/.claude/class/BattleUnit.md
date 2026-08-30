# BattleUnit

연관: [[UIInGameBattle]], `UnitGradeRecord`, `EnemyRecord`, `eBattleSide`

## 2026-08-30-0 — 신설 (전장 유닛 1기)

### 개요
전장에서 움직이고 싸우는 한 기. **아군과 적이 같은 클래스를 쓴다** — 규칙이 대칭이라
(진격 방향만 반대) 클래스를 둘로 나누면 같은 코드를 두 벌 갖게 된다.

파일: `Assets/Scripts/InGame/Battle/BattleUnit.cs`

### 계층 구조 (InGameScene.unity)
```
UnitTemplate        RectTransform 96x96   ★ 비활성 원본, UIInGameBattle이 복제한다  (GO 877726930)
├─ Symbol           Image, stretch                                  (rt 2031921302)
├─ Grade            TMP 60x28, 우하단 (-2, 2)                        (rt 1777146207)
└─ HpBack           Image, 하단 stretch -12 / 높이 8, y -2            (rt 588145758)
   └─ HpFill        Image, Filled Horizontal                        (rt 1058948064)
```

### 방향 표현 — 스프라이트를 뒤집지 않는다
장기·화투 심볼이 한자와 그림이라 좌우 반전하면 글자가 **거울상**이 된다.
대신 실제로 적을 향해 이동하는 것으로 방향을 보인다(`direction = Ally ? +1 : -1`).

### 스탯 단위 — 칸(cell)이지 픽셀이 아니다
`Range`·`MoveSpeed`는 테이블에 **칸 단위**로 적고 `CELL_TO_PIXEL = 108f`을 곱해 쓴다.
화면 픽셀로 테이블에 적으면 레이아웃이 바뀔 때마다 밸런스가 흔들린다.

### 행동 규칙 (`Tick`)
1. 쿨다운 감소
2. 목표가 사거리 안 → 때리고 `쿨다운 = 1 / AtkSpeed`, **그 프레임엔 이동하지 않는다**
3. 아니면 적진 쪽으로 `MoveSpeed * deltaTime` 전진

`_deltaTime`은 호출자가 배속을 이미 곱해서 넘긴다 — 여기서 `Time.deltaTime`을 읽지 않는다.

### 사망 처리
`m_Hp <= 0`이면 `gameObject.SetActive(false)`. 파괴하지 않는 이유는 웨이브가 끝날 때
`UIInGameBattle.Clear()`가 한 번에 정리하기 때문이고, 전투 중 리스트에서 지우면
순회 중 컬렉션 변경이 된다.

### 공개 API
| 멤버 | 설명 |
|---|---|
| `Setup(side, lane, sprite, grade, hp, atk, atkSpeed, range, moveSpeed, startPos)` | 초기화 + 활성화 |
| `Tick(deltaTime, target)` | 한 프레임 진행 |
| `TakeDamage(int)` | 피해. 0이 되면 비활성화 |
| `isAlive` / `side` / `lane` / `positionX` | 읽기 |

### 검증 상태 — Codex QA 통과 (2026-08-30)
아군 x 60 → 497 우측 이동, 적 x 1040 → 604 좌측 이동, 아군 HP 10 → 8 → 4 → 0 및 사망 시 비활성화 확인.
