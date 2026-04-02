using System;
using System.Globalization;
using DamageNumbersPro;
using UnityEngine;

public class DamageIndicatorService : Singleton<DamageIndicatorService>
{
    private const double BigDoubleThresholdAsDouble = 1_000_000d;

    private enum IndicatorVariant
    {
        Normal,
        Critical
    }

    [Header("Prefabs")]
    [SerializeField] private DamageNumberMesh normalPrefab;
    [SerializeField] private DamageNumberMesh criticalPrefab;

    [Header("Spawn")]
    [SerializeField] private float worldYOffset = 0.35f;
    [SerializeField] private float minimumDisplayDamage = 0.01f;
    [SerializeField] private bool forceWhiteText = true;

    [Header("Pooling")]
    [SerializeField] private bool preloadOnAwake = true;
    [SerializeField, Min(0)] private int normalPrewarmCount = 20;
    [SerializeField, Min(0)] private int criticalPrewarmCount = 8;

    [Header("Formatting")]
    [SerializeField] private string bigDoubleFormat = "F0";

    private bool normalPrewarmQueued;
    private bool criticalPrewarmQueued;
    private bool normalPrewarmed;
    private bool criticalPrewarmed;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        Preload();
        if (preloadOnAwake)
            Prewarm();
    }

    public void ShowDamage(Transform target, float damage, bool isCritical = false)
    {
        if (target == null || !IsFinite(damage) || damage <= minimumDisplayDamage)
            return;

        ShowDamageAt(ResolveSpawnPosition(target), damage, isCritical);
    }

    public void ShowDamage(Transform target, BigDouble damage, bool isCritical = false)
    {
        if (target == null || damage <= BigDouble.Zero)
            return;

        ShowDamageAt(ResolveSpawnPosition(target), damage, isCritical);
    }

    public void ShowDamageAt(Vector3 worldPosition, float damage, bool isCritical = false)
    {
        if (!IsFinite(damage) || damage <= minimumDisplayDamage)
            return;

        SpawnIndicator(worldPosition, FormatDamage(damage), isCritical);
    }

    public void ShowDamageAt(Vector3 worldPosition, BigDouble damage, bool isCritical = false)
    {
        if (damage <= BigDouble.Zero)
            return;

        SpawnIndicator(worldPosition, FormatDamage(damage), isCritical);
    }

    public void Preload()
    {
        ValidatePrefab(IndicatorVariant.Normal, normalPrefab);
        ValidatePrefab(IndicatorVariant.Critical, criticalPrefab);
    }

    public void Prewarm()
    {
        BeginPrewarm(IndicatorVariant.Normal, normalPrefab, normalPrewarmCount);
        BeginPrewarm(IndicatorVariant.Critical, criticalPrefab, criticalPrewarmCount);
    }

    private void SpawnIndicator(Vector3 worldPosition, string text, bool isCritical)
    {
        if (string.IsNullOrEmpty(text))
            return;

        DamageNumberMesh popupPrefab = isCritical ? criticalPrefab : normalPrefab;
        if (!ValidatePrefab(isCritical ? IndicatorVariant.Critical : IndicatorVariant.Normal, popupPrefab))
            return;

        SpawnPopup(popupPrefab, worldPosition, text);
    }

    private void SpawnPopup(DamageNumberMesh popupPrefab, Vector3 worldPosition, string text)
    {
        if (popupPrefab == null)
            return;

        popupPrefab.enablePooling = true;
        DamageNumber popup = popupPrefab.Spawn(worldPosition, text);
        if (popup == null || !forceWhiteText)
            return;

        popup.SetColor(Color.white);
    }

    private void BeginPrewarm(IndicatorVariant variant, DamageNumberMesh popupPrefab, int count)
    {
        if (count <= 0 || popupPrefab == null || IsPrewarmPendingOrComplete(variant))
            return;

        SetPrewarmQueued(variant, true);
        CompletePrewarm(variant, popupPrefab, count);
    }

    private void CompletePrewarm(IndicatorVariant variant, DamageNumberMesh popupPrefab, int count)
    {
        SetPrewarmQueued(variant, false);

        if (popupPrefab == null)
            return;

        popupPrefab.enablePooling = true;
        popupPrefab.poolSize = Mathf.Max(popupPrefab.poolSize, count);
        popupPrefab.PrewarmPool();

        SetPrewarmed(variant, true);
    }

    private bool ValidatePrefab(IndicatorVariant variant, DamageNumberMesh popupPrefab)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning($"[DamageIndicatorService] {variant} prefab is not assigned.");
            return false;
        }

        return true;
    }

    private Vector3 ResolveSpawnPosition(Transform target)
    {
        if (TryGetTopPosition(target, out Vector3 topPosition))
            return topPosition + Vector3.up * worldYOffset;

        return target.position + Vector3.up * worldYOffset;
    }

    private static bool TryGetTopPosition(Transform target, out Vector3 topPosition)
    {
        if (target.TryGetComponent(out Collider collider))
        {
            topPosition = GetTopPosition(collider.bounds);
            return true;
        }

        if (target.TryGetComponent(out Renderer renderer))
        {
            topPosition = GetTopPosition(renderer.bounds);
            return true;
        }

        Collider childCollider = target.GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            topPosition = GetTopPosition(childCollider.bounds);
            return true;
        }

        Renderer childRenderer = target.GetComponentInChildren<Renderer>();
        if (childRenderer != null)
        {
            topPosition = GetTopPosition(childRenderer.bounds);
            return true;
        }

        topPosition = default;
        return false;
    }

    private static Vector3 GetTopPosition(Bounds bounds)
    {
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private string FormatDamage(float damage)
    {
        double rounded = Math.Max(1d, Math.Round(damage, 0, MidpointRounding.AwayFromZero));
        if (rounded < BigDoubleThresholdAsDouble)
            return rounded.ToString("N0", CultureInfo.InvariantCulture);

        return new BigDouble(rounded).ToString(bigDoubleFormat);
    }

    private string FormatDamage(BigDouble damage)
    {
        double asDouble = damage.ToDouble();
        if (IsFinite(asDouble) && asDouble < BigDoubleThresholdAsDouble)
        {
            double rounded = Math.Max(1d, Math.Round(asDouble, 0, MidpointRounding.AwayFromZero));
            return rounded.ToString("N0", CultureInfo.InvariantCulture);
        }

        return damage.ToString(bigDoubleFormat);
    }

    private bool IsPrewarmPendingOrComplete(IndicatorVariant variant)
    {
        return variant switch
        {
            IndicatorVariant.Normal => normalPrewarmQueued || normalPrewarmed,
            IndicatorVariant.Critical => criticalPrewarmQueued || criticalPrewarmed,
            _ => false
        };
    }

    private void SetPrewarmQueued(IndicatorVariant variant, bool value)
    {
        switch (variant)
        {
            case IndicatorVariant.Normal:
                normalPrewarmQueued = value;
                break;
            case IndicatorVariant.Critical:
                criticalPrewarmQueued = value;
                break;
        }
    }

    private void SetPrewarmed(IndicatorVariant variant, bool value)
    {
        switch (variant)
        {
            case IndicatorVariant.Normal:
                normalPrewarmed = value;
                break;
            case IndicatorVariant.Critical:
                criticalPrewarmed = value;
                break;
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
