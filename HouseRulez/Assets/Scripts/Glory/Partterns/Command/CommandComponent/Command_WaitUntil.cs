using System;

// 조건이 성립할 때까지 흐름을 멈춰두는 커맨드.
//
// Command_DeltaTime은 "몇 초"를 기다리지만, 걸리는 시간이 프레임레이트나 상태에 따라
// 변하는 동작은 시간으로 못 맞춘다. 그런 건 "정말 끝났는지"를 물어야 한다.
//
// 예: 슬롯 릴 감속은 프레임당 이동량 기반이라 정지 소요가 52fps에서 1.0초,
//     23fps에서 1.7초로 달라진다. 고정 대기로는 원리상 맞출 수 없다.
public class Command_WaitUntil : ICommand
{
    private Func<bool> m_Condition;
    private UnityEngine.Events.UnityAction m_Delegate;
    private bool m_isComplete;

    // _delegate는 조건이 성립한 그 프레임에 한 번 불린다. 필요 없으면 null.
    public Command_WaitUntil(Func<bool> _condition, UnityEngine.Events.UnityAction _delegate = null)
    {
        m_Condition = _condition;
        m_Delegate = _delegate;
    }

    public void Execute()
    {
        m_isComplete = false;

        // 조건 자체가 없으면 영원히 안 끝나 FlowCommand 전체가 멈춘다. 그건 버그로 알린다.
        if (m_Condition == null)
        {
            Logger.Error("[Command_WaitUntil] Execute Failed! 조건이 null이다 (기대: 판정 함수 전달)");
            m_isComplete = true;
        }
    }

    public void Update()
    {
        if (m_isComplete == true)
            return;

        if (m_Condition() == false)
            return;

        m_isComplete = true;

        if (m_Delegate != null)
            m_Delegate();
    }

    // 취소되면 조건과 무관하게 끝난 것으로 둔다 — 큐가 다음으로 넘어갈 수 있어야 한다.
    public void Cancel()
    {
        m_isComplete = true;
    }

    public bool IsFinished()
    {
        return m_isComplete;
    }
}
