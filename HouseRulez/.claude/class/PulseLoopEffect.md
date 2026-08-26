# PulseLoopEffect

연관 클래스: `TweenUtil`, `TweenSequenceBuilder`(둘 다 `Assets/Scripts/Glory/Tween/`)

## 개요
Glory 신규 컴포넌트. `Image`의 알파와 스케일을 동시에 목표값까지 갔다가 되돌아오는 것을 `SetLoops(-1, LoopType.Yoyo)`로 무한 반복하는 "숨쉬기" 연출. `RotateLoopEffect`와 같은 결(인스펙터 값 조립 + `TweenSequenceBuilder` 사용, `OnEnable`에서 재생, `OnDisable`에서 Kill)로 작성.

## 파일
`Assets/Scripts/Glory/Tween/PulseLoopEffect.cs`

## 2026-08-26-0
- 신규 생성.
- 알파(`TweenUtil.Fade(Image, ...)`)와 스케일(`TweenUtil.Scale(Transform, ...)`)을 `Append`+`Join`으로 **동시에** 진행시킨다.
  - 사용자 요청 원문("알파먹였다가 줄어들었다가")이 순차로도 읽힐 여지가 있었으나, 로고 숨쉬기 연출은 동시 진행이 자연스러워 Join으로 결정. 필요 시 순차(Append만)로 바꾸는 건 한 줄 수정.
- `OnEnable`에서 원래 알파/스케일을 캐시해두고, `OnDisable`에서 시퀀스를 `Kill()` + 값을 원래대로 복원 — 꺼진 채로 알파/스케일이 중간값에 남는 것 방지.
- 인스펙터 필드(전부 기본값 무해하게, 숨쉬기 톤):
  - `m_TargetImage` (비워두면 `GetComponent<Image>()`로 자동 채움, `[RequireComponent(typeof(Image))]`)
  - `m_TargetAlpha = 0.6f`
  - `m_TargetScale = 0.92f`
  - `m_Duration = 1.2f` (한 방향)
  - `m_Ease = Ease.InOutSine`
  - `m_StartDelay = 0f`
- Glory 프로젝트 비의존 원칙 준수 — 프로젝트 고유 클래스 참조 없음, `DOTween` 직접 호출 없이 `TweenUtil`/`TweenSequenceBuilder`만 경유.
- 씬 연결: `Assets/Scenes/TitleScene.unity`의 `Image (1)`(GameObject fileID 1542094827)에 YAML 직접 편집으로 부착(MCP 미연결). 신규 컴포넌트 fileID `9001000000000000001`, 스크립트 GUID `9a0d540b317343eabed5722fcfb77b8a`(신규 발급, 프로젝트 전체 .meta와 중복 없음 확인). `m_TargetImage`만 명시적으로 `Image` 컴포넌트(fileID 1542094829)에 연결하고, 나머지 필드는 직렬화 생략 — C# 기본값 그대로 적용됨.
- **미검증** — Unity MCP 미연결로 컴파일/Play Mode 확인 불가. YAML 파싱 순서(m_Component 목록 먼저, 컴포넌트 블록 다음)는 지켰음.
