using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveSkillData
{
    //현재 장착중인 스킬 데이터 및 스크롤 갯수 및 순서를 기억하고 있는 cs
    //갯수 및 순서는 리스트로 add

    public int presetCount = 3;

    //저장할 스킬 스크롤 아이디
    public List<int> skillScrollId;
    //각 스킬 스크롤 아이디 별 갯수
    public List<int> skillScrollIdToCount;

    ////그냥 9개씩 저장하자 그리고 3개씩 반환하면 될 듯
    //public int[] skillId;
    //public int[,] m5JemIDs;
    //public int[] m4JemId;

    ////이건 3개(PresetCount)
    //SkillPreset[] presets;
    ////이건 presetCount * a긴 한데 현재 3개이므로 9개
    //SkillPresetSlot[] slots;

    public SaveSkillData() { }

    public void InitSkillData()
    {
        ////값 초기화해서 반환
        //if (skillScrollId == null)
        //{
        //    skillScrollId = new List<int>();
        //    skillScrollIdToCount = new List<int>();
        //    skillId = new int[presetCount * presetCount];
        //    m5JemIDs = new int[presetCount * presetCount * 2,2];
        //    m4JemId = new int[presetCount * presetCount];





        //    slots = new SkillPresetSlot[presetCount * presetCount];
        //    for (int i = 0; i < presetCount; i++)
        //    {
        //        slots[i] = new SkillPresetSlot();
        //    }
        //}

        ////데이터 기반으로 스킬 슬롯 생성
        //else
        //{
        //    int[] m5Jem = new int[2];
        //    slots = new SkillPresetSlot[presetCount * presetCount];
        //    for (int i = 0; i < presetCount; i++)
        //    {
        //        m5Jem[0] = m5JemIDs[i, 0];
        //        m5Jem[1] = m5JemIDs[i, 1];
        //        slots[i] = new SkillPresetSlot(skillId[i], m5Jem, m4JemId[i]);
        //    }
        //}
        //SetPrest();
    }

    //public void SetPrest()
    //{
    //    presets = new SkillPreset[presetCount];
    //    for (int i = 0; i < presetCount; i++)
    //    {
    //        int startIndex = i * presetCount;
    //        int endIndex = i * presetCount + presetCount;
    //        presets[i] = new SkillPreset(presetCount, slots[startIndex..endIndex]);
    //    }
    //}

    //public SkillPreset[] LoadPrest()
    //{
    //    return presets;
    //}


}
