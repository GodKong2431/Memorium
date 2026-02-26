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

    private Dictionary<int, OwnedSkillData> bestGradeBuffer = new();
    private bool isInventoryDirty = true;

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

    [ContextMenu("테스트: 스킬 수량 추가")]
    private void TestAddCount()
    {
        if (!DataManager.Instance.SkillInfoDict.ContainsKey(testSkillID))
        {
            var ids = string.Join(", ", DataManager.Instance.SkillInfoDict.Keys);
            Debug.LogWarning($"등록되지 않은 스킬 ID: {testSkillID}\n등록된 ID 목록: [{ids}]");
            return;
        }
        AddSkill(testSkillID, testGrade, testAmount);
        Debug.Log($"[{testSkillID}] {testGrade} x{testAmount} 추가");
    }

#endif
}