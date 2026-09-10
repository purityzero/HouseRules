# 영문 폰트 도트화 QA (2026-09-10)

브랜치: `work/2026-09-10-pixel-font`

## 정적 검증

- LiberationSans → PressStart2P 폰트/머티리얼 쌍: 65/65
  - UIHouseSelect 20, UIHouseUpgrade 13, UISetting 8, UIRunResult 4
  - InGameScene 16, TitleScene 4
- PressStart2P 폴백: DungGeunMo → PixelMplus → Vonwaon 순서 확인
- InGameScene 로컬 Material 블록: 2 → 0
- 제거한 Material fileID의 잔여 참조: 0
- 컴파일 성공, CSV 테이블 14개 검증 통과

## Play Mode 글리프 검증

화면 버튼과 언어 버튼을 EventSystem pointerClickHandler로 실제 클릭했다.
결손은 `TMP_Text.textInfo.characterInfo[i].character == '□'`로 계수했다.

| 언어 | 결과 |
|---|---|
| English | □ 0, 영문·숫자 PressStart2P 렌더 확인 |
| Korean | □ 0, 한글 DungGeunMo 폴백 확인 |
| Japanese | □ 0, 일본어 PixelMplus 폴백 확인 |
| Chinese | 기존 `柶` 결손 2표시만 재현, 그 외 □ 0. Vonwaon+PixelMplus 폴백 확인 |

재리임포트 후 별도 Play Mode 스모크에서 TitleScene 활성 글자 29/29가
PressStart2P로 렌더됐고 □ 0, 콘솔 오류 0이었다.

## 오버플로 후보

`preferredWidth > rect.width`, `isTextOverflowing`, `isTextTruncated` 중 하나라도
성립한 항목이다. 레이아웃과 폰트 크기는 사용자 판단 대상으로 남겨 수정하지 않았다.

### TitleScene 및 팝업

| 경로/텍스트 | 필요 폭 | 실제 폭 |
|---|---:|---:|
| Menu/HouseSelectButton/Text — Select House | 408.01 | 340 |
| UISetting/BgmLabel — BGM Volume | 270.01 | 240 |
| UISetting/SfxLabel — SFX Volume | 270.01 | 240 |
| UISetting/FpsLabel — Frame Rate | 270.01 | 240 |
| UIHouseSelect/HouseButton/Name — Mahjong·Yutnori | 224.01 | 200 |
| UIHouseSelect/StatBars/Variance — Variance | 208.01 | 160 |
| UIHouseSelect/StatBars/Ceiling — Ceiling | 182.01 | 160 |
| UIHouseSelect/StatBars/Learning — Learning | 208.01 | 160 |
| UIHouseUpgrade/HouseButton/Name — Mahjong·Yutnori | 189.01 | 175 |
| UIHouseUpgrade/Node/Name — Spin Coins·Swap Count | 300.01 | 220 |
| UIHouseUpgrade/Node/Name — Starting Funds | 420.01 | 220 |
| UIHouseUpgrade/Node/Desc — Spin coins per year | 418.01 | 340 |
| UIHouseUpgrade/Node/Level — Lv 0/2·Lv 0/3·Lv 3/3 | 156.01 | 140 |
| UIHouseUpgrade/Node/UpgradeButton — Upgrade | 182.01 | 160 |
| UIHouseUpgrade/Node/MaxText — MAX LEVEL | 234.01 | 160 |

### InGameScene

| 언어/경로/텍스트 | 필요 폭 | 실제 폭 |
|---|---|---:|
| English Hud/GoldLabel — Gold | 96.01 | 90 |
| English Hud/SpinCoinLabel — Spin Coin | 216.01 | 150 |
| 공통 Hud/YearText — YEAR 01/12 | 240.01 | 220 |
| English ExtraSpinButton — Extra Spin 25G (2 left) | 690.01 | 164 |
| Korean ExtraSpinButton — 추가 스핀 25G (2회) | 420.01 | 164 |
| Chinese ExtraSpinButton — 追加旋转 25G (2次) | 390.01 | 164 |
| Japanese Hud/GoldLabel — ゴールド | 96.01 | 90 |
| Japanese ExtraSpinButton — 追加スピン 25G (2回) | 420.01 | 164 |

### UIRunResult

| 언어/경로/텍스트 | 필요 폭 | 실제 폭 |
|---|---:|---:|
| English TitleText — Out of Spin Coins | 884.01 | 540 |
| English YearText — Reached Year 2/12 | 578.01 | 540 |

## 추가 스핀 연차 리셋

실제 SpinButton·BattleStartButton·ExtraSpinButton 클릭으로 검증했다.
Year 1에서 추가 스핀을 2/2회 산 뒤 3개 웨이브를 통과했고,
Year 2 진입 직후 `extraSpinBought 2 → 0`, `spinCoin 6`, `gold 94`,
구매 버튼 활성 상태를 확인했다.

