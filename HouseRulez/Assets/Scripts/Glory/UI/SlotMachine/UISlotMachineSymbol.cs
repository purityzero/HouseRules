using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 릴 한 칸. symbolType(정수)만 들고 있고, 실제로 어떤 스프라이트를 보여줄지는 모른다 —
/// 블러 여부에 따른 스프라이트 결정은 SetBlur()를 오버라이드해서 프로젝트가 채운다
/// (예: "{심볼이름}_blur" 스프라이트로 교체).
/// </summary>
public class UISlotMachineSymbol : MonoBehaviour
{
    [SerializeField] private Image m_IconImage;

    public Image iconImage => m_IconImage;
    public int symbolType { get; private set; }

    public virtual void Open(int _type, bool _isBlur)
    {
        symbolType = _type;
        SetBlur(_isBlur);
    }

    public virtual void Open(Enum _type, bool _isBlur)
    {
        Open(Convert.ToInt32(_type), _isBlur);
    }

    public void Blur(bool _isBlur)
    {
        SetBlur(_isBlur);
    }

    public void Show(bool _isActive)
    {
        if (m_IconImage == null)
            return;

        m_IconImage.gameObject.SetActive(_isActive);
    }

    /// <summary>
    /// 블러 여부에 따라 표시할 스프라이트를 결정하는 지점. 기본 구현은 아무 것도 하지 않는다 —
    /// Glory는 심볼 종류별 스프라이트 경로 규칙을 모르므로 프로젝트가 오버라이드해서 icon.sprite를 교체한다.
    /// </summary>
    protected virtual void SetBlur(bool _isBlur)
    {
    }
}
