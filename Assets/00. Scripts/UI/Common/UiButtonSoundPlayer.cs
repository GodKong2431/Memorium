using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UiButtonSoundPlayer : MonoBehaviour
{
    [SerializeField] private int soundId;

    private Button cachedButton;

    public int SoundId => soundId;

    private Button CachedButton => cachedButton != null ? cachedButton : cachedButton = GetComponent<Button>();

    private void OnEnable()
    {
        CachedButton.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        CachedButton.onClick.RemoveListener(HandleClick);
    }

    public void SetSoundId(int value)
    {
        soundId = value;
    }

    private void HandleClick()
    {
        if (soundId <= 0)
            return;

        SoundManager soundManager = SoundManager.Instance;
        if (soundManager == null)
            return;

        soundManager.PlayUiSfx(soundId);
    }
}
