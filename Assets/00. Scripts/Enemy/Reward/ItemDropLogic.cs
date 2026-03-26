using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 아이템 드랍 로직: 카테고리별 독립 롤, EquipListTable ID 직접 사용.
/// </summary>
public static class ItemDropLogic
{
    private static readonly EquipmentType[] EquipmentTypes = { EquipmentType.Weapon, EquipmentType.Helmet, EquipmentType.Glove, EquipmentType.Armor, EquipmentType.Boots };

    /// <summary>ItemCategory → CurrencyType 매핑 (Currency로 처리되는 카테고리만)</summary>
    private static readonly Dictionary<ItemCategory, CurrencyType> CategoryToCurrency = new()
    {
        { ItemCategory.PixieFragment, CurrencyType.PixieFragment },
        //{ ItemCategory.SkillScroll, CurrencyType.SkillScroll },
    };

    public static bool TryGetCurrencyType(ItemCategory category, out CurrencyType currencyType) =>
        CategoryToCurrency.TryGetValue(category, out currencyType);

    /// <summary>기준 파워 = ((stageLevel-1)/stageGap)*100 + startIP</summary>
    public static int GetBaseEquipmentPower(ItemDropSettings settings, int stageLevel)
    {
        if (settings == null) return 100;
        int term = Mathf.Max(1, settings.stageGap);
        return Mathf.FloorToInt((float)(stageLevel - 1) / term * 100) + settings.startIP;
    }

