using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ItemDropLogic
{
    private const float RareDropChanceThreshold = 0.01f;

    private static readonly EquipmentType[] EquipmentTypes =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Glove,
        EquipmentType.Armor,
        EquipmentType.Boots
    };

    private static readonly Dictionary<ItemCategory, CurrencyType> CategoryToCurrency = new()
    {
        { ItemCategory.PixieFragment, CurrencyType.PixieFragment },
    };

    public static bool TryGetCurrencyType(ItemCategory category, out CurrencyType currencyType) =>
        CategoryToCurrency.TryGetValue(category, out currencyType);

    public static int GetBaseEquipmentPower(ItemDropSettings settings, int stageLevel)
    {
        if (settings == null)
            return 100;

        int term = Mathf.Max(1, settings.stageGap);
        return Mathf.FloorToInt((float)(stageLevel - 1) / term * 100f) + settings.startIP;
    }

    public static int RollEquipmentTier(ItemDropSettings settings, int basePower)
    {
        if (settings == null || settings.offsetTable == null || settings.offsetTable.Length == 0)
            return 1;

        int totalWeight = 0;
        foreach (ItemDropSettings.EquipmentOffsetEntry entry in settings.offsetTable)
            totalWeight += entry.weight;

        int roll = Random.Range(0, totalWeight);
        int offset = 0;
        foreach (ItemDropSettings.EquipmentOffsetEntry entry in settings.offsetTable)
        {
            if (roll < entry.weight)
            {
                offset = entry.offset;
                break;
            }

            roll -= entry.weight;
        }

        if (offset == 0 && settings.offsetTable.Length > 0)
            offset = settings.offsetTable[settings.offsetTable.Length - 1].offset;

        int rawTier = 1 + (basePower + offset - 100) / 12;
        return Mathf.Clamp(rawTier, 1, 25);
    }

    private static bool RollChance(float baseChance, bool isBoss)
    {
        float chance = isBoss ? Mathf.Min(1f, baseChance * 5f) : baseChance;
        return Random.value < chance;
    }

    private static bool IsRareDropChance(float baseChance)
    {
        return baseChance > 0f && baseChance < RareDropChanceThreshold;
    }

    private static int PickRandom(int[] ids)
    {
        if (ids == null || ids.Length == 0)
            return 0;

        return ids[Random.Range(0, ids.Length)];
    }

    private static int PickRandom(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return 0;

        return ids[Random.Range(0, ids.Count)];
    }

    private static int GetEquipmentIdByTypeAndTier(EquipmentType type, int tier)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return 0;

        EquipListTable match = DataManager.Instance.EquipListDict.Values
            .FirstOrDefault(entry => entry.equipmentType == type && entry.equipmentTier == tier);
        return match != null ? match.ID : 0;
    }

    private static void AddDrop(List<ItemDropResult> results, int itemId, int count, ItemCategory category, float baseChance)
    {
        if (results == null || itemId <= 0 || count <= 0)
            return;

        results.Add(new ItemDropResult
        {
            itemId = itemId,
            count = count,
            category = category,
            isRareDrop = IsRareDropChance(baseChance)
        });
    }

    public static void RollAll(ItemDropSettings settings, int stageLevel, bool isBoss, List<ItemDropResult> results)
    {
        results?.Clear();
        if (settings == null)
            return;

        if (RollChance(settings.equipmentChance, isBoss))
        {
            int equipmentId = 0;
            if (settings.equipmentIds != null && settings.equipmentIds.Length > 0)
            {
                equipmentId = PickRandom(settings.equipmentIds);
            }
            else if (DataManager.Instance != null && DataManager.Instance.DataLoad)
            {
                float rand = Random.value;
                float currentDropRate = 0f;
                int equipmentTier = settings.baseEquipmentTier;
                for (int i = 0; i < settings.equipmentDropRate.Count; i++)
                {
                    currentDropRate += settings.equipmentDropRate[i];
                    if (rand < currentDropRate)
                    {
                        equipmentTier += i;
                        break;
                    }
                }

                EquipmentType type = EquipmentTypes[Random.Range(0, EquipmentTypes.Length)];
                int basePower = GetBaseEquipmentPower(settings, stageLevel);
                _ = RollEquipmentTier(settings, basePower);
                equipmentId = GetEquipmentIdByTypeAndTier(type, equipmentTier);
            }

            AddDrop(results, equipmentId, 1, ItemCategory.Equipment, settings.equipmentChance);
        }

        if (RollChance(settings.pixieFragmentChance, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.PixiePiece));
            AddDrop(results, id, 1, ItemCategory.PixieFragment, settings.pixieFragmentChance);
        }

        if (RollChance(settings.skillScrollChance, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.SkillScroll));
            AddDrop(results, id, 1, ItemCategory.SkillScroll, settings.skillScrollChance);
        }

        if (RollChance(settings.skillGemChance, isBoss))
        {
            List<int> elementGems = settings.ReturnItemTableToType(ItemType.ElementGem);
            List<int> uniqueGems = settings.ReturnItemTableToType(ItemType.UniqueGem);

            int elementCount = elementGems != null ? elementGems.Count : 0;
            int uniqueCount = uniqueGems != null ? uniqueGems.Count : 0;
            int total = elementCount + uniqueCount;

            if (total > 0)
            {
                int roll = Random.Range(0, total);
                int id = roll < elementCount ? elementGems[roll] : uniqueGems[roll - elementCount];
                AddDrop(results, id, 1, ItemCategory.SkillGem, settings.skillGemChance);
            }
        }

        if (RollChance(settings.dungeonTicketChance, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.Key));
            AddDrop(results, id, 1, ItemCategory.DungeonTicket, settings.dungeonTicketChance);
        }

        for (int i = 0; i < settings.bingoLinks.Count; i++)
        {
            if (!RollChance(settings.bingoLinks[i], isBoss))
                continue;

            int id = settings.ReturnItemTableToType(ItemType.BingoLink)[i];
            AddDrop(results, id, 1, ItemCategory.BingoLink, settings.bingoLinks[i]);
            Debug.Log($"[ItemDropLogic] BingoLink grade {i} chance {settings.bingoLinks[i]} id {id}");
        }

        if (RollChance(settings.bingoItem_A, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.BingoItem_A));
            AddDrop(results, id, 1, ItemCategory.BingoItem_A, settings.bingoItem_A);
            Debug.Log($"[ItemDropLogic] BingoItem_A id {id}");
        }

        if (RollChance(settings.bingoItem_B, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.BingoItem_B));
            AddDrop(results, id, 1, ItemCategory.BingoItem_B, settings.bingoItem_B);
            Debug.Log($"[ItemDropLogic] BingoItem_B id {id}");
        }

        for (int i = 0; i < settings.bingoSynergy.Count; i++)
        {
            if (!RollChance(settings.bingoSynergy[i], isBoss))
                continue;

            List<int> typeToIds = settings.ReturnItemTableToType(ItemType.BingoSynergy);
            List<int> resultIds = new List<int>();
            foreach (int itemId in typeToIds)
            {
                int grade = itemId / 1000 % 10;
                if (grade == i + 1)
                    resultIds.Add(itemId);
            }

            int id = PickRandom(resultIds);
            AddDrop(results, id, 1, ItemCategory.BingoSynergy, settings.bingoSynergy[i]);
            Debug.Log($"[ItemDropLogic] BingoSynergy grade {i} chance {settings.bingoSynergy[i]} id {id}");
        }
    }

    public struct ItemDropResult
    {
        public int itemId;
        public int count;
        public ItemCategory category;
        public bool isRareDrop;
    }

    public enum ItemCategory
    {
        Equipment,
        PixieFragment,
        SkillScroll,
        SkillGem,
        DungeonTicket,
        BingoLink,
        BingoItem_A,
        BingoItem_B,
        BingoSynergy
    }
}
