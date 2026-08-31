# UIInGameHud

연관: `InGameScene`(`Assets/Scripts/InGame/InGameScene.cs`), [[RunData]], [[GameConfigTable]], [[SutdaBetTable]], `HouseRecord`, `StringTable`

## 2026-08-27-0 — 신설 (인게임 상단 HUD)

### 개요
기획서 §10 ScreenZones의 HUD 영역(네이티브 `0,0 – 640,20`)을 ×3한 **1920×60 띠**. 본거지 HP · 연차 ·
골드 · 스핀 코인 · 판돈을 표시한다. 표시만 하고 값은 바꾸지 않는다 — 소유자는 `InGameScene`.

파일: `Assets/Scripts/InGame/UI/UIInGameHud.cs` (신규 폴더 `Assets/Scripts/InGame/UI/`)

### 계층 구조 (InGameScene.unity)
```
SafeRoot
└─ Hud                     RectTransform 1920x60, SafeRoot 좌상단 (0,0)    ← UIInGameHud
   ├─ Panel                Image #363B4A, stretch, m_RaycastTarget 0
   ├─ HomeLabel            TMP, x=24  w=120  좌측정렬  #ADACA4
   ├─ HomeHpPipRoot        RectTransform, x=150 w=234 (8칸 × 24 + 간격 6 × 7)
   │  └─ PipTemplate       Image #D1554C 24x24  ★ 비활성 원본, 코드가 복제한다
   ├─ YearText             TMP, x=400 w=220  #F6F5F0
   ├─ GoldLabel            TMP, x=630 w=90   #ADACA4
   ├─ GoldValue            TMP, x=730 w=140  #D6A441  fontSize 26
   ├─ SpinCoinLabel        TMP, x=890 w=150  #ADACA4
   ├─ SpinCoinPipRoot      RectTransform, x=1050 w=84 (3칸 × 24 + 간격 6 × 2)
   │  └─ PipTemplate       Image #F6F5F0 24x24  ★ 비활성 원본
   └─ BetText              TMP, 우측 앵커 x=-40 w=320  우측정렬  #D1554C
```
자식은 전부 앵커 `(0,0.5)` · 피벗 `(0,0.5)` · `y=0`(BetText만 우측 앵커) — 부모 좌측 기준 가로 배치 +
세로 중앙. 색은 GDD 09장 팔레트(패널 `#454B5E`, 입력 웰 `#393E4E`, 최암부 한계 `#363B4A`,
아군 본체 `#F6F5F0`, 음영 `#ADACA4`)를 따랐다.

### 핍을 씬에 박지 않고 코드로 복제하는 이유
HP 8칸 / 코인 3개는 **테이블 값**(`HomeHpMax`, `SpinCoinPerYear`)이다. 씬에 8개를 박아두면 CSV를 고쳤을 때
화면만 8칸으로 남아 조용히 어긋난다. 그래서 슬롯머신의 `SymbolTemplate`과 같은 관례로 비활성 원본 1개를
두고 `BuildPipList()`가 `_runData.homeHpMax` 개수만큼 복제한다.

간격 계산의 기준 폭은 **템플릿의 실제 `sizeDelta.x`에서 읽는다** — 코드에 폭을 또 적으면 씬 값과
이중 소스가 되어 나중에 한쪽만 바뀐다. 간격(`m_PipSpacing`)만 직렬화 필드다.

### 직렬화 필드 ↔ 씬 fileID
| 필드 | fileID | 타입 |
|---|---|---|
| `m_HomeLabelText` | 900200192 | TextMeshProUGUI |
| `m_HomeHpPipRoot` | 900200201 | RectTransform |
| `m_HomeHpPipTemplate` | 900200212 | Image |
| `m_YearText` | 900200222 | TextMeshProUGUI |
| `m_GoldLabelText` | 900200232 | TextMeshProUGUI |
| `m_GoldValueText` | 900200242 | TextMeshProUGUI |
| `m_SpinCoinLabelText` | 900200252 | TextMeshProUGUI |
| `m_SpinCoinPipRoot` | 900200261 | RectTransform |
| `m_SpinCoinPipTemplate` | 900200272 | Image |
| `m_BetText` | 900200282 | TextMeshProUGUI |

