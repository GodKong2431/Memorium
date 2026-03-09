using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public sealed class EquipmentInventoryModule : IInventoryModule
{
    // itemId -> 보유 개수
    //private readonly Dictionary<int, int> equipmentCountByItemId = new Dictionary<int, int>();
    private readonly Dictionary<int, EquipmentData> equipmentByItemId = new Dictionary<int, EquipmentData>();
    // 한 번이라도 획득(해금)한 장비 ID
    private readonly HashSet<int> unlockedItemIds = new HashSet<int>();
    // 장비 테이블 전체 ID 캐시(오름차순)
    private readonly List<int> allEquipmentItemIds = new List<int>();
    // 부위별 최종 티어 ID 캐시
    private readonly List<int> finalEquipmentItemIds = new List<int>();

    public bool IsInitialized { get; private set; }

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

    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (!TryConvertAmountToInt(amount, out int count))
            return false;

        IncreaseEquipment(item.ItemId, count);
        return true;
    }

    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (!TryConvertAmountToInt(amount, out int count))
            return false;
        if (!equipmentByItemId.TryGetValue(item.ItemId, out EquipmentData ownedCount))
            return false;
        if (ownedCount.equipmentValue < count)
            return false;

        DecreaseEquipment(item.ItemId, count);
        return true;
    }

    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!CanHandle(item.ItemType))
            return BigDouble.Zero;

        return equipmentByItemId.TryGetValue(item.ItemId, out EquipmentData count)
            ? new BigDouble(count.equipmentValue)
            : BigDouble.Zero;
    }

    public bool Setup(Dictionary<int, EquipmentData> initialCountByItemId)
    {
        // 세이브 데이터를 모듈 상태로 복원한다.
        IsInitialized = false;

        equipmentByItemId.Clear();
        unlockedItemIds.Clear();
        if (initialCountByItemId != null)
        {
            foreach (KeyValuePair<int, EquipmentData> pair in initialCountByItemId)
            {
                unlockedItemIds.Add(pair.Key);
                if (pair.Value.equipmentValue <= 0)
                    continue;

                equipmentByItemId[pair.Key] = pair.Value;
            }
        }

        RebuildEquipmentItemCache();
        RefreshFinalEquipment();
        IsInitialized = true;
        return true;
    }

    public bool RunAutoMerge()
    {
        if (!EnsureEquipmentTable())
            return false;
        InventoryManager manager = InventoryManager.Instance;
        if (manager == null)
            return false;

        // 합성은 InventoryManager 경유로 처리해 저장/이벤트를 공통 경로로 통일한다.
        bool mergedAny = false;
        for (int i = 0; i < allEquipmentItemIds.Count; i++)
        {
            int itemId = allEquipmentItemIds[i];
            if (finalEquipmentItemIds.Contains(itemId))
                continue;
            if (!equipmentByItemId.TryGetValue(itemId, out EquipmentData ownedCount))
                continue;
            if (ownedCount.equipmentValue < 3)
                continue;
            if (i + 1 >= allEquipmentItemIds.Count)
                continue;

            int nextItemId = allEquipmentItemIds[i + 1];
            if (!IsSameEquipmentType(itemId, nextItemId))
                continue;

            int mergedCount = ownedCount.equipmentValue / 3;
            if (mergedCount <= 0)
                continue;

            int consumeCount = mergedCount * 3;
            if (!manager.RemoveItem(itemId, consumeCount))
                continue;

            if (!manager.AddItem(nextItemId, mergedCount))
            {
                manager.AddItem(itemId, consumeCount);
                continue;
            }

            mergedAny = true;
        }

        if (!mergedAny)
            return false;

        RefreshFinalEquipment();
        return true;
    }

    public bool CanAutoMerge()
    {
        foreach (KeyValuePair<int, EquipmentData> pair in equipmentByItemId)
        {
            if (finalEquipmentItemIds.Contains(pair.Key))
                continue;
            if (pair.Value.equipmentValue < 3)
                continue;

            return true;
        }

        return false;
    }

    public bool IsUnlocked(int itemId)
    {
        return unlockedItemIds.Contains(itemId);
    }

    public int GetBestEquipmentId(EquipmentType equipmentType)
    {
        if (!EnsureEquipmentTable())
            return 0;

        // 현재 보유 장비 중 랭크가 가장 높은 ID를 선택한다.
        int bestItemId = 0;

        foreach (KeyValuePair<int, EquipmentData> pair in equipmentByItemId)
        {
            if (pair.Value.equipmentValue <= 0)
                continue;
            if (!DataManager.Instance.EquipListDict.TryGetValue(pair.Key, out EquipListTable equipInfo))
                continue;
            if (equipInfo.equipmentType != equipmentType)
                continue;

            if (bestItemId == 0 || CompareEquipmentRank(pair.Key, bestItemId) > 0)
                bestItemId = pair.Key;
        }

        return bestItemId;
    }

    public void RefreshFinalEquipment()
    {
        // 부위별 최종 티어 장비를 캐시해 자동합성 제외 대상으로 사용한다.
        finalEquipmentItemIds.Clear();
        if (!EnsureEquipmentTable())
            return;

        int typeCount = Enum.GetNames(typeof(EquipmentType)).Length;
        int[] highestTierByType = new int[typeCount];
        int[] itemIdByType = new int[typeCount];

        for (int i = 0; i < allEquipmentItemIds.Count; i++)
        {
            int itemId = allEquipmentItemIds[i];
            EquipListTable equipInfo = DataManager.Instance.EquipListDict[itemId];
            int typeIndex = (int)equipInfo.equipmentType - (int)EquipmentType.Weapon;
            if (typeIndex < 0 || typeIndex >= typeCount)
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

    private void IncreaseEquipment(int itemId, int count)
    {
        if (count <= 0)
            return;

        AddRawCount(itemId, count);
    }

    private void DecreaseEquipment(int itemId, int count)
    {
        if (count <= 0)
            return;
        if (!equipmentByItemId.TryGetValue(itemId, out EquipmentData owned))
            return;

        int nextCount = Mathf.Max(0, owned.equipmentValue - count);
        if (nextCount <= 0)
            equipmentByItemId.Remove(itemId);
        else
        {
            EquipmentData equipmentData = owned;
            equipmentData.equipmentValue = nextCount;
            //equipmentByItemId[itemId] = nextCount;
            equipmentByItemId[itemId] = equipmentData;
        }
    }

    private void AddRawCount(int itemId, int count)
    {
        if (count <= 0)
            return;

        unlockedItemIds.Add(itemId);

        if (equipmentByItemId.TryGetValue(itemId, out EquipmentData owned))
        { 
            EquipmentData equipmentData = owned;
            equipmentData.equipmentValue += count;
            equipmentByItemId[itemId] = equipmentData;
        }

        else
            equipmentByItemId[itemId] = new EquipmentData(itemId,count);
    }

    private void RebuildEquipmentItemCache()
    {
        allEquipmentItemIds.Clear();
        if (!EnsureEquipmentTable())
            return;

        allEquipmentItemIds.AddRange(DataManager.Instance.EquipListDict.Keys);
        allEquipmentItemIds.Sort();
    }

    private static bool EnsureEquipmentTable()
    {
        return DataManager.Instance != null && DataManager.Instance.EquipListDict != null;
    }

    private static bool IsSameEquipmentType(int lhsItemId, int rhsItemId)
    {
        if (!EnsureEquipmentTable())
            return false;
        if (!DataManager.Instance.EquipListDict.TryGetValue(lhsItemId, out EquipListTable lhsInfo))
            return false;
        if (!DataManager.Instance.EquipListDict.TryGetValue(rhsItemId, out EquipListTable rhsInfo))
            return false;

        return lhsInfo.equipmentType == rhsInfo.equipmentType;
    }

    private static int CompareEquipmentRank(int lhsItemId, int rhsItemId)
    {
        if (lhsItemId == rhsItemId)
            return 0;
        if (!EnsureEquipmentTable())
            return 0;
        if (!DataManager.Instance.EquipListDict.TryGetValue(lhsItemId, out EquipListTable lhsInfo))
            return -1;
        if (!DataManager.Instance.EquipListDict.TryGetValue(rhsItemId, out EquipListTable rhsInfo))
            return 1;

        int tierCompare = lhsInfo.equipmentTier.CompareTo(rhsInfo.equipmentTier);
        if (tierCompare != 0)
            return tierCompare;

        int rarityCompare = lhsInfo.rarityType.CompareTo(rhsInfo.rarityType);
        if (rarityCompare != 0)
            return rarityCompare;

        int gradeCompare = lhsInfo.grade.CompareTo(rhsInfo.grade);
        if (gradeCompare != 0)
            return gradeCompare;

        return lhsItemId.CompareTo(rhsItemId);
    }

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

    public EquipmentData GetEquipment(int itemId)
    {
        if (equipmentByItemId.ContainsKey(itemId))
            return equipmentByItemId[itemId];
        else
            return new EquipmentData();
    }

    public void SetEquipment(EquipmentData equipmentData)
    {
        equipmentByItemId[equipmentData.equipmentId] = equipmentData;
    }
}


#region
/*
 * using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EquipmentInventoryModule : IInventoryModule
{
    // itemId -> 보유 개수
    private readonly Dictionary<int, int> equipmentCountByItemId = new Dictionary<int, int>();
    // 한 번이라도 획득(해금)한 장비 ID
    private readonly HashSet<int> unlockedItemIds = new HashSet<int>();
    // 장비 테이블 전체 ID 캐시(오름차순)
    private readonly List<int> allEquipmentItemIds = new List<int>();
    // 부위별 최종 티어 ID 캐시
    private readonly List<int> finalEquipmentItemIds = new List<int>();

    public bool IsInitialized { get; private set; }

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

    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType))
            return false;
        if (!TryConvertAmountToInt(amount, out int count))
            return false;

        IncreaseEquipment(item.ItemId, count);
        return true;
    }

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

    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!CanHandle(item.ItemType))
            return BigDouble.Zero;

        return equipmentCountByItemId.TryGetValue(item.ItemId, out int count)
            ? new BigDouble(count)
            : BigDouble.Zero;
    }

    public bool Setup(Dictionary<int, EquipmentData> initialCountByItemId)
    {
        // 세이브 데이터를 모듈 상태로 복원한다.
        IsInitialized = false;

        equipmentCountByItemId.Clear();
        unlockedItemIds.Clear();
        if (initialCountByItemId != null)
        {
            foreach (KeyValuePair<int, EquipmentData> pair in initialCountByItemId)
            {
                unlockedItemIds.Add(pair.Key);
                if (pair.Value.equipmentValue <= 0)
                    continue;

                equipmentCountByItemId[pair.Key] = pair.Value;
            }
        }

        RebuildEquipmentItemCache();
        RefreshFinalEquipment();
        IsInitialized = true;
        return true;
    }

    public bool RunAutoMerge()
    {
        if (!EnsureEquipmentTable())
            return false;
        InventoryManager manager = InventoryManager.Instance;
        if (manager == null)
            return false;

        // 합성은 InventoryManager 경유로 처리해 저장/이벤트를 공통 경로로 통일한다.
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

            int consumeCount = mergedCount * 3;
            if (!manager.RemoveItem(itemId, consumeCount))
                continue;

            if (!manager.AddItem(nextItemId, mergedCount))
            {
                manager.AddItem(itemId, consumeCount);
                continue;
            }

            mergedAny = true;
        }

        if (!mergedAny)
            return false;

        RefreshFinalEquipment();
        return true;
    }

    public bool CanAutoMerge()
    {
        foreach (KeyValuePair<int, int> pair in equipmentCountByItemId)
        {
            if (finalEquipmentItemIds.Contains(pair.Key))
                continue;
            if (pair.Value < 3)
                continue;

            return true;
        }

        return false;
    }

    public bool IsUnlocked(int itemId)
    {
        return unlockedItemIds.Contains(itemId);
    }

    public int GetBestEquipmentId(EquipmentType equipmentType)
    {
        if (!EnsureEquipmentTable())
            return 0;

        // 현재 보유 장비 중 랭크가 가장 높은 ID를 선택한다.
        int bestItemId = 0;

        foreach (KeyValuePair<int, int> pair in equipmentCountByItemId)
        {
            if (pair.Value <= 0)
                continue;
            if (!DataManager.Instance.EquipListDict.TryGetValue(pair.Key, out EquipListTable equipInfo))
                continue;
            if (equipInfo.equipmentType != equipmentType)
                continue;

            if (bestItemId == 0 || CompareEquipmentRank(pair.Key, bestItemId) > 0)
                bestItemId = pair.Key;
        }

        return bestItemId;
    }

    public void RefreshFinalEquipment()
    {
        // 부위별 최종 티어 장비를 캐시해 자동합성 제외 대상으로 사용한다.
        finalEquipmentItemIds.Clear();
        if (!EnsureEquipmentTable())
            return;

        int typeCount = Enum.GetNames(typeof(EquipmentType)).Length;
        int[] highestTierByType = new int[typeCount];
        int[] itemIdByType = new int[typeCount];

        for (int i = 0; i < allEquipmentItemIds.Count; i++)
        {
            int itemId = allEquipmentItemIds[i];
            EquipListTable equipInfo = DataManager.Instance.EquipListDict[itemId];
            int typeIndex = (int)equipInfo.equipmentType - (int)EquipmentType.Weapon;
            if (typeIndex < 0 || typeIndex >= typeCount)
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

    private void IncreaseEquipment(int itemId, int count)
    {
        if (count <= 0)
            return;

        AddRawCount(itemId, count);
    }

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
    }

    private void AddRawCount(int itemId, int count)
    {
        if (count <= 0)
            return;

        unlockedItemIds.Add(itemId);

        if (equipmentCountByItemId.TryGetValue(itemId, out int owned))
            equipmentCountByItemId[itemId] = owned + count;
        else
            equipmentCountByItemId[itemId] = count;
    }

    private void RebuildEquipmentItemCache()
    {
        allEquipmentItemIds.Clear();
        if (!EnsureEquipmentTable())
            return;

        allEquipmentItemIds.AddRange(DataManager.Instance.EquipListDict.Keys);
        allEquipmentItemIds.Sort();
    }

    private static bool EnsureEquipmentTable()
    {
        return DataManager.Instance != null && DataManager.Instance.EquipListDict != null;
    }

    private static bool IsSameEquipmentType(int lhsItemId, int rhsItemId)
    {
        if (!EnsureEquipmentTable())
            return false;
        if (!DataManager.Instance.EquipListDict.TryGetValue(lhsItemId, out EquipListTable lhsInfo))
            return false;
        if (!DataManager.Instance.EquipListDict.TryGetValue(rhsItemId, out EquipListTable rhsInfo))
            return false;

        return lhsInfo.equipmentType == rhsInfo.equipmentType;
    }

    private static int CompareEquipmentRank(int lhsItemId, int rhsItemId)
    {
        if (lhsItemId == rhsItemId)
            return 0;
        if (!EnsureEquipmentTable())
            return 0;
        if (!DataManager.Instance.EquipListDict.TryGetValue(lhsItemId, out EquipListTable lhsInfo))
            return -1;
        if (!DataManager.Instance.EquipListDict.TryGetValue(rhsItemId, out EquipListTable rhsInfo))
            return 1;

        int tierCompare = lhsInfo.equipmentTier.CompareTo(rhsInfo.equipmentTier);
        if (tierCompare != 0)
            return tierCompare;

        int rarityCompare = lhsInfo.rarityType.CompareTo(rhsInfo.rarityType);
        if (rarityCompare != 0)
            return rarityCompare;

        int gradeCompare = lhsInfo.grade.CompareTo(rhsInfo.grade);
        if (gradeCompare != 0)
            return gradeCompare;

        return lhsItemId.CompareTo(rhsItemId);
    }

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

 */
#endregion