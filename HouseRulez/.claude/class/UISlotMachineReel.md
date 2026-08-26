# UISlotMachineReel

연관: [[UISlotMachineSymbol]], [[UISlotMachineReelState]], [[FsmClass]], [[FsmState]], [[SoundManager]], [[TweenUtil]], FlowCommand/ICommand(Partterns/Command)

Glory 공용 라이브러리(`Assets/Scripts/Glory/UI/SlotMachine/`, `Assets/Scripts/Glory/Partterns/Fsm/`)의 슬롯머신 릴 구동부 + FSM. NewSlot(구 프로젝트) `D:\BackUp\NewSlot\Assets\2_Script\` 아래 릴 코드를 HouseRulez용으로 이식했다.

## 2026-08-26-0 — NewSlot 릴 구동부 + FSM 이식 (신규)

### 개요
NewSlot의 슬롯머신 릴(`UISlotMachineLine` + `BattleSlotReel` + `BattleSlotMachineReelState_*`)과 FSM 프레임워크(`FsmClass`/`FsmState`/`FsmMsg`)를 Glory로 이식했다. HouseRulez는 3×3 슬롯이고, 실제 릴을 쓰는 화면/프리팹은 아직 없다 — 이번 작업은 Glory 쪽 코드 5개 파일(+enum 1개는 Reel 파일에 동봉)까지이고, 프로젝트 파생 클래스/씬/프리팹은 만들지 않았다.

### 만든 파일
- `Assets/Scripts/Glory/Partterns/Fsm/FsmState.cs` — `FsmMsg` + `FsmState<T>`
- `Assets/Scripts/Glory/Partterns/Fsm/FsmClass.cs` — `FsmClass<T>`
- `Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReel.cs` — `eReelState` + `UISlotMachineReel`
- `Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineSymbol.cs` — `UISlotMachineSymbol`
- `Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReelState.cs` — `UISlotMachineReelState`(베이스) + Idle/Spin/Stop/Result 4상태

### 릴이 도는 원리 (원본과 동일한 컨셉)
심볼 오브젝트를 회전시키지 않는다. **릴 트랜스폼(`transform.localPosition.y`)을 계속 아래로 내리다가, 한 칸(`PosmaxDownY()`) 넘게 내려가면 위치를 되돌리고 심볼 "내용"을 한 칸씩 위로 밀어 올린다**(`ChangeSymbol`) — 고정된 칸 배열(`m_SymbolList`)을 순환 버퍼로 쓴다.
- `GetSpeed(stateTime)`가 `MaxSpeed`를 향해 가감속(`SpeedRatio`/`SpeedReverseRatio`).
- 속도가 `BlurSpeed`를 넘으면 심볼을 블러 상태로 그린다(`Open(type, isBlur)`로 전달만 하고, 실제 스프라이트 교체는 `UISlotMachineSymbol.SetBlur()` 오버라이드가 담당).
- STOP 상태: 계속 내려가다 한 칸 경계(`PosmaxDownY`)를 지나면 `m_isStop=true`로 전환 → 이후 `ChangeSymbol` 없이 계속 미끄러지듯 내려가다 `AnswerPosY()`까지 도달하면 RESULT로 전이.
- RESULT 상태: `FlowCommand`로 짧은 정착 트윈(0.15초 기본값, `Ease.OutCubic`) → 결과 심볼을 보이는 칸에 세팅(`ApplyResultToVisibleSymbols`) → 위치 리셋(`ResetPosition(0)`) → IDLE로 전이.

### 칸 수 일반화 (하드코딩 제거)
원본은 `AnswerPosY()`가 `-(rect.height / symbolList.Count) * 5`처럼 리터럴 `5`를 썼다(그 게임의 9칸짜리 프리팹 — 6칸 스크롤 버퍼 + 3칸 숨은 결과 슬롯 — 에 맞춰 튜닝된 값). 이식본은 숨은 결과 슬롯 자체를 없앴으므로(아래 "바꾼 것" 3번) `m_SymbolList`가 순수 스크롤 버퍼만 담는다. 그래서:
- `PosmaxDownY()` = `-(rectHeight / m_SymbolList.Count)` — 버퍼 칸 수로 나눈 한 칸 크기.
- `보이는 칸 시작 인덱스` = `(버퍼 칸 수 - 결과 개수) / 2` — 위아래 오버스캔을 절반씩 나눈다고 가정하고 가운데를 보이는 창으로 삼는다. (버퍼 6칸 + 결과 3칸일 때 값은 1로, 원본이 실제로 쓰던 시작 인덱스 1과 일치 — 원본의 "숨은 슬롯 포함 9칸수식"을 걷어내고 나면 이 형태로 정확히 환원된다.)
- `AnswerPosY()` = `PosmaxDownY() * (버퍼 칸 수 - 보이는 칸 시작 인덱스)`.
- **판단이 갈리는 지점**: "보이는 칸이 버퍼 가운데 정렬"이라는 가정은 원본 프리팹(9칸, 위1/아래2 오버스캔)의 비대칭 배치를 정확히 그대로 복제하진 않는다(센터 정렬로 근사). 실제 릴 프리팹이 만들어지면 이 가정이 맞는지 눈으로 확인 필요 — 필요하면 `PosmaxDownY()`/`AnswerPosY()`가 `virtual`이므로 프로젝트 파생 클래스에서 오버라이드 가능하게 열어뒀다.

### 원본에서 가져온 것 / 뺀 것 / 바꾼 것

**가져온 것 (거의 그대로)**
- FSM 프레임워크 구조(`FsmClass<T>`/`FsmState<T>`/`FsmMsg`) — 등록(`AddFsm`)/전환(`SetState`)/상태변경 중 재진입 가드(`m_isStateChanging`) 로직 그대로.
- 릴의 가감속(`GetSpeed`) 공식, 한 칸 초과 시 위치 되돌림 + `ChangeSymbol` 호출(`Move`) 로직 그대로.
- STOP 상태의 "한 칸 넘으면 감속 없이 계속 미끄러지다 AnswerPosY에서 정지" 2단계 구조 그대로.
- `SetReverse`/역방향 가감속(`SpeedReverseRatio`) — Battle FSM에서는 안 쓰지만 원본 `UISlotMachineLine.cs`의 핵심 로직 일부라 포함.

**뺀 것**
- `eLINE_STATE.DELAY` 상태 — Battle FSM 4상태(Idle/Spin/Stop/Result)에 없어서 스펙대로 제외.
- `UISlotMachineSymbol.isPeek`/`ResetPeek()` — 제공된 4개 핵심 파일(Line/Symbol/LineState/BattleSlotMachineReelState) 어디서도 실제로 읽지 않는 죽은 경로라 이식본에서 생략. (Lobby 전용 프리뷰 기능으로 추정, 이번 이식 범위 밖)
- `UISlotMachineLine.stoptime`/`StopSpeed` 필드 — 원본에서도 어디서도 대입/참조되지 않는 죽은 필드라 생략.
- STOP 상태의 `soundPlayPosCheck` 별도 사운드 트리거 — 원래 `ChangeSymbol` 호출 시 사운드와 별개로 거리 누적 기반 틱 사운드를 하나 더 냈는데, `isStop==false` 구간에서는 두 트리거가 겹쳐 같은 프레임 대역에 사운드가 중복 재생될 수 있는 구조였다. 이식본은 트리거 지점을 `ChangeSymbol` 하나로 단순화했다(스펙 8개 항목 밖의 판단 — **사용자 확인 권장 지점**).
- `UISlotMachineLobbyDlg` 연동 전부(`getMachineDlg`, Lobby 전용 심볼 뽑기 분기) — 이식 대상 아님.

**바꾼 것 (스펙 8개 지시사항)**
1. 결과 주입 방식: `BattleSlotMachineReelState_stop.FakeChange()`(결과 테이블에서 직접 뽑음) 제거 → `UISlotMachineReel.SetResult(int[])`로 스핀 전에 미리 받아둔 결과를 씀. STOP 상태에서 더 이상 아무것도 뽑지 않는다.
2. 심볼 공급: `getMachineDlg`(로비 다이얼로그) 참조 제거 → `public Func<int> OnRequestSymbol` 콜백으로 대체. 비어 있으면 0을 채운다.
3. 숨은 결과 슬롯 제거: 원본은 `symbolList` 뒤쪽(인덱스 6~8)에 결과를 미리 담아두는 비가시 심볼 오브젝트를 뒀다(`getBattleSymbol[i+5]`). 이식본은 결과를 `int[] m_ResultSymbolTypes` 데이터로만 들고 있다가 RESULT 상태에서 `ApplyResultToVisibleSymbols()`로 보이는 칸에 세팅한다 — `m_SymbolList`는 스크롤 버퍼 전용.
4. 전역 static 튜닝값 → 인스턴스 `[SerializeField]` 필드: `MaxSpeed`/`SpeedRatio`/`SpeedReverseRatio`/`BlurSpeed`뿐 아니라 원본에 리터럴로 박혀 있던 RESULT 정착 트윈 시간(0.15초)도 같은 원칙으로 `m_ResultTweenDuration` 인스펙터 필드로 뺐다(GameConfigTable이 없는 프로젝트라 CSV 대신 인스펙터).
5. 사운드 결합 제거: `SoundManager.Instance.PlayUseSpin()` 직접 호출 제거 → `[SerializeField] private AudioClip m_TickClip;` + `SoundManager.instance.PlaySfx(m_TickClip)`(클립 null이면 건너뜀).
6. 블러 방식: 런타임 셰이더 대신 스프라이트 교체. `UISlotMachineSymbol.Open()`이 항상 `SetBlur(bool)`(protected virtual, 기본은 아무 것도 안 함)을 호출하도록 정리 — 원본은 베이스 `Open()`이 블러를 아예 처리 안 하고 `UIBattleSymbol.Open()` 오버라이드만 `Blur()`를 따로 호출하는 일관성 없는 구조였는데, 스펙이 요구한 "블러 여부만 받고 스프라이트 결정은 가상 메서드로 연다"는 설계를 베이스 클래스 수준에서 항상 성립하도록 고쳤다. 프로젝트는 `SetBlur(bool)`만 오버라이드해서 `{이름}_blur` 스프라이트로 교체하면 된다.
7. `UIBase` 상속 제거 → `MonoBehaviour` 직접 상속(Reel/Symbol 둘 다).
8. 커맨드 시퀀스 교체: `QueueFlowCommand`/`Command_TweenLocalPosition`/`Command_UnityAction`/`Command_TUnityAction<float>` → Glory의 `FlowCommand` + `Command_Tween`/`Command_Delegate`. RESULT 상태의 `FixedUpdate()`가 매 프레임 `m_FlowCommand.Update()`를 직접 호출한다(안 부르면 멈춘다는 점 인지하고 구현). 트윈은 `TweenUtil.Move(Transform, Vector3, float)` 사용 — 원본은 `transform.localPosition`을 직접 트윈했는데 `TweenUtil.Move`는 월드 좌표(`DOMove`) 기반이라, 목표 로컬 Y를 부모 기준 월드 좌표로 변환(`parent.TransformPoint`)해서 넘기도록 보정했다(부모 계층이 있어도 정확히 같은 로컬 위치로 향하게 하기 위함).

또한 CODE.MD 네이밍(비공개 필드 `m_`+파스칼, 매개변수 `_`+카멜, 프로퍼티 카멜, enum `e`+파스칼 멤버 파스칼, private bool `m_is`+카멜, bool 비교 `==true`/`==false` 명시, for/foreach 항상 `{}`, 축약어 금지 등)에 맞춰 전면 리네이밍했다(`getStateType`→`stateType`, `getFsm`→`fsm`, `SPEED_RATIO`(static)→`m_SpeedRatio`(인스턴스 필드) 등).

### Public API

**`FsmMsg`** (`Assets/Scripts/Glory/Partterns/Fsm/FsmState.cs:6`)
- `public FsmMsg(int _msgType)`
- `public int msgType { get; }`

**`FsmState<T>`** (`Assets/Scripts/Glory/Partterns/Fsm/FsmState.cs:20`)
- `public FsmState(T _stateType)`
- `public T stateType { get; }`
- `public virtual void Enter(FsmMsg _msg)` / `Update()` / `LateUpdate()` / `FixedUpdate()` / `End()` / `SetMsg(FsmMsg _msg)`

**`FsmClass<T>`** (`Assets/Scripts/Glory/Partterns/Fsm/FsmClass.cs:9`)
- `public FsmState<T> state { get; }`
- `public bool IsState(T _stateType)`
- `public virtual void Init()` / `Clear()`
- `public virtual void AddFsm(FsmState<T> _state)`
- `public virtual void SetState(T _stateType, FsmMsg _msg = null)`
- `public virtual void SetMsg(FsmMsg _msg)`
- `public virtual void Update()` / `LateUpdate()` / `FixedUpdate()`

**`UISlotMachineSymbol`** (`Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineSymbol.cs:10`)
- `public Image iconImage { get; }`
- `public int symbolType { get; }`
- `public virtual void Open(int _type, bool _isBlur)`
- `public virtual void Open(Enum _type, bool _isBlur)`
- `public void Blur(bool _isBlur)`
- `public void Show(bool _isActive)`
- `protected virtual void SetBlur(bool _isBlur)` — 프로젝트가 오버라이드하는 확장 지점.

**`eReelState`** (`Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReel.cs:5`) — `None`/`Idle`/`Spin`/`Stop`/`Result`

**`UISlotMachineReel`** (`Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReel.cs:21`)
- `public Func<int> OnRequestSymbol` — 다음 심볼 타입 콜백(필수 연결).
- `public FsmClass<eReelState> fsm { get; }`
- `public IReadOnlyList<UISlotMachineSymbol> symbolList { get; }`
- `public int reelIndex { get; }`
- `public float resultTweenDuration { get; }`
- `public float speed { get; set; }`
- `public void Init(int _reelIndex)` — FSM 등록 + IDLE 세팅.
- `public void Open()` — 위치/심볼 리셋 후 IDLE.
- `public bool IsState(eReelState _reelState)`
- `public void SetReverse(bool _isReverse)`
- `public void SetResult(int[] _resultSymbolTypes)` — 스핀 전 필수 호출.
- `public void ResetPosition(float _positionY = 0f)`
- `public void ResetSymbol()`
- `public void AllBlur(bool _isBlur)`
- `public void ApplyResultToVisibleSymbols()`
- `public virtual float PosmaxDownY()` / `public virtual float AnswerPosY()`
- `public float GetSpeed(float _stateTime)`
- `public void Move(float _stateTime)`

**`UISlotMachineReelState`**(베이스, abstract) / **`UISlotMachineReelStateIdle`** / **`UISlotMachineReelStateSpin`** / **`UISlotMachineReelStateStop`** / **`UISlotMachineReelStateResult`** (`Assets/Scripts/Glory/UI/SlotMachine/UISlotMachineReelState.cs`)
- 생성자 `(UISlotMachineReel _reel)` 공통. `UISlotMachineReel.Init()`이 4개 상태를 전부 생성해 등록하므로 프로젝트가 직접 `new`할 일은 없다.

### 사용 흐름 (프로젝트 쪽에서 연결해야 할 것 — 아직 미구현)
1. 릴 프리팹: `UISlotMachineReel` + 자식 `UISlotMachineSymbol` N개를 인스펙터에서 `m_SymbolList`에 연결.
2. 프로젝트 전용 심볼 클래스가 `UISlotMachineSymbol`을 상속해 `SetBlur(bool)`을 오버라이드 → `"{심볼이름}_blur"` 스프라이트로 교체(다른 에이전트가 `_blur` PNG를 이미 생성 중 — `Assets/Resources/Image/InGame/Actor/**/*_blur.png` 확인됨).
3. `reel.Init(reelIndex)` 1회 호출 → `reel.OnRequestSymbol = () => ...;` 로 심볼 공급 콜백 연결.
4. 판정기가 결과를 확정하면 `reel.SetResult(new[] { a, b, c })` 호출 → `reel.fsm.SetState(eReelState.Spin)`으로 스핀 시작 → 적절한 시점에 `reel.fsm.SetState(eReelState.Stop)`으로 정지 트리거 → 이후는 FSM이 알아서 Result→Idle까지 진행.

### 검증 상태 — 미검증
이번 세션은 Unity MCP가 연결되지 않아 컴파일/Play Mode 확인을 하지 못했다. 코드 리딩 기반으로만 작성했으며, 실제 릴 프리팹이 아직 없어 `PosmaxDownY`/`AnswerPosY`의 센터 정렬 가정과 트윈 좌표 변환(`parent.TransformPoint`)이 실제 화면에서 의도대로 보이는지 눈으로 확인되지 않았다. 프리팹을 붙이는 다음 작업에서 반드시 컴파일 + 실제 스핀 연출을 확인할 것.
