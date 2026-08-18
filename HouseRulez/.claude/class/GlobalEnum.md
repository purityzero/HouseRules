# GlobalEnum

Glory 공용 라이브러리(`Assets/Scripts/Glory/GlobalEnum.cs`)의 전역 enum 모음.

## 2026-08-18-0 — GeometryDefender에서 Glory 라이브러리 이식, 게임 고유 재화명 제거

### 개요
`eCurrencyType`에 GeometryDefender의 재화명 `Shard`가 하드코딩되어 있었음(`.claude/rules/glory.md`에 이미 "프로젝트 비의존 원칙"의 알려진 예외로 문서화됨). HouseRulez는 재화 체계가 아직 없으므로 게임 고유 값 없이 빈 틀만 이식.

### 수정
**이전**:
```csharp
public enum eCurrencyType
{
    None = 0,
    Shard,
    Max
}
```

**이후**:
```csharp
public enum eCurrencyType
{
    None = 0,
    Max
}
```

HouseRulez에서 재화 체계를 설계하면 `Max` 앞에 실제 재화 값을 추가할 것.

### 검증 상태 — 미검증
`UIAssetBox`(Glory/UI/AssetBox)가 이 enum을 참조한다. 원래 `PlayerManager.instance.GetCurrencyAmount()`를 직접 호출해 컴파일 에러였으나, 2026-08-18-1에 `SetData(eCurrencyType, ObservableVariable<int>)`로 옵저버를 외부에서 주입받는 방식으로 바뀌어 해소됨 — 상세는 [[UIAssetBox]] 참고.
