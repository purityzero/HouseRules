# 스왑 잔재 정리 + 업그레이드 노드 전용 — 씬 정리와 검증

브랜치 `work/2026-09-13-swap-to-extraspin`(`work/2026-09-13-drag-placement` @ `a68f664` 위 스택).
설계 기록은 `.claude/architecture/house-upgrade.md` · `ingame-presentation.md`,
클래스 문서는 `.claude/class/UIInGameAction.md` · `RunData.md` · `GameConfigTable.md`.

## 무엇을 했나

스왑은 **끝내 구현되지 않은 기능**이었다(카운터와 핍만 있고 교환 코드 0곳, GDD 03·13장에 폐기 기록).
드래그 배치가 그 역할을 대신하므로 잔재를 걷었다.

**설계안이 놓친 것이 있었다** — 스왑은 **업그레이드 트리의 한 축**이기도 했다.
`HouseUpgradeTable`에 17행(7종족 × 최대 3레벨, 옥새 3/6/12)이 있고,
**사용자 세이브에 실제 구매 기록이 있다**(chess·hwatu·mahjong Lv3, slot Lv2 = 옥새 72개).

그래서 지우지 않고 **「추가 스핀 상한」으로 전용했다.** 사용자 결정(2026-09-13).

| 파일 | 변경 |
|---|---|
| `HouseUpgradeTable.csv` | 17행의 `TargetKey`를 `SwapCountPerYear` -> **`ExtraSpinMaxPerYear`**, 문자열 키 교체. **`Key`는 `swap` 그대로** |
| `StringTable.csv` | 41·42행을 `HouseUpgradeExtraSpinMax*`로(4개 언어), `ActionSwap` 행 제거 |
| `GameConfigTable.csv` | `SwapCountPerYear` 행 제거 |
| `GameConfigRecord.cs` | `KEY_SWAP_COUNT_PER_YEAR` 상수 제거 |
| `RunData.cs` | swap 카운터 일체 제거 + **`m_ExtraSpinMax`에 업그레이드 보너스 합산 추가** |
| `UIInGameAction.cs` | 스왑 핍·라벨·폭 보정 코드 제거 |

**`Key`를 `swap`으로 유지한 것이 세이브 호환의 핵심이다** — `GetRunConfigBonus`가 세이브의
`m_NodeKey`를 그대로 조회하므로, 산 레벨이 그대로 새 효과가 된다.

## Claude 가 이미 확인한 것 (다시 하지 말 것)

- 괄호 균형 3파일 OK, `using System.Collections.Generic` 제거가 정확함(미사용 됨)
- 코드 전체에서 `Swap`/`swap` 잔여 0(드래그의 `SwapField`·`OnSlotDrop` 제외)
- `swap` 노드 17행 보존, `TargetKey` 교체 확인
- `RunData`가 보너스를 더하는 키가 이제 4개(`HomeHpMax`·`SpinCoinPerYear`·**`ExtraSpinMaxPerYear`**·`RunStartGold`)
- `UIInGameAction`이 읽는 `RunData` 프로퍼티 5개 전부 실재

## 1순위 — 씬 정리 (Codex 몫)

직렬화 필드를 지웠으므로 씬 오브젝트가 **고아가 됐다. 지우기 전까지 화면에 그대로 보인다.**

| 오브젝트 | fileID | 비고 |
|---|---|---|
| `SwapPipRoot` | 900200321 | 자식 `PipTemplate`(900200332) 포함 |
| `SwapText` | 900200342 | |

둘은 `Canvas` 아래 ACTION 바의 자식이다. `UIInGameAction` 컴포넌트에 남은
`m_PipSpacing`·`m_PipFilledColor`·`m_PipEmptyColor` 직렬화 값은 Unity 가 무시하므로 둬도 된다.

**덤으로 확인해 줄 것** — 브리프 `2026-09-10-extra-spin-button.md`에 `SwapText [424~584]`가
`ExtraSpinButton [476~740]`과 **겹친다**고 기록돼 있다. 스왑이 사라지면 그 겹침이 해소되는지,
그리고 추가 스핀 버튼이 정상으로 보이는지 봐 달라. **레이아웃 재배치는 이번 범위가 아니다**
(그 버튼은 172px 틈에 급히 밀어 넣은 것이라 공간이 생겼지만, 옮기는 것은 별건).

## 2순위 — 컴파일

6파일이 걸렸고 상수·프로퍼티가 사라졌다. 리임포트/재컴파일부터.

## 3순위 — ★ 세이브 호환 (가장 중요)

`PlayerPrefs`의 `PlayerData`에 `{"m_NodeKey":"swap","m_Level":3}` 같은 기록이 있다.

1. **업그레이드 화면에 그 노드가 「추가 스핀 상한」으로 보이고, 산 레벨이 그대로인가**
2. **옥새가 줄지 않았는가**(측정 시점 `m_Royal: 145`)
3. 그 종족으로 런을 시작해 **`RunData.extraSpinMax`가 `2 + 산 레벨`인가**
   - chess·hwatu·mahjong = Lv3 -> **5**
   - slot = 세이브에 Lv2 기록이 있으나 **테이블에 `slot/swap` 행이 없어 2가 정답**(2026-09-13 정정)
   - poker = 없음 -> **2**

3번이 `RunData`에 새로 넣은 보너스 배선의 직접 검증이다.

## 4순위 — 인게임에 스왑이 없는가

- 액션 바에 핍과 스왑 라벨이 보이지 않는가(씬 정리 후)
- 콘솔에 `BuildSwapPipList Failed!` 같은 옛 오류가 없는가
- 배속·추가 스핀 버튼이 그대로 동작하는가
- 추가 스핀을 **상한까지 실제로 살 수 있는가** — Lv3 종족이면 5회

## 5순위 — 부정 경로

- `swap` 노드를 **안 산 종족**(poker)에서 상한이 기본값 2인가
- 상한에 도달하면 추가 스핀 버튼이 꺼지는가(`IsExtraSpinBuyable`)

## 제약

- **`PlayerPrefs`를 고치지 말 것.** 세이브 호환 검증이 목적이라 현재 상태가 그대로여야 한다.
  선택 종족은 확인을 위해 바꿔도 되지만 끝나고 `poker`로 되돌릴 것
- 폰트 아틀라스 4개 미접촉. 산출물은 `Temp/` 아래에만
- 씬은 위 두 오브젝트 제거만. 레이아웃 값은 건드리지 말 것

## 보고

`D:/Orca/reports/swap-to-extraspin-qa-2026-09-13.md` · 항목별 통과/실패와 근거,
특히 **3순위의 종족별 `extraSpinMax` 실측값**과 **옥새 잔액**
