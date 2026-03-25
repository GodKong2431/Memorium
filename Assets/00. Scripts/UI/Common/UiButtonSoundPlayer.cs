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
        Rebind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    public static UiButtonSoundPlayer Ensure(Button button, int targetSoundId)
    {
        if (button == null)
            return null;

        UiButtonSoundPlayer player = button.GetComponent<UiButtonSoundPlayer>();
        if (player == null)
            player = button.gameObject.AddComponent<UiButtonSoundPlayer>();

        player.SetSoundId(targetSoundId);
        player.Rebind();
        return player;
    }

    public void SetSoundId(int value)
    {
        soundId = value;
    }

    public void Rebind()
    {
        Button button = CachedButton;
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void Unbind()
    {
        if (cachedButton == null)
            return;

        cachedButton.onClick.RemoveListener(HandleClick);
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
