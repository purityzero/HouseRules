# 드래그 배치 가능 영역 표시 — Play Mode 검증

브랜치 `work/2026-09-14-drag-area` (`work/2026-09-14-poker-range` @ `abeb3aa` 위).
제가 구현했으니 검증을 부탁드립니다.

## 무엇을 만들었나

유닛을 드래그하는 동안 **배치 가능 영역이 깔리고, 끌고 있는 유닛이 초록/빨강으로**
지금 자리에 놓을 수 있는지 알려줍니다.

그전에는 놓아봐야 알 수 있었고, 튕겨 돌아와도 이유를 알 길이 없었습니다.
사용자가 겪던 혼란이 둘이었습니다 —

- 영역 밖으로 끌면 `FieldLayout.Clamp`가 조용히 잘라서 **엉뚱한 자리에 붙는다**
- 영역 안인데도 다른 유닛과 54px 미만이면 `IsPositionFree`가 거부해 **원래 자리로 튕긴다**

## 바뀐 파일 셋 (전부 `.cs`, 씬·프리팹 수정 없음)

| 파일 | 추가 |
|---|---|
| `Assets/Scripts/InGame/FieldLayout.cs:74` | `IsInside(Vector2)` |
| `Assets/Scripts/InGame/UI/UIInGameField.cs:215` | `ShowPlacementArea(bool)` · `IsPlacementValid(int, Vector2)` · `CreatePlacementArea()` |
| `Assets/Scripts/InGame/UI/UIInGameFieldSlot.cs:203` | `RefreshDragTint()`, `SetSymbolAlpha` → `SetSymbolColor(Color, float)` |

문서는 `.claude/class/UIInGameField.md`와 `.claude/class/UIInGameFieldSlot.md`에 갱신해뒀습니다.

## 제가 확인한 것과 못 한 것

확인했습니다 — `recompile_status` `completed`/`failed:false`/`errors:[]`,
`verify-compile` 통과, 리플렉션으로 세 멤버가 DLL에 들어간 것.

**못 했습니다 — 실제로 화면에 보이는지는 전혀 보지 못했습니다.** 아래가 그 목록입니다.

## 봐주셨으면 하는 것

### 1. 영역이 맞는 자리에 맞는 크기로 뜨는가

칸의 `pivot`이 `(0,0)`이라 `anchoredPosition`은 **좌하단 모서리**입니다.
`FieldLayout.AREA_*`는 그 모서리의 허용 범위이므로, 영역이 실제로 덮어야 하는 범위는
**`(0,0) ~ (372, 200)`** (= 276+96, 104+96)입니다.

이 계산이 제 머릿속에서만 맞고 화면에서는 어긋날 수 있습니다. 9칸이 기본 격자에 있을 때
**가장 바깥 네 칸이 영역 안에 정확히 들어오는지**로 보시면 빠를 것 같습니다.

### 2. 색이 제대로 갈리는가

- 유효한 자리 → 초록 `(0.55, 1, 0.55)`
- 다른 유닛과 54px 미만 → 빨강 `(1, 0.45, 0.45)`
- 영역 밖 → 빨강

드래그 중 알파가 `0.45`라 색이 옅습니다. **두 색이 실제로 구분되는지** 봐주시면 좋겠습니다.
구분이 어려우면 그것도 결과입니다 — 채도나 알파를 올리는 안을 잡겠습니다.

### 3. ★ 드래그 도중 전투가 시작되면 영역이 꺼지는가

**이번 구현에서 가장 위험하다고 보는 곳입니다.**

`UIInGameFieldSlot.Clear()`가 그 경로인데, 이때는 `OnEndDrag`가 오지 않습니다.
그래서 `RestoreSymbolTransform()`에도 영역 끄기를 넣었습니다. 원래 주석이
"드래그 중에 전투가 시작되는 등으로 칸이 비워질 수 있다"고 이미 가리키고 있던 자리입니다.

**유닛을 끌고 있는 상태에서 전투가 시작되게 만들어** 영역이 남지 않는지 봐주시면 좋겠습니다.
재현이 까다로우면 `Clear()`를 직접 부르는 방식으로 대신하셔도 됩니다
(다만 그 경우 프로덕션 경로를 안 탄 검증이라고 적어주시면 좋겠습니다).

### 4. 그리기 순서와 포인터

- 영역이 **유닛보다 뒤에** 깔리는지 (`SetAsFirstSibling`으로 처리했습니다)
- 영역이 드래그 판정을 가로채지 않는지 (`raycastTarget = false`로 뒀습니다)
- 드래그를 여러 번 반복해도 영역이 **하나만** 있는지 (첫 호출에만 만들고 재사용합니다)

### 5. 색 복원

드래그가 끝나면 유닛이 **흰색·알파 1**로 돌아오는지. 특히 무효 상태(빨강)에서 놓아
튕겨 돌아왔을 때 붉은 기가 남지 않는지 봐주세요.
`SetSymbolAlpha`를 `SetSymbolColor`로 합친 것이 이 실수를 막으려던 것인데,
호출처 세 곳을 제가 다 고쳤다고 믿고 있을 뿐입니다.

## 참고 — Unity CLI

`capture_game_view`나 `screenshot`으로 화면을 남기시면 색 구분 판단에 도움이 될 것 같습니다.
`--project-path "D:/Unity/HouseRules/HouseRulez"`를 붙이셔야 하고,
절차는 `D:/Orca/runbook/unity-bridge.md`에 있습니다.

## 제약

- 소스와 CSV는 수정하지 말아 주세요. 고칠 곳을 찾으시면 위치와 근거만 알려주시면 제가 고치겠습니다
- 산출물은 `Temp/` 아래에만 부탁드립니다
- 폰트 아틀라스 4개는 건드리지 말아 주세요
- 선택 종족을 바꾸셨다면 끝나고 `poker`로, `PlayerPrefs`에 QA 키는 남기지 말아 주세요

## 보고

`D:/Orca/reports/drag-area-qa-2026-09-14.md`

1. 위 다섯 항목 각각의 결과
2. 색 구분이 실제로 되는지에 대한 의견 (미감 판단이라 사람 몫이지만, 보신 인상을 적어주시면 참고하겠습니다)
3. 고쳐야 할 곳이 있으면 위치와 근거
4. Console Error/Warning 유무
