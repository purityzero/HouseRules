# 저장소 참고 — 폰트 아틀라스 기준선 · 다른 컴퓨터에서 이어받기

> `.claude/UNFINISHED.md`에서 분할된 파일이다(2026-09-10, 200줄 규칙).

### ✅ 폰트 아틀라스 기준선 — GeometryDefender와 동일하게 맞춤 (2026-09-09, 커밋 `59a6960`)

**사용자 요청이 원래 "GeometryDefender처럼 똑같이"였다.** 그게 반영 안 된 상태로 오래 있었다.

| 파일 | 커밋본(전) | 지금 | GD |
|---|---|---|---|
| `DungGeunMo Bitmap.asset` | 3125줄 | **244줄** | 244줄 |
| `PixelMplus Bitmap.asset` | 285줄 | **235줄** | 235줄 |
| `Vonwaon Bitmap.asset` | 235줄 | 235줄 | 235줄 (원래 맞았음) |

세 파일 모두 GD의 같은 파일과 **바이트 단위로 일치**한다. 설정도 원래부터 같았다 —
`m_AtlasPopulationMode: 1`(Dynamic), `m_ClearDynamicDataOnBuild: 1`.

**어긋난 건 git에 커밋된 내용뿐이었다.** TMP 동적 폰트는 에디터가 텍스트를 렌더할 때마다
글리프가 아틀라스에 쌓여 asset이 부푼다. 어느 시점에 그 부푼 상태를 커밋해버려서
이후로 계속 더티가 떴다. GD는 **비어 있는 상태**(`m_GlyphTable: []` / `m_CharacterTable: []`)를
커밋해두고 깨끗하게 유지한다. 런타임에 어차피 다시 채워지므로 그게 맞는 기준선이다.

> ⚠️ **`git checkout -- "Assets/font/*.asset"`으로 되돌리면 안 된다.**
> 2026-09-09에 한 번 그렇게 조언했다가 정정했다 — 되돌리는 쪽이 **부푼 오염본**이었다.
> 판단 기준은 "더티인가"가 아니라 **"GD의 244/235줄과 같은가"**다.

→ 앞으로도 에디터를 켜면 글리프가 다시 쌓여 더티가 뜬다. **그건 커밋하지 않는다.**
   커밋 직전에 부풀어 있으면 GD본과 대조해 비운 상태로 되돌린 뒤 올린다.

### 다른 컴퓨터에서 이어받을 때

```
git clone https://github.com/purityzero/HouseRules.git
cd HouseRules && git checkout work/2026-08-30-enemy-art
```
Unity **6000.3.16f1**로 `HouseRulez/`를 연다. 확인해둔 것:
- `ProjectSettings` 26개 · `Packages/manifest.json`+`packages-lock.json` · `Assets` 1087개 전부 추적됨
- DOTween은 `Assets/Plugins/Demigiant/`에 **dll째 커밋돼 있다**(47개 파일) — 별도 설치 불필요
- `.meta` 정합성 **깨끗**: 누락 0건 / 고아 0건
- `.claude/` 60개(class 34 · design 7 · agents 8 …)도 함께 따라온다
- ⚠️ **`.gitattributes`가 없다.** 새 머신의 `core.autocrlf` 설정에 따라 전 파일이 CRLF/LF 차이로
  변경된 것처럼 보일 수 있다. 그 컴퓨터에서 `git config core.autocrlf true`(Windows)를 맞춰둘 것
- ⚠️ `manifest.json`의 `com.coplaydev.unity-mcp`가 **git `#main` 참조**다 — 그 시점의 main을 받아오므로
  머신마다 MCP 패키지 버전이 다를 수 있다
- 커밋 안 되는 로컬 전용: `Assets/_Recovery/`(크래시 복구 씬 백업), `QACapture/`, `QA_Recordings/`

---

