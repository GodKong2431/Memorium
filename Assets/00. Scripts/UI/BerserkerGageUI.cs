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

        if (berserkerOrb != null)
        {
            berserkerOrb.OnBerserkerOrbChanged += Refresh;
            berserkerOrb.OnBerserkerOrbFull += OnBerserkerModeClicked;
        }
            

        BerserkerModeController.OnBerserkerModeChanged += _ => Refresh();

        Refresh();
    }

    private void OnDisable()
    {
        ActiveUis.Remove(this);

        if (berserkerOrb != null)
        {
            berserkerOrb.OnBerserkerOrbChanged -= Refresh;
            berserkerOrb.OnBerserkerOrbFull -= OnBerserkerModeClicked;
        }
            

        BerserkerModeController.OnBerserkerModeChanged -= _ => Refresh();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnBerserkerModeClicked);
    }

    private void Update()
    {
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
        ApplyGaugeVisual();

        if (_button == null)
            return;

        int current = berserkerOrb != null ? berserkerOrb.CurrentBerserkerOrb : 0;
        int max = PlayerBerserkerOrb.MaxBerserkerOrb;

        bool canUseMode =
            berserkerOrb != null &&
            berserkerModeController != null &&
            !berserkerModeController.IsActive &&
            current >= max;

        _button.interactable = canUseMode;
    }

    private void ApplyGaugeVisual()
    {
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

    private void OnBerserkerModeClicked()
    {
        if (berserkerOrb == null || berserkerModeController == null)
            return;

        if (berserkerModeController.IsActive)
            return;

        if (!berserkerOrb.TryConsumeBerserkerOrbs(PlayerBerserkerOrb.MaxBerserkerOrb))
            return;
        
        berserkerModeController.Activate();
    }
}
