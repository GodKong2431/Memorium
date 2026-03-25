public static class InventoryTypeMapper
{
    // ItemType을 CurrencyType으로 변환 가능한지 검사한다.
    public static bool TryToCurrencyType(ItemType itemType, out CurrencyType currencyType)
    {
        switch (itemType)
        {
            case ItemType.FreeCurrency:
                currencyType = CurrencyType.Gold;
                return true;
            case ItemType.PaidCurrency:
                currencyType = CurrencyType.Crystal;
                return true;
            case ItemType.Key:
                currencyType = CurrencyType.DungeonTicket;
                return true;
            default:
                currencyType = default;
                return false;
        }
    }

    // ItemType을 EquipmentType으로 변환 가능한지 검사한다.
    public static bool TryToEquipmentType(ItemType itemType, out EquipmentType equipmentType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                equipmentType = EquipmentType.Weapon;
                return true;
            case ItemType.Helmet:
                equipmentType = EquipmentType.Helmet;
                return true;
            case ItemType.Glove:
                equipmentType = EquipmentType.Glove;
                return true;
            case ItemType.Armor:
                equipmentType = EquipmentType.Armor;
                return true;
            case ItemType.Boots:
                equipmentType = EquipmentType.Boots;
                return true;
            default:
                equipmentType = default;
                return false;
        }
    }
}
