using UnityEngine;
using UnityEngine.UI;

public class UIButton : Button
{
    [SerializeField] private string m_ClickSoundKey = string.Empty;

    protected override void Awake()
    {
        base.Awake();
        onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        BaseScene.Current?.PlaySfx(m_ClickSoundKey);
    }
}
