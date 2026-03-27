using System;
using UnityEngine;

public static class GameOptionSettings
{
    private const string HidePixieDebuffEffectKey = "option.hidePixieDebuffEffect";
    private const string SkipGachaCrystalPopupKey = "option.skipGachaCrystalPopup";
    private const string UseManualBerserkerModeKey = "option.useManualBerserkerMode";

    private static bool? hidePixieDebuffEffect;
    private static bool? skipGachaCrystalPopup;
    private static bool? useManualBerserkerMode;

    public static event Action<bool> HidePixieDebuffEffectChanged;
    public static event Action<bool> SkipGachaCrystalPopupChanged;
    public static event Action<bool> UseManualBerserkerModeChanged;

    public static bool HidePixieDebuffEffect
    {
        get => hidePixieDebuffEffect ??= LoadBool(HidePixieDebuffEffectKey, false);
        set => SetBool(ref hidePixieDebuffEffect, HidePixieDebuffEffectKey, value, HidePixieDebuffEffectChanged);
    }

    public static bool SkipGachaCrystalPopup
    {
        get => skipGachaCrystalPopup ??= LoadBool(SkipGachaCrystalPopupKey, false);
        set => SetBool(ref skipGachaCrystalPopup, SkipGachaCrystalPopupKey, value, SkipGachaCrystalPopupChanged);
    }

    public static bool UseManualBerserkerMode
    {
        get => useManualBerserkerMode ??= LoadBool(UseManualBerserkerModeKey, true);
        set => SetBool(ref useManualBerserkerMode, UseManualBerserkerModeKey, value, UseManualBerserkerModeChanged);
    }

    private static bool LoadBool(string key, bool defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    private static void SetBool(ref bool? cache, string key, bool value, Action<bool> changed)
    {
        if (cache.HasValue && cache.Value == value)
            return;

        cache = value;
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        changed?.Invoke(value);
    }
}
