AGENT.MD

Client(구현) 버킷 일감을 서브에이전트에 위임하는 기준. Design(기획/밸런싱) 버킷(design-planner/design-issue-resolver)은 이 기준 밖 — 별도.

## 감독 방식
별도 감독 에이전트를 따로 두지 않는다 — **현재 세션(나 자신)이 아래 기준으로 분류해 맞는 에이전트를 순차로(병렬 아님) 호출**한다.

| 축 | 세부 | 에이전트 | model | effort | 기준 |
|---|---|---|---|---|---|
| 설계 | — | `code-architect` | opus | high | 새 시스템/리팩터링 방향처럼 구조 판단이 필요 |
| 컨텐츠 | — | `code-writer` | sonnet | low | 설계가 끝난 스펙을 그대로 구현 |
| 분량 | 큰 범위 | `client-bugfixer` | sonnet | medium | 여러 파일/이슈에 걸친 진단된 버그 수정 |
| 분량 | 작은 범위 | `quick-fix` | haiku | low | 원인이 명확한 단일 파일 국소 수정 |
| QA | 큰 범위 | `qa-tester` | opus | high | 여러 시스템 걸친 전체 플레이+영상분석+판단 |
| QA | 작은 범위 | `qa-quick-check` | sonnet | medium | 이미 고친 버그/특정 시나리오 하나만 재확인 |

- `effort`는 에이전트 정의에 고정되어 호출 시점에 못 바꾼다(`model`은 호출 시 오버라이드 가능) — 그래서 축마다 별도 에이전트 파일로 나눠뒀다.
- 애매한 경우(둘 이상 축에 걸침) 더 무거운 쪽(설계 > QA-큰 > 분량-큰 > QA-작은 > 컨텐츠 > 분량-작은) 기준으로 판단.

## Design 버킷 에이전트의 model/effort (라우팅 기준은 별도, 여기선 값만)
| 에이전트 | model | effort |
|---|---|---|
| `design-planner` | opus | high |
| `design-issue-resolver` | sonnet | medium |

## 위 8개 에이전트(Client 6 + Design 2) 공통 규칙
- **자체 재위임 금지 — 구조적으로 강제**: 전부 `disallowedTools: Agent`가 걸려 있어 Agent 툴 자체가 없다. 스스로 다른 서브에이전트를 호출할 수 없고, 위임 순서는 감독(메인 세션)만 결정한다.
- **QA 계열엔 `maxTurns` 안전장치**: `qa-tester`는 80, `qa-quick-check`는 30 — Play Mode 포커스/프레임 문제 등으로 반복 재시도에 빠졌을 때 무한정 턴을 소모하지 않도록 상한을 둔다. 나머지 에이전트는 보통 스스로 끝나는 작업이라 아직 안 걸어뒀다.
- **설계→구현 핸드오프**: `code-architect`가 구현까지 안 가고 설계안만 낸 경우, 그 설계안은 `.claude/architecture/{주제}.md`에 저장한다 — `code-writer`는 매번 새 컨텍스트로 뜨는 서브에이전트라 대화 내용을 못 보므로, 이어서 구현을 맡길 땐 반드시 이 파일 경로를 함께 전달한다. (`.claude/design/`은 design-planner의 기획 스펙 전용이라 혼용하지 않는다.)

## 완료 알림 (카카오톡)
위 8개 에이전트(Client 6 + Design 2) 또는 `Agent` 툴로 호출한 fork/서브에이전트가 끝나면(성공/실패 무관, 결과를 받은 직후), `mcp__claude_ai_PlayMCP__KakaotalkChat-MemoChat` 도구로 완료 알림을 보낸다. 결과가 나중 턴에 알림으로 도착하는 백그라운드 fork도 동일 — 그 결과를 확인한 시점에 바로 보낸다.

**포맷** (줄바꿈 포함, 메시지 200자 제한 — 각 항목은 핵심만 짧게):
```
[Agent : {에이전트명 또는 fork 이름}]
[desc : {무엇을 했는지 한 줄 요약}]
[다음 추천 작업 : {이어서 하면 좋을 작업 한 줄, 없으면 "없음"}]
```

- 사용자가 이번 요청에서 "알림 필요 없다"고 명시하면 이번 건만 생략한다(이 규칙 자체를 지우지는 않는다).
- 단순 조회성 Explore/general-purpose 같은 가벼운 탐색 호출까지는 대상 아님 — 위 8개 에이전트와 명시적 fork(작업 위임 성격)에 한정.

상세 절차는 각 에이전트 정의 파일(`.claude/agents/{이름}.md`) 참고.
