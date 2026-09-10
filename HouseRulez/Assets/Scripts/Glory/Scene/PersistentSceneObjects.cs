using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

// 씬을 넘어 **정확히 하나만** 있어야 하는 오브젝트를 들고 있는 루트.
//
// 왜 필요한가 — Additive 로드 → 이전 씬 언로드 순서의 전환에서는 잠시 두 씬이
// 동시에 존재한다. 각 씬에 EventSystem과 Global Light 2D가 있으면 그 구간마다
// 유니티가 중복 경고를 뱉는다.
//   "There can be only one active Event System."
//   "There are 2 event systems in the scene."
//   "More than one global light on layer Default for light blend style index 0"
//
// 로드→언로드 순서 자체는 바꿀 수 없다. 반대로 하면 Unity가 "마지막 남은 씬" 취급으로
// 언로드를 거부해 FlowCommand 전체가 멈춘다(Command_LoadScene 주석 참고).
// 그래서 **중복될 수 있는 쪽을 씬 밖으로 빼는 것**이 맞는 해결이다.
//
// ★ Command_CleanupDontDestroy(SceneManager.cs:42)에 안 지워진다 —
//   그 정리는 MonoSingleton을 가진 루트를 건너뛰는데, 이 클래스가 MonoSingleton이다.
public class PersistentSceneObjects : MonoSingleton<PersistentSceneObjects>
{
    // 씬의 글로벌 라이트에서 그대로 옮겨온 값. 두 씬이 동일한 설정을 쓰고 있어
    // 하나로 합쳐도 보이는 결과가 달라지지 않는다(2026-09-10 실측으로 확인).
    private const float LIGHT_INTENSITY = 1f;
    private const float LIGHT_FALLOFF_INTENSITY = 0.5f;
    private const int LIGHT_BLEND_STYLE_INDEX = 0;

    // 첫 씬이 열리기 전에 만들어 둔다. 씬이 스스로 부르게 하면 그 호출을 빠뜨린 씬에서만
    // 조용히 입력이 죽는다 — 진입점을 하나로 두어 빠뜨릴 수 없게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        PersistentSceneObjects created = instance;
        if (created == null)
            Logger.Error("[PersistentSceneObjects] OnBeforeSceneLoad Failed! 인스턴스 생성 실패 (기대: MonoSingleton이 생성)");
    }

    protected override void Awake()
    {
        base.Awake();

        // 중복 싱글톤이면 base.Awake()가 자신을 파괴한다. 그 경우 아래를 만들면 안 된다.
        if (instance != this)
            return;

        EnsureEventSystem();
        EnsureGlobalLight();
    }

    private void EnsureEventSystem()
    {
        if (GetComponentInChildren<EventSystem>(true) != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.transform.SetParent(transform, false);
        eventSystemObject.AddComponent<EventSystem>();

        // 씬에 있던 것과 같은 모듈을 쓴다 — 이 프로젝트는 새 Input System을 쓴다(실측).
        eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private void EnsureGlobalLight()
    {
        if (GetComponentInChildren<Light2D>(true) != null)
            return;

        GameObject lightObject = new GameObject("Global Light 2D");
        lightObject.transform.SetParent(transform, false);

        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = Color.white;
        light.intensity = LIGHT_INTENSITY;
        light.falloffIntensity = LIGHT_FALLOFF_INTENSITY;
        light.blendStyleIndex = LIGHT_BLEND_STYLE_INDEX;
    }
}
