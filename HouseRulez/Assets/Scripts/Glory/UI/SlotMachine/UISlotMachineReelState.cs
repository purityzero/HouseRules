using DG.Tweening;
using UnityEngine;

/// <summary>
/// 릴 FSM 상태 공통 베이스. 상태 경과 시간(m_StateTime) 누적을 담당한다.
/// </summary>
public abstract class UISlotMachineReelState : FsmState<eReelState>
{
    protected UISlotMachineReel m_Reel;
    protected float m_StateTime;

    protected UISlotMachineReelState(UISlotMachineReel _reel, eReelState _state) : base(_state)
    {
        m_Reel = _reel;
    }

    public override void Enter(FsmMsg _msg)
    {
        m_StateTime = 0f;
    }

    public override void FixedUpdate()
    {
        m_StateTime += Time.deltaTime;
    }

    public override void End()
    {
        m_StateTime = 0f;
    }
}

public class UISlotMachineReelStateIdle : UISlotMachineReelState
{
    public UISlotMachineReelStateIdle(UISlotMachineReel _reel) : base(_reel, eReelState.Idle)
    {
    }
}

public class UISlotMachineReelStateSpin : UISlotMachineReelState
{
    public UISlotMachineReelStateSpin(UISlotMachineReel _reel) : base(_reel, eReelState.Spin)
    {
    }

    public override void Enter(FsmMsg _msg)
    {
        base.Enter(_msg);
        m_Reel.speed = 0f;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        m_Reel.Move(m_StateTime);
    }
}

public class UISlotMachineReelStateStop : UISlotMachineReelState
{
    private bool m_isStop;

    public UISlotMachineReelStateStop(UISlotMachineReel _reel) : base(_reel, eReelState.Stop)
    {
    }

    public override void Enter(FsmMsg _msg)
    {
        base.Enter(_msg);
        m_isStop = false;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (m_isStop == true)
        {
            m_Reel.ResetPosition(-m_Reel.transform.localPosition.y + m_Reel.GetSpeed(m_StateTime));

            if (m_Reel.AnswerPosY() >= m_Reel.transform.localPosition.y)
                m_Reel.fsm.SetState(eReelState.Result);

            return;
        }

        m_Reel.Move(m_StateTime);

        if (m_Reel.PosmaxDownY() >= m_Reel.transform.localPosition.y)
            m_isStop = true;
    }
}

public class UISlotMachineReelStateResult : UISlotMachineReelState
{
    private FlowCommand m_FlowCommand;

    public UISlotMachineReelStateResult(UISlotMachineReel _reel) : base(_reel, eReelState.Result)
    {
    }

    public override void Enter(FsmMsg _msg)
    {
        base.Enter(_msg);

        Vector3 localPosition = m_Reel.transform.localPosition;
        Vector3 targetLocalPosition = new Vector3(localPosition.x, m_Reel.AnswerPosY(), localPosition.z);
        Vector3 targetWorldPosition = (m_Reel.transform.parent != null) ? m_Reel.transform.parent.TransformPoint(targetLocalPosition) : targetLocalPosition;

        Tween settleTween = TweenUtil.Move(m_Reel.transform, targetWorldPosition, m_Reel.resultTweenDuration).SetEase(Ease.OutCubic);

        m_FlowCommand = new FlowCommand();
        m_FlowCommand.Add(new Command_Tween(settleTween));
        m_FlowCommand.Add(new Command_Delegate(m_Reel.ApplyResultToVisibleSymbols));
        m_FlowCommand.Add(new Command_Delegate(() => m_Reel.ResetPosition(0f)));
        m_FlowCommand.Add(new Command_Delegate(GoToIdle));
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        m_FlowCommand.Update();
    }

    private void GoToIdle()
    {
        m_Reel.fsm.SetState(eReelState.Idle);
    }
}
