# 버튼 폰트 GD 방식 통일 (2026-09-09-1)

> `.claude/UNFINISHED.md`에서 분할된 파일이다(2026-09-10, 200줄 규칙).

## 2026-09-09-1 — 버튼 폰트를 GD 방식으로 통일 (✅ 폰트 교체 검증 통과 · ✅ 독립 재검증 일치 · 🐛 별건 결함 1건 남음)

브랜치 `work/2026-09-09-font-unify` (main에서 분기).
2026-09-09에 **MCP가 붙은 세션에서 Play Mode 전수 검증을 마쳤다** — 아래 「검증 결과」 참고.

### 무엇을 고쳤나
`UIHouseUpgrade.prefab`(13곳) · `UISetting.prefab`(8곳)의 TMP 텍스트가
`DungGeunMo Bitmap`을 **직접 참조**하고 있었다. 프로젝트의 나머지 화면
(`UIHouseSelect.prefab` · `InGameScene` · `TitleScene`)은 전부
`LiberationSans SDF` + 폴백 체인 방식이라 두 프리팹만 튀었다.

GeometryDefender는 씬·프리팹 **115곳 전부 LiberationSans**, DungGeunMo 직접 참조 **0곳**이다.
→ 21곳을 `m_fontAsset` + `m_sharedMaterial` 쌍으로 교체해 60곳 전부 LiberationSans로 통일했다.

상세는 [[UIHouseUpgrade]] · [[UISetting]] 각 2026-09-09-0.

### 왜 이렇게 보였나
폴백 체인(`LiberationSans SDF.asset`의 `m_FallbackFontAssetTable`)은 **원래부터 GD와 동일**했다.
**한글은 어느 쪽이든 결국 DungGeunMo로 그려져 같아 보인다. 갈리는 건 영문·숫자다** —
직접 참조하면 숫자도 DungGeunMo 비트맵 글리프가 되고, 폴백 방식은 LiberationSans가 된다.
업그레이드 팝업은 비용·레벨 숫자가 많아 특히 눈에 띄었다.

### ✅ 기계로 확인한 것 (편집 직후)
- 치환 후 DungGeunMo 직접 참조 잔량 **0건**
- 프로젝트 전체 `m_fontAsset` 60건 : `m_sharedMaterial(2180264)` 60건 — 짝 일치
- 변경 파일은 프리팹 2개뿐

### ✅ 검증 결과 (2026-09-09, Play Mode 전수)

강제 리임포트 후 Play Mode에서 **프로덕션 경로로** 확인했다 —
타이틀의 `UpgradeButton`/`SettingButton`을 `ExecuteEvents` + `pointerClickHandler`로 실제 클릭해
팝업을 열고, 언어 전환도 설정 팝업의 언어 버튼을 실제로 눌러서 순회했다.

**4개 언어 × 두 팝업, 표시 문자 651자 전수 판정**

| 언어 | UISetting | UIHouseUpgrade |
|---|---|---|
| Korean | 29자 · □0 · DungGeunMo 21 / Liberation 8 | 103자 · □0 · DungGeunMo 73 / Liberation 30 |
| English | 57자 · □0 · Liberation 55 / DungGeunMo 2 | 197자 · □0 · Liberation 197 |
| Japanese | 31자 · □0 · PixelMplus 21 / Liberation 8 / DungGeunMo 2 | 119자 · □0 · PixelMplus 97 / Liberation 22 |
| Chinese | 26자 · □0 · Vonwaon 3 / PixelMplus 13 / Liberation 8 / DungGeunMo 2 | 89자 · **□1** · Vonwaon 24 / PixelMplus 44 / Liberation 21 |

- 컴파일 에러·경고 **0**
- 폰트 배정은 전부 의도대로 — 한글→DungGeunMo, 영문·숫자→LiberationSans,
  일본어→PixelMplus, 중국어→Vonwaon+PixelMplus
- 검증 후 언어를 원래 값(`English`)으로 되돌리고 Play Mode 종료함

**★ 이 변경은 취향 문제가 아니라 버그 수정이었다.**
`DungGeunMo Bitmap`의 `m_FallbackFontAssetTable`은 **비어 있다**(실측).
즉 변경 **전**에는 이 21곳에서 일본어·중국어가 **전부 `□`였다.**
바꾼 뒤 남은 결손은 아래 `柶` 한 글자뿐이다.

### 🐛 검증 중 새로 잡은 결함 — 폰트 교체와는 별건

1. **중국어 `柶`(U+67F6) 글리프 누락** — "윷놀이"의 중국어 `柶戏`가 `□戏`로 뜬다.
   `UIHouseUpgrade`의 종족 탭 `Name`. Vonwaon·PixelMplus·DungGeunMo **어느 아틀라스에도 없다**
   (재검증에서 폴백 체인 전체를 훑어 재확인).
   → **고칠 자리는 한 곳뿐이다: `Assets/Resources/Table/StringTable.csv:51`**
   ```
   50,HouseYut,윷놀이,Yutnori,柶戏,ユンノリ
   ```
   문자열은 이 한 줄이고, 종족 버튼 목록과 선택된 종족명 **두 곳에 렌더돼** 콘솔 경고가 2회 찍힌다
   (재검증에서 스캔 1건 : 경고 2회로 어긋나 보였던 것의 정체 — 표 원본은 1행이다).
   → 중국어 번역어를 폰트가 커버하는 글자로 바꾸거나 Vonwaon 아틀라스에 글리프를 추가한다.
   **번역 표현 변경은 기획 판단이라 임의로 정하지 않는다.**
