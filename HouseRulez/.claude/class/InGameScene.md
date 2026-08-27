# InGameScene

연관: [[UIHouseSlotMachine]], `UIHouseSlotReel`/`UIHouseSlotSymbol`(`Assets/Scripts/InGame/Slot/`), `BaseScene`(Glory, `Assets/Scripts/Glory/Scene/BaseScene.cs`), `TitleScene`(관례 참고 대상, 수정하지 않음), `TableManager`, `PlayerManager`

## 2026-08-26-0 — 인게임 씬 신설 (REEL 영역만, SPIN 동작)

### 개요
"SPIN을 누르면 릴 3개가 돌다가 순차로 멈춘다"까지를 목표로 인게임 씬을 새로 만들었다. 전투/판정/배치, HUD/BATTLEFIELD/SIDE 영역은 범위 밖 — REEL 영역만 실제로 배치했다. Unity MCP 미연결이라 씬 YAML을 직접 작성했다.

### 만든 파일
- `Assets/Scripts/InGame/InGameScene.cs` — 씬 진입점(`BaseScene` 상속)
- `Assets/Scenes/InGameScene.unity` / `.meta`
- `ProjectSettings/EditorBuildSettings.asset`에 씬 추가(TitleScene 항목은 그대로 둠)

### 계층 구조
```
InGameScene                                          ← InGameScene (스크립트)
Main Camera                                           (TitleScene 보일러플레이트 복사)
Global Light 2D                                       (TitleScene 보일러플레이트 복사)
EventSystem                                            (TitleScene 보일러플레이트 복사)
Canvas                                                 CanvasScaler(1920x1080, ScaleWithScreenSize) + GraphicRaycaster
└─ SafeRoot                     RectTransform 1920x864, Canvas 중앙 고정
   ├─ Background                  RawImage 1920x864, 좌상단 앵커 (0,0)           ★ 정지 배경, m_RaycastTarget: 0
   └─ SlotMachine                RectTransform 408x780, SafeRoot 좌상단 기준 (12,-72)   ← UIHouseSlotMachine
      ├─ Frame                   Image 408x780 (스프라이트는 Apply()가 frame_{종족키}로 교체)
      ├─ ReelWindow               RectTransform 288x288, SlotMachine 로컬 (60,-162)      ← RectMask2D ★
      │  ├─ Reel0                 RectTransform 96x672, ReelWindow 로컬 (0,0)             ← UIHouseSlotReel
      │  ├─ Reel1                 (96,0)                                                  ← UIHouseSlotReel
      │  └─ Reel2                 (192,0)                                                 ← UIHouseSlotReel
      ├─ SymbolTemplate           Image 96x96, 비활성 원본                                 ← UIHouseSlotSymbol
      └─ SpinButton                Button + Image, 240x80, (84,-605)
         └─ Label                  TextMeshProUGUI, "SPIN" 하드코딩(TODO: StringTable 키 미정)
```

### 좌표 배율 근거
기획서 §10 ScreenZones의 REEL 영역 네이티브 좌표(4,24 – 140,284, 136×260)를 **×3** 해서 화면 좌표를 만들었다. 640×288 네이티브 대비 1080/288=3.75라 정수 배율이 안 나오는데, 기획서 §10이 "릴 셀은 정수 배율(×2/×3)만 허용"이라 못 박고 있어 ×3을 채택하고 1920×864 영역(SafeRoot)을 화면 중앙에 두는 세로 레터박스 방식으로 처리했다. 프레임 내부 좌표(릴 창, 헤더바, 하단 SPIN 자리)도 전부 같은 ×3 배율로 환산했다 — 계산 근거는 이 작업을 지시한 프롬프트에 있던 수치를 그대로 따름.

### RectMask2D가 왜 필요한가
`UISlotMachineReel`(Glory 베이스)은 릴을 "칸 하나 높이만큼 아래로 내려가면 되돌리는" 순환 스크롤 버퍼 방식으로 굴린다(`.claude/class/UISlotMachineReel.md` 참고). 릴 루트(`Reel0`~`Reel2`)의 실제 높이는 칸 7개 × 96px = **672px**인데, 보이는 창(`ReelWindow`)은 3칸 × 96px = **288px**뿐이다. 릴이 자기 높이(672) 전체로 그려지고 `ReelWindow`에 `RectMask2D`가 없으면, 스크롤 중인 나머지 4칸(위아래 버퍼)이 `ReelWindow` 바깥, 즉 `Frame`과 화면 전체로 삐져나와 흘러다니게 된다. `ReelWindow`에 `RectMask2D`를 달아 자식(Reel0~2)을 그 288×288 영역으로 잘라내야 "3칸만 보이는 슬롯머신 창"이 된다.

