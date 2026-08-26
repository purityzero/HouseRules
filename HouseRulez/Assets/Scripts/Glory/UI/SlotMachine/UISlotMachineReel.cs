using System;
using System.Collections.Generic;
using UnityEngine;

public enum eReelState
{
    None,
    Idle,
    Spin,
    Stop,
    Result,
}

/// <summary>
/// 슬롯 릴 1열. 심볼 오브젝트를 직접 회전시키지 않고, 릴 트랜스폼을 아래로 내리다가
/// 한 칸(PosmaxDownY)만큼 넘게 내려가면 위치를 되돌리고 심볼 "내용"을 한 칸씩 위로 밀어 올리는
/// (ChangeSymbol) 순환 버퍼 방식으로 회전 연출을 낸다.
/// 결과 심볼은 SetResult()로 미리 주입받아 쓰고(직접 뽑지 않는다), 스핀 중 새로 굴러 들어오는
/// 심볼은 매번 OnRequestSymbol 콜백으로 물어본다 — Glory는 프로젝트의 심볼 풀/결과 테이블을 모른다.
/// </summary>
public class UISlotMachineReel : MonoBehaviour
{
    [SerializeField] private List<UISlotMachineSymbol> m_SymbolList;
    [SerializeField] private float m_MaxSpeed = 30f;
    [SerializeField] private float m_SpeedRatio = 0.1f;
    [SerializeField] private float m_SpeedReverseRatio = 0.1f;
    [SerializeField] private float m_BlurSpeed = 40f;
    [SerializeField] private float m_ResultTweenDuration = 0.15f;
    [SerializeField] private AudioClip m_TickClip;

    /// <summary>
    /// 스핀 중 버퍼 마지막 칸에 채울 다음 심볼 타입을 물어보는 콜백.
    /// 비어 있으면(연결 안 하면) 항상 0으로 채운다.
    /// </summary>
    public Func<int> OnRequestSymbol;

    private RectTransform m_RectTransform;
    private FsmClass<eReelState> m_Fsm = new FsmClass<eReelState>();
    private int[] m_ResultSymbolTypes = new int[0];
    private float m_Speed;
    private bool m_isReverse;
    private int m_ReelIndex;

    public FsmClass<eReelState> fsm => m_Fsm;
    public IReadOnlyList<UISlotMachineSymbol> symbolList => m_SymbolList;
    public int reelIndex => m_ReelIndex;
    public float resultTweenDuration => m_ResultTweenDuration;

