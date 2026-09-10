using TMPro;
using UnityEngine;

// 런이 닫힌 사유. 제목 문구가 이걸로 갈린다.
public enum eRunEndReason
{
    HomeFallen,     // 본거지 HP 0 — 함락
    Cleared,        // 최종 연차 최종 웨이브 승리 — 완주
    OutOfSpinCoin,  // 스핀 코인 소진 — 더 굴릴 수단이 없다
}

// 런 종료 결과 팝업. 도달 연차와 획득 옥새를 보여준다.
//
// 옥새를 화면에 안 보여주면 플레이어가 영구 성장을 배울 통로가 없다 —
// 그게 배너 한 줄이 아니라 팝업으로 만든 이유다(기획: run-end-flow.html Q1-A).
// 지급 자체는 InGameScene.EndRun()이 이미 끝냈고, 이 팝업은 결과를 읽어 그리기만 한다.
public class UIRunResult : UIPopup
{
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private TextMeshProUGUI m_YearText;
    [SerializeField] private TextMeshProUGUI m_RoyalText;
    [SerializeField] private UIButton m_TitleSceneButton;

    private const string STRING_KEY_HOME_FALLEN = "RunEndHomeFallen";
    private const string STRING_KEY_CLEARED = "RunEndCleared";
    private const string STRING_KEY_OUT_OF_SPIN_COIN = "RunEndOutOfSpinCoin";
    private const string STRING_KEY_YEAR = "RunEndYear";
    private const string STRING_KEY_ROYAL = "RunEndRoyal";

    private const string TITLE_SCENE_NAME = "TitleScene";

    private bool m_isClosing;

    public void Apply(eRunEndReason _reason, int _year, int _yearMax, int _royal)
    {
        Show();

        // UIManager 루트가 크기 0이라 그 아래에 생성되면 화면을 못 채운다.
        // UISetting이 같은 이유로 쓰는 처리를 그대로 따른다 — 부모 Rect 크기에 기대지 않는다.
        RectTransform rectTransform = transform as RectTransform;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(1920f, 1080f);

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
        {
            Logger.Error("[UIRunResult] Apply Failed! StringTable not found (기대: TableManager에 등록됨)");
            return;
        }

        if (m_TitleText != null)
            m_TitleText.SetText(stringTable.GetString(GetTitleKey(_reason)));

        if (m_YearText != null)
            m_YearText.SetText(stringTable.GetString(STRING_KEY_YEAR, _year, _yearMax));

        if (m_RoyalText != null)
            m_RoyalText.SetText(stringTable.GetString(STRING_KEY_ROYAL, _royal));

        // 이 팝업은 재사용되므로 -= 를 먼저 부른다. 안 그러면 런을 반복할수록 리스너가 쌓인다.
        if (m_TitleSceneButton != null)
        {
            m_TitleSceneButton.onClick.RemoveListener(OnClickTitleSceneButton);
            m_TitleSceneButton.onClick.AddListener(OnClickTitleSceneButton);
        }
    }

    public void OnClickTitleSceneButton()
    {
        // NextScene()은 부를 때마다 같은 커맨드 묶음을 큐에 더 쌓기만 해서 연타에 취약하다.
        if (m_isClosing == true)
            return;

        m_isClosing = true;

        Close();
        SceneManager.instance.NextScene(TITLE_SCENE_NAME);
    }

    // 런이 끝난 화면이라 뒤로가기로 닫으면 인게임에 아무것도 못 하는 상태로 남는다.
    // 타이틀로 나가는 것만 허용한다.
    public override void OnPressBackBtn()
    {
        OnClickTitleSceneButton();
    }

    private string GetTitleKey(eRunEndReason _reason)
    {
        switch (_reason)
        {
            case eRunEndReason.Cleared:
                return STRING_KEY_CLEARED;
            case eRunEndReason.OutOfSpinCoin:
                return STRING_KEY_OUT_OF_SPIN_COIN;
            default:
                return STRING_KEY_HOME_FALLEN;
        }
    }
}