### 프리팹 미존재 — 씬에 직접 배치
`.claude/class/UIHouseSlotMachine.md`가 이미 기록했듯 이 프로젝트에는 참고할 기존 프리팹이 없다(전체 프로젝트에 `.prefab` 파일 0건). 이번에도 프리팹을 새로 만들지 않고 씬에 직접 배치했다 — 이 구조(특히 ReelWindow+RectMask2D 계층)는 원본 프로젝트에 참고본이 없어 이번 세션에서 새로 정한 것이다.

### SPIN 동작
`InGameScene.OnClickSpinButton()` → `UIHouseSlotMachine.Spin()` 호출 후 `m_SpinDuration`(기본 1.5초, 인스펙터 조정 가능) 대기 → `StopAll()`. 판정기가 아직 없어(§`UIHouseSlotMachine.md` 참고) 결과는 항상 무작위다. 전투/판정이 붙으면 이 대기 시간과 `StopAll()` 호출 시점을 판정 로직 쪽에 맡기도록 바뀔 수 있다.

### SpinButton 클릭 연결
PREFAB.MD 지침대로 YAML에 UnityEvent Persistent Call을 손으로 쓰지 않고, `InGameScene.OnSetup()`에서 `m_SpinButton.onClick.AddListener(OnClickSpinButton)`으로 코드에서 연결했다. `Button.m_TargetGraphic`은 `SpinButton`의 `Image`(fileID 900200142, `m_RaycastTarget: 1`)로 채워 클릭이 잡히게 했다.

### StringTable 키 미정
SPIN 라벨 텍스트는 `StringTable`에 대응 키가 없어(확인함) `"SPIN"` 하드코딩 후 TODO로 남겼다 — 키가 생기면 `InGameScene`이나 별도 텍스트 세팅 로직에서 교체해야 한다.

### 자체 대조 결과 (씬 YAML)
- 모든 GameObject의 `m_Component` 항목 → 대응 블록 존재 확인(dangling 없음)
- 모든 블록이 어딘가의 `m_Component`/`m_Children`에서 참조됨(고아 없음, 씬 전역 싱글톤 4개(Occlusion/RenderSettings/Lightmap/NavMesh)는 원래 미참조가 정상)
- `m_Father`/`m_GameObject` 역참조 상호 일치 확인
- 스크립트 GUID 대조: `UIHouseSlotMachine`(a86857062eb046909eef59205183a581), `UIHouseSlotReel`(8cf58ffc8ecd40e78cd7b8b4d007ad7e), `UIHouseSlotSymbol`(1d39f98986634fe2af2eaffe45149cdb) — 모두 기존 `.cs.meta`와 대조 완료. `InGameScene.cs`는 신규 GUID(13a2d9f952da405f97959da09a0cce05) 발급 후 프로젝트 전체에 중복 없음을 grep으로 확인.
- 위 대조는 정규식 기반 스크립트로 기계적으로 검증한 것이고, **Unity 에디터로 실제로 열어보지는 못했다.**

### 검증 상태 — 미검증
Unity MCP 미연결로 컴파일/씬 열기/Play Mode 확인을 하지 못했다. 특히 다음은 실제 에디터에서 재확인이 필요하다:
- 씬이 실제로 파싱되어 열리는지(YAML 포맷 오류 없는지)
- `RectMask2D`의 직렬화 필드(`m_Padding`, `m_Softness`)가 이 Unity/UGUI 버전과 정확히 맞는지(패키지 소스로 대조는 했으나 실행 확인은 못 함)
- SPIN 클릭 → 릴 3개 스핀 → 순차 정지가 실제 화면에서 의도대로 보이는지(좌표/마스크가 픽셀 단위로 맞는지)
- 프레임 PNG(`frame_{종족키}.png`)가 실제로 로드되어 붙는지

---

## 2026-08-26-1 — 인게임 씬 배경 추가 (정지 배경)

### 개요
"인게임 씬 배경을 종족 테마로 정지 상태(스크롤 없음)로 깔아라"는 요청으로 배경 오브젝트를 추가했다.

### 수정한 파일
- `Assets/Scenes/InGameScene.unity` — Background GameObject 추가 (fileID: 900200160-163)
- `Assets/Scripts/InGame/InGameScene.cs` — `m_BackgroundImage` 필드 + `ApplyBackground()` 메서드 추가

### 변경 상세

#### YAML: Background 오브젝트 추가
- GameObject (fileID: 900200160): RectTransform + CanvasRenderer + RawImage
- RectTransform (fileID: 900200161): 1920×864, 좌상단 앵커/피벗 (0,1), AnchoredPosition (0,0) — SafeRoot를 정확히 채운다
- CanvasRenderer (fileID: 900200162): 표준 설정
- RawImage (fileID: 900200163): m_RaycastTarget: 0 (배경이 클릭을 가로채면 SPIN 버튼이 안 눌림), m_Texture: {fileID: 0} (코드가 런타임에 넣음), m_UVRect 1.0×1.0 (정지 배경)

