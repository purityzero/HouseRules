# TitleScene

연관: [[BaseScene]], [[SoundRecord]], [[TableManager]], [[SoundManager]], [[ResUtil]], [[Logger]], [[TitleBackgroundScroller]]

타이틀 씬의 진입점(`Assets/Scripts/Title/TitleScene.cs`). `BaseScene`을 상속해 씬 내 `IUpdatable` 갱신 루프의 주인 역할을 하고, 진입 시 BGM을 재생한다.

## 2026-08-25-0 — GeometryDefender의 TitleScene 구성 이식

### 개요
타이틀 화면 작업을 시작하기 위해, GeometryDefender(`D:\Unity\GeometryDefender\GeometryDefender`)의 타이틀 클래스 구성을 HouseRulez로 가져옴(사용자 요청). GeometryDefender 쪽은 `Assets/Scripts/Title/` 아래 `TitleScene` / `TitleSquareEffect` / `TitleHexagonEffect` 3개 구성이지만, **이번엔 `TitleScene.cs` 하나만 이식**하기로 결정 — 나머지 둘은 GeometryDefender의 도형 비주얼 전용이라 HouseRulez의 보드게임 테마(체스/화투/장기/포커)에 맞는 배경 연출을 따로 설계한 뒤 만든다.

### 파일
- `Assets/Scripts/TitleScene.cs` → `Assets/Scripts/Title/TitleScene.cs` (폴더 신설 후 이동, `.meta`도 함께 이동해 guid `0788a19ffe71f7240b50fc538cfec890` 유지)

### 증상 / 배경
이동 전 `TitleScene.cs`는 Unity 기본 템플릿 그대로인 빈 `MonoBehaviour`(빈 `Start()`/`Update()`)였고, `Assets/Scenes/TitleScene.unity` 어느 오브젝트에도 붙어 있지 않았다(씬 YAML에서 위 guid 미검출). 그래서 내용을 통째로 교체해도 씬 참조가 깨질 위험이 없었다.

### 수정 (클래스 전체)
**이전**
```csharp
public class TitleScene : MonoBehaviour
{
    void Start() { }
    void Update() { }
}
```

**이후**
```csharp
[DefaultExecutionOrder(-1000)]
public class TitleScene : BaseScene
{
    protected override void OnSetup()
    {
        PlayBgm();
    }

    private void PlayBgm()
    {
        SoundTable soundTable = TableManager.instance.GetTable<SoundTable>();
        SoundRecord record = soundTable?.GetRecordByKey("TitleTheme");
        if (record == null)
        {
            Logger.Error($"[TitleScene] PlayBgm Failed! SoundRecord not found - TitleTheme");
            return;
        }

        AudioClip clip = ResUtil.Load<AudioClip>(record.ClipPath);
        if (clip == null)
            return;

        SoundManager.instance.PlayBgm(clip);
    }
}
```

### 원본에서 뺀 것 — 버튼 핸들러 4개
GeometryDefender 원본에는 `OnClickPlayButton` / `OnClickMetatreeButton` / `OnClickSettingsButton` / `OnClickHowToPlayButton`이 있고 각각 `UIManager.instance.Get<UIDifficultySelect>()` 등을 호출한다. 이 팝업 클래스들은 GeometryDefender 게임 고유라 HouseRulez에 존재하지 않아 **그대로 가져오면 컴파일이 깨진다**. 빈 껍데기로 남기는 선택지도 있었으나 사용자가 전부 제외를 택함 — 타이틀 화면 구성을 확정한 뒤 필요한 버튼만 붙인다.

### `[DefaultExecutionOrder(-1000)]`을 함께 가져온 이유
Unity는 "모든 `Awake`가 끝난 뒤 `Start`가 불린다"는 보장은 하지만 `OnEnable` 순서에는 보장이 없다. HouseRulez의 `BaseScene`은 `OnEnable()`에서 `Current = this`를 세팅하므로(GeometryDefender와 세부 위치가 다름), `UpdatableBehaviour.OnEnable()`이 먼저 돌면 `BaseScene.Current`가 아직 null이다. `Register(this)`가 널 조건 연산자(`?.`)에 걸려 **에러 로그 없이 조용히 건너뛰어지고** 그 오브젝트의 `UpdateLogic()`이 영영 호출되지 않는다. 원본 주석은 GeometryDefender의 `InGameScene.cs`를 가리키고 있어(HouseRulez엔 그 파일이 없음) 이 이유를 자립적으로 풀어 쓴 주석으로 교체함.

### 알려진 미완 사항 — BGM 리소스 없음
`Assets/Resources/Table/SoundTable.csv`는 헤더(`Key,ClipPath,MaxConcurrent`)만 있는 빈 템플릿이라 `"TitleTheme"` 조회가 실패하고, 진입할 때마다 `Logger.Error`로 `SoundRecord not found - TitleTheme`가 찍힌다. 컴파일/동작에는 영향 없음(조기 리턴). BGM을 붙이려면 오디오 파일을 `Assets/Resources/Sound/Bgm/` 등에 넣고 `SoundTable.csv`에 `TitleTheme` 행을 추가해야 한다.

### 검증 상태 — Play Mode 실행까지 확인 완료
컴파일 에러 0건, `Assets/Scripts/Title.meta` 생성, 새 경로 임포트, guid 유지 확인.

2026-08-25 16:0x에 씬 배치 후 Play Mode 실행까지 검증했다. `TitleScene.unity` 루트에 `TitleScene`이라는 빈 GameObject를 만들어 이 컴포넌트를 부착했고, Play Mode에서 `BaseScene.Current`가 `TitleScene`으로 정상 등록되는 것을 확인했다. 같은 씬의 [[TitleBackgroundScroller]]가 `UpdatableBehaviour`로서 누락 없이 등록되어 실제로 동작하는 것으로, `[DefaultExecutionOrder(-1000)]`이 의도대로 작동함이 간접 증명됐다.

`OnSetup()` → `PlayBgm()`은 호출되지만 `SoundTable.csv`가 빈 템플릿이라 `SoundRecord not found - TitleTheme` 에러 로그를 남기고 조기 리턴한다(알려진 미완 사항, 동작 지장 없음).

#### 주의 — Unity가 켜진 상태에서 외부 파일 이동 시 AssetDatabase가 깨진다
이번에 실제로 겪은 사고: 에디터가 실행 중인 채로 파일 시스템에서 `TitleScene.cs`를 옮겼더니, `AssetImportWorker`가 옛 경로를 계속 임포트하려 들며 아래 에러로 리프레시가 실패했다. 에디터에 포커스를 줘도 풀리지 않았고, **컴파일 자체가 돌지 않았다**(에러 0건처럼 보이지만 어셈블리 재빌드가 없었음 — 로그의 "에러 없음"만 보고 통과로 판단하면 안 되고 `Assembly-CSharp.dll` 타임스탬프를 함께 봐야 한다).

```
ERROR: Build asset version error: assets/scripts/titlescene.cs in SourceAssetDB has
modification time of '2026-08-23T12:39:41Z' while content on disk has
modification time of '0001-01-01T00:00:00Z' with error code 2
```

Unity 재시작으로 해소됨(시작 시 Assets 폴더를 새로 스캔하며 옛 항목 정리). 다음부터는 에디터를 닫은 뒤 파일을 옮기거나, MCP 연결 상태라면 에디터를 통해 이동할 것.
