using System;

/// <summary>
/// FSM 상태 전환/갱신 시 전달하는 메시지. msgType 값의 의미는 프로젝트가 정의해서 사용한다.
/// </summary>
public class FsmMsg
{
    public int msgType { get; private set; }

    public FsmMsg(int _msgType)
    {
        msgType = _msgType;
    }
}

/// <summary>
/// FsmClass{T}에 등록되는 상태 1개. Enter/Update/LateUpdate/FixedUpdate/End를 오버라이드해서 상태별 로직을 채운다.
/// </summary>
[Serializable]
public class FsmState<T> where T : Enum
{
    public T stateType { get; private set; }

    public FsmState(T _stateType)
    {
        stateType = _stateType;
    }

    public virtual void Enter(FsmMsg _msg) { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void FixedUpdate() { }
    public virtual void End() { }
    public virtual void SetMsg(FsmMsg _msg) { }
}
