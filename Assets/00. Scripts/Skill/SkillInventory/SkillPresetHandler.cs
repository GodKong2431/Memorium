
using System.Collections.Generic;
public class SkillPresetHandler
{
    private Dictionary<OwnedSkillKey, OwnedSkillData> inventory;
    private SkillPreset[] presets;
    private int currentPresetIndex = 0;

    private const int PRESET_COUNT = 3;

    public int CurrentPresetIndex
    {
        get { return currentPresetIndex; }
    }

    public SkillPresetHandler(Dictionary<OwnedSkillKey, OwnedSkillData> inventory, SkillPreset[] presets)
    {
        this.inventory = inventory;
        this.presets = presets;
    }

    public SkillPreset GetCurrentPreset()
    {
        return presets[currentPresetIndex];
    }

    public SkillPreset GetPreset(int index)
    {
        return presets[index];
    }

    public void SwitchPreset(int index)
    {
        if (index < 0 || index >= PRESET_COUNT) return;
        currentPresetIndex = index;
    }

    public bool SetPresetSlot(int slotIndex, OwnedSkillKey skillKey)
    {
        var preset = presets[currentPresetIndex];
        if (slotIndex < 0 || slotIndex >= preset.slots.Length) return false;

        inventory.TryGetValue(skillKey, out var skillData);
        if (skillData == null || !skillData.IsEquippable) return false;

        preset.slots[slotIndex].skillKey = skillKey;
        return true;
    }

    public void ClearPresetSlot(int slotIndex)
    {
        var preset = presets[currentPresetIndex];
        if (slotIndex < 0 || slotIndex >= preset.slots.Length) return;
        preset.slots[slotIndex].Clear();
    }

    public bool SetM5Jem(int presetSlotIndex, int m5SlotIndex, int jemID)
    {
        var preset = presets[currentPresetIndex];
        if (presetSlotIndex < 0 || presetSlotIndex >= preset.slots.Length) return false;

        var slot = preset.slots[presetSlotIndex];
        if (slot.IsEmpty) return false;

        inventory.TryGetValue(slot.skillKey, out var skillData);
        if (skillData == null) return false;
        if (!skillData.IsM5JemSlotOpen(m5SlotIndex)) return false;

        slot.m5JemIDs[m5SlotIndex] = jemID;
        return true;
    }

    public bool SetM4Jem(int presetSlotIndex, int jemID)
    {
        var preset = presets[currentPresetIndex];
        if (presetSlotIndex < 0 || presetSlotIndex >= preset.slots.Length) return false;

        var slot = preset.slots[presetSlotIndex];
        if (slot.IsEmpty) return false;

        inventory.TryGetValue(slot.skillKey, out var skillData);
        if (skillData == null) return false;
        if (!skillData.IsM4JemSlotOpen) return false;

        slot.m4JemID = jemID;
        return true;
    }
}
