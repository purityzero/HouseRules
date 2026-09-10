# 미완료 작업

---

> 📁 이 파일은 **색인**이다. 항목 본문은 `.claude/unfinished/` 아래에 주제별로 나뉘어 있고,
> 완료·커밋된 과거 기록은 `.claude/archive/` 아래로 옮겼다(2026-09-10).
> 원래 1084줄 한 덩어리라 실제 미완료 항목이 파묻혀 있었다. **지운 게 아니라 옮긴 것이다.**

## 📍 현재 저장소 상태 (2026-09-10 실측)

### 브랜치 (원격 `https://github.com/purityzero/HouseRules.git`)

| 브랜치 | HEAD | main 병합 |
|---|---|---|
| `main` | 최신 원격과 일치 | — |

### 2026-09-10 QA·Git 마무리

`work/2026-09-09-font-unify`의 인게임 결과 처리·배너 타이밍·설정 닫기 로컬라이제이션은
Play Mode QA를 통과해 `07d6a1f`로 커밋했다. 배너 상세는 `archive/2026-09-10-banner-spoiler.md` 참고.

전투 연출 `80121a0`도 QA 통과 후 `main`에 `--no-ff` 병합하고 push했다(`f009e31`).
상세는 `archive/2026-09-09-battle-motion.md` 참고.

폰트 통일·설정 닫기 로컬라이제이션·인게임 결과 처리·배너 수정은 최종 결합 QA 후
`main`에 `--no-ff` 병합하고 push했다(`9744ff4`). 완료된 작업 브랜치는 정리 대상이다.

런 종료 흐름·추가 스핀 구매·도트 영문 폰트·레이아웃·씬 공용 EventSystem/Global Light·
인게임 FlowCommand 전환은 기계 QA와 사용자 시각 판정을 모두 통과했다.
`work/2026-09-10-pixel-font`의 전체 변경을 2026-09-10에 `main`으로 병합했다.
중앙 배너 스포일러와 전투 연출 미검증 항목도 각각 재검증까지 끝나 미완료 목록에서 제외했다.

⚠️ **폰트 아틀라스 4개**(`Assets/font/*.asset` 3개 + `LiberationSans SDF - Fallback.asset`)도
더티로 뜨지만 **커밋하지 않는다** — 에디터가 켜지면 글리프가 쌓이는 알려진 현상이다(`unfinished/repo-notes.md` 참고).

### 2026-09-09 병합 (사용자가 이 건에 한해 Claude Code에 Git 마무리를 위임)

`work/2026-08-30-enemy-art`의 **검증된 19커밋을 `main`에 fast-forward** 병합하고 원격에 push했다
(`d6ab769` → `33dca3f`). 병합 커밋이 생기지 않았다 — main에만 있던 커밋이 0개였다.

- **`80121a0`(전투 연출)**은 2026-09-10 Play Mode QA 통과 후 `main` 병합·push까지 완료했다.
  상세는 `archive/2026-09-09-battle-motion.md` 참고.
- `work/2026-08-29-upgrade-content`(`93846fb`)는 enemy-art에 포함돼 있어 main에 함께 들어갔다.
  **로컬·원격 모두 삭제 완료.**
- `work/2026-08-28-ingame-canvas-fit`(`eac6d6e`)은 2026-09-10 대조 후 **병합하지 않고 폐기했다.**
  인게임 stretch/HUD 문제는 `4d71c80`에서 이미 해결됐고, 나머지는 프리팹 이전 전의
  TitleScene 종족 버튼 구조·폰트 아틀라스 오염·옛 문서뿐이라 살릴 변경이 없었다.

옛 브랜치 5개(`2026-08-26-player-data` · `2026-08-26-slot-reel` · `2026-08-27-title-to-ingame` ·
`2026-08-27-ingame-hud` · `2026-08-27-slot-house`)는 **로컬·원격 모두 이미 삭제됐다.**
그 내용이 main에 녹아든 뒤의 과거 기록은 `.claude/archive/branches-1-5.md`에 있다.

### 2026-09-09 커밋 (사용자가 이 건에 한해 Claude Code에 Git 마무리를 위임)

| 파일 | 내용 | 검증 |
|---|---|---|
| `.claude/UNFINISHED.md` · `.claude/class/BattleUnit.md` | 기록을 실제 저장소 상태로 정정 | ✅ `git log` 대조 |
| `.gitignore` | `Assets/_Recovery/` 무시 규칙 추가 | ✅ 자명 |
| `Assets/Scripts/InGame/Battle/BattleUnit.cs` | 전투 공격/피격/사망 연출(DOTween) +109/−3 | ⬜ **미검증** — `unfinished/2026-09-09-battle-motion.md`. 코드만 별도 커밋으로 분리했다 |

---

## 📑 미완료 항목 — 파일별 색인

이 파일이 470줄까지 불어나서 2026-09-10에 주제별로 나눴다(**한 파일 200줄 이하** 유지).
아래 표가 전체 목록이다. 세션 시작 시 이 표만 보고 어디를 열지 정한다.

| 상태 | 항목 | 파일 |
|---|---|---|
| 🐛 결함·다듬기 3건 | `柶`(U+67F6) 결손 · SwapText 폭 여유 0 · 한국어 혼합 폰트 굵기 차이 | [`unfinished/2026-09-09-font-unify.md`](unfinished/2026-09-09-font-unify.md) |
| 🐛 진단만 2건 | 토스트 시스템 사망 · 윷 심볼 3.4배 작음 | [`unfinished/known-issues.md`](unfinished/known-issues.md) |
| 📋 백로그 | 골드 소모 경로 없음 · AccentColor 미연결 · 아트 보강 · `Assets/Screenshots/` 정리 | [`unfinished/backlog.md`](unfinished/backlog.md) |
| 📌 계속 유효 | 재발 방지 메모(MCP 연결 시점, Codex 위임 한계, `read_console` 함정) | [`unfinished/lessons.md`](unfinished/lessons.md) |
| 📌 참고 | 폰트 아틀라스 기준선(커밋하면 안 되는 더티) · 다른 컴퓨터에서 이어받기 | [`unfinished/repo-notes.md`](unfinished/repo-notes.md) |

**완료·커밋된 과거 기록**은 [`.claude/archive/`](archive/README.md)에 있다(미완료 아님, 되짚을 때만 연다).

2026-09-10 인게임 전수 QA 결과는 [`archive/2026-09-10-ingame-qa.md`](archive/2026-09-10-ingame-qa.md)에 있다.

### 사용자 결정을 기다리는 것
1. **`柶` 중국어 번역어** — `尤茨`·`投掷游戏`·`木棒游戏`·`韩国游戏`는 폰트가 커버함을 기계로 확인했다
2. **SwapText 폭** — `Swap 2` 기준 필요폭/배정폭이 `144/144px`라 여유가 0이고, 두 자리(`Swap 10`)가 되면 초과한다
3. **한국어 혼합 폰트** — 한글과 숫자·영문이 한 줄에 섞일 때 굵기와 크기가 튄다. 기능 영향 없는 미적 다듬기 항목이다
