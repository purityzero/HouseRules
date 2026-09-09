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
   │  └─ Text                     (TextMeshProUGUI + UIText — key `SettingsClose`)
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

### 검증 — ✅ 통과 (2026-09-09, Play Mode 전수)
- ✅ 치환 후 DungGeunMo 직접 참조 잔량 **0건**
- ✅ **4개 언어 전부 순회, 결손 글리프(`□`) 0건**.
  `SettingButton`을 `ExecuteEvents` + `pointerClickHandler`로 실제 클릭해 열고,
  언어 전환도 언어 버튼을 실제로 눌러서 확인했다

| 언어 | 표시 문자 | □ | 폰트 배정 |
|---|---|---|---|
| Korean | 29 | 0 | DungGeunMo 21 / Liberation 8 |
| English | 57 | 0 | Liberation 55 / DungGeunMo 2 |
| Japanese | 31 | 0 | PixelMplus 21 / Liberation 8 / DungGeunMo 2 |
| Chinese | 26 | 0 | Vonwaon 3 / PixelMplus 13 / Liberation 8 / DungGeunMo 2 |

- ✅ **가장 위험하다고 봤던 언어 버튼 폴백은 정상이었다** —
  `한국어`→DungGeunMo, `日本語`→PixelMplus, `中文`→PixelMplus로 각각 넘어간다
- ★ `DungGeunMo Bitmap`의 폴백 체인은 **비어 있다**(실측). 즉 변경 **전**에는
  이 프리팹에서 일본어·중국어가 **전부 `□`였다.** 이 교체는 취향이 아니라 버그 수정이었다

---

## 2026-09-09-1 — 🐛 닫기 버튼이 언어를 따라가지 않는다 (✅ 수정·검증 완료)

### 증상
`Panel/CloseButton/Text`가 **어느 언어에서든 `"닫기"` 한글로 고정**이다.
영어·중국어·일본어로 바꿔도 그대로다(위 검증의 폰트 배정 표에서
English/Japanese/Chinese 행에 `DungGeunMo 2`가 남아 있는 것이 이 두 글자다).

### 원인
프리팹에 박힌 정적 문자열이고 로컬라이제이션 키가 없다.
`UISetting.cs`는 `RefreshOptionValueText()`에서 언어 값과 FPS 값만 갱신하고
닫기 버튼 텍스트는 건드리지 않는다.

### 수정 (2026-09-09)

**⚠️ 원래 여기 적혀 있던 처방(`Show()`/`RefreshOptionValueText()`에서 코드로 세팅)은 쓰지 않았다.**
이 프리팹의 다른 정적 라벨 5곳이 이미 [[UIText]] 컴포넌트를 쓰고 있어서 그걸 재사용했다 —
**C# 코드는 한 줄도 고치지 않았다.** 코드로 세팅하면 같은 일을 두 방식으로 하게 된다.

1. `Assets/Resources/Table/StringTable.csv`에 행 추가(끝에 append, 기존 행 무변경)
   ```
   62,SettingsClose,닫기,Close,关闭,閉じる
   ```
   동일 문구의 공용 키를 먼저 검색했고 **없었다**(`Common*` 접두 키 자체가 0건).
   키 이름은 기존 `Settings*` 관례를 따랐다.
2. `Panel/CloseButton/Text`에 `UIText` 부착 (MCP 프리팹 스테이지 경유)

| 오브젝트 | 컴포넌트 | 전 | 후 |
|---|---|---|---|
| `Panel/CloseButton/Text` | `UIText` (fileID `4669150976654967088`) | 없음 | `m_Key: SettingsClose`<br>`m_Text: {fileID: 1624287168518156382}` |
| `Panel/CloseButton/Text` | `TextMeshProUGUI` (fileID `1624287168518156382`) | `m_text: "닫기"` (하드코딩) | 그대로 — `UIText`가 런타임에 덮어쓴다 |

`m_Text`가 가리키는 `1624287168518156382`은 같은 오브젝트의 TMP 컴포넌트다(저장 후 YAML 대조 확인).

### ★ 폰트 회귀가 날 뻔했던 지점
`CloseButton/Text`가 만약 `DungGeunMo Bitmap`을 **직접 참조**하고 있었다면,
번역을 넣는 순간 중국어·일본어가 **전부 `□`**가 됐을 것이다 —
DungGeunMo의 `m_FallbackFontAssetTable`은 비어 있기 때문(위 2026-09-09-0 참고).
실제로는 이미 `LiberationSans SDF`(`guid: 8f586378b4e144a9851e7b34d9b748ee`)였다.
**정적 라벨에 로컬라이제이션 키를 새로 붙일 땐 그 TMP의 폰트가 폴백 체인을 타는지 먼저 확인할 것.**

