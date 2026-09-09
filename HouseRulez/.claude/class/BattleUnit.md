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
> ⚠️ **2026-09-09-0에서 바뀌었다** — 즉시 끄지 않고 0.22초 축소 연출 뒤에 끈다. 아래 절 참고.

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

---

## 2026-09-09-0 — 전투 연출 추가 (DOTween) ⬜ 미커밋·미검증

> 이전 세션이 작업 트리에 남긴 변경을 2026-09-09에 diff로 읽어 복원한 기록이다.
> **어느 문서에도 기록이 없었다.** 실행 검증은 아직 하지 않았다(그 세션에 Unity MCP가 안 붙었다).

### 왜 넣었나
HP 바 숫자만 줄어들어서 **누가 누구를 때리는지 화면에서 안 읽혔다.**

### 수정 전 → 후 (함수 단위)

**`TakeDamage`**
```csharp
// 전
public void TakeDamage(int _amount)
{
    ...
    if (isAlive == false)
        gameObject.SetActive(false);
}

// 후 — 방향 오버로드 추가, 즉시 끄지 않는다
public void TakeDamage(int _amount)          // 때린 주체가 없을 때(본거지 선 통과 등)
{
    TakeDamage(_amount, Vector2.zero);
}

public void TakeDamage(int _amount, Vector2 _hitDirection)
{
    ...
    if (isAlive == true)
    {
        PlayHitMotion(_hitDirection);
        return;
    }
    PlayDeathMotion();
}
```

**`Tick`** — 공격 시 방향을 계산해 양쪽에 넘긴다
```csharp
// 전
_target.TakeDamage(m_Atk);

// 후
Vector2 direction = toTarget.normalized;
PlayAttackMotion(direction);
_target.TakeDamage(m_Atk, direction);
```

**`Setup`** — 재사용 대비 초기화 추가
```csharp
KillMotion();
m_RectTransform.localScale = Vector3.one;
// m_SymbolImage != null 블록 안에서:
m_SymbolRect = m_SymbolImage.rectTransform;
m_SymbolRestPosition = m_SymbolRect.anchoredPosition;
m_SymbolRect.localScale = Vector3.one;
```

**신설**: `PlayAttackMotion` / `PlayHitMotion` / `PlayDeathMotion` / `ResetSymbolMotion` / `KillMotion` / `OnDestroy`

### 상수
| 이름 | 값 | 의미 |
|---|---|---|
| `ATTACK_LUNGE` / `ATTACK_DURATION` | 26f / 0.14f | 공격 찌르기. 공속 1.0(=1초 1회)보다 훨씬 짧아야 겹치지 않는다 |
| `HIT_KNOCKBACK` / `HIT_DURATION` / `HIT_SCALE_PUNCH` | 16f / 0.18f / 0.35f | 피격 밀림 + 부풀기 |
| `DEATH_DURATION` | 0.22f | 사망 축소 |

### 설계 판단 3가지 (코드 주석에 근거 있음)
1. **연출은 심볼(자식)만 움직인다.** 루트 `anchoredPosition`은 `Tick`의 이동 로직이 매 프레임 쓰므로,
   거기에 트윈을 걸면 둘이 같은 값을 덮어써 제자리에서 떨거나 목표를 못 따라간다.
2. **사망만 루트를 줄인다.** 죽으면 `Tick`이 조기 반환해 위치를 안 건드리므로 트윈과 다투지 않는다.
3. **색 플래시를 안 쓴다.** 적 심볼이 거의 검정이라 `Image` 컬러 틴트가 곱셈이어서 티가 안 난다.
   움직임은 아군(흰색)·적(검정) 어느 쪽에서도 똑같이 읽힌다.

### ⬜ 남은 검증 (전부)
1. 컴파일 (`using DG.Tweening` 추가 — dll은 `Assets/Plugins/Demigiant/`에 커밋돼 있다)
2. **`m_SymbolRect` null 경로** — `Setup`이 `m_SymbolImage != null`일 때만 대입한다.
   심볼이 없는 유닛이면 `m_SymbolRestPosition`이 `(0,0)`인 채 남고 `KillMotion`이 그 좌표로 되돌린다
3. **풀 재사용** — 사망 트윈의 `OnComplete`가 재사용 직후 도착하면 살아난 유닛을 다시 끌 수 있다
   (`Setup`의 `KillMotion`이 트윈을 죽이므로 아마 안전하나 미확인)
4. 사망 0.22s 지연 중 계속 맞는지 (`isAlive == false` 조기 반환으로 막힐 것으로 보이나 미확인)
5. 눈으로 보기 — 공격/피격이 실제로 읽히는지
