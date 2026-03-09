public readonly struct InventoryItemContext
{
    public int ItemId { get; } // 인벤토리에서 다룰 실제 아이템 ID.
    public ItemType ItemType { get; } // 테이블에서 해석된 아이템 분류 타입.

    public InventoryItemContext(int itemId, ItemType itemType)
    {
        ItemId = itemId;
        ItemType = itemType;
    }
}
