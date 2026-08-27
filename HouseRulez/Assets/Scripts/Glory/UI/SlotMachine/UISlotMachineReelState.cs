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

        // 가속해서 문턱을 넘는 순간 블러가 걸린다.
        m_Reel.UpdateBlurBySpeed(m_Reel.speed);
    }
}

public class UISlotMachineReelStateStop : UISlotMachineReelState
{
    private bool m_isStop;
    private float m_DecelStartRemain;

    public UISlotMachineReelStateStop(UISlotMachineReel _reel) : base(_reel, eReelState.Stop)
    {
    }

    public override void Enter(FsmMsg _msg)
    {
        base.Enter(_msg);
        m_isStop = false;
        m_DecelStartRemain = 0f;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (m_isStop == true)
        {
            // 정착 지점까지 남은 거리의 비율로 감속한다 — 1이면 이제 막 감속을 시작한 참, 0이면 정착.
            float remainDistance = m_Reel.transform.localPosition.y - m_Reel.AnswerPosY();
            float remainRatio = (m_DecelStartRemain > 0f) ? (remainDistance / m_DecelStartRemain) : 0f;

            // 속도를 갱신하는 메서드라 이 프레임에선 한 번만 받아 쓴다.
            float currentSpeed = m_Reel.GetStopSpeed(remainRatio);
            m_Reel.ResetPosition(-m_Reel.transform.localPosition.y + currentSpeed);

            // 감속하다 문턱 아래로 떨어지는 순간 블러가 풀린다 — 멎는 게 눈에 보이기 시작하는 지점이다.
            m_Reel.UpdateBlurBySpeed(currentSpeed);

            // 정착 위치를 살짝 지나쳐 멈춘다 — RESULT의 트윈이 그만큼 위로 되올라오며 바운스가 된다.
            float overshootPositionY = m_Reel.AnswerPosY() - currentSpeed * 2f;
            if (overshootPositionY > m_Reel.transform.localPosition.y)
            {
                m_Reel.ResetPosition(-overshootPositionY);

                // 멈춘 뒤에는 반드시 선명해야 한다 — 감속이 문턱 위에서 끝나는 설정값이어도 여기서 확실히 푼다.
                m_Reel.SetBlurState(false);
                m_Reel.fsm.SetState(eReelState.Result);
            }

            return;
        }

        m_Reel.Move(m_StateTime);
        m_Reel.UpdateBlurBySpeed(m_Reel.speed);

        if (m_Reel.PosmaxDownY() >= m_Reel.transform.localPosition.y)
        {
            m_isStop = true;

            // 여기서부터 정착까지가 감속 구간이다. 그 시작 거리를 기준으로 잡아야 남은 비율을 낼 수 있다.
            m_DecelStartRemain = m_Reel.transform.localPosition.y - m_Reel.AnswerPosY();

            // 순환(ChangeSymbol)이 멈추는 이 시점에 결과를 심어둔다. 결과가 들어가는 칸은 아직 창 위쪽
            // 화면 밖이라, 릴이 정착 위치까지 내려오는 동안 결과가 자연스럽게 스크롤되어 등장한다.
            // 정착이 끝난 뒤에 채우면 이미 보이는 칸의 그림이 눈앞에서 갈아끼워져 티가 난다.
            m_Reel.ApplyResultToVisibleSymbols();
        }
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

        // 정착 위치(AnswerPosY)에 그대로 두고 끝낸다 — 여기서 ResetPosition(0)으로 릴을 되돌리면
        // "마지막 칸까지 미끄러져 화면이 빈 뒤 결과가 순간이동으로 나타나는" 끊김이 생긴다.
        // 결과가 들어가는 칸(ApplyResultToVisibleSymbols)이 정착 위치에서 창에 보이도록
        // 심볼을 배치하는 건 릴 구현 쪽 책임이다(UIHouseSlotReel.BuildSymbols 참고).
        m_FlowCommand = new FlowCommand();
        m_FlowCommand.Add(new Command_Tween(settleTween));
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
