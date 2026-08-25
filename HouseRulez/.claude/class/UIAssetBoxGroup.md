# UIAssetBoxGroup

연관: [[UIAssetBox]], [[GlobalEnum]]

Glory 공용 라이브러리(`Assets/Scripts/Glory/UI/AssetBox/UIAssetBoxGroup.cs`)의 재화 표시 위젯 묶음.
인스펙터에 연결된 `UIAssetBox[]`를 순회해 일괄 갱신하는 얇은 래퍼다.

## 2026-08-24-0 — 컴파일 에러 유발하던 무인자 `SetData()` 제거

### 증상
Unity가 **세이프 모드로 진입**해 에디터가 정상 동작하지 않음(Editor.log에 `[MODES] ModeService[safe_mode].LoadModes`).
원인 로그:
```
Assets\Scripts\Glory\UI\AssetBox\UIAssetBoxGroup.cs(22,29):
error CS1501: No overload for method 'SetData' takes 0 arguments
```

### 원인
`UIAssetBoxGroup.SetData()`가 `m_AssetBoxes[i].SetData()`를 **인자 없이** 호출하는데,
`UIAssetBox`에는 무인자 오버로드가 없다. `UIAssetBox`가 가진 오버로드는 셋뿐:

- `SetData(eCurrencyType, ObservableVariable<int>)`
- `SetData(eCurrencyType, long)`
- `SetData(long)`

무인자 `SetData()`는 [[UIAssetBox]] 2026-08-18-1(PlayerManager 하드 의존 제거) 작업에서
"값을 어디서 가져올지 알 방법이 없어져 의미가 사라졌다"는 이유로 `UIAssetBox`에서 제거됐는데,
같은 시점에 만들어진 `UIAssetBoxGroup` 쪽 호출부가 함께 정리되지 않아 컴파일이 깨진 채 커밋됨(`605277d`).

### 수정 (메서드 단위)
**이전**:
```csharp
public void SetData()
{
    for (int i = 0; i < m_AssetBoxes.Length; ++i)
    {
        m_AssetBoxes[i].SetData();
    }
}
public void Refresh() { ... }
```

**이후**:
```csharp
// SetData() 제거 — Refresh()만 남음
public void Refresh()
{
    for (int i = 0; i < m_AssetBoxes.Length; ++i)
    {
        m_AssetBoxes[i].Refresh();
    }
}
```

### 판단 근거
사용자 확정(2026-08-24): 각 `UIAssetBox`가 인스펙터에 `m_CurrencyType`을 이미 들고 있으므로
그룹은 데이터를 뿌릴 필요 없이 `Refresh()`만 일괄 호출하면 된다.
그룹에 데이터 주입 API가 필요해지면 각 박스에 `SetData(eCurrencyType, ObservableVariable<int>)`를
호출부(프로젝트 코드)에서 개별로 연결한다 — 그룹이 재화 저장소를 알게 만들지 않는다(Glory 프로젝트 비의존 원칙, `.claude/rules/glory.md`).

### 영향 범위
프로젝트 전체 grep 결과 `UIAssetBoxGroup`을 참조하는 코드·씬·프리팹이 **하나도 없다** — 호출부 파급 없음.

### 검증 상태 — 미검증
- Unity MCP 서버가 이 환경에 등록돼 있지 않아(`mcpforunity` 리소스 없음) 에디터를 통한 컴파일 확인 불가.
- Unity가 세이프 모드에서 빠져나와 재컴파일에 성공하는지 사용자 확인 필요.
- 작업 브랜치: `work/2026-08-24-assetboxgroup-setdata` (리포 루트는 `D:\Unity\HouseRules`)

## 참고 — 함께 발견된 별개 이슈 (미수정)
- `Assets/Resources/Table/*.csv` 4개(`SoundTable`/`ToggleListTable`/`ToggleMenuTable`/`UITable`)가 **헤더 한 줄뿐, 데이터 행 0개**. 최초 커밋(`605277d`)부터 그 상태 — 깨진 게 아니라 아직 안 채운 것.
- `.claude/rules/glory.md`의 "재화 표시는 UIAssetBox(단일) / UIAssetBoxGroup(일괄 Refresh) 재사용 — 보유량은 PlayerManager 경유" 문장 중 **"보유량은 PlayerManager 경유"는 이미 낡은 서술**([[UIAssetBox]] 2026-08-18-1에서 PlayerManager 하드 의존을 제거함). 이번 작업 범위 밖이라 건드리지 않음.
