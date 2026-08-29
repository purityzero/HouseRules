using TMPro;
using UnityEngine;

// StringTable 키를 들고 스스로 갱신하는 정적 라벨. GeometryDefender의 같은 이름 컴포넌트를 이식했다.
// 원본은 PlayerManager.languageObservable을 구독하지만, 이 프로젝트는 언어 관찰 값을
// StringTable이 들고 있어(읽기 경로와 같은 곳) 그쪽을 구독한다.
//
// 정적 라벨 전용이다. 다음은 대상이 아니며 기존처럼 코드가 채운다.
//  - 포맷 인자가 있는 표시 (연차 01/12, 판돈 배율 등)
//  - 키가 런타임에 정해지는 표시 (레코드의 NameKey/DescKey 등)
//  - 템플릿을 복제해 여러 개로 늘어나는 목록 항목
public class UIText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private string m_Key;

    private void OnEnable()
    {
        StringTable.LANGUAGE.RegisterObserver(OnLanguageChanged);
    }

    private void OnDisable()
    {
        StringTable.LANGUAGE.UnregisterObserver(OnLanguageChanged);
    }

    private void OnLanguageChanged(eLanguage _oldLanguage, eLanguage _newLanguage)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (m_Text == null)
            return;

        if (string.IsNullOrEmpty(m_Key) == true)
            return;

        // 씬에 박힌 오브젝트는 OnEnable이 테이블 로드(BaseScene.OnSetup)보다 먼저 돈다.
        // 그 시점엔 조용히 넘기고, 씬이 준비를 마친 뒤 RefreshAll()이 다시 부른다.
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        if (stringTable == null)
            return;

        m_Text.SetText(stringTable.GetString(m_Key).Replace("\\n", "\n"));
    }

    // 테이블과 저장 언어가 준비된 직후 씬이 한 번 부른다.
    // 저장된 언어가 기본값과 같으면 관찰 값이 바뀌지 않아 알림이 오지 않으므로,
    // 최초 1회는 알림에 기대지 않고 직접 돌려야 한다.
    public static void RefreshAll()
    {
        UIText[] listText = FindObjectsByType<UIText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < listText.Length; ++i)
        {
            listText[i].Refresh();
        }
    }
}
