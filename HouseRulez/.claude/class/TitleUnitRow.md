# TitleUnitRow

연관 클래스: `HouseSpriteLoader`, `HouseRecord`, `TweenUtil`/`TweenSequenceBuilder`(`Assets/Scripts/Glory/Tween/`)

## 개요
타이틀 화면 아래 서 있는 말 슬롯(`m_SlotImages`, 4개: Unit0~Unit3)을 관리. `Apply(HouseRecord)`로 현재 종족의 말 스프라이트를 랜덤으로 채운다. 씬 확인 결과 이 오브젝트는 `UIHouseSelect.m_HideOnOpen` 목록에 없음 — 패널이 열려도 계속 활성 상태로 남고, `UIHouseSelect.Select()`가 종족을 고를 때마다 `Apply()`만 호출된다(정정: 2026-08-26-1, 이전 기록이 부정확했음).

## 파일
`Assets/Scripts/Title/TitleUnitRow.cs`

## 2026-08-26-0 — 랜덤 점프 연출 추가
### 변경 전
- `Apply(HouseRecord)`만 존재. 슬롯 스프라이트/크기 세팅만 담당.

### 변경 후
- 일정 간격(랜덤 구간)마다 활성 슬롯 중 하나를 무작위로 골라 위로 점프(올라갔다 제자리)시키는 연출 추가.
- 신규 인스펙터 필드: `m_JumpHeight`(20f), `m_JumpDuration`(0.3f, 왕복 전체), `m_JumpIntervalMin`(1f), `m_JumpIntervalMax`(3f) — 씬에 이미 붙어있는 컴포넌트라 씬 편집 없이 기본값만으로 동작(직렬화 안 된 새 필드는 기본값 적용).
- 스케줄링은 `Update()` 폴링 대신 `TweenUtil.DelayedCall`을 재귀 호출하는 방식(`ScheduleNextJump` → `OnScheduledJump` → 다시 `ScheduleNextJump`) — `BaseScene` 중앙 루프에 미등록 상태라 이 프로젝트 관례(Update 매 프레임 타이머 금지)를 따름.
- 점프 자체는 `TweenSequenceBuilder.Create().Append(위로 MoveAnchored).Append(원위치로 MoveAnchored).Play()` — `TweenUtil.MoveAnchored`(기존 헬퍼)만 재사용, 신규 DOTween 직접 호출 없음.
- `Apply()` 재호출(종족 변경) 시 `ResetJump()`로 진행 중인 점프/스케줄 트윈을 `Kill()`하고, 점프 중이던 슬롯의 `anchoredPosition`을 원위치로 되돌린 뒤 스프라이트를 다시 세팅 — 어긋난 좌표에서 스프라이트만 갈리는 문제 방지.
- `gameObject.SetActive(false)`인 슬롯은 `GetActiveSlotIndices()`에서 제외되어 점프 대상에서 빠짐. 활성 슬롯이 0개면 `OnScheduledJump`가 점프를 건너뛰고 다음 스케줄만 잡음(무한 루프/예외 없음).
- `OnEnable`에서 원본 `anchoredPosition`을 1회 캐시(`m_OriginalAnchoredPositions`, null 가드로 재캐시 안 함) 후 스케줄 시작, `OnDisable`/`OnDestroy`에서 `ResetJump()`로 트윈 정리 + 위치 복원 — 패널이 열려 이 오브젝트가 꺼졌다 켜져도 정상 동작.

## 2026-08-26-1 — 검토: Apply() 재호출 시 점프 스케줄이 영구 정지하는 버그 수정
- `Apply()`가 `ResetJump()`로 `m_ScheduleTween`을 Kill만 하고 다시 스케줄을 걸지 않아서, `UIHouseSelect.Select()`로 종족을 한 번이라도 바꾸면 그 뒤로는 점프가 다시는 일어나지 않았음(재귀 스케줄의 유일한 진입점이 `OnEnable`뿐이었는데, `Apply()`가 그 예약을 끊어버림). `Apply()` 끝에 `ScheduleNextJump()` 호출을 추가해 재호출 후에도 스케줄이 이어지도록 수정.
- 위 "종족 선택 패널이 열리면 SetActive(false)" 서술은 실제 씬(`UIHouseSelect.m_HideOnOpen`)과 달라 정정함 — UnitRow는 계속 활성 상태이므로 `Apply()`가 여러 번 반복 호출될 수 있는 경로였고, 그래서 이 버그가 실제로 발현됨.

## 슬롯 구조 (씬 확인, 2026-08-26 기준)
`UnitRow`(TitleUnitRow) → `m_SlotImages`: Unit0~Unit3 (Image[4])

## 검증
**미검증** — Unity MCP 미연결로 컴파일/Play Mode 확인 불가.
