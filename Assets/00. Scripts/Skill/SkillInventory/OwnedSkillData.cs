using System.Collections.Generic;

[System.Serializable]
public class OwnedSkillData
{
    public int skillID;
    public int level;
    private Dictionary<SkillGrade, int> gradeCountMap = new();


    public int GetCount(SkillGrade grade)
    {
        return gradeCountMap.TryGetValue(grade, out var count) ? count : 0;
    }

    public void AddCount(SkillGrade grade, int amount)
    {
        if (!gradeCountMap.ContainsKey(grade))
            gradeCountMap[grade] = 0;
        gradeCountMap[grade] += amount;
        if (gradeCountMap[grade] <= 0)
            gradeCountMap.Remove(grade);
    }

    public SkillGrade HighestGrade
    {
        get
        {
            SkillGrade best = SkillGrade.Fragment;
            foreach (var pair in gradeCountMap)
            {
                if (pair.Value > 0 && pair.Key > best)
                    best = pair.Key;
            }
            return best;
        }
    }

    public int HighestGradeCount
    {
        get { return GetCount(HighestGrade); }
    }

    public int MaxLevel
    {
        get
        {
            switch (HighestGrade)
            {
                case SkillGrade.Fragment: return 0;
                case SkillGrade.Common: return 0;
                case SkillGrade.Rare: return 50;
                case SkillGrade.Epic: return 150;
                case SkillGrade.Legendary: return 300;
                case SkillGrade.Mythic: return 500;
                default: return 0;
            }
        }
    }

    public bool CanLevelUp
    {
        get { return HighestGrade >= SkillGrade.Rare && level < MaxLevel; }
    }


    public const int M5_JEM_SLOT_COUNT = 2;
    private static readonly int[] M5_UNLOCK_LEVELS = { 10, 100 };
    private const int M4_UNLOCK_LEVEL = 500;

    public bool IsM5JemSlotOpen(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= M5_UNLOCK_LEVELS.Length) return false;
        return level >= M5_UNLOCK_LEVELS[slotIndex];
    }

    public bool IsM4JemSlotOpen
    {
        get { return level >= M4_UNLOCK_LEVEL; }
    }


    public bool IsEquippable
    {
        get { return HighestGrade >= SkillGrade.Common; }
    }

    public bool CanMerge
    {
        get { return HighestGrade < SkillGrade.Mythic; }
    }

}