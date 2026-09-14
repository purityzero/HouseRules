# 2026-09-14 — Poker Back 사거리 + Unity CLI 도입

브랜치 `work/2026-09-14-poker-range` (`work/2026-09-14-spawn-weight` @ `c86cdc8` 위).

## 상태

`UnitTable.csv`의 Poker Back 4행(10·J·Q·K) `RangeBonus` **0 → 1**. 측정은 **끝났다**.
보고서:

- `D:/Orca/reports/poker-range-qa-2026-09-14.md`
- `D:/Orca/reports/poker-range-2back-2026-09-14.md`

- 사거리 효과 확인: 격리 비교 9건 전부 유리. A 배치의 12-1·12-2가 패배→승리로 전환
- 원칙 A 4런: 보스 도달 3/4(75%), 12연차 클리어 1/4(25%)
- 역할 열 배치(C)는 여전히 기본 격자(A)보다 나쁨 — 밀집(B)이 지배 전략인 것은 그대로
- Q·K 2기만 +1인 비교안: 보스 도달 2/4(50%), 클리어 0/4. 92802 클리어가 사라지고
  92803은 6-3에서 조기 붕괴해 **Back 4기 +1 유지 권고**

## 남은 것

1. **사람이 한 판 돌려보고 병합 판단** — 측정은 Codex가 했고 컴파일·테이블 검증은 통과했지만,
   CLAUDE.md 기준으로 `main` 합류에는 사람의 플레이가 남아 있다.

Back 2기(Q·K)만 주는 안의 재측정은 완료했다. 92804의 보스 잔여 HP는 26.9 → 54.2로
멀어졌지만, 92802가 클리어 → HP 3.3 잔존으로 바뀌고 92803이 6-3에서 무너졌다.
작은 상향에 뒤집힐 경계만 다른 seed로 옮기면서 안정성을 잃으므로 4기 안을 유지한다.

## 이 브랜치에 섞여 있는 별개 변경 (병합 시 분리 판단 필요)

| 파일 | 무엇 | 누구 |
|---|---|---|
| `Packages/manifest.json` | `com.unity.pipeline` 0.5.0 → **0.6.0-exp.1** | 이번 세션. **Unity CLI 전제라 유지 필요** |
| `Packages/manifest.json` | `com.unity.ai.assistant`, `com.unity.ai.inference` 추가 | 사용자. 유지하기로 확인됨(2026-09-14) |
| `ProjectSettings/ProjectSettings.asset` | `scriptingDefineSymbols`에 `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY` (Android) | 위 AI 패키지가 딸고 들어옴 |
| `ProjectSettings/EditorBuildSettings.asset` | `com.unity.dt.app-ui` config object | 〃 |
| `ProjectSettings/Packages/com.unity.ai.assistant/Settings.json` | 신규 | 〃 |
| 폰트 아틀라스 4개 | 에디터 자동 갱신 | **커밋 금지 대상** |

`scriptingDefineSymbols` 변경은 Android 빌드에 영향을 주므로, 병합 전에 의도한 것인지 한 번 더 본다.

## Unity CLI — 이번에 확인한 것

`unity` CLI가 `com.unity.pipeline`을 통해 돌고 있는 에디터를 직접 조작한다.
**MCP 브릿지(8080)가 죽은 세션에서도 붙는다** — 매 호출마다 새로 연결하기 때문이다.
절차와 함정은 `D:/Orca/runbook/unity-bridge.md`.

이 프로젝트의 커스텀 명령 2개가 이미 있다(`Assets/Editor/Verification/ProjectVerificationCommands.cs:15`, 커밋 `f0d0913`):

```
unity command verify-compile     # 컴파일 성패 — 기존 3단계 수동 증거를 대체
unity command verify-tables      # Resources/Table CSV 15개를 Record 필드와 대조
```

2026-09-14 실측: 둘 다 통과. `eval`로 프로덕션 경로 조회도 확인했다
(Play Mode 밖에서 `TableManager.instance.GetTable<UnitTable>()`로 Poker 9~12번 `RangeBonus=1` 확인).
