using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum eBattleSide
{
    Ally = 0,
    Enemy,
}

// 전장에서 움직이고 싸우는 한 기. 아군과 적이 같은 클래스를 쓴다 —
// 규칙이 대칭이라(진격 방향만 반대) 클래스를 둘로 나누면 같은 코드를 두 벌 갖게 된다.
//
// 방향 표현은 스프라이트를 뒤집지 않는다. 장기·화투 심볼이 한자와 그림이라
// 좌우 반전하면 글자가 거울상이 된다. 대신 **실제로 적을 향해 이동**하는 것으로 방향을 보인다.
public class BattleUnit : MonoBehaviour
{
    [SerializeField] private Image m_SymbolImage;
    [SerializeField] private Image m_HpFillImage;
    [SerializeField] private TextMeshProUGUI m_GradeText;

    private RectTransform m_RectTransform;
    private eBattleSide m_Side;
    private int m_MaxHp;
    private int m_Hp;
    private int m_Atk;
    private float m_AtkSpeed;
    private float m_Range;
    private float m_MoveSpeed;
    private float m_AtkCooldown;

    public eBattleSide side => m_Side;
    public bool isAlive => m_Hp > 0;
    public int lane { get; private set; }
    public float positionX => m_RectTransform.anchoredPosition.x;

    // 사거리와 이동 속도는 칸 단위로 적는다. 화면 픽셀로 적으면 레이아웃이 바뀔 때마다 밸런스가 흔들린다.
    private const float CELL_TO_PIXEL = 108f;

    public void Setup(eBattleSide _side, int _lane, Sprite _sprite, int _grade,
        int _hp, int _atk, float _atkSpeed, int _range, float _moveSpeed, Vector2 _startPosition)
    {
        m_RectTransform = transform as RectTransform;
        m_Side = _side;
        lane = _lane;
        m_MaxHp = Mathf.Max(1, _hp);
        m_Hp = m_MaxHp;
        m_Atk = _atk;
        m_AtkSpeed = Mathf.Max(0.1f, _atkSpeed);
        m_Range = _range * CELL_TO_PIXEL;
        m_MoveSpeed = _moveSpeed * CELL_TO_PIXEL;
        m_AtkCooldown = 0f;

        m_RectTransform.anchoredPosition = _startPosition;

        if (m_SymbolImage != null)
        {
            m_SymbolImage.sprite = _sprite;
            m_SymbolImage.enabled = (_sprite != null);
        }

        if (m_GradeText != null)
        {
            bool showGrade = (_grade >= 2);
            m_GradeText.gameObject.SetActive(showGrade);
            if (showGrade == true)
                m_GradeText.text = $"★{_grade}";
        }

        RefreshHpBar();
        gameObject.SetActive(true);
    }

    // 사거리 안에 목표가 있으면 때리고, 없으면 적진 쪽으로 나아간다.
    public void Tick(float _deltaTime, BattleUnit _target)
    {
        if (isAlive == false)
            return;

        if (m_AtkCooldown > 0f)
            m_AtkCooldown -= _deltaTime;

        if (_target != null && _target.isAlive == true)
        {
            float distance = Mathf.Abs(_target.positionX - positionX);
            if (distance <= m_Range)
            {
                if (m_AtkCooldown <= 0f)
                {
                    _target.TakeDamage(m_Atk);
                    m_AtkCooldown = 1f / m_AtkSpeed;
                }
                return;
            }
        }

        float direction = (m_Side == eBattleSide.Ally) ? 1f : -1f;
        m_RectTransform.anchoredPosition += new Vector2(direction * m_MoveSpeed * _deltaTime, 0f);
    }

    public void TakeDamage(int _amount)
    {
        if (isAlive == false)
            return;

        m_Hp -= _amount;
        if (m_Hp < 0)
            m_Hp = 0;

        RefreshHpBar();

        if (isAlive == false)
            gameObject.SetActive(false);
    }

    private void RefreshHpBar()
    {
        if (m_HpFillImage == null)
            return;

        m_HpFillImage.fillAmount = m_Hp / (float)m_MaxHp;
    }
}
