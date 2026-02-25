
[System.Serializable]
public class SkillPreset
{
    public SkillPresetSlot[] slots;

    public SkillPreset(int slotCount = 3)
    {
        slots = new SkillPresetSlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new SkillPresetSlot();
    }
}