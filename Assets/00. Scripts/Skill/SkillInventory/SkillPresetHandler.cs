
using System.Collections.Generic;
public class SkillPresetHandler
{
    private Dictionary<int, OwnedSkillData> inventory;
    private SkillPreset[] presets;
    private int currentPresetIndex = 0;

    private const int PRESET_COUNT = 3;

    public int CurrentPresetIndex
    {
        get { return currentPresetIndex; }
    }

    public SkillPresetHandler(Dictionary<int, OwnedSkillData> _inventory, SkillPreset[] _presets)
    {
        inventory = _inventory;
        presets = _presets;
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

    public bool SetPresetSlot(int slotIndex, int skillID)
    {
        var preset = presets[currentPresetIndex];
        if (slotIndex < 0 || slotIndex >= preset.slots.Length) return false;

        if (!inventory.TryGetValue(skillID, out var skillData)) return false;
        if (!skillData.IsEquippable) return false;

        preset.slots[slotIndex].skillID = skillID;
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

        if (!inventory.TryGetValue(slot.skillID, out var skillData)) return false;
        if (!skillData.IsM5JemSlotOpen(m5SlotIndex)) return false;

        if (jemID != -1)
        {
            int otherSlotIndex = m5SlotIndex == 0 ? 1 : 0;

            if (slot.m5JemIDs[otherSlotIndex] == jemID) return false;
        }

        slot.m5JemIDs[m5SlotIndex] = jemID;
        return true;
    }

    public bool SetM4Jem(int presetSlotIndex, int jemID)
    {
        var preset = presets[currentPresetIndex];
        if (presetSlotIndex < 0 || presetSlotIndex >= preset.slots.Length) return false;

        var slot = preset.slots[presetSlotIndex];
        if (slot.IsEmpty) return false;

        if (!inventory.TryGetValue(slot.skillID, out var skillData)) return false;
        if (!skillData.IsM4JemSlotOpen) return false;

        slot.m4JemID = jemID;
        return true;
    }
}
