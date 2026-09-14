# UIInGameFieldSlot

연관: [[UIInGameField]], [[UIHouseSlotMachine]], `HouseSlotSymbolSprite`, `SummonSlot`

## 2026-08-30-0 — 신설 (전장 한 칸)

### 개요
전장 3×3의 한 칸. 소환된 유닛 하나를 보여준다. 표시만 하고 전투 로직은 없다
(전투는 [[BattleUnit]]이 맡는다 — 소환 표시와 전투 유닛은 별개의 오브젝트다).

파일: `Assets/Scripts/InGame/UI/UIInGameFieldSlot.cs`

### 계층 구조
```
SlotTemplate        RectTransform 100x100 (코드가 96x96으로 덮어씀)   ← UIInGameFieldSlot (GO 421209072)
├─ Symbol           Image, stretch                                    (rt 1059830052)
└─ Grade            TMP 60x28, 우하단 (-2, 2)                          (rt 1026478292)
```

### 아군 아트를 따로 만들지 않는다
릴 심볼 스프라이트(`HouseSlotSymbolSprite.NormalSprite`)를 그대로 쓴다.
"릴에 나온 말이 그대로 전장에 선다"는 GDD 컨셉과 맞고 아트 비용도 들지 않는다.

### 1성은 등급 표시를 하지 않는다
`GRADE_HIDE_BELOW = 2`. 소환의 대부분이 1성이라(체스 95%) 전부 표시하면 화면이 ★1로 덮인다.
2성 이상만 `★{등급}`을 켠다.

### 공개 API
| 메서드 | 하는 일 |
|---|---|
| `SetUnit(Sprite, int grade)` | 심볼 대입 + 스프라이트 null이면 `Image.enabled = false`, 등급 표시 토글 |
| `Clear()` | 심볼/등급 모두 끔 |

`sprite`만 null로 두고 `enabled`를 안 끄면 흰 사각형이 남는다 — 두 개를 항상 세트로 다룬다.

### 검증 상태 — Codex QA 통과 (2026-08-30)


---

## 2026-09-13 — 드래그 배치 (전장 칸끼리 교환)

**연관**: [[UIInGameField]] · [[RunRoster]]

### 증상 / 요구

칸이 **표시 전용**이었다(`Clear`/`SetUnit`만). 플레이어가 유닛 자리를 옮길 수단이 없었고,
자동 배치가 매 스핀 9칸을 다시 세우므로 옮겨도 덮어써졌다(사용자 요청 2026-09-12).

### 수정

`IBeginDragHandler` · `IDragHandler` · `IEndDragHandler` · `IDropHandler` 구현.

| 추가 | 하는 일 |
|---|---|
| `Setup(UIInGameField, int cell)` | 칸 인덱스를 받고 raycast용 Image를 보장 |
| `OnBeginDrag` | 빈 칸·전투 중이면 무시. 심볼을 반투명(0.45)으로 하고 최상위로 올린다 |
| `OnDrag` | 심볼만 포인터를 따라간다(부모는 그대로) |
| `OnEndDrag` | 위치·알파·raycast 복원 후 주인에게 알린다 |
| `OnDrop` | 목적지 칸만 주인에게 알린다 |

### ★ raycast용 Image 를 코드로 붙인다

씬의 `SlotTemplate`에는 **RectTransform 과 이 스크립트뿐이다**(씬 YAML 실측) — Image 가 없어
그대로는 포인터를 못 받는다. 칸은 템플릿을 `Instantiate` 한 런타임 오브젝트라 인스펙터에서
미리 연결할 대상이 없으므로 `AddComponent<Image>()` 로 붙인다
(CODE.MD 「컴포넌트 참조는 직렬화 필드로」의 명시된 예외).

alpha 0 이어도 `raycastTarget` 이 켜져 있으면 포인터를 받는다.
**심볼 이미지로는 대신할 수 없다** — 빈 칸은 심볼이 꺼져 있어 드롭 대상이 되지 못한다.

### ★ 드래그 중 raycastTarget 을 꺼야 한다

