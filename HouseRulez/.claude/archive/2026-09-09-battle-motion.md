# 전투 연출(DOTween) 검증 완료 (2026-09-09-0)

> `.claude/UNFINISHED.md`에서 분할된 파일이다(2026-09-10, 200줄 규칙).

## 2026-09-09-0 — 전투 연출(DOTween)이 미검증이라 main에 못 올라갔다

브랜치 `work/2026-08-30-enemy-art`의 커밋 **`80121a0`**. 원격 push 완료.
**main에는 일부러 안 올렸다** — main은 `33dca3f`까지만 받았고, 이 커밋 하나만 브랜치에 남아 있다.
이 검증을 통과시키는 것이 곧 브랜치를 닫는 조건이다.

`Assets/Scripts/InGame/Battle/BattleUnit.cs` (+109/−3). 이전 세션이 만들었으나
**UNFINISHED.md에도 `.claude/class/BattleUnit.md`에도 기록이 없었다** — 2026-09-09에 diff를 읽어 복원한 기록이다.

### 무엇이 들어갔나
| 연출 | 구현 |
|---|---|
| 공격 | 목표 방향으로 `DOPunchAnchorPos` 26px / 0.14s |
| 피격 | 맞은 방향으로 16px 밀림 + `DOPunchScale` 0.35 / 0.18s |
| 사망 | 루트 `DOScale(0)` 0.22s `Ease.InBack` → 완료 콜백에서 `SetActive(false)` |

**설계 판단 3가지가 주석으로 남아 있다** (그대로 유효해 보인다):
1. 연출은 **심볼(자식)만** 움직인다 — 루트 `anchoredPosition`은 `Tick`이 매 프레임 쓰므로 트윈과 다툰다
2. 사망만 루트를 줄인다 — 죽으면 `Tick`이 조기 반환해 위치를 안 건드리므로 안전
3. 색 플래시를 안 쓴다 — 적 심볼이 거의 검정이라 `Image` 컬러 틴트(곱셈)가 안 보인다

`TakeDamage(int)` → `TakeDamage(int, Vector2)` 오버로드 추가(기존 호출부 호환 유지),
`OnDestroy`/`KillMotion`으로 트윈 정리.

### ✅ 검증 완료 (2026-09-10, Codex + Unity MCP)

브랜치를 바꾸면 현재 워킹트리의 미커밋 변경이 위험하므로, 현재 `BattleUnit.cs`가
`80121a0^`와 같은 blob(`d39033a`)임을 먼저 확인한 뒤 `80121a0`의 파일 blob(`7bdd397`)만
임시 적용했다. 검증 후 `HEAD`의 원래 blob으로 복원하고 해시가 다시 일치하는 것도 확인했다.

실제 `PlayButton` → `SpinButton` → `BattleStartButton` 포인터 클릭 경로로 전투를 시작해
전투 유닛의 자식 심볼 위치·스케일과 루트 스케일·활성 상태를 프레임마다 계측했다.

| 확인 항목 | 실측 | 판정 |
|---|---:|---|
| 공격/피격 위치 이동 | 최대 24.15px | ✅ 연출 동작 |
| 피격 스케일 펀치 | 최대 0.340 | ✅ 설정값 0.35에 근접 |
| 사망 루트 축소 | 1.000 → 0.000 | ✅ 끝까지 축소 |
| 축소 후 비활성화 | 관측됨 | ✅ 중간 크기로 잔류 없음 |
| 신규 콘솔 오류 | 0건 | ✅ 기존 EventSystem 중복 오류만 존재 |

컴파일도 오류 0건으로 통과했다. 따라서 `80121a0`은 동작 검증 기준을 충족했고,
`work/2026-08-30-enemy-art`를 main에 합류시키는 Git 마무리만 남았다.

### 과거 검증 계획 — 2개 (2026-09-09에 코드 대조로 3개 해소)

원래 5개를 적어뒀으나, 2026-09-09에 실제 코드를 읽어 **3개는 기우로 판명**됐다.
아래 「해소됨」은 다시 확인하지 않아도 된다.

**완료된 항목**

1. **컴파일** — `using DG.Tweening` 포함 상태로 오류 0건
2. **실동작** — 공격(26px 툭)·피격(밀림+펀치)·사망(0.22s 축소)이 실제 전투에서 동작.
   특히 **사망이 깜빡 사라지거나 줄어들다 만 채 남지 않는지**

**✅ 해소됨 — 코드 확인 완료 (2026-09-09)**

| 원래 우려 | 판정 |
|---|---|
| `m_SymbolRect`가 null이면 `m_SymbolRestPosition`이 `(0,0)`이라 원위치를 잃는다 | ❌ 안 일어난다 — `KillMotion()`(`BattleUnit.cs:208`)이 `m_SymbolRect != null`일 때만 좌표를 쓴다. null이면 아예 안 건드린다 |
| 풀 재사용 시 사망 트윈의 `OnComplete`가 살아난 유닛을 다시 끈다 | ❌ 안 일어난다 — `Setup()`(`BattleUnit.cs:74`)이 `KillMotion()` → `m_RectTransform.DOKill()`을 먼저 부른다. DOTween의 `DOKill()`은 기본값이 `complete: false`라 `OnComplete`를 호출하지 않는다 |
| 사망 0.22s 동안 죽은 유닛이 계속 맞는다 | ❌ 안 일어난다 — `UIInGameBattle.FindTarget`(`UIInGameBattle.cs:256`)이 `isAlive == false`를 거르고, `TakeDamage`(`BattleUnit.cs:147`)도 조기 반환한다 |

→ 2026-09-10 Unity MCP 세션에서 완료.

---
