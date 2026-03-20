using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class SaveSkillData : ISaveData
{
    int presetCount = 3;
    int skillCountByPreset = 3;
    int m5JemCount = 2;

    public List<SkillInfoData> skillInfoData;
    public List<SkillPresetData> skillPresetData;

    public int presetNum = 0;
    int savedPresetNum = 0;
    public int SavedPresetNum => savedPresetNum;

    public bool onCBT=false;

    private bool isDirty = false;
    public bool IsDirty => isDirty;
    public SaveSkillData() { }

    public int SkillCountByPreset => skillCountByPreset;

    //Dictionary<int, SkillGrade> skillGrades = new Dictionary<int, SkillGrade>();

    public void InitSkillData()
    {
        if (!onCBT)
        {
            skillInfoData = null;
            skillPresetData = null;
            presetNum = 0;

            onCBT = true;
        }

        savedPresetNum = presetNum;

        //foreach (SkillGrade grade in Enum.GetValues(typeof(SkillGrade)))
        //    skillGrades[(int)grade] = grade;

        Debug.Log("[SaveSkillData] data init start");
        if (skillInfoData == null)
            skillInfoData = new List<SkillInfoData>();

        if (skillPresetData == null)
            skillPresetData = new List<SkillPresetData>();

        if (skillPresetData.Count < presetCount)
        {
            while (skillPresetData.Count < presetCount)
            {
                SkillPresetData presetData = new SkillPresetData
                {
                    skillPresetSlotData = new List<SkillPresetSlotData>()
                };

                for (int i = 0; i < skillCountByPreset; i++)
                    presetData.skillPresetSlotData.Add(new SkillPresetSlotData(m5JemCount));

                skillPresetData.Add(presetData);
            }
        }

        Debug.Log("[SaveSkillData] data init complete");
    }

    public SkillPreset LoadSkillPreset(int index)
    {
        if (skillPresetData == null || index < 0 || index >= skillPresetData.Count)
            return new SkillPreset();

        SkillPresetSlot[] presetSlots = new SkillPresetSlot[skillCountByPreset];
        SkillPresetData savedPreset = skillPresetData[index] ?? new SkillPresetData();
        List<SkillPresetSlotData> savedSlots = savedPreset.skillPresetSlotData;
        for (int i = 0; i < skillCountByPreset; i++)
        {
            SkillPresetSlotData slotData = savedSlots != null && i < savedSlots.Count
                ? savedSlots[i]
                : new SkillPresetSlotData(m5JemCount);

            presetSlots[i] = CreatePresetSlot(slotData);
        }

        SkillPreset preset = new SkillPreset(presetSlots);
        Debug.Log(preset == null
            ? "[SaveSkillData] preset load failed"
            : "[SaveSkillData] preset load complete");
        return preset;
    }

    public void SaveSkillPreset(int index, SkillPreset preset)
    {
        if (preset == null || preset.slots == null || index < 0)
            return;

        EnsurePresetSaveSlotCount(index);

        Debug.Log($"[SaveSkillData] preset save start. preset={index}, slotCount={preset.slots.Length}");
        SkillPresetData presetData = skillPresetData[index] ?? new SkillPresetData();
        for (int i = 0; i < skillCountByPreset; i++)
        {
            SkillPresetSlot slot = i < preset.slots.Length && preset.slots[i] != null
                ? preset.slots[i]
                : new SkillPresetSlot();
            slot.Normalize();

            SkillPresetSlotData slotData = presetData.skillPresetSlotData[i] ?? new SkillPresetSlotData(m5JemCount);
            slotData.skillId = slot.IsEmpty ? SkillPresetSlot.EmptySkillId : slot.skillID;

            for (int j = 0; j < m5JemCount; j++)
            {
                if (slotData.m5JemIDs == null)
                    slotData.m5JemIDs = new List<int>();
                if (slotData.m5JemIDs.Count <= j)
                    slotData.m5JemIDs.Add(SkillPresetSlot.EmptySkillId);

                slotData.m5JemIDs[j] = j < slot.m5JemIDs.Length
                    ? slot.m5JemIDs[j]
                    : SkillPresetSlot.EmptySkillId;
            }

            slotData.m4JemID = slot.m4JemID;
            presetData.skillPresetSlotData[i] = slotData;
        }

        skillPresetData[index] = presetData;
        isDirty = true;
    }

    public void SaveSkillInfoData(OwnedSkillData skillData)
    {
        int index = skillInfoData.FindIndex(x => x.skillId == skillData.skillID);
        if (index == -1)
        {
            skillInfoData.Add(new SkillInfoData(skillData.skillID));
            index = skillInfoData.Count - 1;
        }

        SkillInfoData infoData = skillInfoData[index];
        infoData.skillLevel = skillData.level;

        //foreach (SkillGrade grade in skillGrades.Values)
        //{
        //    int gradeIndex = infoData.FindGradeIndex((int)grade);
        //    SkillGradeData gradeData = infoData.gradeData[gradeIndex];
        //    gradeData.count = skillData.GetCount(grade);
        //    infoData.gradeData[gradeIndex] = gradeData;
        //}

        skillInfoData[index] = infoData;
        isDirty = true;
    }

    public List<OwnedSkillData> LoadSkillData()
    {
        List<OwnedSkillData> ownedSkillDatas = new List<OwnedSkillData>();

        if (skillInfoData == null || skillInfoData.Count == 0)
            return ownedSkillDatas;

        foreach (SkillInfoData infoData in skillInfoData)
        {
            OwnedSkillData ownedSkillData = new OwnedSkillData
            {
                skillID = infoData.skillId,
                level = infoData.skillLevel
            };

            //if (infoData.gradeData == null)
            //    continue;

            //foreach (SkillGradeData gradeData in infoData.gradeData)
            //{
            //    if ((SkillGrade)gradeData.grade == SkillGrade.Scroll)
            //        continue;
            //    if ((SkillGrade)gradeData.grade == SkillGrade.Count)
            //        continue;

            //    ownedSkillData.SetCount((SkillGrade)gradeData.grade, gradeData.count);
            //}

            ownedSkillDatas.Add(ownedSkillData);
        }

        return ownedSkillDatas;
    }

    public void SavePresetNum(int presetNum)
    {
        this.presetNum = presetNum;
        isDirty = true;
    }

    public void ClearDirty()
    {
        isDirty = false;
    }

    private SkillPresetSlot CreatePresetSlot(SkillPresetSlotData slotData)
    {
        if (slotData == null)
            return new SkillPresetSlot();

        int[] m5GemIds = new int[m5JemCount];
        for (int i = 0; i < m5JemCount; i++)
            m5GemIds[i] = SkillPresetSlot.EmptySkillId;

        if (slotData.m5JemIDs != null)
        {
            int copyCount = slotData.m5JemIDs.Count < m5JemCount ? slotData.m5JemIDs.Count : m5JemCount;
            for (int i = 0; i < copyCount; i++)
                m5GemIds[i] = slotData.m5JemIDs[i];
        }

        return new SkillPresetSlot(slotData.skillId, m5GemIds, slotData.m4JemID);
    }

    private void EnsurePresetSaveSlotCount(int index)
    {
        while (skillPresetData.Count <= index)
        {
            skillPresetData.Add(new SkillPresetData
            {
                skillPresetSlotData = new List<SkillPresetSlotData>()
            });
        }

        SkillPresetData presetData = skillPresetData[index] ?? new SkillPresetData();
        if (presetData.skillPresetSlotData == null)
            presetData.skillPresetSlotData = new List<SkillPresetSlotData>();

        while (presetData.skillPresetSlotData.Count < skillCountByPreset)
            presetData.skillPresetSlotData.Add(new SkillPresetSlotData(m5JemCount));

        skillPresetData[index] = presetData;
    }
}
