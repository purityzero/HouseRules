# UIHouseUpgrade.prefab

경로: `Assets/Resources/Prefabs/UI/UIHouseUpgrade.prefab`

## 연관 스크립트
UIHouseUpgrade

## 계층 구조

```
UIHouseUpgrade                    (UIHouseUpgrade)
├─ DimmedBackground               (Image — 클릭 차단용 반투명 배경)
├─ Panel                          (Image)
│  ├─ TitleText                   (TextMeshProUGUI)
│  ├─ SelectedHouseNameText       (TextMeshProUGUI)
│  ├─ CloseButton                 (UIButton)
│  │  └─ Label                    (TextMeshProUGUI)
│  ├─ HouseButtonRoot
│  │  └─ HouseButtonTemplate      (UIButton)
│  │     ├─ Name                  (TextMeshProUGUI)
│  │     └─ Accent                (Image)
│  ├─ NodeRoot
│  │  └─ NodeTemplate
│  │     ├─ NameText              (TextMeshProUGUI)
│  │     ├─ DescText              (TextMeshProUGUI)
│  │     ├─ LevelText             (TextMeshProUGUI)
│  │     ├─ CostText              (TextMeshProUGUI)
│  │     ├─ UpgradeButton         (UIButton)
│  │     │  └─ Label              (TextMeshProUGUI)
│  │     ├─ Locked                (비활성 사유 표시)
│  │     ├─ MaxText               (TextMeshProUGUI — 최대 레벨 도달)
│  │     └─ Accent                (Image)
│  └─ EmptyStatePanel
│     └─ EmptyText                (TextMeshProUGUI)
└─ RoyalText                      (TextMeshProUGUI)
```

---

## 2026-09-09-0 — 폰트 참조를 GeometryDefender 방식으로 통일

### 증상
버튼과 라벨의 폰트가 화면마다 달라 보였다. 사용자 지적:
"버튼들에 있는 폰트들 다른거야?"

### 원인
이 프리팹의 TMP 텍스트 **13곳이 `DungGeunMo Bitmap`을 직접 참조**하고 있었다.
같은 프로젝트의 `UIHouseSelect.prefab`·`InGameScene`·`TitleScene`은 전부
`LiberationSans SDF`(TMP 기본 폰트)를 쓰고 한글은 **폴백 체인**으로 넘긴다.

`LiberationSans SDF.asset`의 `m_FallbackFontAssetTable`은 이미
GeometryDefender와 **완전히 동일**하게 설정돼 있었다(4개, 순서까지 같음).

```
LiberationSans SDF - Fallback  (2e498d1c8094910479dc3e1b768306a4)
DungGeunMo Bitmap              (7e00a561b2f97e04bbe6e3b6876e22e5)
PixelMplus Bitmap              (a077e206d9ceb4140b0c391c243e5bd9)
Vonwaon Bitmap                 (fc4ec426229b19f4f9cad02bb1c0015d)
```

**한글은 어느 쪽이든 결국 DungGeunMo로 그려지므로 같아 보인다.
갈리는 건 영문·숫자다** — 직접 참조한 쪽은 숫자도 DungGeunMo 비트맵 글리프로 나오고,
폴백 방식은 LiberationSans로 나온다. 이 프리팹은 비용·레벨 숫자가 많아 특히 눈에 띄었다.

GeometryDefender는 씬·프리팹 **115곳 전부 LiberationSans**를 쓰고
DungGeunMo 직접 참조가 **0곳**이다.

### 수정 — TMP 텍스트 13곳 (전 → 후)

`m_fontAsset`과 `m_sharedMaterial`은 **반드시 쌍으로** 바꾼다.
폰트만 바꾸고 머티리얼을 두면 아틀라스가 어긋나 글자가 깨진다.

```yaml
# 전
m_fontAsset:      {fileID: 11400000, guid: 7e00a561b2f97e04bbe6e3b6876e22e5, type: 2}
m_sharedMaterial: {fileID: -4665105272359604480, guid: 7e00a561b2f97e04bbe6e3b6876e22e5, type: 2}

# 후
m_fontAsset:      {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
```

`fileID: -4665105272359604480` = DungGeunMo Atlas Material (폰트 asset 안의 서브 에셋)
`fileID: 2180264` = LiberationSans SDF Material

### 검증
- ⬜ **미검증** — MCP 미연결 세션이라 YAML 직접 편집. 에디터로 확인 안 했다
- ✅ 치환 후 DungGeunMo 직접 참조 잔량 **0건**
- ✅ 프로젝트 전체 `m_fontAsset` 60건 : `m_sharedMaterial(2180264)` 60건 — 짝 일치
- ⬜ 실제 화면에서 숫자·한글이 정상 표시되는지, `□`(글리프 누락)가 없는지 미확인