SafeRoot의 `m_Children` 목록에 Background를 **맨 앞**에 배치 → SlotMachine 뒤에 그려짐(UGUI는 형제 순서대로 렌더링)

#### C#: 배경 로드 로직
- `[SerializeField] private RawImage m_BackgroundImage;` 필드 추가 (씬에서 fileID 900200163으로 연결)
- `OnSetup()`에서 `ApplyBackground(record)` 호출
- `ApplyBackground(HouseRecord _record)` 메서드 추가:
  - `BackgroundPath`가 비어있으면 조용히 return (배경 없어도 게임은 정상 동작)
  - `ResUtil.Load<Texture>()` 로 경로에서 텍스처 로드 (실패 시 return)
  - 로드 성공 시 `m_BackgroundImage.texture` 에 대입
  - 포맷은 UIHouseSelect.ApplyBackground() 와 동일

### 배경 텍스처 목록
`HouseTable.csv` BackgroundPath 컬럼:
- chess: `Image/Title/bg_chess_castle`
- janggi: `Image/Title/bg_janggi_fortress`
- hwatu: `Image/Title/bg_hwatu_moonfield`
- poker: `Image/Title/bg_poker_frontier`
- mahjong: `Image/Title/bg_mahjong_teahouse`

모두 640×288 가로 seamless, 인게임 ×3 배율(1920×864) 스크린에 정확히 맞음

### 검증 상태 — 미검증
Unity MCP 미연결로 컴파일 확인을 하지 못했다. 파일 수정만 완료했으며, 실제 씬 열기/배경 로드/SPIN 버튼 클릭 동작은 에디터 확인 필요.

## 2026-08-27-0 — 상단 HUD / 하단 ACTION 영역 추가, 런 상태 신설

### 개요
기획서 §10 ScreenZones 중 아직 비어 있던 **HUD**(상단 띠)와 **ACTION**(하단 바)을 배치했다.
표시할 값을 담을 곳이 없어 런 상태([[RunData]])와 그 초기값 테이블([[GameConfigTable]])도 함께 만들었다.
BATTLEFIELD / SIDE 영역은 여전히 범위 밖이다.

### 만든 파일
- `Assets/Scripts/InGame/RunData.cs` — 런 상태(비저장)
- `Assets/Scripts/InGame/UI/UIInGameHud.cs`, `UIInGameAction.cs` (+ 폴더 `UI/`)
- `Assets/Scripts/Table/GameConfigRecord.cs`, `SutdaBetRecord.cs`
- `Assets/Resources/Table/GameConfigTable.csv`, `SutdaBetTable.csv`

### 수정한 파일
- `TableManager.init()` — 테이블 2개 등록
- `HouseRecord` + `HouseTable.csv` — `isUseBet` 컬럼(화투만 1)
- `StringTable.csv` — 키 8개(Id 17~24)
- `Assets/Scenes/InGameScene.unity` — 블록 74개 추가

### 계층 구조 (추가분)
```
SafeRoot                              m_Children 순서: Background → Hud → SlotMachine → Action
├─ Background   (기존)
├─ Hud          1920x60,  (0,0)       ← UIInGameHud     상세는 [[UIInGameHud]]
├─ SlotMachine  (기존)
└─ Action       1044x102, (432,-750)  ← UIInGameAction  상세는 [[UIInGameAction]]
```
좌표는 기존 REEL 영역과 같은 규칙 — 네이티브 640×288 좌표를 ×3 해서 SafeRoot(1920×864) 좌상단 기준으로 둔다.
HUD `0,0 – 640,20` → 1920×60 / ACTION `144,250 – 492,284` → 1044×102 @ (432,−750).

### 수정 (함수: `OnSetup()`)
**이전**: 종족 레코드를 얻어 슬롯머신과 배경에 적용하고 SPIN 버튼 리스너를 붙이는 것으로 끝.

**이후**: 그 사이에 `m_RunData.Init()`(테이블에서 초기값 로드)과 `ApplyHud(record)` / `ApplyAction()`이 들어갔다.
두 UI 모두 링크가 비면 `Logger.Error`만 남기고 나머지는 계속 진행한다 — HUD가 없다고 슬롯이 못 돌 이유는 없다.

### 수정 (함수: `OnClickSpinButton()`)
**이전**: 슬롯머신 널 검사 후 바로 코루틴 시작.

