using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 드랍 로직: 카테고리별 독립 롤, 장비 파워 수식 적용.
/// </summary>
public static class ItemDropLogic
{
    /// <summary>기준 파워 = ((stageLevel-1)/stageGap)*100 + startIP (소수점 버림)</summary>
    public static int GetBaseEquipmentPower(ItemDropSettings settings, int stageLevel)
    {
        if (settings == null) return 100;
        int term = Mathf.Max(1, settings.stageGap);
        return Mathf.FloorToInt((float)(stageLevel - 1) / term * 100) + settings.startIP;
    }

    /// <summary>오프셋 가중치 테이블에서 롤 → 최종 파워 = 기준 파워 + 오프셋</summary>
    public static int RollEquipmentPower(ItemDropSettings settings, int basePower)
    {
        if (settings == null || settings.offsetTable == null || settings.offsetTable.Length == 0)
            return basePower;

        int totalWeight = 0;
        foreach (var e in settings.offsetTable)
            totalWeight += e.weight;

        int roll = Random.Range(0, totalWeight);
        foreach (var e in settings.offsetTable)
        {
            if (roll < e.weight)
                return basePower + e.offset;
            roll -= e.weight;
        }
        return basePower + settings.offsetTable[settings.offsetTable.Length - 1].offset;
    }

    /// <summary>보스 여부에 따라 확률 적용 (보스 5배)</summary>
    private static bool RollChance(float baseChance, bool isBoss)
    {
        float chance = isBoss ? Mathf.Min(1f, baseChance * 5f) : baseChance;
        return Random.value < chance;
    }

    private static string PickRandom(string[] ids)
    {
        if (ids == null || ids.Length == 0) return null;
        return ids[Random.Range(0, ids.Length)];
    }

    /// <summary>각 카테고리 독립 롤, 결과 수집</summary>
    public static void RollAll(ItemDropSettings settings, int stageLevel, bool isBoss, List<ItemDropResult> results)
    {
        results?.Clear();
        if (settings == null) return;

        // 장비 5%
        if (RollChance(settings.equipmentChance, isBoss))
        {
            string slotId = PickRandom(settings.equipmentSlotIds);
            if (!string.IsNullOrEmpty(slotId))
            {
                int basePower = GetBaseEquipmentPower(settings, stageLevel);
                int finalPower = RollEquipmentPower(settings, basePower);
                results.Add(new ItemDropResult { itemId = slotId, count = 1, power = finalPower, category = ItemCategory.Equipment });
            }
        }

        // 수호요정 조각 0.01%
        if (RollChance(settings.fairyShardChance, isBoss))
        {
            string id = PickRandom(settings.fairyShardIds);
            if (!string.IsNullOrEmpty(id))
                results.Add(new ItemDropResult { itemId = id, count = 1, power = 0, category = ItemCategory.FairyShard });
        }

        // 스킬 주문서 0.005%
        if (RollChance(settings.skillScrollChance, isBoss))
        {
            string id = PickRandom(settings.skillScrollIds);
            if (!string.IsNullOrEmpty(id))
                results.Add(new ItemDropResult { itemId = id, count = 1, power = 0, category = ItemCategory.SkillScroll });
        }

        // 스킬 잼 0.001%
        if (RollChance(settings.skillGemChance, isBoss))
        {
            string id = PickRandom(settings.skillGemIds);
            if (!string.IsNullOrEmpty(id))
                results.Add(new ItemDropResult { itemId = id, count = 1, power = 0, category = ItemCategory.SkillGem });
        }

        // 던전 입장권 0.001%
        if (RollChance(settings.dungeonTicketChance, isBoss))
        {
            string id = PickRandom(settings.dungeonTicketIds);
            if (!string.IsNullOrEmpty(id))
                results.Add(new ItemDropResult { itemId = id, count = 1, power = 0, category = ItemCategory.DungeonTicket });
        }
    }

    public struct ItemDropResult
    {
        public string itemId;
        public int count;
        public int power;
        public ItemCategory category;
    }

    public enum ItemCategory
    {
        Equipment,
        FairyShard,
        SkillScroll,
        SkillGem,
        DungeonTicket
    }
}
