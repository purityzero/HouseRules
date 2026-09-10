# 진단만 하고 안 고친 결함 2건

> `.claude/UNFINISHED.md`에서 분할된 파일이다(2026-09-10, 200줄 규칙).

## 2026-08-31-3 — 토스트 시스템이 죽어 있다 (진단만, 미수정)

`UIManager.ShowToast()`는 호출해도 **아무 일도 일어나지 않는다.**

| 원인 | 상태 |
|---|---|
| `Resources/Prefabs/UI/UIToastMessage` 프리팹 | **없음** (`Prefabs/UI/`엔 UIHouseSelect·UIHouseUpgrade·UISetting 3개뿐) |
| `TweenEffectPlayer` | 어떤 씬·프리팹에서도 **사용처 0건** |
| `TweenEffectBase` 파생 7종 | 전부 미사용 |

`MemoryPooling.Pop()`이 null → `ShowToast`가 조용히 반환한다. **에러 로그조차 없다.**

### 고치려면
프리팹을 만들면서 `TweenEffectPlayer`의 이펙트 배열을 채워야 한다.
★ **비어 있으면 `Play()`가 에러만 찍고 완료 콜백을 안 부른다** → 토스트가 영원히 안 닫히고 풀이 샌다.
그래서 만들 때 반드시 "떴다가 실제로 닫히는지"를 실행으로 확인해야 한다.

되살리면 게임 전체가 알림 수단을 갖는다(지금은 [[UIInGameBanner]]가 인게임 전용).

## 2026-08-31-0 — ⚠️ 윷 심볼이 형제 종족보다 3.4배 작게 그려진다 (진단만, 미수정)

이번 세션은 **슬롯머신 프레임만** 고쳤다(`frame_yut.png` + meta, Play Mode 검증 완료 —
`.claude/asset/yut-reel-frame.md`). 아래는 같이 발견했지만 **사용자 지시로 범위에서 뺀 것**이다.
윷 심볼 파일 18개 + meta 18개는 손대지 않았고 원본과 바이트 단위로 동일함을 확인했다.

심볼 칸(`SymbolTemplate` 아이콘)은 96×96에 `preserveAspect: 0`으로 스프라이트 rect를 늘려 채운다.
그려지는 크기 = 96 × (불투명 경계상자 / 스프라이트 rect):

| 종족 | 96px 칸에 그려지는 크기 | spriteMode / ppu / filter |
|---|---|---|
| chess·hwatu·janggi·mahjong·poker·slot | 88.6~96.0 × 90.0~96.0 | 2 / 100 / Bilinear |
| **yut** | **24.0~27.0 × 78.0** | **1 / 32 / Point** |

- 원인은 art다. 윷 심볼은 막대 1개라 경계상자가 8~9 × 26이고 형제는 24~28 × 25~32다.
- **meta만 고쳐선 안 된다.** `spriteMode: 2` + 타이트 rect로 바꾸면 9:26 그림이 정사각 칸에 늘어나
  가로로 2.9배 뚱뚱해진다. 실제로 고치려면 art를 다시 그려야 하고, 그건 디자인 결정이다.
- 곁들여 발견(전부 미수정):
  - `*_blur.png` 6장에 순수 검정 `#000000`이 1~3px씩 있다(형제 블러의 최암부는 `#2A2C33`).
    "검정이 보이면 적 유닛" 판독 규칙 위반.
  - `*_x8.png.meta` 6장이 슬라이스 2개짜리 테이블을 들고 있다(그림 아래 떨어진 덩어리).
    지금은 `spriteMode: 1`이라 Unity가 무시해 활성 버그가 아니다 —
    **Multiple로 바꾸는 순간 터지는 함정**(`hwatu-sprite-slices.md` 07·08과 같은 계열).
- 런타임 풀 자체는 정상이다: `HouseSpriteLoader.Load(yut).Count == 6`, 블러 짝 6/6, 기본 6종 정확히 3색.
  - **윷만 `filterMode: 0`(Point)이다** — 나머지 6종족 심볼은 전부 `1`(Bilinear). 2026-08-31 발견.
    6:1이라 윷이 아웃라이어이고, 같은 화면에서 윷만 가장자리 질감이 다르다.
    내가 만든 `Enemy/yut/*`도 아군 meta를 복사해 같은 값을 물려받았다(합쳐 24개 파일).
    ★ 크기 문제로 어차피 아트를 다시 만들어야 하니 **그때 한꺼번에 맞추는 게 낫다** —
    지금 filterMode만 바꾸면 크기는 그대로인 채 렌더링만 달라진다.

**관련 파일**: `Assets/Resources/Image/InGame/Actor/yut/*`(미수정), `.claude/asset/yut-reel-frame.md`

---
