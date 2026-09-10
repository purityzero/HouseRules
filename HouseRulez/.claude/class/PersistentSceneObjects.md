# PersistentSceneObjects

연관: [[SceneManager]], [[MonoSingleton]]

`Assets/Scripts/Glory/Scene/PersistentSceneObjects.cs`의 전역 런타임 부트스트랩이다.
Additive 씬 전환 중에도 정확히 하나만 존재해야 하는 `EventSystem`과
`Global Light 2D`를 `DontDestroyOnLoad` 루트 아래에서 유지한다.

## 생성과 생명주기

- `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`에서 싱글톤을 생성한다.
- `MonoSingleton`이므로 `Command_CleanupDontDestroy` 정리 대상에서 제외된다.
- EventSystem은 `InputSystemUIInputModule`을 사용한다.
- Global Light 2D는 Type Global, Intensity 1, Falloff 0.5, Blend Style 0이다.
- 각 씬에는 별도 EventSystem/Global Light를 두지 않는다.

## 2026-09-10 QA

TitleScene에서 Play 버튼을 EventSystem pointer click으로 눌러 InGameScene으로
실제 전환했다. 전환 전후 EventSystem 1개, Global Light 1개였고 라이트 설정도
동일했다. 인게임 Spin 버튼 클릭으로 코인 `6 → 5`를 확인했다.

콘솔의 다음 경고는 모두 0건이었다.

- `There can be only one active Event System.`
- `There are 2 event systems in the scene.`
- `More than one global light`
