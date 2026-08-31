# 윷 슬롯머신 프레임 — 릴 창이 심볼 격자와 어긋난 문제

연관: [[UIHouseSlotMachine]](`Assets/Scripts/InGame/Slot/UIHouseSlotMachine.cs:99` `ApplyFrame`), `.claude/asset/reel-frames.md`, `.claude/design/yut-house.html`

## 2026-08-31-0 — frame_yut만 규격 밖이라 릴 창이 3×3 격자와 안 맞았다

### 증상
윷 종족으로 인게임에 들어가면 슬롯머신 그림이 다른 종족과 다르게 보인다.
릴 창(심볼 3×3이 보이는 구멍)이 프레임 그림의 창과 어긋나고, 헤더 액센트 바가 없다.

### 대상 파일
- `Assets/Resources/Image/InGame/Reel/frame_yut.png`
- `Assets/Resources/Image/InGame/Reel/frame_yut.png.meta`

### 규격 (형제 6장에서 실측 재확인 — `reel-frames.md`의 좌표와 일치)
`UIHouseSlotMachine`이 프레임을 붙이는 `Frame` 이미지는 **408×780**(`InGameScene.unity` 기준),
심볼 3×3이 도는 `ReelWindow`는 **288×288 @ anchoredPosition (60, -162)** 다.
408/136 = 780/260 = **정확히 3배**이므로, 프레임 원본에서 릴 창은 반드시

| 항목 | 값 |
|---|---|
| 캔버스 | 136×260, 전면 불투명 |
| 릴 창(`#393E4E`) | x 20–115, y 54–149 = **96×96, 9216px** |
| 좌측 베벨 | x19 `#363B4A` |
| 우측 베벨 | x116 `#828CA6` |
| 헤더 바 | y1–19 종족 액센트, y20 `#363B4A` |
| meta | `spriteMode: 1`(Single), `spritePixelsToUnits: 100`, `filterMode: 0`, `textureCompression: 0` |

에 있어야 한다. frame_chess / frame_janggi / frame_poker / frame_slot 4장이 전부 `#393E4E` 9216px를
(20,54)–(115,149)에 갖고 있음을 실측으로 확인했다.

### 원인 — 두 가지가 겹쳤다

**(1) 그림 자체가 규격 밖이었다.**

| 측정 항목 | frame_yut (수정 전) | 형제 프레임 |
|---|---|---|
| 불투명 경계상자 | (3,7)–(132,254) = 130×248 | (0,0)–(135,259) = 136×260 |
| `#393E4E` 릴 창 | **없음** (1473px가 (5,35)–(126,243)에 흩어져 있음) | 9216px @ (20,54)–(115,149) |
| 헤더 바 | 없음 (지붕 실루엣) | y1–19 액센트 |

`.claude/design/yut-house.html` §10에 "외곽과 릴 창 투명"이라고 적힌 대로 만들어졌는데,
**이 서술 자체가 규격과 어긋난 것**이었다. 프레임은 릴 뒤에 깔리는 불투명 패널이고
릴 창은 `#393E4E` 평면으로 채워야 한다(투명이면 배경이 그대로 비쳐 심볼 대비가 사라진다).

**(2) meta가 트리밍 rect를 들고 있어 그림 전체가 늘어났다.**

`spriteMode: 2` + rect `(3,3,132,256)`. `Frame` 이미지는 `preserveAspect: 0`이라
스프라이트 rect를 408×780에 **채우도록 늘린다.** 즉 132×256이 408×780으로 늘어나
가로 ×3.0909, 세로 ×3.0469 — 3배가 아니게 된다.
규격대로 창을 그렸더라도 이 배율만으로 **왼쪽 7.45px, 위 6.61px** 어긋난다.

`spritePixelsToUnits`도 32로 형제(100)와 달랐다(Simple 타입이라 표시에는 영향 없음, 일관성 문제).

### 수정
`reel-frames.md`의 좌표 규격대로 `frame_yut.png`을 다시 그렸다. 장식만 윷 종족(민속촌)으로:
- 상단 밴드 y21–53: 초가지붕 이엉(짚단) 처마 + 액센트 띠
- 좌우 기둥 y21–169: 장승(가로 마디 새김 + 얼굴 각인)
- 하단 밴드 y150–169: 액센트 받침 + 윷판 점 모티프
- 액센트 `#C58B57` (`HouseTable.csv`의 yut `AccentColor`)

meta는 `frame_chess.png.meta`와 같은 값으로 맞췄다 — `spriteMode: 1`, `ppu: 100`,
`textureCompression: 0`(4개 플랫폼 전부), rect `(0,0,136,260)`.
`guid` / `spriteID` / `internalID`는 그대로 뒀다(바꾸면 참조가 끊긴다).

