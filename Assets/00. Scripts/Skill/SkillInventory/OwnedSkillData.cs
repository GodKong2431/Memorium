

[System.Serializable]
public class OwnedSkillData
{
    public int skillID;
    public SkillGrade grade;
    public int level;
    public int count;

    public int MaxLevel
    {
        get
        {
            switch (grade)
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
        get { return grade >= SkillGrade.Common; }
    }

    public bool CanLevelUp
    {
        get { return grade >= SkillGrade.Rare && level < MaxLevel; }
    }

    public bool CanMerge
    {
        get { return grade < SkillGrade.Mythic; }
    }
}