using System;
using System.Collections.Generic;

/// <summary>
/// 유한상태기계. FsmState{T}를 AddFsm()으로 등록해두고 SetState()로 전환한다.
/// 소유자가 매 프레임 Update()/LateUpdate()/FixedUpdate()를 직접 호출해야 동작한다.
/// </summary>
[Serializable]
public class FsmClass<T> where T : Enum
{
    // 인스펙터에서 현재 상태를 눈으로 확인하기 위한 디버그 표시용 — 로직에서 읽지 않는다.
    public string CurrentStateName;

    private Dictionary<T, FsmState<T>> m_HashState = new Dictionary<T, FsmState<T>>();
    private FsmState<T> m_State;
    private bool m_isStateChanging; // FsmState::Enter()/End() 안에서 SetState()가 재귀 호출되는 것을 막기 위한 가드.

    public FsmState<T> state => m_State;

    public bool IsState(T _stateType)
    {
        if (m_State == null)
            return false;

        return m_State.stateType.Equals(_stateType);
    }

    public virtual void Init()
    {
    }

    public virtual void Clear()
    {
        m_HashState.Clear();
        m_State = null;
    }

    public virtual void AddFsm(FsmState<T> _state)
    {
        if (_state == null)
        {
            Logger.Error("[FsmClass] AddFsm Failed! state == null");
            return;
        }

        if (m_HashState.ContainsKey(_state.stateType) == true)
        {
            Logger.Error($"[FsmClass] AddFsm Failed! already have state : {_state.stateType}");
            return;
        }

        m_HashState.Add(_state.stateType, _state);
    }

    public virtual void SetState(T _stateType, FsmMsg _msg = null)
    {
        if (m_HashState.ContainsKey(_stateType) == false)
        {
            Logger.Error($"[FsmClass] SetState Failed! no state : {_stateType}");
            return;
        }

        if (m_isStateChanging == true)
        {
            Logger.Error($"[FsmClass] SetState Failed! state changing : {_stateType}");
            return;
        }

        FsmState<T> nextState = m_HashState[_stateType];

        if (nextState == m_State)
            Logger.Log($"[FsmClass] SetState same state : {_stateType}");

        m_isStateChanging = true;

        if (m_State != null)
            m_State.End();

        m_State = nextState;
        m_State.Enter(_msg);

        m_isStateChanging = false;
    }

    public virtual void SetMsg(FsmMsg _msg)
    {
        if (m_State == null)
            return;

        m_State.SetMsg(_msg);
    }

    public virtual void Update()
    {
        if (m_State == null)
            return;

        m_State.Update();
        CurrentStateName = m_State.stateType.ToString();
    }

    public virtual void LateUpdate()
    {
        if (m_State == null)
            return;

        m_State.LateUpdate();
    }

    public virtual void FixedUpdate()
    {
        if (m_State == null)
            return;

        m_State.FixedUpdate();
    }
}
