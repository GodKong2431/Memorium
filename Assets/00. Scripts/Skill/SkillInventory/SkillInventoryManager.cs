
using System;
using System.Collections.Generic;

public class SkillInventoryManager : Singleton<SkillInventoryManager>
{
    private Dictionary<OwnedSkillKey, OwnedSkillData> inventory = new();
    private SkillPreset[] presets;

    private SkillMergeHandler mergeHandler;
    private SkillPresetHandler presetHandler;

    private const int PRESET_COUNT = 3;

    public static event Action OnInventoryChanged;
    public static event Action<int> OnPresetChanged;

    private Dictionary<int, OwnedSkillData> bestGradeBuffer = new();
    private List<OwnedSkillData> cachedHighestSkills = new();
    private bool isInventoryDirty = true;

    protected override void Awake()
    {
        base.Awake();
        presets = new SkillPreset[PRESET_COUNT];
        for (int i = 0; i < PRESET_COUNT; i++)
            presets[i] = new SkillPreset();

        mergeHandler = new SkillMergeHandler(inventory);
        presetHandler = new SkillPresetHandler(inventory, presets);
    }

    #region 레벨업

    public bool TryLevelUp(OwnedSkillKey key)
    {
        if (!inventory.TryGetValue(key, out var data)) return false;
        if (!data.CanLevelUp) return false;

        BigDouble cost = new BigDouble(data.level * 100);//레벨업 비용 데이터 테이블 기반으로 바꿀계획
        if (!CurrencyManager.Instance.TrySpend(CurrencyType.Gold, cost)) return false;

        data.level++;
        isInventoryDirty = true;
        OnInventoryChanged?.Invoke();
        return true;
    }

    #endregion

    #region 스킬 추가/조회

    public void AddSkill(int skillID, SkillGrade grade = SkillGrade.Fragment, int amount = 1, bool notify = true)
    {
        var key = new OwnedSkillKey(skillID, grade);
        if (inventory.TryGetValue(key, out var data))
        {
            data.count += amount;
        }
        else
        {
            inventory[key] = new OwnedSkillData
            {
                skillID = skillID,
                grade = grade,
                level = (grade >= SkillGrade.Rare) ? 1 : 0,
                count = amount
            };
        }

        if (notify)
        {
            isInventoryDirty = true;
            OnInventoryChanged?.Invoke();
        }
    }

    public OwnedSkillData GetSkill(OwnedSkillKey key)
    {
        inventory.TryGetValue(key, out var data);
        return data;
    }

    public List<OwnedSkillData> GetAllSkills()
    {
        return new List<OwnedSkillData>(inventory.Values);
    }

    public List<OwnedSkillData> GetHighestGradeSkills()
    {
        if (!isInventoryDirty) return cachedHighestSkills;

        bestGradeBuffer.Clear();
        foreach (var data in inventory.Values)
        {
            if (!bestGradeBuffer.TryGetValue(data.skillID, out var current) || data.grade > current.grade)
                bestGradeBuffer[data.skillID] = data;
        }
        cachedHighestSkills.Clear();
        cachedHighestSkills.AddRange(bestGradeBuffer.Values);
        isInventoryDirty = false;
        return cachedHighestSkills;
    }

    #endregion

    #region 합성 (MergeHandler)

    public int MergeChain(int skillID, bool notify = true)
    {
        int total = mergeHandler.MergeChain(skillID, (id, g, amount) => AddSkill(id, g, amount, false));

        if (total > 0 && notify)
        {
            isInventoryDirty = true;
            OnInventoryChanged?.Invoke();
        }
        return total;
    }

    public int MergeAllSkills()
    {
        int total = mergeHandler.MergeAllSkills((id, g, amount) => AddSkill(id, g, amount, false));

        if (total > 0)
        {
            isInventoryDirty = true;
            OnInventoryChanged?.Invoke();
        }
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

    public bool SetPresetSlot(int slotIndex, OwnedSkillKey skillKey)
    {
        if (!presetHandler.SetPresetSlot(slotIndex, skillKey)) return false;
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
}