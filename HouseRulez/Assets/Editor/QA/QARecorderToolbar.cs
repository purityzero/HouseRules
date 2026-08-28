using UnityEditor.Toolbars;

// Unity 6.3의 공식 메인 툴바 확장 API로 QA 녹화 버튼을 등록한다.
public static class QARecorderToolbar
{
    private const string StartPath = "QA/Start Recording";
    private const string StopPath = "QA/Stop Recording";

    [MainToolbarElement(
        StartPath,
        defaultDockPosition = MainToolbarDockPosition.Right,
        defaultDockIndex = 0)]
    public static MainToolbarElement CreateStartButton()
    {
        MainToolbarContent content = new MainToolbarContent(
            "● QA Rec",
            "QA 녹화를 시작합니다.");

        return new MainToolbarButton(content, QARecorder.StartRecording);
    }

    [MainToolbarElement(
        StopPath,
        defaultDockPosition = MainToolbarDockPosition.Right,
        defaultDockIndex = 1)]
    public static MainToolbarElement CreateStopButton()
    {
        MainToolbarContent content = new MainToolbarContent(
            "■ QA Stop",
            "QA 녹화를 중지하고 결과 파일을 저장합니다.");

        return new MainToolbarButton(content, QARecorder.StopRecording);
    }
}
