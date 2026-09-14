# 드래그 배치 검증 — 전장 칸끼리 교환

브랜치 `work/2026-09-13-drag-placement`(`work/2026-09-13-poker-variance` 위에 스택).
설계는 `.claude/architecture/ingame-presentation.md` §1, 클래스 문서는
`.claude/class/UIInGameFieldSlot.md` · `UIInGameField.md` · `RunRoster.md`.

## 무엇을 했나

플레이어가 전장 9칸의 유닛을 **드래그로 맞바꿀 수 있게** 했다. 보관함 UI는 이번 범위가 아니다.

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/InGame/RunRoster.cs` | `ArrangeFieldByGrade()` -> **`FillEmptyFieldFromBench()`**(빈 칸만 채움) + `FindHighestGradeBenchIndex()` |
| `Assets/Scripts/InGame/UI/UIInGameFieldSlot.cs` | 드래그 인터페이스 4개 구현, `Setup(owner, cell)` 추가 |
| `Assets/Scripts/InGame/UI/UIInGameField.cs` | 명부·풀 캐시, `RefreshSlots()`, 드래그 중재 4개 메서드 |
| `Assets/Scripts/InGame/InGameScene.cs` | 호출부 2곳을 새 이름으로 |

**선행 조건이 함께 처리됐다**: `ArrangeFieldByGrade()`가 매 스핀 9칸을 성급 순으로 다시 세워
플레이어 배치를 덮어썼다. 그 함수 주석의 제거 조건("드래그 핸들러가 붙는 시점")이 충족돼 좁혔다.

## Claude 가 이미 확인한 것 (다시 하지 말 것)

- 괄호 균형 4파일 OK, `using` 완비, 호출한 API 전부 실재, 인터페이스 구현 메서드 4개 완비
- `EventSystem`은 씬에 없고 `PersistentSceneObjects.EnsureEventSystem()`이 런타임 생성(의도된 설계).
  모듈은 `InputSystemUIInputModule`, `activeInputHandler: 1`(New Input System) — 드래그 지원
- Canvas 에 `GraphicRaycaster` 있음
- 씬의 `SlotTemplate`에는 `RectTransform`과 스크립트뿐이고 **`Image`가 없다** — 그래서 칸이
  `AddComponent<Image>()`(alpha 0)로 스스로 붙인다. 씬 수정은 필요하지 않다

## 검증 항목 (우선순위 순)

### 1순위 — 컴파일

4파일이 서로를 부르고 이름이 바뀐 메서드가 있다. **DLL 이 소스보다 오래됐다**(측정 시점 기준
소스 19:13~19:17 / `Assembly-CSharp.dll` 14:12) — Unity 가 백그라운드라 아직 컴파일하지 않았다.
리임포트/재컴파일부터 해라.

### 2순위 — 드래그 교환이 실제로 동작하는가

- 유닛이 있는 칸을 잡아 **다른 유닛이 있는 칸**에 놓으면 둘이 맞바뀌는가
- 유닛이 있는 칸을 잡아 **빈 칸**에 놓으면 그 칸으로 옮겨지는가

### 3순위 — ★ 명부가 실제로 바뀌는가 (화면만 바뀐 게 아닌지)

**프로덕션 경로로 확인해야 한다.** 배치를 바꾼 뒤 전투를 시작해
**그 배치대로 아군이 서는지** 본다. `UIInGameBattle.SpawnAllies`가 명부를 읽으므로,
화면만 바뀌었다면 전투에서 옛 배치가 나온다.

### 4순위 — ★ 자동 배치가 플레이어 배치를 덮지 않는가 (이번 변경의 핵심)

1. 드래그로 유닛 자리를 바꾼다
2. **스핀을 한 번 더 돌린다**
3. 바꿔 놓은 자리가 **그대로 유지되는지** 확인한다

새로 얻은 유닛은 **빈 칸에만** 들어가야 한다. 기존 자리가 재배치되면 `FillEmptyFieldFromBench()`
가 실패한 것이다. 전투 시작 시에도 같은 함수가 불리므로 그때도 확인한다.

### 5순위 — 부정 경로

| 경우 | 기대 |
|---|---|
| **전투 중** 드래그 | 아무 일도 없어야 한다(`Clear()`가 명부를 놓아 `IsDragAllowed()`가 false) |
| **빈 칸**을 끌기 | 드래그가 시작되지 않아야 한다 |
| 칸 **밖**에 놓기 | 심볼이 원래 자리로 돌아오고 명부는 그대로 |
| 같은 칸에 놓기 | 아무 일도 없어야 한다 |

### 6순위 — 상태 오염

- **여러 번 연속 드래그**해도 심볼이 반투명하게 남거나 자리가 어긋나지 않는가
  (`OnEndDrag`가 위치·알파·`raycastTarget`을 모두 복원한다)
- 드래그 중 유닛이 **앞 레인 칸에 가려지지 않는가**(`SetAsLastSibling`)
- 드래그 후 다음 스핀에서 칸 표시가 정상인가

## 제약

- 프로젝트 파일 수정 금지, 산출물은 `Temp/` 아래에만
- 폰트 아틀라스 4개는 건드리지도 되돌리지도 말 것
- 선택 종족을 바꿨으면 끝나고 `poker` 로 되돌릴 것. `PlayerPrefs` 에 QA 키를 남기지 말 것
- 드래그는 포인터 조작이 필요하다. MCP 로 포인터 이벤트를 만들 수 없으면
  **`UIInGameField` 의 공개 메서드(`OnSlotDragBegin`/`OnSlotDrop`/`OnSlotDragEnd`)를 순서대로 불러
  같은 경로를 태워도 된다** — 다만 그건 포인터 raycast 부분을 건너뛴다는 것을 보고에 명시해라.
  `IsDragAllowed()` 와 `RunRoster` 반영은 그 경로로도 검증된다

## 별건 — 이번에 손대지 않은 것

- **보관함 UI 없음** — `RunRoster.MoveBenchToField()` · `MoveFieldToBench()` 는 호출부가 없다
- **스왑 잔재**(`RunData.m_SwapCount` 등) 미정리 — 드래그 검증이 먼저다. 같이 건드리면 원인을 못 가린다
- `RunRoster.GetFieldPower()` 는 **원래부터 호출부가 0건**이다(미사용). 내 변경과 무관하므로 두었다

## 보고

`D:/Orca/reports/drag-placement-qa-2026-09-13.md` · 항목별 통과/실패와 근거, 재현 절차