### 검증 (Unity 6000.3.16f1, Play Mode 실측)
- `verify-compile`: `Latest Unity script compilation succeeded.`
- `Resources.Load<Sprite>("Image/InGame/Reel/frame_yut")` → rect `(0,0,136,260)`, ppu 100 — `frame_chess`와 동일
- 생성 시 자체 검사: `#393E4E` 픽셀 **9216개**, 경계상자 **(20,54)–(115,149)**, 투명 픽셀 0, `#363B4A`보다 어두운 픽셀 0
- Play Mode(윷 선택, InGameScene)에서 프레임의 릴 창 픽셀 좌표를 월드로 환산해 `ReelWindow`와 대조:
  - frame well → L=-8.222223 T=0.8333333 R=-5.555557 B=-1.833333
  - ReelWindow  → L=-8.222223 T=0.8333333 R=-5.555557 B=-1.833333
  - **어긋남 2.7e-05 px** (부동소수점 오차)
- 스핀(SpinButton `onClick.Invoke()` = 프로덕션 경로) 정상, 콘솔 에러 0
- 스크린샷: `QACapture/yut_reel_fix.png`

### 아직 안 고친 것 — 윷 심볼이 형제 종족보다 3.4배 작게 그려진다
**사용자 지시로 이번 작업 범위에서 제외했다(슬롯머신만 수정).** 사실만 남긴다.

`SymbolTemplate`의 아이콘 이미지는 96×96 칸에 `preserveAspect: 0`으로 스프라이트 rect를 늘려 채운다.
따라서 화면에 그려지는 그림 크기 = 96 × (불투명 경계상자 / 스프라이트 rect) 다.

| 종족 | spriteMode | ppu | filter | 96px 칸에 그려지는 크기 |
|---|---|---|---|---|
| chess | 2 | 100 | Bilinear | 88.6 × 92.7~93.0 |
| hwatu | 2 | 100 | Bilinear | 88.6~89.6 × 92.3~96.0 |
| janggi | 2 | 100 | Bilinear | 89.1 × 93.0 |
| mahjong | 2 | 100 | Bilinear | 88.6 × 90.0 |
| poker | 2 | 100 | Bilinear | 88.6~90.0 × 92.9~96.0 |
| slot | 2 | 100 | Bilinear | 96.0 × 96.0 |
| **yut** | **1** | **32** | **Point** | **24.0~27.0 × 78.0** |

- 원인은 art다. 윷 심볼은 막대 1개라 불투명 경계상자가 **8~9 × 26**(32×32 캔버스 안)인데,
  형제는 24~28 × 25~32로 캔버스를 거의 채운다.
- `spriteMode: 1`이라 rect가 32×32 정사각형이 되어 **왜곡은 없지만 그만큼 작게** 그려진다.
  단순히 meta를 `spriteMode: 2` + 타이트 rect로 바꾸면 9:26 그림이 정사각 칸에 늘어나
  가로로 2.9배 뚱뚱해진다 — meta만으로는 못 고친다. **art를 다시 그려야 한다.**
- 곁들여 발견한 것(전부 미수정):
  - `*_blur.png` 6장에 **순수 검정 `#000000`** 픽셀이 1~3개씩 있다. 형제 블러는 가장 어두운 값이
    `#2A2C33`이다. "검정이 보이면 적 유닛"이라는 판독 규칙 위반.
  - `*_x8.png.meta` 6장이 슬라이스 2개짜리 테이블을 들고 있다(그림 아래 떨어진 작은 덩어리 때문).
    지금은 `spriteMode: 1`이라 Unity가 이 테이블을 무시해 `Resources.LoadAll`이 18개를 돌려주고
    풀은 정상 6이다 — **활성 버그는 아니고, 누가 Multiple로 바꾸는 순간 터지는 함정**이다.
    (`hwatu-sprite-slices.md`의 07·08과 같은 계열)
  - 런타임 풀은 정상: `HouseSpriteLoader.Load(yut).Count == 6` (= `HouseTable.csv` PoolCount 6),
    블러 짝짓기 6/6 성립, 팔레트는 기본 6종 전부 정확히 3색(`#F6F5F0`/`#ADACA4`/`#2A2C33`).

### 같은 함정을 다시 안 밟으려면
- **새 종족 프레임을 추가할 때는 `#393E4E` 픽셀 수가 9216인지, 경계상자가 (20,54)–(115,149)인지 먼저 센다.**
  이 두 숫자는 `Frame`(408×780)과 `ReelWindow`(288×288 @ 60,-162)에서 역산되는 값이라 협상 대상이 아니다.
- **`preserveAspect: 0`인 이미지에 트리밍된 스프라이트를 물리면 그림 전체가 늘어난다.**
  프레임처럼 좌표가 의미를 갖는 그림은 반드시 `spriteMode: 1`(트리밍 없음)로 둔다.
- 기획 문서의 아트 서술("외곽과 릴 창 투명" 등)이 규격과 충돌하면 **규격이 우선이다.** 문서를 고친다.
