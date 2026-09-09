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

### 검증 — ✅ 통과 (2026-09-09, Play Mode 전수)
- ✅ 치환 후 DungGeunMo 직접 참조 잔량 **0건**
- ✅ 프로젝트 전체 `m_fontAsset` 60건 : `m_sharedMaterial(2180264)` 60건 — 짝 일치
- ✅ **4개 언어 전부 순회.** `UpgradeButton`을 `ExecuteEvents` + `pointerClickHandler`로
  실제 클릭해 열고 표시된 글자를 전수 판정했다

| 언어 | 표시 문자 | □ | 폰트 배정 |
|---|---|---|---|
| Korean | 103 | 0 | DungGeunMo 73 / Liberation 30 |
| English | 197 | 0 | Liberation 197 |
| Japanese | 119 | 0 | PixelMplus 97 / Liberation 22 |
| Chinese | 89 | **1** | Vonwaon 24 / PixelMplus 44 / Liberation 21 |

- ✅ 의도한 분리가 실제로 일어난다 — 한글은 DungGeunMo, **숫자·영문은 LiberationSans**.
  예: `옥새 60` → DungGeunMo 2 + Liberation 2, `Lv 3/3` → Liberation 5
- ★ `DungGeunMo Bitmap`의 폴백 체인은 **비어 있다**(실측). 변경 **전**에는
  이 프리팹에서 일본어·중국어가 **전부 `□`였다.** 이 교체는 취향이 아니라 버그 수정이었다
- ⬜ 숫자 모양이 픽셀 아트 테마에 어울리는지는 **취향 판단**으로 남아 있다

---

## 2026-09-09-1 — 🐛 중국어 `柶` 글리프 누락 (진단만, 미수정)

### 증상
언어를 중국어로 두면 종족 탭 맨 오른쪽이 **`□戏`**로 뜬다.
"윷놀이"의 중국어 표기 `柶戏`에서 앞 글자가 깨진 것. 대상은 `HouseButtonTemplate/Name`.

### 원인
`柶`(U+67F6)이 폴백 체인의 **어느 아틀라스에도 없다**.
비트맵 폰트라 런타임에 글리프를 생성하지 못한다.

```
Vonwaon.HasCharacter('柶')    = False
PixelMplus.HasCharacter('柶') = False
```

Vonwaon(간체 전용)과 PixelMplus(일본 신자체·공통 한자)는 글리프 집합이 거의 배타적이라
서로를 보완하는 구조인데, `柶`는 양쪽 다 빠져 있다.

### 고치려면
**고칠 자리는 한 곳뿐이다 — `Assets/Resources/Table/StringTable.csv:51`**

```
50,HouseYut,윷놀이,Yutnori,柶戏,ユンノリ
```

표의 원본 문자열은 이 **한 행**이고, 이것이 종족 버튼 목록(`HouseButtonTemplate/Name`)과
선택된 종족명 **두 곳에 렌더돼** 콘솔 경고가 2회 찍힌다.
(2026-09-09 독립 재검증에서 "스캔은 1건인데 경고는 2회"로 어긋나 보였던 것의 정체 —
 화면 인스턴스가 2개일 뿐 표는 1행이다. 어느 쪽을 고르든 고치는 곳은 이 한 줄이다.)

둘 중 하나 —
1. Vonwaon 아틀라스에 `柶` 글리프를 추가한다
2. 중국어 번역어를 폰트가 커버하는 표기로 바꾼다 — **번역 표현 변경은 기획 판단**이라
   임의로 정하지 않는다

### 재검증 (2026-09-09, `qa-quick-check` 독립 위임)
Play Mode 프로덕션 경로로 4개 언어를 순회해 이 결손이 **재현됨을 확인**했다.
중국어 Upgrade 팝업 89자 중 `□` 1건이 이 글자다.
폴백 체인 전체(Vonwaon·PixelMplus·DungGeunMo)에 없다는 것도 재확인됐다.
상세는 `.claude/UNFINISHED.md` 2026-09-09-1.

### ⚠️ 함께 관찰된 것 — 중국어 자간 겹침
`朝鲜象棋`처럼 **Vonwaon 글자와 PixelMplus 글자가 한 줄에 섞이면 자간이 겹친다**
(`鲜`+`象` 부분). 두 폰트의 advance width가 달라서다.
일본어는 PixelMplus 단독이라 이 현상이 없다. 중국어 표시 품질 문제로 별도 판단이 필요하다.