### ✅ 검증 (2026-09-09, `qa-quick-check` 위임 · Play Mode)
구현한 주체가 아닌 별도 에이전트가 검증했다. 타이틀 `SettingButton`을
`ExecuteEvents.pointerClickHandler`로 실제 클릭해 팝업을 열고, `LanguageButton`으로 4개 언어 순회.

| 언어 | 표시 문자열 | □ | 렌더 폰트 |
|---|---|---|---|
| 한국어 | `닫기` | 0 | DungGeunMo Bitmap |
| 영어 | `Close` | 0 | LiberationSans SDF |
| 중국어 | `关闭` | 0 | Vonwaon Bitmap |
| 일본어 | `閉じる` | 0 | PixelMplus Bitmap |

- **팝업 전체 `□` 0건** (4개 언어 전부)
- **비한국어 DungGeunMo 글자 수 `2 → 0`** — 하드코딩 `"닫기"`가 사라졌다는 결과 증거
- 키 문자열 `"SettingsClose"`가 그대로 뜨는 경우 없음
  (`StringTable.GetString`은 키를 못 찾으면 키 자체를 반환하므로, 이게 CSV 로드 실패의 부정 경로 판정이다)
- `CloseButton` 실제 클릭 → 팝업 `activeSelf: False` — 컴포넌트 추가로 기존 동작이 깨지지 않았다
- 결함 주입 확인: U+FFF9를 일시 주입해 `□` 1건 검출을 확인한 뒤 본 판정에 사용
- 새 에러·경고 0건

### ⚠️ 프리팹 스테이지가 저장하며 함께 바꾼 것 (내가 지시하지 않은 변경)

MCP로 프리팹 스테이지를 열고 저장하면 Unity가 TMP 필드를 정규화해 **부수 변경이 함께 들어간다.**
컴포넌트 하나 추가(약 14줄)를 기대했는데 diff가 54줄이었다. 내역:

| 변경 | 건수 | 처리 |
|---|---|---|
| `m_TextStyleHashCode: 0 → -1183493901` | 8 | 그대로 둠 — TMP가 채우는 "Normal" 스타일 해시. 무해 |
| `m_fontColor32.rgba` 캐시값 갱신 | 5 | 그대로 둠 — 이미 있던 `m_fontColor`(float)와 일치시키는 캐시 |
| **`m_sharedMaterial` → `{fileID: 0}`** (CloseButton/Text) | 1 | ❌ **되돌렸다** (아래) |
| `UISetting` 컴포넌트의 고아 직렬화 필드 5개 삭제 | 5 | 그대로 둠 — 보고만 (아래) |

**되돌린 것 — `m_sharedMaterial`**
스테이지에서 TMP가 머티리얼 *인스턴스*(`LiberationSans SDF Material (Instance)`)를 들고 있었고,
저장 시 인스턴스는 직렬화되지 못해 `{fileID: 0}`으로 떨어졌다.
이러면 2026-09-09-0이 세운 불변식(`m_fontAsset` : `m_sharedMaterial(2180264)` **짝 일치**)이
이 프리팹에서 8:7로 깨진다. YAML을 직접 고쳐 `2180264`로 되돌렸고 **8:8을 재확인**했다.
리임포트 후에도 값이 유지되는 것까지 확인했다.
> 렌더는 `{fileID: 0}` 상태에서도 정상이었다(TMP가 폰트 에셋의 기본 머티리얼로 대체).
> 되돌린 값은 같은 프리팹의 **이미 검증된 나머지 7곳과 동일**하므로 Play Mode 재검증은 하지 않았다.

**보고만 하고 안 지운 것 — 고아 직렬화 필드**
`UISetting` 컴포넌트에서 아래 5개가 사라졌다. 전부 **현재 `UISetting.cs`에 없는 필드**다
(라벨들이 [[UIText]]로 전환되며 C#에서 제거됐는데 프리팹 YAML엔 남아 있었다).
Unity가 저장하며 정리한 것이고 내가 지운 게 아니다. 되살릴 이유가 없어 그대로 뒀다.
```
m_TitleText / m_LanguageLabel / m_BgmLabel / m_SfxLabel / m_FpsLabel
```

★ **교훈: MCP 프리팹 스테이지로 저장한 뒤에는 diff 크기를 먼저 확인할 것.**
의도한 변경보다 크면 무해한 정규화인지, 위처럼 되돌려야 할 것이 섞였는지 한 줄씩 판정한다.
