# 전투 후 제자리 복귀 — 검증

브랜치 `work/2026-09-13-battle-return`(`work/2026-09-13-swap-to-extraspin` @ `0027f3f` 위 스택).
설계는 `.claude/architecture/ingame-presentation.md` §2(정한 것 3가지 포함),
클래스 문서는 `.claude/class/BattleUnit.md` · `UIInGameBattle.md` · `InGameScene.md`.

## 고친 증상

전투가 끝나도 `m_Battle.Clear()`를 부르지 않아 **살아남은 유닛이 적진 앞에 몰린 자리 그대로**
남았다. 동시에 `m_Field`는 `OnBattleStart`에서 비워진 상태라 전장 9칸 표시도 없었다.
즉 전투와 다음 스핀 사이에 **"아무 상태도 아닌" 구간**이 있었다.

## 무엇을 했나

| 파일 | 변경 |
|---|---|
| `BattleUnit.cs` | `m_HomePosition`(`Setup`의 시작 좌표를 기억), `isReturning`, `ReturnToHome(speedScale)` |
| `UIInGameBattle.cs` | `ReturnSurvivorsToHome(speedScale)` — 움직이기 시작한 수를 반환, `isReturning` |
| `InGameScene.cs` | `m_BattleReturnFlow` 신규, `StartBattleReturn()` · `FinishBattleReturn()`, `OnBattleStart`에서 취소 |

흐름: 전투 종료 -> 배너·피해·UI 갱신 -> 살아남은 유닛이 자기 칸으로 걸어감
-> 모두 도착 -> `m_Battle.Clear()` + `m_Field.Show(roster, null, pool)`

## Claude 가 이미 확인한 것 (다시 하지 말 것)

- 괄호 균형 3파일 OK, 새 멤버·호출 대상 전부 실재
- `FlowCommand`에 `Add`/`Clear`/`Update` 있음, `Command_WaitUntil(Func<bool>)`·`Command_Delegate(UnityAction)` 시그니처 확인
- `OnBattleStart`가 `m_Field.Clear()`를 부르는 것 확인(설계안 주장과 일치)

## 검증 항목

### 1순위 — 컴파일

3파일이 걸렸고 `BattleUnit`에 새 public 멤버가 생겼다.

### 2순위 — ★ 복귀가 실제로 보이는가

1. 전투를 승리로 끝낸다
2. 살아남은 유닛이 **적진 앞에서 자기 칸으로 이동**하는가(순간이동이 아니라 이동)
3. 도착 후 **전장 9칸 표시가 다시 뜨는가**
4. 전투 오브젝트가 정리되어 **유닛이 두 겹으로 보이지 않는가**

### 3순위 — ★ 루트가 움직이는가 (심볼만 움직이면 결함)

`BattleUnit`은 평소 **심볼(자식)만** 연출한다. 복귀는 **루트**를 트윈해야 한다.
심볼만 옮기면 **루트가 적진 앞에 남아 HP 바와 성급 표시가 엉뚱한 자리에 뜬다.**

복귀 후 유닛의 `anchoredPosition`(루트)이 `SpawnAllies`가 준 칸 좌표와 같은지 확인해 달라.
칸 좌표는 `GetLanePosition(lane, m_AllyStartX + column * 108f)`이다.

### 4순위 — 부정 경로

| 경우 | 기대 |
|---|---|
| **아군 전멸(패배)** | 기다리지 않고 즉시 정리. 전장 표시가 바로 돌아온다 |
| 이미 제자리인 유닛만 남음 | 대기 없이 즉시 정리(`movingCount == 0`) |
| **죽은 유닛** | 복귀하지 않는다. 다만 정리 후 `m_Field`에는 다시 나타난다(명부에 살아 있음) |
| 배속 2배 | 복귀도 2배로 빨라진다 |

### 5순위 — ★ 전투 재시작 충돌 (설계안에 없던 경로)

복귀가 걸어가는 **중에** 다음 전투를 시작하면, 뒤늦게 끝난 복귀가 그 전투를 `Clear()` 할 위험이 있다.
두 겹으로 막았다 — `OnBattleStart`의 `m_BattleReturnFlow.Clear()`와
`FinishBattleReturn`의 `m_isBattleActive` 확인.

**재현**: 전투 종료 직후(유닛이 걸어가는 동안) 스핀을 돌려 다음 전투를 시작한다.
새 전투의 유닛이 사라지거나 화면이 비면 실패다.

### 6순위 — 연속 전투 상태 오염

3웨이브를 연달아 치러 매번 복귀가 정상인지, 펀치 모션이 남아 심볼이 어긋난 자리에 떠 있지 않은지.

## 제약

- 프로젝트 파일 수정 금지, 산출물은 `Temp/` 아래에만
- 폰트 아틀라스 4개 미접촉. `PlayerPrefs`에 QA 키를 남기지 말 것
- 선택 종족을 바꿨으면 끝나고 `poker`로 되돌릴 것
- 씬 수정은 필요하지 않다(직렬화 필드 추가 없음)

## 보고

`D:/Orca/reports/battle-return-qa-2026-09-13.md` · 항목별 통과/실패,
특히 **3순위의 복귀 후 루트 좌표 실측값**과 **5순위 재현 결과**
