# 드래그 실제 포인터 검증 — 마지막 남은 조각

브랜치 `work/2026-09-14-late-curve`(7단 스택의 맨 위).
선행 검증 `D:/Orca/reports/drag-placement-qa-2026-09-13.md`

## 왜 다시 하는가

드래그 배치 QA에서 **OS 포인터 raycast hit-test 만 생략**됐다. 그때 보고에
"MCP 한계로 생략하고 공개 핸들러 경로를 호출했다"고 정확히 적혀 있었는데,
**내가 그것을 "사람만 할 수 있다"로 옮겨 적었다.** 그 문장 하나 때문에 커밋 7개가 미병합으로 묶였다.

"그 통로로는 안 된다"와 "방법이 없다"는 다르다. 아래 두 경로가 있다.

## 이미 검증된 것 (다시 하지 말 것)

핸들러 로직과 명부 반영은 끝났다 — 점유칸 교환, 빈칸 이동, `RunRoster` 실제 변경,
`OnBattleStart`에서 바뀐 배치대로 생성, 다음 스핀에서 자리 유지, 부정 경로 4건, sibling 복원.

**남은 것은 하나다.** 칸이 `AddComponent<Image>()`(alpha 0)로 스스로 붙인 투명 이미지가
**실제 포인터 raycast 를 받는가.**

## 1순위 — `EventSystem.RaycastAll` (빠진 조각을 정확히 겨냥)

전체 드래그를 재현할 필요 없이 **raycast 한 번**이면 된다.

```csharp
// 슬롯의 스크린 좌표를 만든다. Canvas 가 ScreenSpaceCamera(m_RenderMode: 1) 이므로 카메라가 필요하다.
Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, slotRect.position);

PointerEventData ped = new PointerEventData(EventSystem.current) { position = screen };
List<RaycastResult> results = new List<RaycastResult>();
EventSystem.current.RaycastAll(ped, results);
```

**확인할 것**

1. `results` 가 비어 있지 않은가
2. `results[0].gameObject` 가 **그 칸(또는 그 자식)** 인가 — 다른 UI 가 위를 덮고 있으면 실패다
3. **유닛이 있는 칸과 빈 칸 모두**에서 맞는가. 빈 칸도 드롭 대상이어야 하므로 이게 중요하다
4. 9칸 전부 순회해 각각 자기 칸이 최상위로 잡히는가

**이것만 통과하면 남은 위험이 닫힌다.** 핸들러는 이미 검증됐고 raycast 가 마지막 연결이었다.

## 2순위 — Input System 가상 마우스로 전체 흐름

`com.unity.inputsystem 1.19.0` 이 있고 UI 모듈이 `InputSystemUIInputModule` 이므로,
마우스 상태를 큐에 넣으면 **실제 입력과 같은 경로**로 흐른다.

```csharp
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();

// 위치만
InputSystem.QueueStateEvent(mouse, new MouseState { position = from });
InputSystem.Update();
// 버튼 누름
InputSystem.QueueStateEvent(mouse, new MouseState { position = from }.WithButton(MouseButton.Left, true));
InputSystem.Update();
```

**주의 셋**

- `EventSystem` 은 프레임 단위로 처리한다. 각 단계 사이에 **몇 프레임 진행**시켜야 한다
- `OnBeginDrag` 는 `EventSystem.current.pixelDragThreshold`(기본 10px)를 넘어야 불린다.
  **이동을 여러 단계로 쪼개** threshold 를 확실히 넘겨라
- 뗄 때도 `WithButton(..., false)` 로 한 번 더 큐에 넣는다

**확인할 것** — 유닛이 있는 칸에서 다른 칸으로 실제 드래그해서

1. `OnBeginDrag` → `OnDrag` → (대상 칸)`OnDrop` → `OnEndDrag` 가 **전부 불리는가**
2. 드래그 중 심볼이 포인터를 따라가는가
3. 놓은 뒤 **`RunRoster` 에서 실제로 교환됐는가**
4. 칸 밖에 놓으면 원위치로 돌아오는가

## 이 검증이 실패하면

칸의 투명 Image 가 포인터를 못 받는다는 뜻이다. 그 경우 **씬의 `SlotTemplate` 에 Image 를
추가하는 작업**으로 넘어간다(그건 씬 편집이므로 별도 인계). 실패해도 원인을 특정해 보고해 달라 —
`RaycastAll` 이 비었는지, 다른 오브젝트가 위를 덮는지, `raycastTarget` 이 꺼져 있는지.

## 제약

- 프로젝트 파일 수정 금지. 산출물은 `Temp/` 아래에만
- 폰트 아틀라스 4개 미접촉. 선택 종족은 끝나고 `poker` 로
- `PlayerPrefs` 에 QA 키를 남기지 말 것
- **가상 디바이스를 추가했으면 끝나고 `InputSystem.RemoveDevice` 로 정리**하고 그 사실을 보고에 적어 달라

## 보고

`D:/Orca/reports/drag-pointer-qa-2026-09-14.md`

1. 1순위 raycast 결과 — 9칸 각각의 `results[0]` 이 무엇이었는지
2. 2순위 전체 흐름 결과 — 불린 핸들러 순서와 명부 변화
3. 실패한 항목이 있으면 원인
4. 가상 디바이스 정리 여부
