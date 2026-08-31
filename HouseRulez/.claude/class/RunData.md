# RunData

연관: `InGameScene`(소유자, `Assets/Scripts/InGame/InGameScene.cs`), [[UIInGameHud]], [[UIInGameAction]], `GameConfigTable`(초기값 출처, `Assets/Scripts/Table/GameConfigRecord.cs`), `PlayerData`(대비되는 저장 계층, `Assets/Scripts/Player/PlayerData.cs`)

## 2026-08-27-0 — 신설 (런 상태 담는 그릇)

### 개요
인게임 HUD가 표시할 값(본거지 HP, 연차, 골드, 스핀 코인, 판돈, 스왑, 배속)을 담을 곳이 없어 새로 만들었다.
`MonoBehaviour`가 아닌 순수 클래스이고, **저장하지 않는다**.

### PlayerData에 넣지 않은 이유
`PlayerData`의 클래스 주석이 "GDD 06장 — 코어·각인·재화는 런 안에서만 사는 값이라 여기 들어오지 않는다"로
담을 수 있는 필드의 상한을 이미 못 박아 뒀다. 그 경계를 그대로 지켰다 — 저장되는 진행도(선택 종족, 해금)와
런 한 판만 사는 값(HP/골드/코인)은 다른 클래스가 든다.

### 소유와 갱신 경로
`InGameScene`이 인스턴스를 들고, 값 변경도 전부 거기서 한다. UI(`UIInGameHud`/`UIInGameAction`)는
참조만 받아 읽고 그린다 — UI가 상태를 고치기 시작하면 갱신 경로가 둘로 갈라져 서로 덮어쓰게 된다.

### 필드 (전부 private + 읽기 전용 프로퍼티)
| 필드 | 초기값 출처 |
|---|---|
| `m_HomeHp` / `m_HomeHpMax` | `GameConfigTable.KEY_HOME_HP_MAX` (8) |
| `m_Year` / `m_YearMax` | 1 부터 / `KEY_RUN_YEAR_MAX` (12) |
| `m_Gold` | `KEY_RUN_START_GOLD` (0) |
| `m_SpinCoin` / `m_SpinCoinMax` | `KEY_SPIN_COIN_PER_YEAR` (3) |
| `m_SwapCount` / `m_SwapCountMax` | `KEY_SWAP_COUNT_PER_YEAR` (2) |
| `m_BetLevel` | 0 |
| `m_BattleSpeed` / `m_BattleSpeedFast` | 1 / `KEY_BATTLE_SPEED_FAST` (2) |

숫자는 코드에 하나도 박지 않았다. `Init()`의 `GetValue(key, 기본값)` 두 번째 인자는 CSV 자체가 없을 때의
최후 방어값이고, 실제 튜닝은 `GameConfigTable.csv`만 고쳐서 한다.

### 메서드
- `Init()` — 테이블에서 초기값을 읽어 채운다. 인게임 진입 시 `InGameScene.OnSetup()`에서 1회.
- `SpendSpinCoin()` — 코인 1개 소모. 남은 게 없으면 `false`를 돌려주고 아무것도 바꾸지 않는다.
- `ToggleBattleSpeed()` — ×1 과 `m_BattleSpeedFast` 두 단계만 오간다.

### 아직 없는 것
HP 감소(선전포고 패배 −2), 연차 진행, 골드 증감, 판돈 변경, 스왑 소모 — 전부 그 단계(전투/외교/배치)가
아직 없어 변경 API를 만들지 않았다. 필요해지는 시점에 그 단계와 함께 붙인다.

## 2026-08-30-0 — 웨이브 진행과 본거지 피해 추가