끌고 있는 칸이 포인터를 먹으면 **그 아래 칸이 `OnDrop` 을 받지 못한다.**
`OnBeginDrag` 에서 칸과 심볼 양쪽의 `raycastTarget` 을 false 로 하고 `OnEndDrag` 에서 되돌린다.
`SetAsLastSibling()` 로 올리는 것도 필요하다 — `UIInGameField.LayoutSlots` 가 앞 레인 칸을
나중에 그리므로, 안 올리면 끌고 있는 유닛이 앞 레인에 가려진다.

### 권한 경계

칸은 **자기가 끌렸다는 것만** 안다. 어디에 놓였는지 판단과 명부 수정은 [[UIInGameField]]가 한다.
칸이 명부를 직접 고치면 9개 주체가 같은 상태를 각자 만지게 된다.

### 환경 확인 (씬 YAML · ProjectSettings 실측)

- `EventSystem` 은 **씬에 없고** `PersistentSceneObjects.EnsureEventSystem()` 이 런타임에 만든다(의도된 설계)
- 모듈은 `InputSystemUIInputModule`, `activeInputHandler: 1`(New Input System) — 드래그 인터페이스 지원
- Canvas 에 `GraphicRaycaster` 있음

### 2026-09-13-1 — 결함 수정: sibling 복원 누락

**QA가 잡았다**(`D:/Orca/reports/drag-placement-qa-2026-09-13.md`).

`OnBeginDrag`가 `SetAsLastSibling()`으로 칸을 최상위로 올리는데 `OnEndDrag`가 되돌리지 않았다.
연속 드래그 뒤 실측 `cell:sibling = [0:9, 1:6, 2:8, 3:1, 4:2, 5:7, 6:3, 7:4, 8:5]`
(정상은 `cell + 1`, 즉 `[0:1 … 8:9]`).

**왜 문제인가** — `UIInGameField.LayoutSlots`가 "앞 레인이 뒤 레인을 가리도록" 그리기 순서를
맞춰 둔다. 순서가 어긋나면 **뒤 레인 유닛이 앞 레인 위에 그려진다.** 원근 표현이 깨진다.

**수정**: `m_SiblingHomeIndex`에 드래그 시작 시 순서를 기억하고 복원할 때 되돌린다.

```csharp
m_SiblingHomeIndex = transform.GetSiblingIndex();
transform.SetAsLastSibling();
```

`cell + 1`로 계산하지 않은 이유 — **그 규칙은 `UIInGameField`의 것이다.** 칸이 그것을 알면
두 곳이 같은 규칙을 각자 들게 된다(CLAUDE.md 「소유자는 하나다」).

`RestoreSymbolTransform()`은 `Clear()`에서도 불리므로 **올린 적이 있을 때만**(`> SIBLING_HOME_NONE`)
되돌린다. 무조건 부르면 드래그하지 않은 칸의 순서를 망친다.

**alpha · anchoredPosition · raycastTarget 복원은 정상이었다**(QA 확인).


---

## 2026-09-13-2 — 연차 이동용 제자리걸음 (`SetWalking`)

### 왜 까딱임인가

**스프라이트에 걷기 프레임이 없다.** 종족 폴더를 확인하니 심볼마다 원본 · `_blur` · `_x8`
세 장뿐이다(예: `chess_king.png` / `_blur` / `_x8`). 걷기 애니메이션을 넣으려면 아트가 필요하다.

그래서 심볼을 위아래로 6px 까딱인다(`DOAnchorPosY`, 0.22초, Yoyo 무한). **아트 추가 0으로
"걷고 있다"가 읽힌다.** 배경이 흐르는 것과 합쳐져야 "이동한다"가 되므로 큰 움직임은 필요 없다.

### ★ 무한 루프 트윈이라 정리 경로를 여러 겹 뒀다

`SetLoops(-1)`은 끊지 않으면 영원히 돈다. 끊는 곳이 다섯이다.

| 경로 | 언제 |
|---|---|
| `SetWalking(false)` | 연출이 끝났다(`InGameScene.FinishYearTravel`) |
| `Clear()` | 전투가 시작돼 칸이 비워진다 |
| `OnBeginDrag` | 드래그가 시작된다 — 포인터 이동과 같은 값을 서로 덮어쓴다 |
| `OnDestroy` | 씬을 나간다. 살아 있는 트윈이 사라진 대상을 건드린다 |
| `InGameScene.OnBattleStart` | 연출 중 전투가 시작된다(`FinishYearTravel` 직접 호출) |

