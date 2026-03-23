
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OwnedSkillData
{
    public int skillID;
    public int level;

    public void AddLevel()
    {   
        if(CanLevelUp)
            level++;
    }

    public int MaxLevel => 25;

    public bool CanLevelUp => level < MaxLevel;

    public SkillGrade GetGrade()
    {
        if (level < 6) return SkillGrade.Common;
        else if (level < 11) return SkillGrade.Rare;
        else if (level < 16) return SkillGrade.Epic;
        else if (level < 21) return SkillGrade.Legendary;
        else return SkillGrade.Mythic;

    }
    private static readonly SkillGrade[] M5_UNLOCK_LEVELS = { SkillGrade.Rare, SkillGrade.Epic };
    private const SkillGrade M4_UNLOCK_LEVEL = SkillGrade.Legendary;

    public bool IsM5JemSlotOpen(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= M5_UNLOCK_LEVELS.Length) return false;
        return GetGrade() >= M5_UNLOCK_LEVELS[slotIndex];
    }

    public bool IsM4JemSlotOpen
    {
        get { return GetGrade() >= M4_UNLOCK_LEVEL; }
    }


    public bool IsEquippable
    {
        get { return GetGrade() >= SkillGrade.Common; }
    }
    private int GetScrollItemId()
    {
        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillID, out var table))
            return 0;
        return table.skillScrollID;
    }

    public BigDouble GetOwnedScrollCount()
    {
        int scrollId = GetScrollItemId();
        if (scrollId <= 0) return BigDouble.Zero;
        return InventoryManager.Instance.GetItemAmount(scrollId);
    }

    public BigDouble GetLevelUpCost()
    {
        if (!CanLevelUp) return BigDouble.Zero;
        var module = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (module == null) return BigDouble.Zero;
        module.TryGetLevelUpCost(skillID, out BigDouble cost);
        return cost;
    }
}