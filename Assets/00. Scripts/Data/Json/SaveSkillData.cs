using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class SaveSkillData :ISaveData
{
    //현재 장착중인 스킬 데이터 및 스크롤 갯수 및 순서를 기억하고 있는 cs
    //갯수 및 순서는 리스트로 add

    //저장할 것 : 프리셋 3개, 각 스킬 단계별 갯수
    int presetCount = 3;
    //하나의 프리셋당 저장할 스킬의 갯수
    int skillCountByPreset = 3;
    //m5 젬 갯수
    int m5JemCount = 2;

    public List<SkillInfoData> skillInfoData;

    public List<SkillPresetData> skillPresetData;

    public int presetNum = 0;
    int savedPresetNum = 0;
    public int SavedPresetNum=>savedPresetNum;

    //변경 여부 체크
    private bool isDirty = false;
    public bool IsDirty => isDirty;
    public SaveSkillData() { }

    public int SkillCountByPreset => skillCountByPreset;

    //List<SkillGrade> skillGrades= new List<SkillGrade>();
    Dictionary<int, SkillGrade> skillGrades = new Dictionary<int, SkillGrade>();
    public void InitSkillData()
    {
        savedPresetNum=presetNum;

        foreach (SkillGrade grade in Enum.GetValues(typeof(SkillGrade)))
        {
            skillGrades[(int)grade]=grade;
        }

        Debug.Log("[SaveSkillData] 데이터 초기 설정 시작");
        if (skillInfoData == null)
        {
            skillInfoData = new List<SkillInfoData>();
        }

        if (skillPresetData == null)
        {
            skillPresetData = new List<SkillPresetData>();
        }

        if (skillPresetData.Count < presetCount)
        {
            //정해진 프리셋 갯수보다 부족할 경우
            while (skillPresetData.Count < presetCount)
            {
                //스킬 프리셋 데이터 생성
                SkillPresetData presetData = new SkillPresetData();
                presetData.skillPresetSlotData = new List<SkillPresetSlotData>();

                //하나의 스킬 프리셋 당 저장할 스킬 횟수만큼 추가
                for (int i = 0; i < skillCountByPreset; i++)
                    presetData.skillPresetSlotData.Add(new SkillPresetSlotData(m5JemCount));
                
                skillPresetData.Add(presetData);
            }
        }

        Debug.Log("[SaveSkillData] 데이터 초기 설정 성공");
    }

    public SkillPreset LoadSkillPreset(int index)
    {
        SkillPresetSlot[] presetSlots = new SkillPresetSlot[skillCountByPreset];
        for (int i = 0; i < skillCountByPreset; i++)
        {
            presetSlots[i] = new SkillPresetSlot(skillPresetData[index].skillPresetSlotData[i].skillId,
                skillPresetData[index].skillPresetSlotData[i].m5JemIDs.ToArray(),
                skillPresetData[index].skillPresetSlotData[i].m4JemID);
        }
        SkillPreset preset = new SkillPreset(presetSlots);

        if (preset == null)
        {
            Debug.Log("[SaveSkillData] 프리셋 데이터 제작 실패");
        }
        else
        {
            Debug.Log("[SaveSkillData] 프리셋 데이터 제작 성공");
        }
        return preset;
    }

    public void SaveSkillPreset(int index ,SkillPreset preset)
    {
        Debug.Log($"[SaveSkillData] {index} 번 프리셋에 데이터 저장 시작, 스킬 갯수 {preset.slots.Length}");
        for (int i = 0; i < presetCount; i++)
        {
            SkillPresetSlotData slotData = skillPresetData[index].skillPresetSlotData[i];
            slotData.skillId = preset.slots[i].skillID;
            Debug.Log($"[SaveSkillData] {index} 번 프리셋 {i}번 스킬 id = {slotData.skillId}");
            Debug.Log($"[SaveSkillData] {index} 번 프리셋 {i}번 스킬 m5젬 저장 시작");
            for (int j = 0; j < preset.slots[i].m5JemIDs.Length; j++)
            {
                if (slotData.m5JemIDs.Count <= j)
                {
                    slotData.m5JemIDs.Add(-1);
                }
                slotData.m5JemIDs[j] = preset.slots[i].m5JemIDs[j];
            }
            Debug.Log($"[SaveSkillData] {index} 번 프리셋 {i}번 스킬 m5젬 저장 종료");
            slotData.m4JemID = preset.slots[i].m4JemID;
            Debug.Log($"[SaveSkillData] {index} 번 프리셋 {i}번 스킬 m4젬 저장 종료");

            skillPresetData[index].skillPresetSlotData[i]=slotData;
            Debug.Log($"[SaveSkillData] {index} 번 프리셋 {i}번 스킬 저장 완료");
        }

        isDirty = true;
    }

    public void SaveSkillInfoData(OwnedSkillData skillData)
    {
        int index = skillInfoData.FindIndex
            (x => x.skillId == skillData.skillID);
        if (index == -1)
        {
            skillInfoData.Add(new SkillInfoData(skillData.skillID));
            index = skillInfoData.Count - 1;
        }

        SkillInfoData infoData = skillInfoData[index];

        infoData.skillLevel = skillData.level;

        foreach (SkillGrade grade in skillGrades.Values)
        {
            int gradeIndex = infoData.FindGradeIndex((int)grade);
            SkillGradeData gradeData = infoData.gradeData[gradeIndex];
            gradeData.count = skillData.GetCount(grade);
            infoData.gradeData[gradeIndex] = gradeData;
        }
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
            OwnedSkillData ownedSkillData = new OwnedSkillData();

            ownedSkillData.skillID = infoData.skillId;
            ownedSkillData.level = infoData.skillLevel;

            if (infoData.gradeData == null)
                continue;

            foreach (SkillGradeData gradeData in infoData.gradeData)
            {
                //스크롤 갯수 추가는 CurrencyManager에서 관리하므로 스크롤 제외한 값을 넣는다
                if ((SkillGrade)gradeData.grade == SkillGrade.Scroll)
                    continue;
                if ((SkillGrade)gradeData.grade == SkillGrade.Count)
                    continue;
                ownedSkillData.SetCount((SkillGrade)gradeData.grade, gradeData.count);
            }
            ownedSkillDatas.Add(ownedSkillData);
        }
        return ownedSkillDatas;
    }

    public void SavePresetNum(int presetNum)
    {
        this.presetNum=presetNum;
        isDirty = true;
    }

    public void ClearDirty()
    {
        isDirty = false;
    }

}
