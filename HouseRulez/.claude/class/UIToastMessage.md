# UIToastMessage

연관: [[UIManager]], [[TweenEffectPlayer]]

Glory 공용 라이브러리(`Assets/Scripts/Glory/UI/Toast/UIToastMessage.cs`)의 토스트 팝업 컴포넌트. `UIManager.ShowToast()`가 내부적으로 풀링해 사용.

## 2026-08-18-0 — GeometryDefender에서 Glory 라이브러리 이식, Febucci 의존 제거

### 개요
GeometryDefender의 Glory 폴더는 원래 `TextAnimationPlayer`(Text Animator by Febucci 패키지 기반, `Glory/TextAnimation/`)로 타자기 출력을 했으나, HouseRulez엔 Febucci 패키지가 미설치라 `.claude/rules/glory.md`에 문서화된 절차대로 `TextAnimation` 폴더 자체를 이식 대상에서 제외하고, 이 컴포넌트는 즉시 텍스트 대입(`SetText`) 방식으로 되돌림.

### 수정 (함수: `Show(string, UnityAction<UIToastMessage>)`)
**이전**:
```csharp
[SerializeField] private TextAnimationPlayer m_TextPlayer;
...
public void Show(string _message, UnityEngine.Events.UnityAction<UIToastMessage> _onClosed)
{
    m_OnClosed = _onClosed;
    m_TextPlayer.Play(_message);
    m_TweenPlayer.Play(OnShowComplete);
}
```

**이후**:
```csharp
// m_TextPlayer 필드 제거
public void Show(string _message, UnityEngine.Events.UnityAction<UIToastMessage> _onClosed)
{
    m_OnClosed = _onClosed;
    m_MessageText.text = _message;
    m_TweenPlayer.Play(OnShowComplete);
}
```

### 검증 상태 — 미검증
컴파일/실행 확인 안 됨. 나중에 Febucci 패키지를 설치하고 타자기 연출을 다시 쓰고 싶으면 `Glory/TextAnimation/` 폴더(TextAnimationPlayer/TextAnimatorUtil)를 GeometryDefender에서 다시 가져오고 이 파일을 원복해야 함.
