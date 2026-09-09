# 중앙 배너 스포일러 수정 (2026-09-10-0) — 완료

> `.claude/UNFINISHED.md`에서 분할된 파일이다(2026-09-10, 200줄 규칙).

브랜치 `work/2026-09-09-font-unify`(주제가 섞였다 — 색인의 미커밋 표 참고). **미커밋.**

## 증상
슬롯 릴 3개가 아직 순차 정지 중인데 화면 중앙 배너가 족보 이름(`윷`·`모` 등)을 먼저 띄웠다.
플레이어가 릴에서 결과를 보기 전에 정답이 새어나갔다.

---

## 1차 수정 — 방향은 맞았으나 **불충분했다**

| 파일 | 내용 |
|---|---|
| `InGameScene.cs` | 배너 표시를 `WaitForSeconds(m_SummonDelay)` 뒤로 이동 |
| `UIInGameBanner.cs` | 메시지 **큐** 추가(`CoPlayPendingMessages`) + `OnDisable`에서 큐·코루틴 핸들 정리 |

**큐를 왜 넣었나** — 배너를 뒤로 옮기면 무료 스핀 배너(`ShowBonusSpin`)가 거의 동시에 호출된다.
기존 `Show()`는 이전 연출을 `Kill`하고 덮어써서, 윷·모에서 **족보 이름이 뜨자마자 지워진다.**
하필 배너가 가장 필요한 경우다. DOTween `OnComplete`/`OnKill` 콜백 순서에 기대면 수동 Kill과
자연 완료가 섞여 재진입 위험이 있어 코루틴 구동으로 갔다.

★ `OnDisable`에서 `m_PlayRoutine`을 **반드시 null로 비운다.** 오브젝트가 꺼지면 유니티가
코루틴을 강제로 멈추는데, 핸들이 non-null로 남으면 "이미 재생 중"으로 오판해 **배너가 영영 안 뜬다.**

---

## ❌ QA가 잡아낸 것 — 스포일러가 안 없어졌다 (`qa-tester`)

**`m_SummonDelay`가 고정 0.9초인데 실제 릴 정지는 그보다 오래 걸린다.**

계측(`EditorApplication.update`마다 `CanvasGroup.alpha` + 릴 3개 FSM 상태 + `localPosition.y` 동시 샘플링):

| 조건 | StopAll → 전 릴 Idle 실측 | 고정 대기 | 결과 |
|---|---|---|---|
| 약 52fps (녹화 off) | **1.004초** | 0.9초 | 배너가 0.09초 이르다 |
| 약 23fps (녹화 on) | **1.69초** | 0.9초 | 배너 완전 불투명 시점이 정지보다 **0.64초 이르다** |

**근본 원인**: 릴 감속이 시간 기반이 아니라 **프레임당 이동량**이다
(`Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReel.cs:254` `GetStopSpeed()` 주석에 명시).
그래서 정지 소요가 프레임레이트를 탄다. **고정 시간 대기로는 원리상 맞출 수 없다.**

> ⚠️ Android 타깃에서 30fps로 떨어지면 저프레임 케이스가 그대로 재현된다.

---

## 2차 수정 — 고정 대기 → 실제 정지 완료 대기

| 파일 | 전 | 후 |
|---|---|---|
| `Assets/Scripts/InGame/Slot/UIHouseSlotMachine.cs:270` | `IsAllReelIdle()`가 private | `public bool isAllReelIdle` 접근자 추가 |
| `Assets/Scripts/InGame/InGameScene.cs:204` | `yield return new WaitForSeconds(m_SummonDelay);` | `yield return new WaitUntil(() => m_SlotMachine.isAllReelIdle);` |
| `Assets/Scripts/InGame/InGameScene.cs:22` | `[SerializeField] private float m_SummonDelay = 0.9f;` | **제거** — 이 변경으로 미사용이 됐다 |

`InGameScene.unity:683`의 `m_SummonDelay: 0.9`는 고아 직렬화 필드로 남는다.
유니티가 씬을 다음에 저장할 때 정리한다(무해).

- ✅ 컴파일 통과 — 에러 0건 (경고는 `TableManager.cs(80,32) CS1998` 기존 1건뿐, 무관)
- ✅ **재검증 통과** — 아래 참고

### 2차 수정 QA 결과 (2026-09-10, Codex + Unity MCP)

