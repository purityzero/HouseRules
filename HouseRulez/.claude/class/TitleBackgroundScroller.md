# TitleBackgroundScroller

연관: [[UpdatableBehaviour]], [[BaseScene]], [[TitleScene]]

타이틀 배경을 가로로 무한히 흘리는 컴포넌트(`Assets/Scripts/Title/TitleBackgroundScroller.cs`). `RawImage.uvRect`의 x를 매 프레임 밀어서 스크롤한다.

## 2026-08-25-0 — 신규 작성 + 체스(중세 유럽) 배경 텍스처 추가

### 개요
체스 종족의 타이틀/배경으로 쓸 "끝없이 이어지는 야외 풍경"을 만들고(사용자 요청), 그것을 흘려보낼 스크롤러를 새로 작성했다. 종족마다 나라·시대 테마를 따로 입히는 방향이라(체스=유럽 중세, 이후 장기·화투·포커는 각자 테마), 이 스크롤러는 텍스처에 비의존적으로 만들어 다른 종족 배경에도 그대로 재사용한다.

### 파일
- `Assets/Scripts/Title/TitleBackgroundScroller.cs` (신규)
- `Assets/Resources/Image/Title/bg_chess_castle.png` (신규, 640×288)
- `Assets/Resources/Image/Title/bg_chess_castle.png.meta` (신규, 직접 작성 — guid `0fbf627b172a496eb81022d7e5e19f03`)

### 구현
```csharp
public class TitleBackgroundScroller : UpdatableBehaviour
{
    [SerializeField] private RawImage m_BackgroundImage;
    [SerializeField] private float m_ScrollSpeed = 0.02f;   // 1초에 텍스처 폭의 몇 배

    public override void UpdateLogic()
    {
        if (m_BackgroundImage == null)
            return;

        Rect uvRect = m_BackgroundImage.uvRect;
        uvRect.x += m_ScrollSpeed * Time.deltaTime;

        if (uvRect.x >= 1f)
            uvRect.x -= 1f;

        m_BackgroundImage.uvRect = uvRect;
    }
}
```

### 설계 판단
- **`MonoBehaviour.Update()`가 아니라 `UpdatableBehaviour.UpdateLogic()`** — `BaseScene` 갱신 루프를 타야 씬 일시정지(`isPaused`)와 씬 전환 중 정지가 자동으로 함께 걸린다. `Update()`로 짜면 일시정지 중에도 배경만 계속 흐른다.
  - 이것 때문에 [[TitleScene]]의 `[DefaultExecutionOrder(-1000)]`이 실제로 필요해졌다. 이 컴포넌트의 `OnEnable()`이 `BaseScene.OnEnable()`보다 먼저 돌면 `BaseScene.Current`가 null이라 `Register(this)`가 `?.`에 걸려 조용히 누락되고, 배경이 영영 안 움직인다(에러 로그도 없음).
- **`uvRect.x`가 1f 이상이면 1f를 뺀다** — uv를 무한정 누적하면 float 정밀도가 떨어져 도트가 미세하게 떨린다.

### 텍스처 임포트 설정 — 자동 생성 meta를 쓰면 안 된다
Unity가 자동 생성하는 `.meta`는 **Bilinear 필터 + 압축 + Clamp wrap**으로 들어와서, 도트가 뭉개지고 좌우 이어붙임도 깨진다. 그래서 `.meta`를 직접 작성해 넣었고, 임포트 후에도 값이 유지되는 것을 확인했다.

| 항목 | 값 | 이유 |
|---|---|---|
| `filterMode` | 0 (Point) | 픽셀 보간 금지 |
| `wrapU` / `wrapV` | 0 (Repeat) | 가로로 이어붙이기 |
| `textureCompression` | 0 (무압축) | 압축 블록이 도트를 뭉갬 |
| `enableMipMap` | 0 | 축소 시 흐려짐 방지 |
| `textureType` | 0 (Default) | `RawImage.uvRect` 스크롤용 (Sprite면 uv wrap이 안 먹음) |
| `spritePixelsToUnits` | 32 | GDD의 PPU 32 기준 |

**주의**: 640×288은 2의 거듭제곱이 아니다(NPOT). 최신 GPU는 NPOT + Repeat를 지원하지만, 구형 모바일 타겟을 추가할 때는 이어붙임이 깨지지 않는지 확인이 필요하다.

### 배경 텍스처 생성 방식
`bg_chess_castle.png`는 그림 파일을 손으로 그린 게 아니라 **PowerShell + System.Drawing으로 픽셀을 직접 찍어 생성**했다(생성 스크립트는 세션 스크래치패드에 있으며 프로젝트에는 포함하지 않음). 레이어를 뒤에서 앞으로 일곱 겹 쌓는다 — 하늘(3색 Bayer 디더) → 구름 → 원경 성탑 → 성벽 뒤 숲 → 흉벽 성벽 + 아치 성문 → 지면 → 앞 나무.