### 추가
| 멤버 | 설명 |
|---|---|
| `WAVE_PER_YEAR = 3` | 연차당 웨이브 3개. 스핀 코인 1개가 웨이브 1개다 |
| `waveIndex` | 현재 웨이브(1~3). `WaveTable.GetRecord(year, waveIndex)`의 키가 된다 |
| `AdvanceWave()` | 웨이브 +1, 3을 넘기면 1로 되돌리고 연차 +1 |
| `TakeHomeDamage(int)` | 본거지 HP 감소. 0 밑으로 내려가지 않는다 |

### 아직 호출부가 없다
`AdvanceWave()`·`TakeHomeDamage()`·`GetRoyalReward()` 셋 다 **부르는 곳이 없다.**
웨이브 종료 처리(승패 확정 → 보상 → 다음 웨이브)가 아직 없기 때문이다.
[[UIInGameBattle]]이 `result`와 `homeHit`을 이미 들고 있으므로, 종료 단계가 생기면 그 둘을 여기로 넘긴다.
★ 안 적어두면 다음 사람이 "이미 연결돼 있다"고 믿고 그 위에 쌓는다.

### 연차 전환 시 회복은 아직 없다
`AdvanceWave()`에 스핀 코인·스왑 회복과 상점/외교 단계 진입이 `TODO`로만 있다.

## 2026-08-31-0 — 당첨 배당(골드) 추가

### 배경
**골드는 시작값 0에 증가 경로가 아예 없었다.** HUD의 `골드 0`이 계속 0이던 이유다.
소비처(추가 스핀 25골드)도 `InGameScene.cs` 주석으로만 있었다.

### `AwardGoldByPower(float _power)`
`GameConfigTable`의 `GoldPerPower`(현재 2)를 곱해 지급하고 지급액을 돌려준다.
`InGameScene.CoSpinAndStop`이 판정 직후 부르고 HUD를 갱신한다.

★ **전력이 조금이라도 있으면 최소 1골드를 보장한다.** 반올림으로 0이 되면
"맞았는데 빈손"이 골드 쪽에서 다시 생긴다.

### 왜 전력과 분리된 축인가
소환 수는 이미 종족별 평균 6.1~6.3으로 맞춰놨다. 골드를 그 축에 얹으면 전부 다시 잡아야 한다.
그리고 이 통로가 **전력 0.5 미만이라 소환이 0기인 스핀(화투 6.57%)의 빈손을 메운다** —
`Judge.BuildSummon`의 `Mathf.RoundToInt(Power)` 때문에 생기는 구간이다.

감각: 평균 전력 6.2 × 2 = 스핀당 12골드 ≈ 연차당 37골드 ≈ 추가 스핀 1.5회(25골드).

### 검증 — Codex QA 통과 (2026-08-31, Play Mode)
`SpinButton.onClick.Invoke()`로 반복 호출하고 **HUD에 찍힌 실제 문자열**을 읽었다
(`RunData.gold` 값이 아니라).
- 골드 문자열 `15 → 21 → 23 → 26 → 49 → 50`
- 소환 0기 사례 확인: 화투, Power 0.39045, `ListSummon.Count = 0`, 그런데 골드는 `49 → 50` — **빈손 아님**

## 2026-08-31-1 — `AddSpinCoin(int)` 추가 (무료 스핀 지급)

실제로 채운 개수를 돌려준다. 0이면 이미 가득 차서 연출할 게 없다는 뜻이다.

### ★ `spinCoinMax`에서 막는다
넘기면 HUD 핍이 `spinCoinMax` 개수만 만들어져 있어 **초과분이 화면에서 그냥 사라진다.**
본거지 HP 핍([[UIInGameHud]] 2026-08-30-0)과 ACTION 바 스왑 칸([[UIInGameAction]] 2026-08-30-1)에서
이미 두 번 겪은 함정이다 — 이번엔 지급 쪽에서 미리 막았다.

스핀은 항상 코인을 먼저 쓰므로(`SpendSpinCoin`) 지급 시점엔 최대치 미만이라
실제로 상한이 걸릴 일은 드물다. 그래서 **"무료 스핀 = 방금 쓴 코인을 돌려준다"**로 읽힌다.
