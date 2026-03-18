
[System.Serializable]
public class SkillPreset
{
    public SkillPresetSlot[] slots;


    private const int SLOT_COUNT = 3;

    public SkillPreset()
    {
        slots = new SkillPresetSlot[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
            slots[i] = new SkillPresetSlot();
    }

    public SkillPreset(SkillPresetSlot[] slots)
    {
        this.slots = slots;
    }

    public SkillPreset Clone()
    {
        SkillPresetSlot[] clonedSlots = new SkillPresetSlot[SLOT_COUNT];

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            SkillPresetSlot sourceSlot = slots != null && i < slots.Length && slots[i] != null
                ? slots[i]
                : new SkillPresetSlot();
            clonedSlots[i] = sourceSlot.Clone();
        }

        return new SkillPreset(clonedSlots);
    }
}