    public float speed
    {
        get => m_Speed;
        set => m_Speed = value;
    }

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        m_Fsm.Update();
        m_Fsm.FixedUpdate();
    }

    /// <summary>
    /// FSM 상태를 등록하고 IDLE로 세팅한다. 씬/프리팹에 배치된 뒤 1회 호출한다.
    /// </summary>
    public void Init(int _reelIndex)
    {
        m_ReelIndex = _reelIndex;

        m_Fsm.Clear();
        m_Fsm.AddFsm(new UISlotMachineReelStateIdle(this));
        m_Fsm.AddFsm(new UISlotMachineReelStateSpin(this));
        m_Fsm.AddFsm(new UISlotMachineReelStateStop(this));
        m_Fsm.AddFsm(new UISlotMachineReelStateResult(this));
        m_Fsm.SetState(eReelState.Idle);

        ResetSymbol();
    }

    public void Open()
    {
        ResetPosition(0f);
        ResetSymbol();
        m_Fsm.SetState(eReelState.Idle);
    }

    public bool IsState(eReelState _reelState)
    {
        return m_Fsm.IsState(_reelState);
    }

    public void SetReverse(bool _isReverse)
    {
        m_isReverse = _isReverse;
    }

    /// <summary>
    /// 스핀을 걸기 전에 결과를 미리 주입한다 — 판정은 호출부(프로젝트)가 이미 끝낸 뒤라는 전제.
    /// 배열 길이가 곧 "보이는 칸 수"로 쓰인다.
    /// </summary>
    public void SetResult(int[] _resultSymbolTypes)
    {
        if (_resultSymbolTypes == null || _resultSymbolTypes.Length <= 0)
        {
            Logger.Error("[UISlotMachineReel] SetResult Failed! resultSymbolTypes == null or empty");
            return;
        }

        if (_resultSymbolTypes.Length > m_SymbolList.Count)
        {
            Logger.Error($"[UISlotMachineReel] SetResult Failed! resultSymbolTypes({_resultSymbolTypes.Length}) > symbolList({m_SymbolList.Count})");
            return;
        }

        m_ResultSymbolTypes = _resultSymbolTypes;
    }

    public void ResetPosition(float _positionY = 0f)
    {
        Vector3 localPosition = transform.localPosition;
        transform.localPosition = new Vector3(localPosition.x, -_positionY, localPosition.z);
    }

    public void ResetSymbol()
    {
        for (int index = 0; index < m_SymbolList.Count; ++index)
        {
            m_SymbolList[index].Open(0, false);
        }
    }

    public void AllBlur(bool _isBlur)
    {
        for (int index = 0; index < m_SymbolList.Count; ++index)
        {
            m_SymbolList[index].Blur(_isBlur);
        }
    }

    /// <summary>
    /// SetResult()로 받아둔 결과를 "보이는 칸"(버퍼 가운데, 결과 개수만큼)에 세팅한다. RESULT 상태에서 호출.
    /// </summary>
    public void ApplyResultToVisibleSymbols()
    {
        int visibleStartIndex = GetVisibleStartIndex();

        for (int index = 0; index < m_ResultSymbolTypes.Length; ++index)
        {
            m_SymbolList[visibleStartIndex + index].Open(m_ResultSymbolTypes[index], false);
        }
    }

    // 버퍼 칸 수와 보이는 칸 수(결과 개수)만으로 유도한다 — 특정 칸 수를 코드에 박지 않는다.
    // 위아래 오버스캔(스크롤 여유 칸)을 절반씩 나눈다고 가정하고 가운데를 보이는 창으로 쓴다.
    private int GetVisibleStartIndex()
    {
        int visibleCount = (m_ResultSymbolTypes.Length > 0) ? m_ResultSymbolTypes.Length : m_SymbolList.Count;
        return (m_SymbolList.Count - visibleCount) / 2;
    }

    // 한 칸의 크기 = 릴 전체 높이를 버퍼 칸 수만큼 나눈 값.
    public virtual float PosmaxDownY()
    {
        return -(m_RectTransform.rect.height / m_SymbolList.Count);
    }

    // 결과가 "보이는 칸"에 정확히 맞춰 정착하는 위치.
    // 버퍼 칸 수에서 보이는 칸 시작 인덱스를 뺀 만큼(정착까지 더 내려가야 하는 칸 수) 내려간 지점이다.
    public virtual float AnswerPosY()
    {
        int settleStepCount = m_SymbolList.Count - GetVisibleStartIndex();
        return PosmaxDownY() * settleStepCount;
    }

    public float GetSpeed(float _stateTime)
    {
        if (m_isReverse == false)
        {
            if (m_Speed > m_MaxSpeed)
            {
                m_Speed -= m_MaxSpeed * (_stateTime * m_SpeedRatio);

                if (m_Speed < m_MaxSpeed)
                    m_Speed = m_MaxSpeed;
            }
            else
            {
                m_Speed += m_MaxSpeed * (_stateTime * m_SpeedRatio);

                if (m_Speed >= m_MaxSpeed)
                    m_Speed = m_MaxSpeed;
            }
        }
        else
        {
            m_Speed -= m_MaxSpeed * (_stateTime * m_SpeedReverseRatio);

            if (m_Speed <= m_MaxSpeed)
                m_Speed = m_MaxSpeed;
        }

        return m_Speed;
    }

    public void Move(float _stateTime)
    {
        if (PosmaxDownY() > transform.localPosition.y)
        {
            float remain = -transform.localPosition.y + PosmaxDownY();
            ResetPosition(remain + GetSpeed(_stateTime));
            ChangeSymbol(m_Speed > m_BlurSpeed);
            return;
        }

        ResetPosition(-transform.localPosition.y + GetSpeed(_stateTime));
    }

    private void ChangeSymbol(bool _isBlur)
    {
        PlayTickSound();

        int lastIndex = m_SymbolList.Count - 1;
        for (int index = 0; index < m_SymbolList.Count; ++index)
        {
            if (index >= lastIndex)
            {
                int nextSymbolType = (OnRequestSymbol != null) ? OnRequestSymbol() : 0;
                m_SymbolList[index].Open(nextSymbolType, _isBlur);
                continue;
            }

            m_SymbolList[index].Open(m_SymbolList[index + 1].symbolType, _isBlur);
        }
    }

    private void PlayTickSound()
    {
        if (m_TickClip == null)
            return;

        SoundManager.instance.PlaySfx(m_TickClip);
    }
}
