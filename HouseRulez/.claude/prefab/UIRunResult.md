# UIRunResult.prefab

경로: `Assets/Resources/Prefabs/UI/UIRunResult.prefab`

## 연관 스크립트
UIRunResult, UIButton, UIText

## 계층 구조

```
UIRunResult                    (UIRunResult)
├─ DimmedBackground            (Image — 클릭 차단용 반투명 배경, alpha 0.72)
└─ Panel                       (Image)
   ├─ TitleText                (TextMeshProUGUI — 종료 사유)
   ├─ YearText                 (TextMeshProUGUI — 도달 연차)
   ├─ RoyalText                (TextMeshProUGUI — 획득 옥새)
   └─ TitleSceneButton         (UIButton + Image)
      └─ Text                  (TextMeshProUGUI + UIText — key `RunEndToTitle`)
```

**Canvas가 없다.** [[UISetting]]과 같은 구조로 부모 캔버스에 얹힌다 —
`UISetting.cs`의 "자체 Overlay Canvas를 가지므로" 주석은 **낡은 서술이다**(실제로 없음, 2026-09-10 확인).
대신 스크립트가 `Show()` 직후 Rect를 1920×1080으로 다시 잡는다.

## 2026-09-10-0 — 신설

### 만든 방법
MCP 호출을 수십 번 하는 대신 **에디터 코드(`execute_code`)로 한 번에 구성**하고
`PrefabUtility.SaveAsPrefabAsset`으로 저장했다. 오브젝트 7개 + 컴포넌트 배선까지 1회 호출로 끝난다.

### 직렬화 필드 (전부 신규)

| 오브젝트 | 컴포넌트 | 필드 | 값 |
|---|---|---|---|
| `UIRunResult` | `UIRunResult` | `m_TitleText` | fileID `8545652928523174013` (TitleText의 TMP) |
| | | `m_YearText` | fileID `2853578872753872570` |
| | | `m_RoyalText` | fileID `5676867299798041834` |
| | | `m_TitleSceneButton` | fileID `5836201484964849473` (UIButton) |
| `TitleSceneButton/Text` | `UIText` | `m_Text` | fileID `6472253141922955273` |
| | | `m_Key` | `RunEndToTitle` |

### 저장 후 확인한 것 (PREFAB.MD 기준)
- **참조 fileID 5개가 전부 실재하는 정의를 가리킨다** — 각 1개씩 확인.
  프리팹 오버라이드가 없는 ID를 가리키면 **에러 없이 조용히 무시**되므로 반드시 대조한다.
- `m_fontAsset : m_sharedMaterial` = **4 : 4** 짝 일치
  (`LiberationSans SDF` guid `8f586378…` / 머티리얼 fileID `2180264`)
- `.meta` 함께 생성됨 — 빠뜨리면 체크아웃마다 GUID가 새로 생긴다

### 레이아웃
- 루트 1920×1080, `Panel` 620×440 중앙
- `TitleText` y=140 (52pt) · `YearText` y=40 (34pt) · `RoyalText` y=−30 (34pt)
- `TitleSceneButton` y=−150, 320×76

### ⬜ 남은 것
- **영문 폰트 도트화 대상이다.** 현재 4개 텍스트가 전부 `LiberationSans SDF`(매끈한 산세리프)라
  픽셀아트 테마와 이질적이다. 프로젝트에 `PressStart2P-Regular SDF`가 있으나
  **어디에도 배선되지 않은 상태**다(2026-09-10 실측). 전체 65곳 교체 작업에 이 프리팹도 포함된다.
