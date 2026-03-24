using System;
using UnityEngine;

public static class IconManager
{
    public static StatIconSO StatIconSO = Resources.Load<StatIconSO>("Icons/StatIconSO");
    public static StoneIconSO StoneIconSO = Resources.Load<StoneIconSO>("Icons/StoneIconSO");
    public static CurrencyIconSO CurrencyIconSO = Resources.Load<CurrencyIconSO>("Icons/CurrencyIconSO");

    public static Sprite GetStatIcon(StatType statType)
    {
        if (StatIconSO == null || StatIconSO.StatIconDict == null)
            return null;

        return StatIconSO.StatIconDict.TryGetValue(statType, out var icon) ? icon : null;
    }

    public static Sprite GetCurrencyIcon(CurrencyType currencyType)
    {
        if (CurrencyIconSO == null || CurrencyIconSO.CurrencyIconDict == null)
            return null;

        return CurrencyIconSO.CurrencyIconDict.TryGetValue(currencyType, out var icon) ? icon : null;
    }

    public static Sprite GetEquipmentIcon(EquipListTable table)
    {
        if (table == null)
            return null;

        string key = string.IsNullOrEmpty(table.iconResource)
            ? table.equipmentName
            : table.iconResource;

        return GetEquipmentIcon(key);
    }

    public static Sprite GetEquipmentIcon(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string normalizedKey = NormalizeResourcesPath(key);
        if (string.IsNullOrEmpty(normalizedKey))
            return null;

        return Resources.Load<Sprite>(normalizedKey);
    }

    public static Sprite GetStatIcon(object key)
    {
        if (key is StatType statType)
            return GetStatIcon(statType);

        if (key is CurrencyType currencyType)
            return GetCurrencyIcon(currencyType);

        return null;
    }

    private static string NormalizeResourcesPath(string rawKey)
    {
        string path = rawKey.Trim().Replace('\\', '/');
        const string resourcesToken = "Resources/";

        int resourcesIndex = path.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
            path = path.Substring(resourcesIndex + resourcesToken.Length);

        int extensionIndex = path.LastIndexOf('.');
        if (extensionIndex > 0)
            path = path.Substring(0, extensionIndex);

        return path.TrimStart('/');
    }
}
