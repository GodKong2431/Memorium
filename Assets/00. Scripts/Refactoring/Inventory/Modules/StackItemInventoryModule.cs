using System;
using System.Collections.Generic;

public sealed class StackItemInventoryModule : IInventoryModule
{
    private readonly Dictionary<int, BigDouble> amountByItemId = new Dictionary<int, BigDouble>(); // 스택 아이템 ID별 수량.

    #region IInventoryModule
    // 재화/장비/스킬주문서를 제외한 일반 스택형 아이템을 처리한다.
    public bool CanHandle(ItemType itemType)
    {
        if (InventoryTypeMapper.TryToCurrencyType(itemType, out _))
            return false;
        if (InventoryTypeMapper.TryToEquipmentType(itemType, out _))
            return false;
        if (itemType == ItemType.SkillScroll)
            return false;

        return true;
    }

    // 스택형 아이템 수량을 증가시킨다.
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (amount <= BigDouble.Zero)
            return false;

        EnsureItemKey(item.ItemId);
        amountByItemId[item.ItemId] = amountByItemId[item.ItemId] + amount;
        return true;
    }

    // 스택형 아이템 수량을 차감한다.
    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (amount <= BigDouble.Zero)
            return false;

        EnsureItemKey(item.ItemId);
        if (amountByItemId[item.ItemId] < amount)
            return false;

        amountByItemId[item.ItemId] = amountByItemId[item.ItemId] - amount;
        if (amountByItemId[item.ItemId] <= BigDouble.Zero)
            amountByItemId.Remove(item.ItemId);

        return true;
    }

    // 스택형 아이템 현재 수량을 반환한다.
    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!CanHandle(item.ItemType))
            return BigDouble.Zero;

        return amountByItemId.TryGetValue(item.ItemId, out var amount)
            ? amount
            : BigDouble.Zero;
    }
    #endregion

    // 키가 없으면 0 수량으로 초기화한다.
    private void EnsureItemKey(int itemId)
    {
        if (!amountByItemId.ContainsKey(itemId))
            amountByItemId[itemId] = BigDouble.Zero;
    }
}
