
using System;
using System.Collections.Generic;

public class SkillMergeHandler
{
    private Dictionary<OwnedSkillKey, OwnedSkillData> inventory;
    private const int MERGE_REQUIRED_COUNT = 3;

    public SkillMergeHandler(Dictionary<OwnedSkillKey, OwnedSkillData> inventory)
    {
        this.inventory = inventory;
    }

    public int MergeChain(int skillID, Action<int, SkillGrade, int> addSkill)
    {
        int total = 0;
        for (var g = SkillGrade.Fragment; g < SkillGrade.Mythic; g++)
        {
            var key = new OwnedSkillKey(skillID, g);
            if (!inventory.TryGetValue(key, out var data)) continue;

            int mergeCount = data.count / MERGE_REQUIRED_COUNT;
            if (mergeCount <= 0) continue;

            data.count %= MERGE_REQUIRED_COUNT;
            if (data.count <= 0) inventory.Remove(key);

            addSkill(skillID, g + 1, mergeCount);
            total += mergeCount;
        }
        return total;
    }

    public int MergeAllSkills(Action<int, SkillGrade, int> addSkill)
    {
        int total = 0;

        var skillIDs = new List<int>();
        foreach (var data in inventory.Values)
        {
            if (data.CanMerge && data.count >= MERGE_REQUIRED_COUNT && !skillIDs.Contains(data.skillID))
                skillIDs.Add(data.skillID);
        }

        for (int i = 0; i < skillIDs.Count; i++)
        {
            total += MergeChain(skillIDs[i], addSkill);
        }
        return total;
    }
}
