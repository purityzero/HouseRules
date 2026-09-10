# 작업 브리프 — 영문 폰트 도트화 (2026-09-10)

대상: `D:\Unity\HouseRules\HouseRulez`
**새 브랜치**: `work/2026-09-10-pixel-font` (현재 `work/2026-09-10-run-end-flow`에서 분기)
한 브랜치엔 한 주제만 담는다 — 65곳을 건드리는 작업이라 문제가 나면 독립적으로 되돌릴 수 있어야 한다.

## 왜 하는가

이 게임은 픽셀아트인데 **영문·숫자만 매끈한 산세리프**로 뜬다.
한글(DungGeunMo)·일본어(PixelMplus)·중국어(Vonwaon)는 전부 도트인데,
영문은 주 폰트인 `LiberationSans SDF`에서 **바로 해결돼 폴백을 안 타기 때문**이다.

프로젝트에 도트 영문 폰트 `PressStart2P-Regular SDF`가 **이미 있는데 어디에도 배선되지 않았다**
(2026-09-10 실측: 씬·프리팹 0곳, 폴백 체인에도 없음). `fbdf255 GeometryDefender의 픽셀 폰트 세트 이식`에서
파일만 들어오고 연결이 안 된 상태다.

> ⚠️ 폴백 체인만 바꾸는 것으로는 안 된다. 영문은 주 폰트에서 해결되므로 **주 폰트 자체를 바꿔야 한다.**

## 사전 실측 (이미 끝났다 — 다시 재지 마라)

`PressStart2P-Regular.ttf` cmap 직접 파싱 결과:
- 숫자 10/10, 영문 대문자 26/26, **소문자 26/26**, 기본 기호 31/31
- **`StringTable.csv`의 En 열 실사용 문자 61종 결손 0**
- 없는 글자는 `※`(U+203B)·`₩`(U+20A9) 둘뿐 — 현재 미사용이고 폴백의 DungGeunMo가 커버한다
- 총 656자, `m_AtlasPopulationMode: 0`(**Static**) — 없는 글자는 즉시 폴백으로 넘어간다

## 해야 할 일

### 1. 주 폰트 교체 (65곳)

| 항목 | 현재 | 바꿀 값 |
|---|---|---|
| `m_fontAsset` | `{fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}` | `{fileID: 11400000, guid: e1d2fa0ffa0cc2542a43fba2dae021f7, type: 2}` |
| `m_sharedMaterial` | `{fileID: 2180264, guid: 8f586378…, type: 2}` | `{fileID: -6700491040415472100, guid: e1d2fa0ffa0cc2542a43fba2dae021f7, type: 2}` |

**머티리얼 fileID가 음수다**(`PressStart2P-Regular SDF Material`, 그 에셋의 내부 서브에셋). 부호 빠뜨리지 말 것.

대상 파일과 건수(실측):

| 파일 | 건수 |
|---|---|
| `Assets/Resources/Prefabs/UI/UIHouseSelect.prefab` | 20 |
| `Assets/Resources/Prefabs/UI/UIHouseUpgrade.prefab` | 13 |
| `Assets/Resources/Prefabs/UI/UISetting.prefab` | 8 |
| `Assets/Resources/Prefabs/UI/UIRunResult.prefab` | 4 |
| `Assets/Scenes/InGameScene.unity` | 16 |
| `Assets/Scenes/TitleScene.unity` | 4 |
| **합계** | **65** |

### 2. `PressStart2P-Regular SDF`의 폴백 체인 연결

현재 `m_FallbackFontAssetTable: []`로 **비어 있다.** CJK 3종을 순서대로 넣는다:

```
1. DungGeunMo Bitmap   guid 7e00a561b2f97e04bbe6e3b6876e22e5   (한글)
2. PixelMplus Bitmap   guid a077e206d9ceb4140b0c391c243e5bd9   (일본어)
3. Vonwaon Bitmap      guid fc4ec426229b19f4f9cad02bb1c0015d   (중국어)
```

이게 없으면 **한글·일본어·중국어가 전부 `□`가 된다.** 교체와 반드시 같이 가야 한다.