실제 `SpinButton`의 포인터 클릭 경로로 스핀을 시작하고, `EditorApplication.update`에서
`UIHouseSlotMachine.isAllReelIdle`과 `UIInGameBanner`의 `CanvasGroup.alpha`를 함께 계측했다.

| 전 릴 Idle | 배너 alpha > 0 | 선노출 |
|---:|---:|---|
| 2.188초 | 2.195초 | **없음** |

이전 QA의 저프레임 실패 조건보다 정지가 더 오래 걸린 실행에서도 배너가 전 릴 정지 뒤에 나타났다.
따라서 고정 대기 제거와 실제 Idle 대기는 의도대로 동작한다.

---

## ✅ QA에서 통과한 것 (1차 수정분, 2차 수정과 무관하게 유효)

**큐가 실제로 동작한다** — 24회 스핀 중 보너스 2회 관측, 둘 다 동일:
```
"완주 1"    alpha 1.0로 약 0.93초 유지 → 페이드아웃
"무료 스핀!"  alpha 1.0로 약 0.90초 유지
두 메시지 간격 1.374초 = fadeIn 0.12 + hold 0.9 + fadeOut 0.35 와 정확히 일치
```
앞 메시지가 잘리지 않는다.

**비활성화 후에도 배너가 산다** — 가장 깨지기 쉬운 경로(큐 재생 중 `SetActive(false)`)로 확인:
`OnDisable` 직후 `m_ListPendingMessage.Count = 0`, `m_PlayRoutine = null`로 정상 정리되고,
재활성화 후 스핀에서 배너가 다시 정상 표시됐다.

## ⬜ 아직 확인 못 한 것

**무판정(Power 0) 스핀에서 배너가 안 뜨는지** — 윷 종족으로 24회 돌렸으나 전부 `Power > 0`이었다.
`Judge.EvaluateYut`의 무판정("낙")은 3줄 전부 landing<0이어야 성립해 사실상 안 나온다.
**다른 종족(슬롯·마작 등)으로 재현해야 한다.** 코드상 `judgeResult.Power > 0f` 가드는 있으나 실플레이 미검증.

## 📌 재검증 시 알아둘 것 — QA가 80턴을 다 쓴 이유

콘솔 경고(EventSystem 2개, 스크린샷 저장 실패)는 **원인이 아니었다.**

- **무료 스핀이 스핀당 10.7% 확률**(윷/모 줄, `YUT_MOVE >= 4`가 3칸 연속 = 1/27 × 3줄)인데
  **스핀 코인이 런당 6개뿐이고 웨이브로 회복되지 않는다.** 그래서 런을 4번(스핀 24회) 돌려야 했다.
- **인게임에 타이틀로 나가는 버튼이 없다.** 런 교체마다 Play Mode Stop→Play→타이틀 Play 버튼 클릭을
  반복해야 해서 이게 턴을 가장 많이 먹었다.

→ 재검증은 **스포일러 항목에 집중**하면 된다. 보너스 케이스는 이미 통과했으므로 다시 뽑을 필요가 없다.
   스포일러는 스핀 1~2회로 판정 가능하다(배너 alpha가 0을 벗어나는 시점 vs 전 릴 Idle 시점 비교).

## 🐛 QA가 덤으로 관측한 것 (이번 수정과 무관, 미수정)

**`BonusBanner`의 씬 저작 `localScale`이 108이다**(Canvas localScale 0.009259의 역수).
`PlayMessage()`가 매번 0.7→1로 덮어써서 실사용엔 문제가 없지만, `Awake()`의 `alpha = 0`이
없었다면 첫 프레임에 화면 폭 97,000px짜리 텍스트가 깔린다. 저작값이 잘못된 잔재로 보인다.

## 관련 파일
- `Assets/Scripts/InGame/InGameScene.cs:181`(`CoSpinAndStop`) · `:204`(정지 완료 대기)
- `Assets/Scripts/InGame/UI/UIInGameBanner.cs:40`(`Show`) · `:54`(`CoPlayPendingMessages`) · `:97`(`OnDisable`)
- `Assets/Scripts/InGame/Slot/UIHouseSlotMachine.cs:270`(`isAllReelIdle`)
- `Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReel.cs:254`(`GetStopSpeed` — 프레임당 이동량)
- 영상: `QA_Recordings/qa_20260910_014334.mp4` · `qa_20260910_015329.mp4`
- 계측 원본: `Temp/qa_spin1.csv` · `qa_spin3.csv` · `qa_log2.csv` · `qa_run4.csv` (Unity Temp, 형상관리 대상 아님)