### 드래그와의 순서

`OnBeginDrag`에서 **걷기를 먼저 끈다.** `SetWalking(false)`가 `anchoredPosition`을 홈으로
되돌리므로, 그 뒤에 `m_SymbolHomePosition`을 읽어야 **까딱인 중간 좌표가 홈으로 기록되지 않는다.**

빈 칸과 드래그 중인 칸은 걷지 않는다(`m_SymbolImage.enabled` · `m_isDragging` 확인).


### 2026-09-14 — 투명 Image 의 raycast 실측 통과

`AddComponent<Image>()`(alpha 0)로 코드가 붙인 이미지가 **실제로 포인터를 받는지** 확인했다.

`EventSystem.RaycastAll` 을 9칸의 스크린 좌표로 각각 호출한 결과 **9/9** 가 자기 슬롯 또는
자기 `Symbol` 자식을 top hit 으로 돌려줬다. 특히 **빈 칸 8개에서 alpha 0 인 칸 자신이 단독 hit** 이다 —
이것이 빈 칸을 드롭 대상으로 만드는 근거이고, 심볼 이미지로는 대신할 수 없는 이유다.

Input System 가상 마우스(`InputSystem.QueueStateEvent` + `InputSystemUIInputModule`)로
전체 흐름도 확인했다: `cell0 ↔ 1` 실제 교환, 드래그 중 심볼 이동, 종료 복원, 칸 밖 드롭 시 명부 불변.

**alpha 가 0 이어도 `raycastTarget` 이 켜져 있으면 포인터를 받는다**는 전제가 실측으로 확인됐다.


---

## 2026-09-14 — 자유 배치: 칸 자체가 움직인다

### 무엇이 바뀌었나

| 전 (격자) | 후 (자유) |
|---|---|
| **심볼만** 옮기고 끝에 되돌린다 | **칸 루트**를 옮기고 그 자리에 머문다 |
| `IDropHandler` 로 대상 칸을 받는다 | 없다 — 놓인 좌표를 그대로 넘긴다 |
| 빈 칸도 `raycastTarget = true`(드롭 대상) | **유닛이 있을 때만 켠다** |

심볼만 옮기던 이유는 자리를 칸 번호가 정했기 때문이다. 이제는 **놓은 자리가 곧 유닛의 자리**라
루트가 움직여야 한다.

### ★ 빈 칸은 포인터를 받지 않는다

격자에서는 빈 칸이 **드롭 대상**이어야 했다("빈 칸으로 옮기기"). 자유 배치에는 그 개념이 없다.

켜 둔 채로 두면 **보이지 않는 빈 칸이 유닛 위를 덮어 드래그를 가로챈다** —
9칸이 서로 다른 자리에 흩어져 있다가 유닛들이 한쪽으로 모이면 실제로 생길 수 있는 상황이다.
`SetUnit` 이 켜고 `Clear` 가 끈다.

### 거부되면 원위치

`OnEndDrag` 가 `TryMoveUnit` 을 부르고 **false 면 드래그 시작 위치로 되돌린다**
(다른 유닛과 `MIN_DISTANCE` 안이면 명부가 거부한다). 성공하면 `RefreshSlots` 가
저장된(잘린) 좌표로 다시 그리므로 여기서 좌표를 손대지 않는다.

sibling 도 실패했을 때만 되돌린다 — 성공하면 `RefreshDrawOrder` 가 y 로 다시 정한다.

## 2026-09-14-1 — 드래그 중 유효·무효 색 피드백

### 개요
끄는 동안 **지금 자리에 놓을 수 있는지**를 색으로 알린다. 놓아봐야 아는 것이 아니라
끌면서 알 수 있어야 한다(2026-09-14 사용자 요청). 영역 표시는 [[UIInGameField]]가 맡고,
칸은 자기 색만 바꾼다.

### 추가·변경