- 좌우 seamless는 그리기 함수가 x좌표를 폭으로 나눈 나머지로 감싸는 방식으로 보장한다. 흉벽 주기는 40px이고 640이 40의 배수라 정확히 맞물린다.
- 하늘 중간색이 GDD의 체스 액센트 `#7FA7C9` 그 자체다. 다른 종족 배경을 만들 때 이 색만 바꿔도 같은 구조로 테마가 갈린다.
- 전 색상이 환경 최암부 한계 `#363B4A`(L 59.2)보다 밝다(가장 어두운 색이 나무 줄기 그늘 L 70.1). GDD의 "검정은 적군 전용" 절대 규칙을 지키기 위해서다.
- 원경 성탑을 어둡게가 아니라 밝은 안개색으로 처리한 것도 같은 이유(GDD 지시 그대로).

### 작업 중 겪은 함정 두 가지
1. **그리기 순서** — 지면을 나무보다 나중에 그려서 나무 밑동이 통째로 지워졌다. 앞 나무는 반드시 지면 뒤에 그려야 성벽과 지면을 물고 선다.
2. **투명 픽셀** — 하늘을 `skyBottom`까지만 칠했더니 흉벽 골(성벽보다 위에 뚫린 구간)이 투명으로 남아 뷰어에서 흰 사각형으로 보였다. 하늘 칠하는 범위를 지면선까지 내려 해결.

### 검증 상태 — Play Mode 실행까지 확인 완료
2026-08-25 16:0x, Unity MCP 브릿지 연결 후 씬 배치 → Play Mode 진입까지 실행 검증했다.

| 확인 항목 | 결과 |
|---|---|
| 컴파일 | 에러 0건 |
| 배경 렌더 | 화면에 정상 표시, 로고가 위에 얹힘 |
| **스크롤 동작** | `uvRect.x`가 0 → 0.0012로 증가 (실제로 흐름) |
| `BaseScene.Current` | `TitleScene`으로 정상 등록 (`Register` 누락 없음) |
| 색 정확도 | 원본 `#5E8CB8` = 화면 캡처 `#5E8CB8` **완전 일치** |
| 텍스처 임포트 | `sRGBTexture=True`, `R8G8B8_SRGB`, Point, Repeat, mipmap off |

색 검증에서 한 번 헛짚었다 — 캡처 이미지를 640px로 축소한 인라인 미리보기가 밝게 보여 Linear 색공간과 `Light2D`를 의심했으나, 저장된 원본 해상도 파일의 픽셀을 원본 텍스처와 직접 대조하니 완전히 일치했다. **축소 리샘플링된 미리보기로 색을 판단하면 안 된다.**

### 씬 배치 (2026-08-25 완료)
```
TitleScene.unity
├─ Main Camera
├─ Global Light 2D
├─ Canvas (ScreenSpaceCamera, 1920×1080 ScaleWithScreenSize)
│   ├─ Background        ← siblingIndex 0 (맨 뒤에 그려짐), layer UI
│   │    · RawImage      : texture = bg_chess_castle, uvRect = (0, 0, 0.8, 1), raycastTarget = false
│   │    · TitleBackgroundScroller : m_BackgroundImage = 자신의 RawImage
│   └─ Title
├─ EventSystem
└─ TitleScene           ← [[TitleScene]] 컴포넌트 (씬 진입점)
```

**`uvRect.width = 0.8`의 근거**: 화면 1920 폭에 텍스처 640을 배율 3.75로 표시(1920 ÷ (640 × 3.75) = 0.8). 세로도 1080 ÷ 288 = 3.75라 픽셀이 정사각으로 유지된다.

다만 3.75는 **비정수 배율**이라 GDD가 경고한 "도트 무너짐"에 이론상 해당한다(Point 필터라 뭉개지진 않지만 일부 픽셀이 3px, 일부가 4px로 불균일해짐). 정수 배율을 원하면 ×4로 잡고 `uvRect = (0, 0, 0.75, 0.9375)`로 바꾸면 되는데, 이 경우 세로가 화면보다 커져(288 × 4 = 1152 > 1080) 위아래가 약간 잘린다. 1920×1080에서 640×288을 정수 배율로 꽉 채우는 조합은 없다.

### 남은 것 — BGM
Play Mode에서 `[TitleScene] PlayBgm Failed! SoundRecord not found - TitleTheme` 에러가 예상대로 발생한다. `Assets/Resources/Table/SoundTable.csv`가 헤더만 있는 빈 템플릿이라 그렇다. 동작에는 지장 없음(조기 리턴). 오디오 파일 + CSV 행을 추가하면 해소된다.
