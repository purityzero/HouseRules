using DG.Tweening;
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
    public Vector2 position => m_RectTransform.anchoredPosition;

    // 사거리와 이동 속도는 칸 단위로 적는다. 화면 픽셀로 적으면 레이아웃이 바뀔 때마다 밸런스가 흔들린다.
    private const float CELL_TO_PIXEL = 108f;

    // 연출은 **심볼(자식)만** 움직인다. 루트의 anchoredPosition은 Tick의 이동 로직이 매 프레임 쓰고 있어서,
    // 거기에 트윈을 걸면 둘이 같은 값을 서로 덮어써 유닛이 제자리에서 떨거나 목표를 못 따라간다.
    private RectTransform m_SymbolRect;
    private Vector2 m_SymbolRestPosition;

    // 공격 속도 1.0이면 1초에 한 번 때린다. 모션은 그보다 훨씬 짧아야 다음 공격과 겹치지 않는다.
    private const float ATTACK_LUNGE = 26f;
    private const float ATTACK_DURATION = 0.14f;

    private const float HIT_KNOCKBACK = 16f;
    private const float HIT_DURATION = 0.18f;
    private const float HIT_SCALE_PUNCH = 0.35f;

    private const float DEATH_DURATION = 0.22f;

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

        // 재사용되는 오브젝트라 지난 웨이브의 연출이 남아 있을 수 있다. 시작할 때 원점으로 되돌린다.
        KillMotion();
        m_RectTransform.localScale = Vector3.one;

        if (m_SymbolImage != null)
        {
            m_SymbolImage.sprite = _sprite;
            m_SymbolImage.enabled = (_sprite != null);

            m_SymbolRect = m_SymbolImage.rectTransform;
            m_SymbolRestPosition = m_SymbolRect.anchoredPosition;
            m_SymbolRect.localScale = Vector3.one;
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

    // 사거리 안에 목표가 있으면 때리고, 없으면 **목표 쪽으로** 나아간다.
    // 목표가 아예 없으면(반대편이 전멸) 적진 방향으로 계속 전진한다.
    public void Tick(float _deltaTime, BattleUnit _target)
    {
        if (isAlive == false)
            return;

        if (m_AtkCooldown > 0f)
            m_AtkCooldown -= _deltaTime;

        if (_target == null || _target.isAlive == false)
        {
            // 상대가 없으면 진격 방향으로만 간다. 적은 이 진행으로 본거지 선을 넘는다.
            float direction = (m_Side == eBattleSide.Ally) ? 1f : -1f;
            m_RectTransform.anchoredPosition += new Vector2(direction * m_MoveSpeed * _deltaTime, 0f);
            return;
        }

        // 거리는 2D로 잰다. x만 재면 바로 위 칸의 적을 "사거리 안"으로 오판해 제자리에서 허공을 친다.
        Vector2 toTarget = _target.position - position;
        if (toTarget.sqrMagnitude <= m_Range * m_Range)
        {
            if (m_AtkCooldown <= 0f)
            {
                // 때리는 쪽은 목표 쪽으로 찌르고, 맞는 쪽은 그 방향으로 밀린다.
                // 둘 다 없으면 화면에서는 HP 바 숫자만 조용히 줄어들어 무슨 일이 일어나는지 안 보인다.
                Vector2 direction = toTarget.normalized;
                PlayAttackMotion(direction);
                _target.TakeDamage(m_Atk, direction);

                m_AtkCooldown = 1f / m_AtkSpeed;
            }

            return;
        }

        // 목표를 향해 비스듬히 간다. 레인이 비면 옆 레인으로 넘어가 맞붙는다.
        m_RectTransform.anchoredPosition += toTarget.normalized * (m_MoveSpeed * _deltaTime);
    }

    // 본거지 선을 넘은 적처럼 때린 주체가 없는 경우에 쓴다.
    public void TakeDamage(int _amount)
    {
        TakeDamage(_amount, Vector2.zero);
    }

    public void TakeDamage(int _amount, Vector2 _hitDirection)
    {
        if (isAlive == false)
            return;

        m_Hp -= _amount;
        if (m_Hp < 0)
            m_Hp = 0;

        RefreshHpBar();

        if (isAlive == true)
        {
            PlayHitMotion(_hitDirection);
            return;
        }

        PlayDeathMotion();
    }

    // 목표 쪽으로 짧게 찌르고 돌아온다. 누가 때렸는지, 어느 쪽을 때렸는지가 이걸로 읽힌다.
    private void PlayAttackMotion(Vector2 _direction)
    {
        if (m_SymbolRect == null)
            return;

        ResetSymbolMotion();
        m_SymbolRect.DOPunchAnchorPos(_direction * ATTACK_LUNGE, ATTACK_DURATION, 1, 0.4f);
    }

    // 맞은 쪽은 뒤로 밀리고 살짝 부푼다.
    // 색 플래시를 안 쓰는 이유: 적 심볼이 거의 검정이라 Image 컬러 틴트가 곱셈이라 티가 안 난다.
    // 움직임은 아군(흰색)·적(검정) 어느 쪽에서도 똑같이 읽힌다.
    private void PlayHitMotion(Vector2 _hitDirection)
    {
        if (m_SymbolRect == null)
            return;

        ResetSymbolMotion();
        m_SymbolRect.DOPunchAnchorPos(_hitDirection * HIT_KNOCKBACK, HIT_DURATION, 1, 0.6f);
        m_SymbolRect.DOPunchScale(Vector3.one * HIT_SCALE_PUNCH, HIT_DURATION);
    }

    // 즉시 사라지면 무엇이 죽었는지 못 본다. 줄어들며 사라진 뒤에 끈다.
    private void PlayDeathMotion()
    {
        KillMotion();

        // 루트를 줄인다 — 죽은 뒤엔 Tick이 조기 반환해서 위치를 안 건드리므로 트윈과 다투지 않는다.
        m_RectTransform.DOScale(0f, DEATH_DURATION)
            .SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }

    // 펀치는 끝날 때 원래 값으로 되돌리지만, 중간에 새 펀치가 겹치면 어긋난 자리에 남는다.
    // 그래서 매번 쉬는 자리에서 다시 시작한다.
    private void ResetSymbolMotion()
    {
        m_SymbolRect.DOKill();
        m_SymbolRect.anchoredPosition = m_SymbolRestPosition;
        m_SymbolRect.localScale = Vector3.one;
    }

    private void KillMotion()
    {
        if (m_SymbolRect != null)
        {
            m_SymbolRect.DOKill();
            m_SymbolRect.anchoredPosition = m_SymbolRestPosition;
            m_SymbolRect.localScale = Vector3.one;
        }

        if (m_RectTransform != null)
            m_RectTransform.DOKill();
    }

    // 웨이브가 끝나면 UIInGameBattle이 유닛을 파괴한다.
    // 트윈이 살아 있는 채로 파괴되면 사라진 대상을 계속 건드린다.
    private void OnDestroy()
    {
        KillMotion();
    }

    private void RefreshHpBar()
    {
        if (m_HpFillImage == null)
            return;

        m_HpFillImage.fillAmount = m_Hp / (float)m_MaxHp;
    }
}