2. ~~**설정 팝업 CloseButton 텍스트가 `"닫기"` 한글 고정**~~ → **✅ 2026-09-09 수정·검증 완료.**
   `StringTable.csv`에 `SettingsClose` 키(Id 62)를 추가하고 `CloseButton/Text`에 기존
   `UIText` 컴포넌트를 붙였다 — **C# 코드는 무변경**(같은 프리팹의 다른 라벨 5곳이 이미 쓰던 방식).
   Play Mode 검증에서 4개 언어 전부 `□` 0건, 비한국어 `DungGeunMo 2자 → 0`,
   닫기 동작 유지를 확인했다. 상세는 [[UISetting]] 2026-09-09-1 · [[StringTable]] 2026-09-09-0
3. (관찰) 중국어에서 Vonwaon과 PixelMplus를 **한 줄에 섞으면 자간이 겹친다**
   (`朝鲜象棋`의 `鲜`+`象`). 두 폰트의 advance width가 달라서다. 일본어는 PixelMplus 단독이라 깔끔하다.
   → ⚠️ **이 항목만 아직 1인 관찰이다.** 재검증은 글리프 누락 기계 판정에 지시가 한정돼
   녹화를 생략했다. 눈으로만 판단되는 항목이라 아직 교차 확인되지 않았다

### ★ 방법론 — `HasCharacter()`는 결손 판정에 쓸 수 없다

처음엔 `TMP_FontAsset.HasCharacter(ch, searchFallbacks, tryAlternativeTypefaces)`로 판정해
"결손 0"을 얻었다. 그런데 **일부러 없는 글자(`㋡㍿⚗`)를 넣어보니 그것도 0이 나왔다** — 판정이 무효였다.

원인: **TMP는 못 찾은 글자를 이미 `□`(U+25A1)로 치환한 뒤 `characterInfo`에 넣는다.**
그래서 `characterInfo[i].character`를 검사하면 원본이 아니라 **치환 결과**를 검사하게 되고,
`□`는 LiberationSans에 있으므로 항상 `true`가 된다.

→ **올바른 판정은 `characterInfo[i].character == '□'` 계수.**
이 방법으로 바꾸고 나서야 `柶`가 잡혔다. 결함 주입 확인이 없었으면 그대로 놓쳤다.

### ✅ 독립 재검증 통과 (2026-09-09, `qa-quick-check` 위임)

새 세션에서 에디터를 켜 둔 채 `.claude/briefs/2026-09-09-font-unify-qa.md`로 위임했다.
브리프의 「대조용」 절은 **일부러 빼고 넘겼다** — 결론을 미리 주면 독립 검증이 아니다.

**8행 전부, 문자 수까지 일치했다(총 651자).**

| 언어 · 팝업 | 재검증 | 메인 세션 |
|---|---|---|
| English · Setting | 57 / □0 / Lib 55, Dung 2 | 동일 |
| English · Upgrade | 197 / □0 / Lib 197 | 동일 |
| 中文 · Setting | 26 / □0 / Von 3, Pix 13, Lib 8, Dung 2 | 동일 |
| 中文 · Upgrade | 89 / **□1** / Von 24, Pix 44, Lib 21 | 동일 |
| 日本語 · Setting | 31 / □0 / Pix 21, Lib 8, Dung 2 | 동일 |
| 日本語 · Upgrade | 119 / □0 / Pix 97, Lib 22 | 동일 |
| 한국어 · Setting | 29 / □0 / Dung 21, Lib 8 | 동일 |
| 한국어 · Upgrade | 103 / □0 / Dung 73, Lib 30 | 동일 |

- 프로덕션 경로 준수 — `ExecuteEvents.pointerClickHandler`로 `UpgradeButton`/`SettingButton`을
  실제 클릭, 언어 전환도 `LanguageButton` 실제 클릭. Play Mode **1회 진입**으로 4개 언어 순회
- 컴파일 에러 0건
- **결함 주입 확인을 서로 다른 방법으로 했다** — 메인은 `㋡㍿⚗`, 재검증은 `\U0001FA90` 이모지.
  둘 다 판정이 깨지는 것을 확인했다. 독립적으로 같은 결론에 도달했으므로
  아래 「방법론」의 `□` 계수 판정이 유효하다는 증거가 둘이 됐다
- 비한국어 Setting 팝업의 `DungGeunMo 2자`가 결함 2번의 하드코딩 `"닫기"`라는 것을
  재검증이 **독립적으로 같은 결론으로** 짚었다(`.claude/prefab/UISetting.md` 2026-09-09-1에
  이미 기록돼 있던 내용 — 새 발견이 아니라 교차 확인이다)
- 종료 처리 — 언어를 `English`로 되돌리고 Play Mode 종료. 파일 수정 0건

→ **폰트 교체 자체는 검증 통과다.** 남은 것은 아래 별건 결함 2건뿐이다.

### ⬜ 남은 것

1. **`柶`(U+67F6) 결손이 미수정이다** — 결함 1번. CloseButton(결함 2번)은 위에서 닫혔다.
   중국어 번역어를 무엇으로 바꿀지가 **기획 판단**이라 방향 확정이 먼저다.
   고칠 자리는 `Assets/Resources/Table/StringTable.csv:51` 한 줄
   (`50,HouseYut,윷놀이,Yutnori,柶戏,ユンノリ`)
2. **숫자 모양은 취향 판단이 남아 있다** — 한국어 화면에서 `Lv 3/3`·`MAX LEVEL`만
   매끈한 산세리프로 뜬다. GD와 같은 방식이지만 픽셀 아트 테마와는 이질감이 있다.
   되돌리는 문제가 아니라 GD와 다르게 갈 것인지를 정하는 문제다

---

