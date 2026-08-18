using UnityEngine;
using UnityEngine.EventSystems;

public class UIAssetBox : UIBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI m_AmountText;
    [SerializeField] private eCurrencyType m_CurrencyType = eCurrencyType.Max;

    private ObservableVariable<int> m_Observable;

    protected override void OnEnable()
    {
        base.OnEnable();
        RegisterCurrencyObserver();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnregisterCurrencyObserver();
    }

    // 재화 값이 바뀔 때마다 자동으로 갱신하고 싶으면, 호출부(프로젝트)가 재화 저장소(PlayerManager 등)에서
    // 직접 꺼낸 ObservableVariable<int>를 넘긴다 — Glory는 재화가 어느 클래스/저장소에서 오는지 알지 못한다
    // (프로젝트 비의존 원칙, .claude/rules/glory.md 참고). 등록 즉시 현재 값으로 1회 콜백이 와서 텍스트가 채워진다.
    public void SetData(eCurrencyType _currencyType, ObservableVariable<int> _observable)
    {
        UnregisterCurrencyObserver();

        m_CurrencyType = _currencyType;
        m_Observable = _observable;

        if (isActiveAndEnabled == true)
            RegisterCurrencyObserver();
    }

    // 자동 갱신이 필요 없는 1회성 표시(옵저버 미등록)
    public void SetData(eCurrencyType _currencyType, long _amount)
    {
        UnregisterCurrencyObserver();

        m_CurrencyType = _currencyType;
        m_Observable = null;

        m_AmountText.SetText(_amount.ToString());
    }

    public void SetData(long _amount)
    {
        if (m_CurrencyType == eCurrencyType.Max)
        {
            Logger.Error($"[UIAssetBox] SetData Failed! currencyType is Max! - {gameObject.name}");
            return;
        }

        m_AmountText.SetText(_amount.ToString());
    }

    public void Refresh()
    {
        if (m_CurrencyType == eCurrencyType.Max)
        {
            gameObject.SetActive(false);
            return;
        }

        if (m_Observable == null)
        {
            Logger.Error($"[UIAssetBox] Refresh Failed! observable not set - {gameObject.name}. SetData(currencyType, observable)로 먼저 연결할 것.");
            return;
        }

        m_AmountText.SetText(m_Observable.Value.ToString());
        gameObject.SetActive(true);
    }

    private void RegisterCurrencyObserver()
    {
        if (m_Observable == null)
            return;

        m_Observable.RegisterObserver(OnCurrencyChanged);
    }

    private void UnregisterCurrencyObserver()
    {
        if (m_Observable == null)
            return;

        m_Observable.UnregisterObserver(OnCurrencyChanged);
    }

    private void OnCurrencyChanged(int _oldAmount, int _newAmount)
    {
        m_AmountText.SetText(_newAmount.ToString());
    }
}
