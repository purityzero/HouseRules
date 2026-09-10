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

## 오버플로 재검증

영문 문구 축약과 레이아웃 조정 후 65곳을 4개 언어로 다시 측정했다.
판정은 `preferredWidth > rect.width + 0.5`, `isTextOverflowing`,
`isTextTruncated` 중 하나라도 성립하는 경우로 했다.

| 화면 | English | Chinese | Japanese | Korean |
|---|---:|---:|---:|---:|
| TitleScene 및 팝업 | 0 | 0 | 0 | 0 |
| InGameScene (활성 TMP 13곳) | 0 | 0 | 0 | 0 |
| UIRunResult (TMP 4곳) | 0 | 0 | 0 | 0 |

남은 오버플로는 0곳이다. 신규 □도 0이며, 중국어의 기존 `柶` 결손
2표시만 TitleScene 선택/업그레이드 UI에서 재현됐다.

### ACTION 영역 재배치

| 요소 | 변경 전 x 구간 | 변경 후 x 구간 |
|---|---:|---:|
| BattleStartButton | 24–312 | 0–184 |
| SwapPipRoot | 336–408 | 192–264 (y=20) |
| SwapText | 424–584 | 192–336 (y=-20) |
| ExtraSpinButton | 588–752 | 344–776 |
| BattleSpeedButton | 756–1020 | 780–1044 |

추가로 YearText 220→240, GoldLabel 90→96,
UIHouseUpgrade 레벨 140→156, 하우스명 버튼 200/195→224로 넓혔다.
컴파일 성공, CSV 테이블 14개 검증 통과. Play Mode는 종료했다.

## ACTION 겹침 및 전역 씬 오브젝트 후속 QA

- BattleStartButton을 `x=24`, 폭 184로 조정해 장식 Panel과의 겹침을 제거했다.
- TitleScene/InGameScene의 EventSystem과 Global Light 2D를 제거하고
  `PersistentSceneObjects`에서 각각 하나씩 유지하도록 바꿨다.
- 타이틀 Play 버튼 실제 클릭으로 InGameScene 전환 완료.
- 인게임 Spin 버튼 실제 클릭: 스핀 코인 `6 → 5`.
- 전환 전후 EventSystem 1개, Global Light 2D 1개.
- 라이트 설정: Type Global, Intensity 1, Falloff 0.5, Blend Style 0.
- EventSystem/Global Light 중복 경고 3종: 0건. 기타 경고·오류도 0건.
- Play Mode 종료.

## 추가 스핀 연차 리셋

실제 SpinButton·BattleStartButton·ExtraSpinButton 클릭으로 검증했다.
Year 1에서 추가 스핀을 2/2회 산 뒤 3개 웨이브를 통과했고,
Year 2 진입 직후 `extraSpinBought 2 → 0`, `spinCoin 6`, `gold 94`,
구매 버튼 활성 상태를 확인했다.
