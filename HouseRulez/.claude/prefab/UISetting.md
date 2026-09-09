# UISetting.prefab

경로: `Assets/Resources/Prefabs/UI/UISetting.prefab`

## 연관 스크립트
UISetting

## 계층 구조

```
UISetting                         (UISetting)
├─ DimmedBackground               (Image — 클릭 차단용 반투명 배경)
└─ Panel                          (Image)
   ├─ Title                       (TextMeshProUGUI)
   ├─ CloseButton                 (UIButton)
   ├─ BgmLabel                    (TextMeshProUGUI)
   ├─ BgmSlider                   (Slider)
   │  ├─ Background               (Image)
   │  ├─ Fill Area / Fill         (Image)
   │  └─ Handle Slide Area / Handle (Image)
   ├─ SfxLabel                    (TextMeshProUGUI)
   ├─ SfxSlider                   (Slider)
   │  ├─ Background               (Image)
   │  ├─ Fill Area / Fill         (Image)
   │  └─ Handle Slide Area / Handle (Image)
   ├─ FpsLabel                    (TextMeshProUGUI)
   ├─ FpsButton                   (UIButton)
   │  └─ Text                     (TextMeshProUGUI)
   ├─ LanguageLabel               (TextMeshProUGUI)
   └─ LanguageButton              (UIButton)
      └─ Text                     (TextMeshProUGUI)
```

---

## 2026-09-09-0 — 폰트 참조를 GeometryDefender 방식으로 통일

### 증상
버튼과 라벨의 폰트가 화면마다 달라 보였다. 사용자 지적:
"버튼들에 있는 폰트들 다른거야?"

### 원인
이 프리팹의 TMP 텍스트 **8곳이 `DungGeunMo Bitmap`을 직접 참조**하고 있었다.
프로젝트의 나머지 화면은 `LiberationSans SDF`(TMP 기본 폰트) + 폴백 체인 방식이다.

배경과 폴백 체인 구성은 [[UIHouseUpgrade]] 2026-09-09-0에 같은 내용으로 적어뒀다.
요지는 **한글은 어느 쪽이든 DungGeunMo로 그려져 같아 보이지만, 영문·숫자에서 갈린다**는 것.

DungGeunMo를 직접 참조하던 프리팹은 이 파일과 `UIHouseUpgrade.prefab` **둘뿐**이었다.

### 수정 — TMP 텍스트 8곳 (전 → 후)

`m_fontAsset`과 `m_sharedMaterial`을 **쌍으로** 교체한다.

```yaml
# 전
m_fontAsset:      {fileID: 11400000, guid: 7e00a561b2f97e04bbe6e3b6876e22e5, type: 2}
m_sharedMaterial: {fileID: -4665105272359604480, guid: 7e00a561b2f97e04bbe6e3b6876e22e5, type: 2}

# 후
m_fontAsset:      {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
```

### 검증
- ⬜ **미검증** — MCP 미연결 세션이라 YAML 직접 편집. 에디터로 확인 안 했다
- ✅ 치환 후 DungGeunMo 직접 참조 잔량 **0건**
- ⬜ 설정 팝업의 언어 버튼은 표시 문자열이 언어마다 바뀐다 —
  **일본어/중국어 선택 시 폴백이 PixelMplus·Vonwaon으로 제대로 넘어가는지** 확인 필요.
  이 프리팹에서 폴백 체인이 실제로 쓰이는 유일한 지점이라 여기가 가장 위험하다
