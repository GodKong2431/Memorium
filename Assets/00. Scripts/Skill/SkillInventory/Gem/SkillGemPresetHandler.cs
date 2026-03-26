using System.Collections.Generic;

[System.Serializable]
public struct GemPresetSaveData
{
    public int presetIndex;
    public int skillId;
    public int m5JemID0;
    public int m5JemID1;
    public int m4JemID;

    public GemPresetSaveData(int presetIndex, int skillId, int m5JemID0, int m5JemID1, int m4JemID)
    {
        this.presetIndex = presetIndex;
        this.skillId = skillId;
        this.m5JemID0 = m5JemID0;
        this.m5JemID1 = m5JemID1;
        this.m4JemID = m4JemID;
    }
}

public class SkillGemPresetHandler
{
    private const int PresetCount = 3;
    private readonly SkillGemPreset[] gemPresets = new SkillGemPreset[PresetCount];



    public SkillGemPresetHandler()
    {
        for (int i = 0; i < PresetCount; i++)
            gemPresets[i] = new SkillGemPreset();
    }

    public SkillGemPreset GetPreset(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= PresetCount)
            return null;
        return gemPresets[presetIndex];
    }

    #region Save / Load
    public List<GemPresetSaveData> GetSaveList()
    {
        List<GemPresetSaveData> list = new List<GemPresetSaveData>();

        for (int p = 0; p < PresetCount; p++)
        {
            var gemPreset = gemPresets[p];
            if (gemPreset == null) continue;

            foreach (var pair in gemPreset.GetAll())
            {
                var data = pair.Value;
                if (data == null) continue;

                if (data.m5JemIDs[0] == -1 && data.m5JemIDs[1] == -1 && data.m4JemID == -1)
                    continue;

                list.Add(new GemPresetSaveData(
                    p,
                    data.skillID,
                    data.m5JemIDs[0],
                    data.m5JemIDs[1],
                    data.m4JemID
                ));
            }
        }

        return list;
    }

    public void LoadFromList(List<GemPresetSaveData> saveList)
    {
        for (int i = 0; i < PresetCount; i++)
            gemPresets[i] = new SkillGemPreset();

        if (saveList == null) return;

        foreach (var save in saveList)
        {
            var gemPreset = GetPreset(save.presetIndex);
            if (gemPreset == null) continue;

            var gemData = gemPreset.GetOrCreate(save.skillId);
            gemData.m5JemIDs[0] = save.m5JemID0;
            gemData.m5JemIDs[1] = save.m5JemID1;
            gemData.m4JemID = save.m4JemID;
        }
    }

    #endregion
    /// <summary>
    /// 프리셋 전환 시 호출.
    /// </summary>
    public void OnPresetSwitch(int prevIndex, int nextIndex, SkillPreset prevPreset, SkillPreset nextPreset)
    {
        SyncFromEquipSlots(prevIndex, prevPreset);

        SyncToEquipSlots(nextIndex, nextPreset);
    }

    /// <summary>
    /// 현재 슬롯의 스킬 젬 정보를 전체 프리셋에 저장
    /// </summary>
    public void SyncFromEquipSlots(int presetIndex, SkillPreset equipPreset)
    {
        if (equipPreset?.slots == null) return;
        var gemPreset = GetPreset(presetIndex);
        if (gemPreset == null) return;

        for (int i = 0; i < equipPreset.slots.Length; i++)
        {
            var slot = equipPreset.slots[i];
            if (slot == null || slot.IsEmpty) continue;

            var gemData = gemPreset.GetOrCreate(slot.skillID);
            gemData.m5JemIDs[0] = slot.m5JemIDs[0];
            gemData.m5JemIDs[1] = slot.m5JemIDs[1];
            gemData.m4JemID = slot.m4JemID;
        }
    }

    /// <summary>
    /// 전체 젬 프리셋에서 장착 슬롯으로 복원
    /// </summary>
    public void SyncToEquipSlots(int presetIndex, SkillPreset equipPreset)
    {
        if (equipPreset?.slots == null) return;
        var gemPreset = GetPreset(presetIndex);
        if (gemPreset == null) return;

        for (int i = 0; i < equipPreset.slots.Length; i++)
        {
            var slot = equipPreset.slots[i];
            if (slot == null || slot.IsEmpty) continue;

            var gemData = gemPreset.Get(slot.skillID);
            if (gemData == null) continue;

            slot.m5JemIDs[0] = gemData.m5JemIDs[0];
            slot.m5JemIDs[1] = gemData.m5JemIDs[1];
            slot.m4JemID = gemData.m4JemID;
        }
    }

    /// <summary>
    /// 젬 장착/해제 시 호출. 양쪽 동기화.
    /// </summary>
    public void SetGem(int presetIndex, int skillId, int m5SlotIndex, int gemId, bool isM4)
    {
        var gemPreset = GetPreset(presetIndex);
        if (gemPreset == null) return;

        var gemData = gemPreset.GetOrCreate(skillId);
        if (isM4)
            gemData.m4JemID = gemId;
        else if (m5SlotIndex >= 0 && m5SlotIndex < gemData.m5JemIDs.Length)
            gemData.m5JemIDs[m5SlotIndex] = gemId;
    }

}