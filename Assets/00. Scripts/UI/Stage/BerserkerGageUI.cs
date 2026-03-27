using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DefaultExecutionOrder(100)]
public class BerserkerGageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerBerserkerOrb berserkerOrb;
    [SerializeField] private BerserkerModeController berserkerModeController;

    [Header("Colors")]
    [SerializeField] private Color chargingColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color readyColor = new Color(1f, 0.35f, 0.2f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.15f, 0.15f, 1f);

    private static readonly HashSet<BerserkerGageUI> ActiveUis = new HashSet<BerserkerGageUI>();

    private Button _button;
    private Color _resolvedChargingColor;
    private bool _hasResolvedChargingColor;
    private bool _hasReadyState;
    private bool _wasReady;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnBerserkerModeClicked);
            _button.onClick.AddListener(OnBerserkerModeClicked);
        }

        ResolveChargingColor();
    }

    private void OnEnable()
    {
        ActiveUis.Add(this);
        EnsureRuntimeTargets();
        BindBerserkerOrbEvents();
        BerserkerModeController.OnBerserkerModeChanged += OnBerserkerModeChanged;

        Refresh();
    }

    private void OnDisable()
    {
        ActiveUis.Remove(this);
        UnbindBerserkerOrbEvents();
        BerserkerModeController.OnBerserkerModeChanged -= OnBerserkerModeChanged;
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnBerserkerModeClicked);
    }

    private void Update()
    {
        EnsureRuntimeTargets();

        if (berserkerModeController != null && berserkerModeController.IsActive)
            ApplyGaugeVisual();
    }

    public static void RefreshAll()
    {
        foreach (var ui in ActiveUis)
            ui.Refresh();
    }

    private void Refresh()
    {
        EnsureRuntimeTargets();
        ApplyGaugeVisual();

        if (_button == null)
            return;

        int current = berserkerOrb != null ? berserkerOrb.CurrentBerserkerOrb : 0;
        int max = PlayerBerserkerOrb.MaxBerserkerOrb;
        bool isReady = max > 0 && current >= max && (berserkerModeController == null || !berserkerModeController.IsActive);

        UpdateReadySoundState(isReady);

        bool canUseMode =
            berserkerOrb != null &&
            berserkerModeController != null &&
            !berserkerModeController.IsActive &&
            current >= max;

        _button.interactable = canUseMode;
    }

    private void ApplyGaugeVisual()
    {
        EnsureRuntimeTargets();

        if (fillImage == null)
            return;

        ResolveChargingColor();

        if (berserkerModeController != null && berserkerModeController.IsActive)
        {
            fillImage.fillAmount = berserkerModeController.RemainingDurationNormalized;
            fillImage.color = activeColor;
            return;
        }

        int current = berserkerOrb != null ? berserkerOrb.CurrentBerserkerOrb : 0;
        int max = PlayerBerserkerOrb.MaxBerserkerOrb;
        float normalized = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        fillImage.fillAmount = normalized;
        fillImage.color = normalized >= 1f ? readyColor : _resolvedChargingColor;
    }

    private void ResolveChargingColor()
    {
        if (_hasResolvedChargingColor)
            return;

        if (chargingColor.a > 0f)
        {
            _resolvedChargingColor = chargingColor;
        }
        else if (fillImage != null)
        {
            _resolvedChargingColor = fillImage.color;
        }
        else
        {
            _resolvedChargingColor = Color.white;
        }

        _hasResolvedChargingColor = true;
    }

    private void OnBerserkerModeChanged(bool _)
    {
        Refresh();
    }

    private void OnBerserkerModeClicked()
    {
        TryActivate(playClickSound: true);
    }

    private void OnBerserkerModeAutoTriggered()
    {
        TryActivate(playClickSound: false);
    }

    private void TryActivate(bool playClickSound)
    {
        EnsureRuntimeTargets();

        if (berserkerOrb == null || berserkerModeController == null)
            return;

        if (berserkerModeController.IsActive)
            return;

        if (!berserkerOrb.TryConsumeBerserkerOrbs(PlayerBerserkerOrb.MaxBerserkerOrb))
            return;

        if (playClickSound && SoundManager.Instance != null)
            SoundManager.Instance.PlayCombatSfx(UiSoundIds.BerserkerActivate);

        berserkerModeController.Activate();
    }

    private void UpdateReadySoundState(bool isReady)
    {
        if (!_hasReadyState)
        {
            _hasReadyState = true;
            _wasReady = isReady;
            return;
        }

        if (!_wasReady && isReady && SoundManager.Instance != null)
            SoundManager.Instance.PlayCombatSfx(UiSoundIds.BerserkerGaugeReady);

        _wasReady = isReady;
    }

    private void EnsureRuntimeTargets()
    {
        PlayerBerserkerOrb runtimeOrb = PlayerBerserkerOrb.Instance;
        if (runtimeOrb != null && !ReferenceEquals(berserkerOrb, runtimeOrb))
        {
            UnbindBerserkerOrbEvents();
            berserkerOrb = runtimeOrb;
            BindBerserkerOrbEvents();
        }

        if (berserkerModeController == null || !ReferenceEquals(berserkerModeController, BerserkerModeController.Instance))
            berserkerModeController = BerserkerModeController.Instance;
    }

    private void BindBerserkerOrbEvents()
    {
        if (berserkerOrb == null)
            return;

        berserkerOrb.OnBerserkerOrbChanged -= Refresh;
        berserkerOrb.OnBerserkerOrbFull -= OnBerserkerModeAutoTriggered;
        berserkerOrb.OnBerserkerOrbChanged += Refresh;
        berserkerOrb.OnBerserkerOrbFull += OnBerserkerModeAutoTriggered;
    }

    private void UnbindBerserkerOrbEvents()
    {
        if (berserkerOrb == null)
            return;

        berserkerOrb.OnBerserkerOrbChanged -= Refresh;
        berserkerOrb.OnBerserkerOrbFull -= OnBerserkerModeAutoTriggered;
    }
}