### 3. 씬에 박힌 로컬 머티리얼 정리

`InGameScene.unity`에 `!u!21 Material` 2개가 박혀 있다(`LiberationSans SDF Material (Instance)` 등).
**커밋본(HEAD)에는 0개였다** — 오늘 작업 중 TMP가 만든 인스턴스다.
65곳 머티리얼을 다시 지정하면서 참조가 끊기므로, **고아가 된 Material 블록을 제거**한다.

### 4. ★ 오버플로 전수 검사 (이번 작업의 핵심 리스크)

Press Start 2P는 8×8 격자 기반이라 **글자가 굵고 넓다.** LiberationSans 기준으로 맞춰둔
버튼·라벨 폭을 넘칠 수 있다. 예: `Reached Year 2/12`, `Out of Spin Coins`, `Extra Spin 25G (2 left)`.

교체 후 **65곳 전부**를 기계로 훑어라:
- 각 `TMP_Text`의 `preferredWidth`(또는 `renderedWidth`)와 `rectTransform.rect.width`를 비교
- `isTextOverflowing` / `isTextTruncated`도 함께 본다
- 4개 언어를 전부 적용해 각각 재라 — 한국어·일본어·중국어는 폴백 폰트라 폭이 또 다르다

넘치는 곳은 **고치지 말고 목록으로 보고**하라(오브젝트 경로 + 필요 폭 + 실제 폭).
어디를 어떻게 줄일지는 사용자 판단이 필요하다.
단, auto-size가 이미 켜진 곳은 잘림 여부만 보면 된다.

## 이어서 — QA

교체가 끝나면 Play Mode로 확인한다.

1. **4개 언어 전부 순회**해 `□`가 없는지. `characterInfo[i].character == '□'` 계수로 판정하라.
   ⚠️ `TMP_FontAsset.HasCharacter()`는 못 쓴다 — TMP가 못 찾은 글자를 이미 `□`로 치환한 뒤
   `characterInfo`에 넣어서, 원본이 아니라 치환 결과를 검사하게 된다(전례 있음).
2. **영문·숫자가 실제로 PressStart2P로 렌더되는지** — `characterInfo[i].fontAsset.name`으로 집계
3. **한/일/중이 각각 DungGeunMo·PixelMplus·Vonwaon으로 폴백되는지** 집계
4. **미검증으로 남은 항목** — 연차 전환 시 추가 스핀 구매 횟수가 리셋되어 다시 살 수 있는가.
   (직전 QA에서 연차 전환에 도달 못해 못 봤다. 이번에 같이 확인해 달라)
5. 콘솔 에러 0건 — 기존 `CS1998` 경고와 EventSystem 중복 경고는 **제외**(알려진 기존 결함)

## 마무리 — 커밋 + 푸시

사용자가 이 건은 직접 처리를 허용했다. 작업 브랜치에 커밋하고 origin에 푸시하라. **main 병합은 하지 마라.**

### ★ 커밋하면 안 되는 것
```
Assets/font/DungGeunMo Bitmap.asset
Assets/font/PixelMplus Bitmap.asset
Assets/font/Vonwaon Bitmap.asset
Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset
```
에디터를 켜면 TMP 동적 폰트에 글리프가 쌓여 더티가 뜨는 알려진 현상이다.
커밋 기준선은 GeometryDefender와 동일한 **비어 있는 상태**다.

> `PressStart2P`는 Static이라 이 현상이 없다. 다만 **주 폰트가 바뀌면 DungGeunMo 등에
> 글리프가 쌓이는 양상도 달라질 수 있으니** 커밋 직전에 위 4개가 스테이지에 없는지 다시 확인하라.

## 보고

- 교체 건수(65곳이 전부 바뀌었는지 수치로)
- 폴백 체인 연결 확인
- 로컬 Material 제거 결과
- **오버플로 목록** (넘치는 곳 없으면 "없음"을 수치 근거와 함께)
- QA 5개 항목 결과
- 커밋 해시 · 푸시 대상 · 남은 위험