    /// <summary>오프셋 가중치 → equipmentTier(1~25)</summary>
    public static int RollEquipmentTier(ItemDropSettings settings, int basePower)
    {
        if (settings == null || settings.offsetTable == null || settings.offsetTable.Length == 0)
            return 1;

        int totalWeight = 0;
        foreach (var e in settings.offsetTable)
            totalWeight += e.weight;

        int roll = Random.Range(0, totalWeight);
        int offset = 0;
        foreach (var e in settings.offsetTable)
        {
            if (roll < e.weight) { offset = e.offset; break; }
            roll -= e.weight;
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

    private static int PickRandom(int[] ids)
    {
        if (ids == null || ids.Length == 0) return 0;
        return ids[Random.Range(0, ids.Length)];
    }
    private static int PickRandom(List<int> ids)
    {
        if (ids == null || ids.Count == 0) return 0;
        return ids[Random.Range(0, ids.Count)];
    }

    private static int GetEquipmentIdByTypeAndTier(EquipmentType type, int tier)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return 0;
        var match = DataManager.Instance.EquipListDict.Values
            .FirstOrDefault(e => e.equipmentType == type && e.equipmentTier == tier);
        return match != null ? match.ID : 0;
    }

    /// <summary>각 카테고리 독립 롤, itemId = EquipListTable/ItemInfoTable ID</summary>
    public static void RollAll(ItemDropSettings settings, int stageLevel, bool isBoss, List<ItemDropResult> results)
    {
        results?.Clear();
        if (settings == null) return;

        if (RollChance(settings.equipmentChance, isBoss))
        {
            int equipmentId = 0;
            if (settings.equipmentIds != null && settings.equipmentIds.Length > 0)
                equipmentId = PickRandom(settings.equipmentIds);
            else if (DataManager.Instance != null && DataManager.Instance.DataLoad)
            {
                float rand = Random.value;
                float curDropRate = 0f;
                int equipmentTier = settings.baseEquipmentTier;
                for (int i = 0; i < settings.equipmentDropRate.Count; i++)
                {
                    curDropRate += settings.equipmentDropRate[i];
                    if (rand < curDropRate)
                    {
                        equipmentTier += i;
                        break;
                    }
                }

                var type = EquipmentTypes[Random.Range(0, EquipmentTypes.Length)];
                int basePower = GetBaseEquipmentPower(settings, stageLevel);
                int tier = RollEquipmentTier(settings, basePower);
                //equipmentId = GetEquipmentIdByTypeAndTier(type, tier);
                equipmentId = GetEquipmentIdByTypeAndTier(type, equipmentTier);
            }
            if (equipmentId > 0)
                results.Add(new ItemDropResult { itemId = equipmentId, count = 1, category = ItemCategory.Equipment });
        }

        if (RollChance(settings.pixieFragmentChance, isBoss))
        {
            //int id = PickRandom(settings.pixieFragmentIds);
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.PixiePiece));
            if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.PixieFragment }); }
        }
        if (RollChance(settings.skillScrollChance, isBoss))
        {
            //int id = PickRandom(settings.skillScrollIds);
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.SkillScroll));
            if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.SkillScroll }); }
        }
        if (RollChance(settings.skillGemChance, isBoss))
        {
            //int id = PickRandom(settings.skillGemIds);

            var elementGems = settings.ReturnItemTableToType(ItemType.ElementGem);
            var uniqueGems = settings.ReturnItemTableToType(ItemType.UniqueGem);

            int c1 = elementGems != null ? elementGems.Count : 0;
            int c2 = uniqueGems != null ? uniqueGems.Count : 0;
            int total = c1 + c2;

            if (total > 0)
            {
                int roll = Random.Range(0, total);
                int id = roll < c1 ? elementGems[roll] : uniqueGems[roll - c1];
                results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.SkillGem });
            }
        }
        if (RollChance(settings.dungeonTicketChance, isBoss))
        {
            //int id = PickRandom(settings.dungeonTicketIds);
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.Key));
            if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.DungeonTicket }); }
        }

        for (int i = 0; i < settings.bingoLinks.Count; i++)
        {
            if (RollChance(settings.bingoLinks[i], isBoss))
            {
                //Debug.Log($"[ItemDropLogic] 빙고 링크 등급 {i} 확률 {settings.bingoLinks[i]}");
                int id = settings.ReturnItemTableToType(ItemType.BingoLink)[i];
                if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.BingoLink }); }
                Debug.Log($"[ItemDropLogic] 빙고 링크 등급 {i} 확률 {settings.bingoLinks[i]} 아이디 {id} 이름 {DataManager.Instance.ItemInfoDict[id].itemName} 드랍");
            }
        }

        if (RollChance(settings.bingoItem_A, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.BingoItem_A));
            if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.BingoItem_A }); }
            Debug.Log($"[ItemDropLogic] 빙고 아이템 A 아이디 {id} 이름 {DataManager.Instance.ItemInfoDict[id].itemName}드랍");
        }

        if (RollChance(settings.bingoItem_B, isBoss))
        {
            int id = PickRandom(settings.ReturnItemTableToType(ItemType.BingoItem_B));
            if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.BingoItem_B }); }
            Debug.Log($"[ItemDropLogic] 빙고 아이템 B 아이디 {id} 이름 {DataManager.Instance.ItemInfoDict[id].itemName}드랍");
        }

        for (int i = 0; i < settings.bingoSynergy.Count; i++)
        {
            if (RollChance(settings.bingoSynergy[i], isBoss))
            {
                List<int> typeToIds = settings.ReturnItemTableToType(ItemType.BingoSynergy);
                List<int> resultIds = new List<int>();
                foreach (int itemId in typeToIds)
                {
                    int grade = itemId / 1000 % 10;
                    if (grade == i + 1)
                    {
                        resultIds.Add(itemId);
                    }
                }
                int id = PickRandom(resultIds);
                Debug.Log($"[ItemDropLogic] 빙고 시너지 등급 {i} 확률 {settings.bingoSynergy[i]} 아이디 {id} 이름 {DataManager.Instance.ItemInfoDict[id].itemName} 드랍");
                if (id > 0) { results.Add(new ItemDropResult { itemId = id, count = 1, category = ItemCategory.BingoSynergy }); }
            }
        }
    }

    public struct ItemDropResult
    {
        public int itemId;
        public int count;
        public ItemCategory category;
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