**이후**: 맨 앞에 스핀 코인 소모가 들어갔다.
```csharp
if (m_RunData.SpendSpinCoin() == false)
    return;

if (m_Hud != null)
    m_Hud.Refresh();
```
GDD 03장의 "연차당 기본 스핀 3회"가 처음으로 실제로 물린 지점이다. 코인이 0이면 SPIN이 아무 일도 하지 않는다.
**추가 스핀 구매(골드 25 → +1, 연차당 2회)는 아직 없다** — 상점과 골드 획득 경로가 생긴 뒤에 이 분기에서 갈라진다.

### 추가 (함수: `ApplyHud()` / `ApplyAction()` / `OnBattleStart()` / `OnBattleSpeed()`)
- `ApplyAction()`은 UI의 두 이벤트를 구독한다. 씬 오브젝트끼리라 같이 파괴되므로 해제는 걸지 않았다(SPIN 버튼과 같은 관례).
- `OnBattleStart()`는 지금 로그만 찍는다 — 배치/전투 단계 자체가 없다(TODO).
- `OnBattleSpeed()`는 `RunData.ToggleBattleSpeed()` 후 `m_Action.Refresh()`. UI는 상태를 직접 고치지 않는다.

### 직렬화 필드 추가
`m_Hud`(fileID 900200172) / `m_Action`(fileID 900200292). 씬의 필드 순서도 클래스 선언 순서에 맞췄다.

### 씬 YAML 자체 대조 결과
스크립트로 기계 검증(통과):
- fileID 중복 0건 (전체 138 블록, 신규 74)
- dangling 참조 0건 / 신규 블록 중 고아 0건
- GameObject `m_Component` ↔ 컴포넌트 `m_GameObject` 상호 일치
- RectTransform `m_Children` ↔ `m_Father` 상호 일치
- 개행 LF 유지(CR 혼입 없음)

### 검증 상태 — 미검증
**Unity MCP가 이 세션에 붙지 않았다**(에디터를 세션이 열린 뒤에 켰다 — `UNFINISHED.md`의 재발 방지 메모 그대로).
그래서 씬 YAML 직접 편집 경로로 갔고, **컴파일도 플레이도 하지 못했다.** 위 대조는 파일 정합성일 뿐
"화면에 제대로 뜨는가"와는 무관하다 — 2026-08-27 오전에 드러난 이식 결함 6건이 전부 이 대조를 통과했던 종류다.

## 2026-08-27-1 — SPIN 라벨 로컬라이제이션 (하드코딩 제거)

### 개요
2026-08-26-0에서 `StringTable`에 대응 키가 없어 `"SPIN"` 하드코딩 + TODO로 남겨뒀던 것을 해소했다.
인게임 씬의 텍스트 중 유일하게 키가 없던 자리였다.

### 추가한 키
`StringTable.csv` Id 25 — `ReelSpin`. 네 언어 모두 값은 `SPIN`이다.
목업(`Assets/Design/screen_mockup_full.png`)에서 하단의 "전투 시작"은 한글인데 릴 하단 버튼만 `SPIN`으로
영문 표기라, 그 디자인 의도를 그대로 옮겼다. **번역이 빠진 게 아니라 값이 같은 것**이므로,
현지 표기로 바꾸고 싶으면 CSV 한 줄만 고치면 된다.

키 이름이 `Action*`이 아니라 `Reel*`인 이유: 이 버튼은 하단 ACTION 바가 아니라 기획서 §10의 REEL 영역
(`4,24 – 140,284`, "3×3 릴 · 홀드 · 판정 결과 · SPIN") 소속이다.

### 수정 (필드)
`[SerializeField] private TextMeshProUGUI m_SpinButtonText;` 추가 — 씬 fileID `900200152`
(SpinButton의 자식 `Label`). `using TMPro;`도 함께 추가했다.

### 추가 (함수: `ApplyLocalizedText()` / `SetText()`)
`TitleScene.ApplyLocalizedText()`와 같은 형태다. `OnSetup()`에서 `m_RunData.Init()` 직후,
슬롯머신 적용보다 앞에 부른다.

HUD/ACTION 라벨은 각각 `UIInGameHud`/`UIInGameAction`이 자기 것을 채우므로 여기서 다루지 않는다 —
이 메서드가 맡는 건 어느 UI 컴포넌트에도 안 속한 SPIN 버튼 하나뿐이다.

### 검증 시 주의
키 값이 씬 YAML의 플레이스홀더(`m_text: SPIN`)와 **글자가 같다.** 다른 라벨들은 영어로 남아 있으면
로컬라이즈 경로가 끊긴 것으로 바로 알 수 있지만, SPIN만은 링크가 빠져도 화면이 똑같아 눈으로 구분되지 않는다.
확인하려면 `ReelSpin` 행의 Kr 값을 잠깐 다른 글자로 바꿔보는 수밖에 없다.

### 검증 상태 — 미검증
MCP 미연결 세션. 씬 정합성 재대조는 통과(fileID 138 블록, 중복/dangling/고아 0건).
