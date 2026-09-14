# 연차 이동 연출 — 검증

브랜치 `work/2026-09-13-year-travel`(`work/2026-09-13-battle-return` @ `3ffabf2` 위 스택).
설계는 `.claude/architecture/ingame-presentation.md` §3(정한 것 3가지 포함),
클래스 문서는 `.claude/class/InGameScene.md` · `UIInGameFieldSlot.md`.

## 무엇을 했나

연차가 넘어갈 때 **배경이 오른쪽으로 흐르고 전장 9칸이 제자리걸음**을 한다(2.5초).

```
웨이브 승리 -> AdvanceToNextWave 가 연차 전환 감지 -> m_isYearTravelPending = true
  -> (전투 후 복귀 진행)
  -> FinishBattleReturn 이 전장 9칸을 그린 뒤 StartYearTravel()
  -> 배경 uvRect 스크롤 + 9칸 까딱임 (2.5초)
  -> FinishYearTravel: 배경 정지 · 걷기 정지
```

| 파일 | 변경 |
|---|---|
| `UIInGameFieldSlot.cs` | `SetWalking(bool)` — 심볼을 위아래 6px 까딱임(무한 루프 트윈), `OnDestroy` 추가 |
| `UIInGameField.cs` | `SetWalking(bool)` — 9칸에 전달 |
| `InGameScene.cs` | `m_YearTravelFlow`, `m_isYearTravelPending`, `StartYearTravel()` · `FinishYearTravel()` · `ScrollBackground()` |

**유닛을 화면 밖까지 옮기지 않는다.** 배경이 흐르는 동안 제자리걸음만으로 "이동했다"가 읽히고,
옮기면 전장 9칸 배치가 흐트러져 되돌릴 것이 늘어난다.

## Claude 가 이미 확인한 것 (다시 하지 말 것)

- 괄호 균형 3파일 OK, 새 멤버·호출 대상 전부 실재
- **스프라이트에 걷기 프레임이 없다** — 종족 폴더가 심볼마다 원본·`_blur`·`_x8` 세 장뿐
- `Command_DeltaTime(maxTime, delegate)`는 매 프레임 콜백이 없어 스크롤은 `Update`에서 한다
- `TitleBackgroundScroller`를 붙이지 않았다(씬 수정 불필요) — `InGameScene`이 이미
  `m_BackgroundImage`를 들고 있고, 타이틀은 항상 흐르지만 인게임은 연차 전환 때만 흘러야 한다

## 검증 항목

### 1순위 — 컴파일

3파일. `UIInGameFieldSlot`에 `using DG.Tweening`이 새로 들어갔다.

### 2순위 — ★ 연출이 실제로 보이는가

1연차 3웨이브를 승리해 2연차로 넘어간다.

1. **배경이 오른쪽으로 흐르는가**(`m_BackgroundImage.uvRect.x`가 증가)
2. **전장 9칸의 심볼이 위아래로 까딱이는가**
3. 약 2.5초 뒤 **둘 다 멈추는가**
4. 연차 배너와 겹쳐도 화면이 깨지지 않는가

`uvRect.x` 증가량이 2.5초 × 0.15 = **약 0.375**인지 확인해 달라(1을 넘으면 -1 되돌림).

### 3순위 — ★ 순서 (복귀가 먼저, 이동이 나중)

**연차 전환 시점에 유닛이 적진 앞에 흩어져 있으면 실패다.** 전장 9칸이 제자리에 그려진
**뒤에** 걷기가 시작돼야 한다. `m_isYearTravelPending` 플래그가 그 순서를 만든다.

### 4순위 — ★ 무한 루프 트윈이 남지 않는가 (가장 중요)

까딱임은 `SetLoops(-1)`이라 끊지 않으면 영원히 돈다. 끊는 경로가 다섯이다.

| 경우 | 기대 |
|---|---|
| 연출 2.5초 종료 | 심볼이 **원래 y 좌표로** 돌아가 멈춘다 |
| 연출 중 **전투 시작**(스핀→전투) | 걷기와 배경이 즉시 멈춘다(`OnBattleStart`) |
| 연출 중 **드래그** | 끌린 칸의 걷기가 멈추고 포인터를 따라간다(둘이 다투면 실패) |
| 연차 여러 번 통과 | 까딱임이 겹쳐 심볼이 점점 위로 올라가지 않는다 |
| 씬 나가기(런 종료→타이틀) | 콘솔에 파괴된 대상 관련 오류가 없다 |

**특히 마지막 두 개**를 봐 달라. 트윈이 누적되면 심볼 좌표가 홈에서 멀어진다 —
연차를 3~4회 통과한 뒤 심볼 `anchoredPosition`이 처음과 같은지 실측해 주면 확실하다.

### 5순위 — 빈 칸

유닛이 없는 칸은 걷지 않는다(`m_SymbolImage.enabled` 확인). 빈 칸이 까딱이면 실패.

## 제약

- 프로젝트 파일 수정 금지, 산출물은 `Temp/` 아래에만
- 폰트 아틀라스 4개 미접촉. `PlayerPrefs`에 QA 키를 남기지 말 것
- 선택 종족을 바꿨으면 끝나고 `poker`로 되돌릴 것
- 씬 수정은 필요하지 않다(직렬화 필드 추가 없음)

## 보고

`D:/Orca/reports/year-travel-qa-2026-09-13.md` · 항목별 통과/실패,
특히 **2순위의 `uvRect.x` 증가량**과 **4순위의 연차 3~4회 통과 후 심볼 좌표**
