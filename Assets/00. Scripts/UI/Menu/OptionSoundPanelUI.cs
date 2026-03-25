using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OptionSoundPanelUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider combatSfxSlider;
    [SerializeField] private Slider uiSfxSlider;

    private SoundManager soundManager;

    private void Awake()
    {
        ConfigureSlider(masterSlider);
        ConfigureSlider(bgmSlider);
        ConfigureSlider(combatSfxSlider);
        ConfigureSlider(uiSfxSlider);
    }

    private void OnEnable()
    {
        soundManager = SoundManager.Instance;
        if (soundManager == null)
            return;

        RefreshValues();
        soundManager.OnVolumeChanged += RefreshValues;

        masterSlider?.onValueChanged.AddListener(HandleMasterChanged);
        bgmSlider?.onValueChanged.AddListener(HandleBgmChanged);
        combatSfxSlider?.onValueChanged.AddListener(HandleCombatChanged);
        uiSfxSlider?.onValueChanged.AddListener(HandleUiChanged);
    }

    private void OnDisable()
    {
        if (soundManager != null)
            soundManager.OnVolumeChanged -= RefreshValues;

        masterSlider?.onValueChanged.RemoveListener(HandleMasterChanged);
        bgmSlider?.onValueChanged.RemoveListener(HandleBgmChanged);
        combatSfxSlider?.onValueChanged.RemoveListener(HandleCombatChanged);
        uiSfxSlider?.onValueChanged.RemoveListener(HandleUiChanged);
    }

    private void RefreshValues()
    {
        if (soundManager == null)
            return;

        masterSlider?.SetValueWithoutNotify(soundManager.MasterVolume);
        bgmSlider?.SetValueWithoutNotify(soundManager.BgmVolume);
        combatSfxSlider?.SetValueWithoutNotify(soundManager.CombatSfxVolume);
        uiSfxSlider?.SetValueWithoutNotify(soundManager.UiSfxVolume);
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void HandleMasterChanged(float value)
    {
        soundManager?.SetMasterVolume(value);
    }

    private void HandleBgmChanged(float value)
    {
        soundManager?.SetBgmVolume(value);
    }

    private void HandleCombatChanged(float value)
    {
        soundManager?.SetCombatSfxVolume(value);
    }

    private void HandleUiChanged(float value)
    {
        soundManager?.SetUiSfxVolume(value);
    }
}
