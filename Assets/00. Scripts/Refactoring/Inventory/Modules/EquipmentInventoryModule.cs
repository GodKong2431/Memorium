using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class EquipmentInventoryModule : IInventoryModule
{
    private readonly Dictionary<int, int> equipmentCountByItemId = new Dictionary<int, int>(); // 장비 아이템 ID별 보유 수량.
    private readonly List<int> allEquipmentItemIds = new List<int>(); // 장비 테이블 기준 전체 아이템 키(오름차순).
    private readonly List<int> finalEquipmentItemIds = new List<int>(); // 각 부위별 최종 티어 아이템 키.

    private EquipmentHandler equipmentHandler; // 자동합성/자동장착 버튼 제어를 위한 핸들러.
    private PlayerInventory inventoryView; // 장비 인벤토리 UI 렌더링 전용 뷰.

    #region IInventoryModule
    // 장비 타입만 이 모듈에서 처리한다.
    public bool CanHandle(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
            case ItemType.Helmet:
            case ItemType.Glove:
            case ItemType.Armor:
            case ItemType.Boots:
                return true;
            default:
                return false;
        }
    }

    // 허브를 통해 들어온 장비 아이템 추가 요청을 처리한다.
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (!TryConvertAmountToInt(amount, out int count))
            return false;

        IncreaseEquipment(item.ItemId, count);
        return true;
    }

    // 허브를 통해 들어온 장비 아이템 차감 요청을 처리한다.
    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (!TryConvertAmountToInt(amount, out int count))
            return false;
        if (!equipmentCountByItemId.TryGetValue(item.ItemId, out int ownedCount))
            return false;
        if (ownedCount < count)
            return false;

        DecreaseEquipment(item.ItemId, count);
        return true;
    }

    // 허브에서 조회할 장비 수량을 반환한다.
    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!CanHandle(item.ItemType))
            return BigDouble.Zero;

        return equipmentCountByItemId.TryGetValue(item.ItemId, out int count)
            ? new BigDouble(count)
            : BigDouble.Zero;
    }
    #endregion

    // 장비 핸들러/뷰/초기 보유 데이터를 연결하고 장비 UI를 구성한다.
    public bool Setup(EquipmentHandler handler, PlayerInventory view, Dictionary<int, int> initialCountByItemId)
    {
        equipmentHandler = handler;
        inventoryView = view;

        equipmentCountByItemId.Clear();
        if (initialCountByItemId != null)
        {
            foreach (var pair in initialCountByItemId)
            {
                if (pair.Value <= 0)
                    continue;

                equipmentCountByItemId[pair.Key] = pair.Value;
            }
        }

        RebuildEquipmentItemCache();
        RefreshFinalEquipment();
        inventoryView?.BuildEquipmentInventory(allEquipmentItemIds, equipmentCountByItemId);
        RefreshAutoMergeInteractable();
        return true;
    }

    // 장비 자동 합성을 실행한다.
    public bool RunAutoMerge()
    {
        if (!EnsureEquipmentTable())
            return false;

        bool mergedAny = false;
        for (int i = 0; i < allEquipmentItemIds.Count; i++)
        {
            int itemId = allEquipmentItemIds[i];
            if (finalEquipmentItemIds.Contains(itemId))
                continue;
            if (!equipmentCountByItemId.TryGetValue(itemId, out int ownedCount))
                continue;
            if (ownedCount < 3)
                continue;
            if (i + 1 >= allEquipmentItemIds.Count)
                continue;

            int nextItemId = allEquipmentItemIds[i + 1];
            if (!IsSameEquipmentType(itemId, nextItemId))
                continue;

            int mergedCount = ownedCount / 3;
            if (mergedCount <= 0)
                continue;

            int remainCount = ownedCount % 3;
            if (remainCount <= 0)
                equipmentCountByItemId.Remove(itemId);
            else
                equipmentCountByItemId[itemId] = remainCount;

            AddRawCount(nextItemId, mergedCount);
            SyncSlot(itemId);
            SyncSlot(nextItemId);
            mergedAny = true;
        }

        if (!mergedAny)
            return false;

        RefreshFinalEquipment();
        RefreshAutoMergeInteractable();
        equipmentHandler?.CheckAutoEquip();
        return true;
    }

    // 자동 합성 가능 여부를 계산해 버튼 상태를 갱신한다.
    public bool RefreshAutoMergeInteractable()
    {
        if (equipmentHandler == null)
            return false;

        bool canMerge = false;
        foreach (var pair in equipmentCountByItemId)
        {
            if (finalEquipmentItemIds.Contains(pair.Key))
                continue;
            if (pair.Value < 3)
                continue;

            canMerge = true;
            break;
        }

        equipmentHandler.SetAutoMergeButtonInteractable(canMerge);
        return canMerge;
    }

    // 타입별 최상위 장비 ID를 반환한다.
    public int GetBestEquipmentId(EquipmentType equipmentType)
    {
        if (DataManager.Instance == null)
            return 0;

        List<int> keys = GetEquipmentKeysByType(equipmentType);
        if (keys == null || keys.Count == 0)
            return 0;

        keys.Sort();
        keys.Reverse();

        for (int i = 0; i < keys.Count; i++)
        {
            int key = keys[i];
            if (equipmentCountByItemId.ContainsKey(key))
                return key;
        }

        return 0;
    }

    // 각 부위별 최종 티어 키를 갱신한다.
    public void RefreshFinalEquipment()
    {
        finalEquipmentItemIds.Clear();
        if (!EnsureEquipmentTable())
            return;

        int count = Enum.GetNames(typeof(EquipmentType)).Length;
        int[] highestTierByType = new int[count];
        int[] itemIdByType = new int[count];

        for (int i = 0; i < allEquipmentItemIds.Count; i++)
        {
            int itemId = allEquipmentItemIds[i];
            var equipInfo = DataManager.Instance.EquipListDict[itemId];
            int typeIndex = (int)equipInfo.equipmentType - (int)EquipmentType.Weapon;
            if (typeIndex < 0 || typeIndex >= count)
                continue;

            if (equipInfo.equipmentTier <= highestTierByType[typeIndex])
                continue;

            highestTierByType[typeIndex] = equipInfo.equipmentTier;
            itemIdByType[typeIndex] = itemId;
        }

        for (int i = 0; i < itemIdByType.Length; i++)
        {
            if (itemIdByType[i] != 0)
                finalEquipmentItemIds.Add(itemIdByType[i]);
        }
    }

    // 장비 아이템 개수를 증가시키고 UI/버튼 상태를 동기화한다.
    private void IncreaseEquipment(int itemId, int count)
    {
        if (count <= 0)
            return;

        AddRawCount(itemId, count);
        SyncSlot(itemId);
        RefreshAutoMergeInteractable();
        equipmentHandler?.CheckAutoEquip();
    }

    // 장비 아이템 개수를 감소시키고 UI/버튼 상태를 동기화한다.
    private void DecreaseEquipment(int itemId, int count)
    {
        if (count <= 0)
            return;
        if (!equipmentCountByItemId.TryGetValue(itemId, out int owned))
            return;

        int nextCount = Mathf.Max(0, owned - count);
        if (nextCount <= 0)
            equipmentCountByItemId.Remove(itemId);
        else
            equipmentCountByItemId[itemId] = nextCount;

        SyncSlot(itemId);
        RefreshAutoMergeInteractable();
        equipmentHandler?.CheckAutoEquip();
    }

    // 수량 딕셔너리에 원시 개수를 누적한다.
    private void AddRawCount(int itemId, int count)
    {
        if (count <= 0)
            return;

        if (equipmentCountByItemId.TryGetValue(itemId, out int owned))
            equipmentCountByItemId[itemId] = owned + count;
        else
            equipmentCountByItemId[itemId] = count;
    }

    // 특정 슬롯 UI를 현재 보유 수량으로 갱신한다.
    private void SyncSlot(int itemId)
    {
        if (inventoryView == null)
            return;

        int count = equipmentCountByItemId.TryGetValue(itemId, out int owned) ? owned : 0;
        inventoryView.UpdateEquipmentCount(itemId, count);
    }

    // 장비 테이블 캐시를 갱신한다.
    private void RebuildEquipmentItemCache()
    {
        allEquipmentItemIds.Clear();
        if (!EnsureEquipmentTable())
            return;

        allEquipmentItemIds.AddRange(DataManager.Instance.EquipListDict.Keys);
        allEquipmentItemIds.Sort();
    }

    // 장비 테이블 접근 가능 상태인지 확인한다.
    private static bool EnsureEquipmentTable()
    {
        return DataManager.Instance != null && DataManager.Instance.EquipListDict != null;
    }

    // 두 아이템이 같은 장비 부위인지 검사한다.
    private static bool IsSameEquipmentType(int lhsItemId, int rhsItemId)
    {
        if (!EnsureEquipmentTable())
            return false;
        if (!DataManager.Instance.EquipListDict.TryGetValue(lhsItemId, out var lhsInfo))
            return false;
        if (!DataManager.Instance.EquipListDict.TryGetValue(rhsItemId, out var rhsInfo))
            return false;

        return lhsInfo.equipmentType == rhsInfo.equipmentType;
    }

    // 장비 타입별 테이블 키 목록을 반환한다.
    private static List<int> GetEquipmentKeysByType(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                return DataManager.Instance.EquipWeaponDict?.Keys.ToList();
            case EquipmentType.Helmet:
                return DataManager.Instance.EquipHelmetDict?.Keys.ToList();
            case EquipmentType.Glove:
                return DataManager.Instance.EquipGloveDict?.Keys.ToList();
            case EquipmentType.Armor:
                return DataManager.Instance.EquipArmorDict?.Keys.ToList();
            case EquipmentType.Boots:
                return DataManager.Instance.EquipBootsDict?.Keys.ToList();
            default:
                return null;
        }
    }

    // BigDouble 수량을 장비 수량(int)으로 변환한다.
    private static bool TryConvertAmountToInt(BigDouble amount, out int count)
    {
        count = 0;
        if (amount <= BigDouble.Zero)
            return false;

        double value = amount.ToDouble();
        if (double.IsNaN(value) || double.IsInfinity(value))
            return false;

        double floored = Math.Floor(value);
        if (floored < 1 || floored > int.MaxValue)
            return false;

        count = (int)floored;
        return true;
    }
}