### 판돈 표시
화투(섯다)만 쓰는 개념이라 `HouseRecord.isUseBet`이 0이면 `BetText` 오브젝트 자체를 끈다.
종족 키를 코드에서 비교하지 않는다([[SutdaBetTable]] 참고).

### 호출 순서
`InGameScene.OnSetup()` → `Apply(runData, houseRecord)`(라벨 로컬라이즈 + 핍 생성 + 첫 그리기) →
이후 값이 바뀔 때마다 `Refresh()`. 지금 `Refresh()`를 부르는 곳은 스핀 코인이 줄어드는 `OnClickSpinButton()` 하나다.

### StringTable 키 (이번에 추가)
`HudHome` / `HudYear`(`YEAR {0}/{1}`) / `HudGold` / `HudSpinCoin` / `HudBet`(`판돈 {0} ×{1}`)

### 검증 상태 — 미검증
Unity MCP 미연결 세션이라 씬 YAML을 직접 작성했다. 파일 정합성(fileID 중복/고아/역참조/개행)은
스크립트로 대조해 통과했지만 **에디터로 열어보지도, 컴파일하지도, 플레이해보지도 않았다.**

## 2026-08-30-0 — 업그레이드로 늘어난 핍이 YEAR 텍스트를 덮은 문제

### 증상
`본거지 보강` 업그레이드를 올리면 HP 핍이 8개에서 11개로 늘어나는데, `YEAR 01/12`가 그 위에 겹쳐 찍혔다.

### 원인
`HomeHpPipRoot`의 폭이 씬에 **234(8칸분)로 박혀 있었다.** 핍은 코드가 테이블 값만큼 복제하는데
그 부모의 폭은 그대로여서, 11칸(474px)이 루트 밖으로 삐져나와 오른쪽 라벨을 침범했다.
자식은 전부 좌측 앵커 고정 배치라 아무도 밀려나지 않았다.

> 영구 메타 업그레이드를 넣으면서 내가 만든 회귀다. "테이블 값이 바뀌면 화면도 따라온다"를
> 핍 개수에만 적용하고 **그 컨테이너 폭에는 적용하지 않았다.**

### 수정 (두 겹)
1. `BuildPipList()`가 실제 핍 개수로 `_root.sizeDelta`를 갱신한다 — 폭의 소유자를 코드로 옮겼다.
2. Hud 루트를 `HorizontalLayoutGroup`(`m_Spacing: 16`, `m_ChildAlignment: 3` = MiddleLeft)으로 바꿨다.
   폭이 변하는 자식이 생겨도 뒤 항목들이 알아서 밀린다 — 같은 사고가 다른 항목에서 또 나지 않게.

### 검증 상태 — Codex QA 통과 (2026-08-30)
본거지 보강 Lv0(8칸)/Lv3(11칸) 양쪽에서 `YEAR` 텍스트와 핍 영역이 겹치지 않음을 화면 좌표로 확인.

## 2026-08-31-0 — 무료 스핀 강조 연출 `PlaySpinCoinBonus()`

코인 칸 하나가 돌아오는 게 전부라 그냥 두면 **화면 위쪽 작은 칸이라 그냥 지나친다.**
방금 채워진 칸(`spinCoin - 1` 인덱스)에 `DOPunchScale`(0.6배 / 0.45초)을 준다.

`Refresh()`로 색을 이미 채운 **뒤에** 부른다 — 순서가 뒤집히면 아직 비어 있는 칸을 강조하게 된다.

★ 시작 전에 `DOKill()` + `localScale = one`으로 되돌린다.
이전 연출이 살아 있으면 어중간한 스케일에서 시작해 칸 크기가 조금씩 어긋난 채 남는다.

토스트(`무료 스핀!`)는 [[InGameScene]]이 `UIManager.ShowToast()`로 띄운다 — 이 클래스는 HUD만 맡는다.
