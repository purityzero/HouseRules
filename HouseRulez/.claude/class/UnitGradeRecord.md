# UnitGradeRecord / UnitGradeTable

연관: `Judge`, `JudgeResult`, [[BattleUnit]], [[UIInGameBattle]], [[EnemyRecord]], [[UIInGameFieldSlot]]

## 2026-08-30-0 — 전투 스탯 컬럼 추가

### 이전
```
Id,Grade,NameKey,Multiplier
1,1,UnitGrade1,1.0
2,2,UnitGrade2,2.6
3,3,UnitGrade3,7.0
```
`Multiplier`(전력 환산 배율)만 있었다.

### 이후
```
Id,Grade,NameKey,Multiplier,Hp,Atk,AtkSpeed,Range,MoveSpeed
1,1,UnitGrade1,1.0,10,2,1.0,1,1.0
2,2,UnitGrade2,2.6,26,5,1.0,1,1.0
3,3,UnitGrade3,7.0,70,14,1.0,1,1.0
```

### Hp/Atk이 Multiplier와 같은 눈금이다
1성 기본값(Hp 10 / Atk 2)에 `Multiplier`를 곱한 값이다 —
"전력 1 = 1성 1기"라는 환산과 어긋나지 않고, 적 `grunt`(Power 1, Hp 10, Atk 2)와도 같은 눈금이다.
★ `Multiplier`만 고치고 Hp/Atk을 안 고치면 판정 전력과 실제 전투력이 갈라진다. 항상 함께 본다.

### 추가 메서드
`GetRecord(int grade)` — 등급으로 레코드 조회. 없으면 `Logger.Error`로 무엇이/어떤 값이라/무엇을 기대했는지 남긴다.
기존 `GetMultiplier(int)`는 그대로.

### 3성이 상한이다
`Judge.BuildSummon`의 승급이 3성에서 멈춘다. 후반 웨이브 밸런스를 계산할 때
이보다 높은 배율(프리즘 ×18.0 등)을 전제하면 안 된다 — 실제로 그렇게 잡은 [[WaveRecord]] 36행이
재산정 대상으로 남아 있다.
