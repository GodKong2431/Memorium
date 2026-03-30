using System;
using UnityEngine;

public static class IconManager
{
    public static StatIconSO StatIconSO = Resources.Load<StatIconSO>("Icons/StatIconSO");
    public static StoneIconSO StoneIconSO = Resources.Load<StoneIconSO>("Icons/StoneIconSO");
    public static CurrencyIconSO CurrencyIconSO = Resources.Load<CurrencyIconSO>("Icons/CurrencyIconSO");
    public static SynergyIconSO SynergyIconSO = Resources.Load<SynergyIconSO>("Icons/SynergyIconSO");

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
    
    public static Sprite GetSynergyIcon(SynergyStat synergyStat)
    {
        if (SynergyIconSO == null || SynergyIconSO.SynergyIcons == null)
            return null;
                
        return SynergyIconSO.SynergyIcons.TryGetValue(synergyStat, out var icon) ? icon : null;
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
        return GetResourceSprite(key);
    }

    public static Sprite GetItemIcon(ItemInfoTable table)
    {
        return table == null ? null : GetResourceSprite(table.itemIcon);
    }

    public static Sprite GetGachaTicketIcon(GachaType gachaType)
    {
        return GetGachaTicketIcon(ConvertToTicketType(gachaType));
    }

    public static Sprite GetGachaTicketIcon(TicketType ticketType)
    {
        if (!TryGetGachaTicketTable(ticketType, out GachaTicketTable table))
            return null;

        if (DataManager.Instance?.ItemInfoDict != null &&
            DataManager.Instance.ItemInfoDict.TryGetValue(table.ID, out ItemInfoTable itemInfo))
        {
            Sprite itemIcon = GetItemIcon(itemInfo);
            if (itemIcon != null)
                return itemIcon;
        }

        return GetResourceSprite(table.ticketResources);
    }

    public static Sprite GetResourceSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string normalizedKey = NormalizeResourcesPath(key);
        if (string.IsNullOrEmpty(normalizedKey))
            return null;

        return Resources.Load<Sprite>(normalizedKey);
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

    private static bool TryGetGachaTicketTable(TicketType ticketType, out GachaTicketTable table)
    {
        table = null;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.GachaTicketDict == null)
            return false;

        foreach (var pair in DataManager.Instance.GachaTicketDict)
        {
            GachaTicketTable current = pair.Value;
            if (current == null || current.ticketType != ticketType)
                continue;

            table = current;
            return true;
        }

        return false;
    }

    private static TicketType ConvertToTicketType(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Armor:
                return TicketType.Armor;
            case GachaType.SkillScroll:
                return TicketType.SkillScroll;
            case GachaType.Weapon:
            default:
                return TicketType.Weapon;
        }
    }
}
