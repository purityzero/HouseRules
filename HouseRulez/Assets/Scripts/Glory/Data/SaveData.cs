using System;
using UnityEngine;

// 저장 단위 하나의 베이스.
// 필드는 전부 [SerializeField] private 으로 두고 파생 클래스가 Set 메서드로만 고치게 만든다 —
// 밖에서 필드를 직접 대입할 수 있으면 SetChanged() 호출을 빠뜨리게 되고, 그러면 에러 없이 조용히 저장만 안 된다.
[Serializable]
public abstract class SaveData
{
    // 저장 포맷이 바뀌었을 때 마이그레이션 분기의 기준. 파생 클래스가 스키마를 바꾸면 올린다.
    [SerializeField] private int m_Version = 1;

    // JsonUtility는 [SerializeField] 없는 private 필드를 직렬화하지 않는다 — 더티 플래그는 저장 대상이 아니므로 이대로 둔다.
    private bool m_isNeedSave;

    public int version => m_Version;
    public bool isNeedSave => m_isNeedSave;

    // 값이 바뀔 때마다 불린다. 이 데이터를 표시 중인 UI가 구독해서 다시 그리는 용도.
    public event Action OnChanged;

    // 저장본이 없거나 새로 시작할 때의 기본값. 생성 직후에도 한 번 불린다.
    public virtual void Init()
    {
    }

    public void SetNeedSave(bool _isNeedSave = true)
    {
        m_isNeedSave = _isNeedSave;
    }

    public void SetVersion(int _version)
    {
        if (m_Version == _version)
            return;

        m_Version = _version;
        SetChanged();
    }

    // 파생 클래스의 모든 Set 메서드가 마지막에 이걸 부른다.
    protected void SetChanged()
    {
        m_isNeedSave = true;

        if (OnChanged != null)
            OnChanged.Invoke();
    }
}