| 변경 | 전 | 후 |
|---|---|---|
| 색 제어 | `SetSymbolAlpha(float)` | `SetSymbolColor(Color, float)` |
| `OnBeginDrag` | 알파만 낮춤 | 영역 켜기 + `RefreshDragTint()` |
| `OnDrag` | 위치만 이동 | 이동 후 `RefreshDragTint()` |
| `OnEndDrag` | 알파 복원 | 흰색 복원 + 영역 끄기 |
| `RestoreSymbolTransform` | 알파 복원 | 흰색 복원 + 영역 끄기 |

```csharp
private static readonly Color DRAG_TINT_VALID = new Color(0.55f, 1f, 0.55f);
private static readonly Color DRAG_TINT_INVALID = new Color(1f, 0.45f, 0.45f);
```

### 왜 `SetSymbolAlpha`를 `SetSymbolColor`로 바꿨나
색조와 투명도를 **따로 다루면 한쪽만 복원하는 실수가 난다.** 드래그가 끝날 때 알파는
1로 되돌렸는데 색조가 빨강으로 남으면 유닛이 계속 붉게 보인다. 하나의 메서드로 합쳐
둘이 항상 같이 설정되게 했다. 호출처 3곳을 함께 고쳤다.

### `RestoreSymbolTransform`에도 영역 끄기를 넣었다
`Clear()`가 이 메서드를 부른다 — **드래그 도중 전투가 시작되면 `OnEndDrag`가 오지 않는다.**
`OnEndDrag`에만 넣으면 그 경로에서 영역이 켜진 채 남는다.
원래 주석("드래그 중에 전투가 시작되는 등으로 칸이 비워질 수 있다")이 이미 그 위험을
가리키고 있었고, 새로 추가한 표시물도 같은 취급이 필요했다.

### 비용
`RefreshDragTint`는 매 프레임 불리지만 할당이 없고, 판정은 [[UIInGameField]]의
`IsPlacementValid`에 위임한다(유닛 9기 기준 최대 8회 비교).

## 2026-09-14-2 — QA 반영: `m_isDragging` 누수와 색 채도

### `m_isDragging` 이 영영 true 로 남던 누수 (QA 발견)

`m_isDragging` 을 false 로 되돌리는 곳이 **`OnEndDrag` 하나뿐**이었다.
드래그 도중 전투가 시작되면 `Clear()` 만 불리고 `OnEndDrag` 는 오지 않는다 —
그러면 플래그가 남아 `SetWalking` 의 가드에 영영 걸리고, **그 칸만 연차 이동에서 걷지 않는다.**
스스로 풀리는 경로가 없어서 그 런 내내 지속된다.

```csharp
// SetWalking 안의 가드 — 이것 때문에 누수가 증상으로 드러난다
if (m_isDragging == true)
    return;
```

`RestoreSymbolTransform()` 에서 `m_isDragging = false` 와 `m_SiblingHomeIndex` 초기화를 함께 한다.

**영역 표시를 넣기 전부터 있던 버그다.** 다만 같은 메서드를 이번에 수정했고
"드래그 중 전투 시작" 경로를 명시적으로 다뤘으면서도 못 본 것이라 함께 고쳤다.

위치와 sibling 은 별도 조치가 필요 없다 — `UIInGameField.RefreshSlots()` 가 `Clear` 뒤에
좌표를 다시 잡고 `RefreshDrawOrder()` 가 y 로 순서를 다시 정한다.

⚠️ 이 항목은 `UIInGameField.Clear()` 직접 호출로 검증했고 **프로덕션 `OnBattleStart`
전체 경로는 아직 타지 않았다.**

### 색 채도를 올렸다

| | 전 | 후 |
|---|---|---|
| 유효 | `(0.55, 1, 0.55)` | `(0.3, 1, 0.3)` |
| 무효 | `(1, 0.45, 0.45)` | `(1, 0.3, 0.3)` |

QA 소견이 "구분은 되지만 특히 초록이 꽤 옅어 즉시성이 약하다"였다.
`DRAG_ALPHA` 0.45 를 올리는 대신 채도로 해결했다 — 알파는 "들어올렸다"를 나타내는 표현이라
그쪽을 건드리면 원래 의도가 약해진다.
