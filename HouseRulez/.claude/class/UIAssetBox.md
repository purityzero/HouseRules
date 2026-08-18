# UIAssetBox

연관: [[GlobalEnum]], [[TableManager]]

Glory 공용 라이브러리(`Assets/Scripts/Glory/UI/AssetBox/UIAssetBox.cs`)의 재화 표시 위젯.

## 2026-08-18-1 — PlayerManager 하드 의존 제거, SetData로 옵저버 주입받는 방식으로 변경

### 개요
GeometryDefender 원본은 `RegisterCurrencyObserver()`/`GetCurrencyAmount()`가 내부에서 직접 `PlayerManager.instance`를 호출했음 — HouseRulez엔 `PlayerManager` 클래스가 없어 컴파일 에러(`.claude/class/GlobalEnum.md` 2026-08-18-0에서 이미 알려진 이슈로 기록됨). 사용자 요청으로 Glory 프로젝트 비의존 원칙에 맞게, "어디서 재화 값을 가져오는지"를 Glory 밖(호출부)이 결정하도록 변경 — `PlayerManager` 같은 구체 클래스를 Glory가 직접 참조하지 않는다.

### 수정 (필드 + 메서드 전반)
**이전**:
```csharp
private ObservableVariable<int> m_RegisteredObservable;
...
public void SetData(eCurrencyType _currencyType) { ... GetCurrencyAmount(_currencyType) ... }
public void SetData() { ... GetCurrencyAmount(m_CurrencyType) ... }
public void Refresh() { ... GetCurrencyAmount(m_CurrencyType) ... }
private void RegisterCurrencyObserver()
{
    m_RegisteredObservable = PlayerManager.instance.GetCurrencyObservable(m_CurrencyType);
    ...
}
private long GetCurrencyAmount(eCurrencyType _currencyType)
{
    return PlayerManager.instance.GetCurrencyAmount(_currencyType);
}
```

**이후**:
```csharp
private ObservableVariable<int> m_Observable;
...
// 호출부가 재화 저장소에서 직접 꺼낸 ObservableVariable<int>를 넘긴다 — 등록 즉시 현재 값으로 1회 콜백이 온다
public void SetData(eCurrencyType _currencyType, ObservableVariable<int> _observable) { m_CurrencyType = _currencyType; m_Observable = _observable; RegisterCurrencyObserver(); }
public void SetData(eCurrencyType _currencyType, long _amount) { ... } // 1회성 표시, 옵저버 미등록(기존 유지)
public void SetData(long _amount) { ... } // 기존 유지, 원래도 PlayerManager 미사용
public void Refresh() { ... m_Observable.Value 사용 (없으면 에러 로그) ... }
private void RegisterCurrencyObserver() { if (m_Observable == null) return; m_Observable.RegisterObserver(OnCurrencyChanged); }
```

- **제거된 오버로드**: `SetData(eCurrencyType)`(파라미터 1개, 내부 자동 조회), `SetData()`(무인자, 내부 자동 조회) — 값을 어디서 가져올지 알 방법이 없어져 의미가 사라짐. 값을 넘기지 않고 자동 갱신하려면 새 `SetData(eCurrencyType, ObservableVariable<int>)`를 쓸 것.
- **API 사용 예시** (프로젝트에서 PlayerManager를 구현한 뒤):
  ```csharp
  uiAssetBox.SetData(eCurrencyType.Gold, PlayerManager.instance.GetCurrencyObservable(eCurrencyType.Gold));
  ```

### 검증 상태 — 미검증
Unity 에디터 컴파일 확인 안 됨(파일 조작만 수행). HouseRulez엔 아직 이 컴포넌트를 실제로 붙인 프리팹/호출부가 없어 런타임 동작도 미확인.
