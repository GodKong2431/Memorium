
//using System;
//using System.Collections.Generic;

//public class SkillMergeHandler
//{
//    private Dictionary<int, OwnedSkillData> inventory;
//    private const int MERGE_REQUIRED_COUNT = 3;

//    public SkillMergeHandler(Dictionary<int, OwnedSkillData> inventory)
//    {
//        this.inventory = inventory;
//    }

//    public int MergeChain(int skillID, Action<int, SkillGrade, int> addSkill)
//    {
//        if (!inventory.TryGetValue(skillID, out var data)) return 0;

//        int total = 0;
//        for (var grade = SkillGrade.Scroll; grade < SkillGrade.Mythic; grade++)
//        {
//            int count = data.GetCount(grade);
//            int mergeCount = count / MERGE_REQUIRED_COUNT;
//            if (mergeCount <= 0) continue;

//            data.AddCount(grade, -(mergeCount * MERGE_REQUIRED_COUNT));
//            addSkill(skillID, grade + 1, mergeCount);
//            total += mergeCount;
//        }
//        return total;
//    }
//    public int MergeAllSkills(Action<int, SkillGrade, int> addSkill)
//    {
//        int total = 0;
//        var skillIDs = new List<int>(inventory.Keys);

//        for (int i = 0; i < skillIDs.Count; i++)
//        {
//            total += MergeChain(skillIDs[i], addSkill);
//        }
//        return total;
//    }
//}
