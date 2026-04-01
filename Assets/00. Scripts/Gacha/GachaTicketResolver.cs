public static class GachaTicketResolver
{
    public static bool TryGetTicketTable(GachaType gachaType, out GachaTicketTable table)
    {
        return TryGetTicketTable(ConvertToTicketType(gachaType), out table);
    }

    public static bool TryGetTicketTable(TicketType ticketType, out GachaTicketTable table)
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

    public static int GetTicketItemId(GachaType gachaType)
    {
        return TryGetTicketTable(gachaType, out GachaTicketTable table) ? table.ID : 0;
    }

    public static BigDouble GetOwnedTicketAmount(GachaType gachaType)
    {
        int ticketItemId = GetTicketItemId(gachaType);
        if (ticketItemId <= 0 || InventoryManager.Instance == null)
            return BigDouble.Zero;

        return InventoryManager.Instance.GetItemAmount(ticketItemId);
    }

    public static TicketType ConvertToTicketType(GachaType gachaType)
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

    public static CurrencyType GetLegacyCurrencyType(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Armor:
                return CurrencyType.ArmorDrawTicket;
            case GachaType.SkillScroll:
                return CurrencyType.SkillScrollDrawTicket;
            case GachaType.Weapon:
            default:
                return CurrencyType.WeaponDrawTicket;
        }
    }
}
