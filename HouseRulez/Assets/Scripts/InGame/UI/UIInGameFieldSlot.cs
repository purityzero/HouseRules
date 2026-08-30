using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전장 3×3의 한 칸. 소환된 유닛 하나를 보여준다.
// 아군 유닛 아트를 따로 만들지 않고 릴 심볼 스프라이트를 그대로 쓴다 —
// "릴에 나온 말이 그대로 전장에 선다"는 GDD 컨셉과 맞고 아트 비용도 들지 않는다.
public class UIInGameFieldSlot : MonoBehaviour
{
    [SerializeField] private Image m_SymbolImage;
    [SerializeField] private TextMeshProUGUI m_GradeText;

    // 1성은 등급 표시를 하지 않는다. 대부분이 1성이라(체스 95%) 전부 표시하면 화면이 숫자로 덮인다.
    private const int GRADE_HIDE_BELOW = 2;

    public void Clear()
    {
        if (m_SymbolImage != null)
        {
            m_SymbolImage.enabled = false;
            m_SymbolImage.sprite = null;
        }

        if (m_GradeText != null)
            m_GradeText.gameObject.SetActive(false);
    }

    public void SetUnit(Sprite _symbolSprite, int _grade)
    {
        if (m_SymbolImage != null)
        {
            m_SymbolImage.sprite = _symbolSprite;
            m_SymbolImage.enabled = (_symbolSprite != null);
        }

        if (m_GradeText != null)
        {
            bool showGrade = (_grade >= GRADE_HIDE_BELOW);
            m_GradeText.gameObject.SetActive(showGrade);
            if (showGrade == true)
                m_GradeText.text = $"★{_grade}";
        }
    }
}
