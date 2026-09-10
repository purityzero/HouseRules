# 작업 브리프 — 도트 폰트 오버플로 레이아웃 조정 (2026-09-10)

대상: `D:\Unity\HouseRules\HouseRulez` / 브랜치 `work/2026-09-10-pixel-font` (이어서 작업)

## 배경

영문 폰트를 `PressStart2P`(도트)로 교체한 뒤 네가 오버플로 14곳을 보고했다
(`.claude/qa/2026-09-10-pixel-font.md`).

**그중 대부분은 내가 이미 문구 축약으로 해결했다.** `StringTable.csv`의 En 열 17건 +
`RunEndOutOfSpinCoin`(En) · `HouseUpgradeMaxLevel`(Kr)을 고쳤다. 예시:

| 키 | 전 | 후 |
|---|---|---|
| `TitleHouseSelect` | Select House | HOUSE |
| `SettingsBgm/Sfx/Fps` | BGM Volume / SFX Volume / Frame Rate | BGM / SFX / FPS |
| `HouseUpgradeStartGoldName` | Starting Funds | Funds |
| `HouseUpgradeMaxLevel` | MAX LEVEL (En·Kr) | MAX |
| `RunEndOutOfSpinCoin` | Out of Spin Coins | NO SPINS |
| `RunEndYear` | Reached Year {0}/{1} | YEAR {0}/{1} |
| `ActionExtraSpin` | Extra Spin {0}G ({1} left) | +SPIN {0}G ({1}) |

**한국어·중국어·일본어는 사용자 지시로 그대로 뒀다**(위 `MAX` 하나만 예외 — 번역문이 아니라 영문 토큰이라 함께 바꿨다).

→ **CSV는 내 소유고 이미 끝났다. 건드리지 마라.**

## 네가 할 일 — 씬·프리팹 레이아웃만

문구를 줄여도 안 되는 곳과, 애초에 문구가 최소인 곳이다.

### 1. `ActionExtraSpin` — 버튼 폭 확대 (가장 중요)

`InGameScene.unity`의 `Action/ExtraSpinButton`. 현재 **164px**인데 `+SPIN 25G (2)`가 약 **425px** 필요하다(2.6배).

이 버튼은 내가 오늘 급하게 172px 틈(`SwapText` 끝 584 ~ `BattleSpeedButton` 시작 756)에 밀어 넣은 것이다.
**ACTION 영역(폭 1044) 전체를 다시 배치해도 된다.** 현재 배치:

```
Panel              [ -24 ~   24]
BattleStartButton  [  24 ~  312]
SwapPipRoot        [ 336 ~  408]
SwapText           [ 424 ~  584]
ExtraSpinButton    [ 588 ~  752]   ← 164px, 부족
BattleSpeedButton  [ 756 ~ 1020]
```

폭 1044을 넘지 않는 선에서(GDD 10장 ScreenZones의 ACTION = 네이티브 348 × 3)
겹치지 않게 재배치하라. 다른 요소를 줄이거나 옮겨도 된다.
어떻게 바꿨는지 전/후 x 구간 표로 보고하라.

### 2. 폭만 늘리면 되는 곳 (문구가 이미 최소)

| 경로 | 문구 | 필요 | 현재 |
|---|---|---:|---:|
| `InGameScene` `Hud/YearText` | `YEAR {0}/{1}` (4언어 공통) | 240 | 220 |
| `InGameScene` `Hud/GoldLabel` | `Gold` / `ゴールド` | 96 | 90 |
| `UIHouseUpgrade` `Node/Level` | `Lv {0}/{1}` (4언어 공통) | 156 | 140 |
| `UIHouseSelect`·`UIHouseUpgrade` `HouseButton/Name` | `Yutnori`·`Mahjong` | 224 | 200 |

종족명은 고유명사라 줄일 수 없다. 형제와 겹치지 않는 선에서 넓혀라.

### 3. 재검증

문구 축약 15건이 실제로 해소됐는지 포함해 **다시 전수로 재라.**
- 65곳 × 4개 언어, `preferredWidth` vs `rect.width`, `isTextOverflowing`/`isTextTruncated`
- **남은 오버플로가 0인지 수치로 보여라.** 0이 아니면 남은 목록을 보고하라(고치지 말고)
- `□` 계수도 다시 — 문구를 바꿨으니 새 글자가 들어왔다.
  `NO SPINS`, `+SPIN`, `HOUSE`, `Funds`, `Var`, `Cap`, `UP` 등이 PressStart2P에 다 있는지 확인
  (판정은 `characterInfo[i].character == '□'` 계수로. `HasCharacter()`는 쓰지 마라)

## 마무리

같은 브랜치 `work/2026-09-10-pixel-font`에 커밋하고 origin 푸시. **main 병합은 하지 마라.**

### ★ 커밋하면 안 되는 것
```
Assets/font/DungGeunMo Bitmap.asset
Assets/font/PixelMplus Bitmap.asset
Assets/font/Vonwaon Bitmap.asset
Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset
```

`Assets/Resources/Table/StringTable.csv`는 **내가 고친 것이니 네 커밋에 함께 담아라**(내용은 건드리지 말고).

## 보고
- ACTION 재배치 전/후 x 구간 표
- 폭 조정 4곳의 전/후 값
- **재측정 결과 — 남은 오버플로 건수(0이어야 한다)**
- `□` 계수 4개 언어
- 커밋 해시 · 푸시 대상
