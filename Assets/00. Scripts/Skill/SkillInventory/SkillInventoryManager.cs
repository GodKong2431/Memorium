using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class SkillInventoryManager : Singleton<SkillInventoryManager>
{
    private Dictionary<int, OwnedSkillData> inventory = new();
    private SkillPreset[] presets;

    private SkillMergeHandler mergeHandler;
    private SkillPresetHandler presetHandler;

    private const int PRESET_COUNT = 3;

    public static event Action OnInventoryChanged;
    public static event Action<int> OnPresetChanged;


    public Dictionary<int, int> skillScrollIdToSkillIdDict;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        //스킬 스크롤 ID : 스킬 ID 딕셔너리 생성
        skillScrollIdToSkillIdDict= new Dictionary<int, int>();
        foreach (var skill in DataManager.Instance.SkillInfoDict)
        {
            skillScrollIdToSkillIdDict[skill.Value.skillScrollID] = skill.Key;
            //Debug.Log($"[SkillInventoryManager] 스킬 스크롤 아이디 목록 : {skill.Value.skillScrollID} 스킬 아이디 목록 : {skill.Key}");
        }
        //foreach (var id in skillScrollIdToSkillIdDict)
        //{

        //    Debug.Log($"[SkillInventoryManager] 결과>> 스킬 스크롤 아이디 목록 : {id.Key} 스킬 아이디 목록 : {id.Value}");
        //}
    }

    protected override void Awake()
    {
        base.Awake();
        presets = new SkillPreset[PRESET_COUNT];
        for (int i = 0; i < PRESET_COUNT; i++)
            presets[i] = new SkillPreset();

        mergeHandler = new SkillMergeHandler(inventory);
        presetHandler = new SkillPresetHandler(inventory, presets);
    }
    private void Start()
    {
#if UNITY_EDITOR
        testStartAdd();
#endif
    }
    public OwnedSkillData GetSkillData(int skillID)
    {
        inventory.TryGetValue(skillID, out var data);
        return data;
    }

    public bool TryLevelUp(int skillID)
    {
        if (!inventory.TryGetValue(skillID, out var data)) return false;
        if (!data.CanLevelUp) return false;

        BigDouble cost = new BigDouble(data.level * 100);
        if (!CurrencyManager.Instance.TrySpend(CurrencyType.Gold, cost)) return false;

        data.level++;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void AddSkill(int skillID, SkillGrade grade = SkillGrade.Fragment, int amount = 1, bool notify = true)
    {
        if (!inventory.TryGetValue(skillID, out var data))
        {
            data = new OwnedSkillData { skillID = skillID, level = 0 };
            inventory[skillID] = data;
        }
        data.AddCount(grade, amount);

        if (notify)
            OnInventoryChanged?.Invoke();
    }



    #region 합성 (MergeHandler)

    public int MergeChain(int skillID, bool notify = true)
    {
        int total = mergeHandler.MergeChain(skillID, (id, g, amount) => AddSkill(id, g, amount, false));

        if (total > 0 && notify)
            OnInventoryChanged?.Invoke();

        return total;
    }

    public int MergeAllSkills()
    {
        int total = mergeHandler.MergeAllSkills((id, g, amount) => AddSkill(id, g, amount, false));

        if (total > 0)
            OnInventoryChanged?.Invoke();

        return total;
    }

    #endregion

    #region 프리셋 (PresetHandler)

    public SkillPreset GetCurrentPreset()
    {
        return presetHandler.GetCurrentPreset();
    }

    public SkillPreset GetPreset(int index)
    {
        return presetHandler.GetPreset(index);
    }

    public int CurrentPresetIndex
    {
        get { return presetHandler.CurrentPresetIndex; }
    }

    public void SwitchPreset(int index)
    {
        presetHandler.SwitchPreset(index);
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }

    public bool SetPresetSlot(int slotIndex, int skillID)
    {
        if (!presetHandler.SetPresetSlot(slotIndex, skillID)) return false;
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    public void ClearPresetSlot(int slotIndex)
    {
        presetHandler.ClearPresetSlot(slotIndex);
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }

    public bool SetM5Jem(int presetSlotIndex, int m5SlotIndex, int jemID)
    {
        if (!presetHandler.SetM5Jem(presetSlotIndex, m5SlotIndex, jemID)) return false;
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    public bool SetM4Jem(int presetSlotIndex, int jemID)
    {
        if (!presetHandler.SetM4Jem(presetSlotIndex, jemID)) return false;
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    #endregion

#if UNITY_EDITOR

    [Header("테스트")]
    [SerializeField] private int testSkillID;
    [SerializeField] private SkillGrade testGrade = SkillGrade.Fragment;
    [SerializeField] private int testAmount = 10;
    [Header("테스트 스킬")]
    [SerializeField] private SkillDataContext testContext;

    [ContextMenu("스킬 수량 추가")]
    private void TestAddCount()
    {
        int _id = testSkillID;
        if (!DataManager.Instance.SkillInfoDict.ContainsKey(_id))
        {
            var ids = string.Join(", ", DataManager.Instance.SkillInfoDict.Keys);
            Debug.LogWarning($"등록되지 않은 스킬 ID: {testSkillID}\n등록된 ID 목록: [{ids}]");
            return;
        }
        AddSkill(_id, testGrade, testAmount);
    }

    private int[] testIDs = { 4000001, 4000002, 4000003 };
    private void testStartAdd()
    {
        for (int i = 0; i < testIDs.Length; i++)
        {
            AddSkill(testIDs[i], testGrade, testAmount);
        }
    }

    [ContextMenu("테스트 스킬 등록 및 장착")]
    private void TestRegister()
    {
        var info = testContext.skillData.skillTable;
        if (info == null || info.ID == 0)
        {
            return;
        }

        int id = info.ID;

        DataManager.Instance.SkillInfoDict[id] = info;
        if (testContext.skillData.m1Data != null)
            DataManager.Instance.SkillModule1Dict[info.m1ID] = testContext.skillData.m1Data;
        if (testContext.skillData.m2Data != null)
            DataManager.Instance.SkillModule2Dict[info.m2ID] = testContext.skillData.m2Data;
        if (testContext.skillData.m3Data != null)
            DataManager.Instance.SkillModule3Dict[info.m3ID] = testContext.skillData.m3Data;
        if (testContext.m4Data != null)
            DataManager.Instance.SkillModule4Dict[testContext.m4Data.ID] = testContext.m4Data;
        if (testContext.m5Data != null)
            DataManager.Instance.SkillModule5Dict[testContext.m5Data.ID] = testContext.m5Data;

        AddSkill(id, SkillGrade.Common, 1);

        var data = GetSkillData(id);
        if (data != null)
            data.level = 500;

        var preset = GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (preset.slots[i].IsEmpty)
            {
                SetPresetSlot(i, id);

                if (testContext.m4Data != null)
                    SetM4Jem(i, testContext.m4Data.ID);

                if (testContext.m5Data != null)
                    SetM5Jem(i, 0, testContext.m5Data.ID);
                return;
            }
        }
    }

#endif
}