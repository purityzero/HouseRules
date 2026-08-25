using UnityEngine;
using UnityEngine.UI;

// 배경 텍스처가 가로 seamless라, uvRect를 계속 한쪽으로 밀기만 하면 끊김 없이 이어진다.
// BaseScene의 갱신 루프를 타야 씬 일시정지(isPaused)와 씬 전환 중 정지가 함께 걸리므로 UpdatableBehaviour를 상속한다.
public class TitleBackgroundScroller : UpdatableBehaviour
{
    [SerializeField] private RawImage m_BackgroundImage;

    // 1초에 텍스처 폭의 몇 배만큼 흐를지. 0.02면 한 바퀴에 50초.
    [SerializeField] private float m_ScrollSpeed = 0.02f;

    public override void UpdateLogic()
    {
        if (m_BackgroundImage == null)
            return;

        Rect uvRect = m_BackgroundImage.uvRect;
        uvRect.x += m_ScrollSpeed * Time.deltaTime;

        // uv를 무한정 누적하면 float 정밀도가 떨어져 도트가 미세하게 떨린다. 한 바퀴 돌 때마다 되돌린다.
        if (uvRect.x >= 1f)
            uvRect.x -= 1f;

        m_BackgroundImage.uvRect = uvRect;
    }
}
