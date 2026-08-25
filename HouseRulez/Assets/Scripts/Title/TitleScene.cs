using UnityEngine;

// BaseScene의 OnEnable()이 씬 내 다른 스크립트의 OnEnable()보다 먼저 실행되도록 강제 —
// Unity는 "모든 Awake가 끝난 뒤에 Start가 불린다"는 보장은 하지만 OnEnable 순서에는 이 보장이 없다.
// UpdatableBehaviour.OnEnable()이 BaseScene.OnEnable()보다 먼저 돌면 BaseScene.Current가 아직 null이라
// Register(this)가 널 조건 연산자에 걸려 조용히 건너뛰어지고(에러 로그조차 없음) 그 오브젝트의 UpdateLogic()이 영영 안 불린다.
[DefaultExecutionOrder(-1000)]
public class TitleScene : BaseScene
{
    protected override void OnSetup()
    {
        // 주의: 이 메서드에 TableManager.init() 같은 "공용 시스템 재초기화" 로직을 넣지 말 것 —
        // 전체 초기화 경로와 중복 호출되어 테이블 데이터가 배수로 누적되는 버그가 된다.
        // BGM 재생은 그런 공용 시스템과 무관한 이 씬 자체의 1회성 진입 연출이라 안전.
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
