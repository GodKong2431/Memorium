using System;
using System.Collections.Generic;

public sealed class SkillInventoryModule : IInventoryModule
{
    public int goldId = 0;
    private const int PresetCount = 3; // 프리셋 슬롯 그룹 개수.

    private readonly Dictionary<int, OwnedSkillData> skillDataById = new Dictionary<int, OwnedSkillData>(); // 스킬 ID별 보유 정보.
    private readonly Dictionary<int, List<int>> skillIdsByScrollId = new Dictionary<int, List<int>>(); // 스크롤 ID별 스킬 후보 목록.

    private readonly SkillPreset[] presets = new SkillPreset[PresetCount]; // 장착 프리셋 배열.
    private readonly SkillPresetHandler presetHandler; // 프리셋 조작 로직 전담 객체.

    //젬 프리셋 관리
    private readonly SkillGemPresetHandler gemPresetManager = new SkillGemPresetHandler();

    private bool isScrollMapReady; // 스크롤 매핑 초기화 여부.

    public event Action OnInventoryChanged; // 스킬 인벤토리 변경 이벤트.
    public event Action<OwnedSkillData> OnInventoryChagedByData; // 스킬 인벤토리 변경 이벤트(데이터를 매개변수로 함)
    public event Action<int> OnPresetChanged; // 프리셋 변경 이벤트.
    
    public int GetPresstNum => presets.Length;

    public int CurrentPresetIndex => presetHandler.CurrentPresetIndex; // 현재 선택된 프리셋 인덱스.


    private Dictionary<int, BigDouble> skillUpCostByLevel;

    private const int UNLOCK_COST = 3;

    public SkillInventoryModule()
    {
        InventoryManager.Instance.saveSkillData = JSONService.Load<SaveSkillData>();
        InventoryManager.Instance.saveSkillData.InitSkillData();

        List<OwnedSkillData> ownedSkillDatas = InventoryManager.Instance.saveSkillData.LoadSkillData();
        if (ownedSkillDatas.Count > 0)
        {
            foreach (OwnedSkillData data in ownedSkillDatas)
            {
                if (data.level < 1)
                    data.level = 1;
                skillDataById[data.skillID] = data;
            }
        }
        for (int i = 0; i < PresetCount; i++)
            presets[i] = NormalizeLoadedPreset(InventoryManager.Instance.saveSkillData.LoadSkillPreset(i));

        presetHandler = new SkillPresetHandler(skillDataById, presets);

        //프리셋 데이터 혹은 프리셋 변경 시 이벤트
        OnPresetChanged += (ctx) => { 
            InventoryManager.Instance.saveSkillData.SaveSkillPreset(ctx, GetPreset(ctx));
        };
        if (InventoryManager.Instance.saveSkillData.gemPresetSaveData != null
             && InventoryManager.Instance.saveSkillData.gemPresetSaveData.Count > 0)
        {
            gemPresetManager.LoadFromList(InventoryManager.Instance.saveSkillData.LoadGemPresetData());
        }

        //프리셋에 저장된 데이터 불러오기
        OnPresetChanged += InventoryManager.Instance.saveSkillData.SavePresetNum;
        OnPresetChanged += (ctx) =>
        {
            InventoryManager.Instance.saveSkillData.SaveGemPresetData(gemPresetManager.GetSaveList());
        };
        //저장된 프리셋 번호 불러오기
        SwitchPreset(InventoryManager.Instance.saveSkillData.SavedPresetNum);

        OnInventoryChagedByData += InventoryManager.Instance.saveSkillData.SaveSkillInfoData;
        OnInventoryChanged?.Invoke();
    }

    #region IInventoryModule
    // 스킬 모듈은 스킬 주문서 타입만 허브 라우팅 대상으로 처리한다.
    public bool CanHandle(ItemType itemType)
    {
        return false;
    }

    // 스킬 주문서 추가 요청을 랜덤 스킬 지급으로 변환한다.
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        return false;
    }

    // 스킬 주문서 차감은 별도 요구사항이 없어 기본적으로 지원하지 않는다.
    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        return false;
    }

    // 스킬 주문서 보유량은 별도 저장하지 않으므로 0을 반환한다.
    public BigDouble GetAmount(InventoryItemContext item)
    {
        return BigDouble.Zero;
    }
    #endregion
    private void BuildSkillUpCostMapping()
    {
        if (skillUpCostByLevel != null) return;
        skillUpCostByLevel = new Dictionary<int, BigDouble>();

        foreach (var pair in DataManager.Instance.SkillUpDict)
        {
            skillUpCostByLevel[pair.Value.skillLevel] = pair.Value.reqScroll;
        }
    }

    #region UI 표시용

    // 특정 스킬의 보유 데이터를 반환한다.
    public OwnedSkillData GetSkillData(int skillId)
    {
        skillDataById.TryGetValue(skillId, out var data);
        return data;
    }
    /// <summary>
    /// 해당 스킬 해금 가능 여부 반환
    /// </summary>
    public bool CanUnlockSkill(int skillId)
    {
        if (skillDataById.ContainsKey(skillId)) return false;

        int scrollItemId = GetScrollItemId(skillId);
        if (scrollItemId <= 0) return false;

        return InventoryManager.Instance.HasEnoughItem(scrollItemId, new BigDouble(UNLOCK_COST));
    }
    /// <summary>
    /// 해당 스킬이 현재 프리셋에 장착되어있는지 반환
    /// </summary>
    public bool IsEquippedInCurrentPreset(int skillId)
    {
        var preset = presetHandler.GetCurrentPreset();
        if (preset == null || preset.slots == null) return false;

        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (preset.slots[i] != null && !preset.slots[i].IsEmpty && preset.slots[i].skillID == skillId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 레벨업 코스트 반환
    /// </summary>
    public bool TryGetLevelUpCost(int skillId, out BigDouble cost)
    {
        cost = BigDouble.Zero;

        if (!skillDataById.TryGetValue(skillId, out var data))
            return false;

        if (!data.CanLevelUp)
            return false;

        BuildSkillUpCostMapping();
        return skillUpCostByLevel.TryGetValue(data.level, out cost);
    }

    /// <summary>
    /// 해당 스킬 스크롤 보유 갯수 반환
    /// </summary>
    public BigDouble GetOwnedScrollCount(int skillId)
    {
        if (!skillDataById.TryGetValue(skillId, out var data))
            return BigDouble.Zero;
        return data.GetOwnedScrollCount();
    }

    /// <summary>
    /// 해당 스킬 스크롤 레벨업 요구 갯수 반환
    /// </summary>
    public BigDouble GetLevelUpCost(int skillId)
    {
        if (!skillDataById.TryGetValue(skillId, out var data))
            return BigDouble.Zero;
        return data.GetLevelUpCost();
    }

    /// <summary>
    /// 해당 스킬 등급 반환
    /// </summary>
    public SkillGrade GetGrade(int skillId)
    {
        if (!skillDataById.TryGetValue(skillId, out var data))
            return SkillGrade.Common;
        return data.GetGrade();
    }


    /// <summary>
    /// 해당 스킬 장착 가능한지 반환
    /// </summary>
    public bool IsEquippable(int skillId)
    {
        if (!skillDataById.TryGetValue(skillId, out var data))
            return false;
        return data.IsEquippable;
    }

    /// <summary>
    /// 해당 스킬 레벨업 가능 여부 반환(스크롤 충분히 있는지)
    /// </summary>
    public bool CanLevelUpSkill(int skillId)
    {
        if (!TryGetLevelUpCost(skillId, out BigDouble cost))
            return false;

        int scrollItemId = GetScrollItemId(skillId);
        if (scrollItemId <= 0) return false;

        return InventoryManager.Instance.HasEnoughItem(scrollItemId, cost);
    }


    /// <summary>
    /// 해당 스킬 레벨반환
    /// </summary>
    public int GetLevel(int skillId)
    {
        if (!skillDataById.TryGetValue(skillId, out var data))
            return 0;
        return data.level;
    }
    #endregion

    #region UI 버튼용

    // <summary>
    /// 해당 스킬 해금 실행 및 성공 여부 반환
    /// </summary>
    public bool TryUnlockSkill(int skillId)
    {
        if (!CanUnlockSkill(skillId)) return false;

        int scrollItemId = GetScrollItemId(skillId);
        InventoryManager.Instance.RemoveItem(scrollItemId, new BigDouble(UNLOCK_COST));

        skillDataById[skillId] = new OwnedSkillData
        {
            skillID = skillId,
            level = 1
        };

        OnInventoryChanged?.Invoke();
        OnInventoryChagedByData?.Invoke(skillDataById[skillId]);
        return true;
    }
    /// <summary>
    /// 해당 스킬 레벨업 실행 및 성공 여부 반환
    /// </summary>
    public bool TryLevelUpSkill(int skillId)
    {
        if (!CanLevelUpSkill(skillId))
            return false;

        TryGetLevelUpCost(skillId, out BigDouble cost);

        int scrollItemId = GetScrollItemId(skillId);
        if (scrollItemId <= 0) return false;

        InventoryManager.Instance.RemoveItem(scrollItemId, cost);
        skillDataById[skillId].AddLevel();

        OnInventoryChanged?.Invoke();
        OnInventoryChagedByData?.Invoke(skillDataById[skillId]);
        return true;
    }
    #endregion

    private int GetScrollItemId(int skillId)
    {
        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out var table))
            return 0;
        return table.skillScrollID;
    }



    // 현재 선택된 프리셋을 반환한다.
    public SkillPreset GetCurrentPreset()
    {
        return presetHandler.GetCurrentPreset();
    }

    public SkillPreset GetCurrentPresetSnapshot()
    {
        SkillPreset preset = presetHandler.GetCurrentPreset();
        return preset != null ? preset.Clone() : new SkillPreset();
    }

    // 지정 인덱스의 프리셋을 반환한다.
    public SkillPreset GetPreset(int index)
    {
        return presetHandler.GetPreset(index);
    }

    public SkillPreset GetPresetSnapshot(int index)
    {
        SkillPreset preset = presetHandler.GetPreset(index);
        return preset != null ? preset.Clone() : new SkillPreset();
    }

    // 프리셋 탭을 전환한다.
    public void SwitchPreset(int index)
    {
        int prevIndex = presetHandler.CurrentPresetIndex;
        SkillPreset prevPreset = presetHandler.GetCurrentPreset();

        presetHandler.SwitchPreset(index);

        SkillPreset nextPreset = presetHandler.GetCurrentPreset();
        gemPresetManager.OnPresetSwitch(prevIndex, index, prevPreset, nextPreset);

        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }
    public SkillGemSlotData GetGemSlotData(int skillId)
    {
        if (skillId <= 0)
            return null;

        SkillGemSlotData gemData = gemPresetManager.GetPreset(presetHandler.CurrentPresetIndex)?.Get(skillId);
        if (gemData != null)
            return gemData.Clone();

        SkillPreset currentPreset = presetHandler.GetCurrentPreset();
        if (currentPreset?.slots == null)
            return null;

        for (int i = 0; i < currentPreset.slots.Length; i++)
        {
            SkillPresetSlot slot = currentPreset.slots[i];
            if (slot == null || slot.IsEmpty || slot.skillID != skillId)
                continue;

            SkillGemSlotData fallbackData = new SkillGemSlotData(skillId);
            fallbackData.m4JemID = slot.m4JemID;
            fallbackData.m5JemIDs[0] = slot.m5JemIDs != null && slot.m5JemIDs.Length > 0
                ? slot.m5JemIDs[0]
                : SkillPresetSlot.EmptySkillId;
            fallbackData.m5JemIDs[1] = slot.m5JemIDs != null && slot.m5JemIDs.Length > 1
                ? slot.m5JemIDs[1]
                : SkillPresetSlot.EmptySkillId;
            return fallbackData;
        }

        return null;
    }
    // 프리셋 슬롯에 스킬을 장착한다.
    public bool SetPresetSlot(int slotIndex, int skillId)
    {
        if (!presetHandler.SetPresetSlot(slotIndex, skillId))
            return false;

        var slot = presetHandler.GetCurrentPreset().slots[slotIndex];
        slot.m5JemIDs[0] = -1;
        slot.m5JemIDs[1] = -1;
        slot.m4JemID = -1;

        var gemData = gemPresetManager.GetPreset(presetHandler.CurrentPresetIndex)?.Get(skillId);
        if (gemData != null)
        {
            slot.m5JemIDs[0] = gemData.m5JemIDs[0];
            slot.m5JemIDs[1] = gemData.m5JemIDs[1];
            slot.m4JemID = gemData.m4JemID;
        }
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    // 프리셋 슬롯을 비운다.
    public void ClearPresetSlot(int slotIndex)
    {
        presetHandler.ClearPresetSlot(slotIndex);
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }
    public void SetGemDirect(int skillId, int slotIndex, int gemId, bool isM4)
    {
        gemPresetManager.SetGem(presetHandler.CurrentPresetIndex, skillId, slotIndex, gemId, isM4);
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }
    // 지정 프리셋 슬롯의 M4 젬을 설정한다.
    public bool SetM4Jem(int presetSlotIndex, int jemId)
    {
        if (!presetHandler.SetM4Jem(presetSlotIndex, jemId))
            return false;
        var slot = presetHandler.GetCurrentPreset().slots[presetSlotIndex];
        gemPresetManager.SetGem(presetHandler.CurrentPresetIndex, slot.skillID, 0, jemId, true);

        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    // 지정 프리셋 슬롯의 M5 젬을 설정한다.
    public bool SetM5Jem(int presetSlotIndex, int m5SlotIndex, int jemId)
    {
        if (!presetHandler.SetM5Jem(presetSlotIndex, m5SlotIndex, jemId))
            return false;

        var slot = presetHandler.GetCurrentPreset().slots[presetSlotIndex];
        gemPresetManager.SetGem(presetHandler.CurrentPresetIndex, slot.skillID, m5SlotIndex, jemId, false);

        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    // 스크롤 ID를 이용해 랜덤 스킬 ID를 반환한다.
    public bool TryGetRandomSkillIdByScroll(int scrollItemId, out int skillId)
    {
        skillId = 0;
        EnsureScrollMap();

        if (!skillIdsByScrollId.TryGetValue(scrollItemId, out var skillIds) || skillIds.Count == 0)
            return false;

        skillId = skillIds[UnityEngine.Random.Range(0, skillIds.Count)];
        return true;
    }

    // 스킬 테이블을 읽어 스크롤 ID와 스킬 후보 목록을 만든다.
    public void EnsureScrollMap()
    {
        if (isScrollMapReady)
            return;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.SkillInfoDict == null)
            return;

        skillIdsByScrollId.Clear();

        foreach (var skill in DataManager.Instance.SkillInfoDict)
        {
            int scrollId = skill.Value.skillScrollID;
            if (!skillIdsByScrollId.TryGetValue(scrollId, out var list))
            {
                list = new List<int>();
                skillIdsByScrollId[scrollId] = list;
            }

            list.Add(skill.Key);
        }

        isScrollMapReady = true;
    }

    // BigDouble 수량을 정수 반복 횟수로 변환한다.
    public Dictionary<int, int> GetOwnedScrollItemCounts()
    {
        Dictionary<int, int> countsByScrollId = new Dictionary<int, int>();

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.SkillInfoDict == null)
            return countsByScrollId;

        foreach (KeyValuePair<int, OwnedSkillData> pair in skillDataById)
        {
            OwnedSkillData skillData = pair.Value;
            if (skillData == null)
                continue;

            if (!DataManager.Instance.SkillInfoDict.TryGetValue(pair.Key, out SkillInfoTable skillInfo))
                continue;

            int scrollItemId = skillInfo.skillScrollID;
            if (scrollItemId == 0)
                continue;

            //int scrollCount = skillData.GetCount(SkillGrade.Scroll);
            //if (scrollCount <= 0)
            //    continue;

            //if (countsByScrollId.ContainsKey(scrollItemId))
            //    countsByScrollId[scrollItemId] += scrollCount;
            //else
            //    countsByScrollId[scrollItemId] = scrollCount;
        }

        return countsByScrollId;
    }

    private static bool TryConvertAmountToInt(BigDouble amount, out int count)
    {
        count = 0;
        if (amount <= BigDouble.Zero)
            return false;

        double value = amount.ToDouble();
        if (double.IsNaN(value) || double.IsInfinity(value))
            return false;

        double floored = Math.Floor(value);
        if (floored < 1 || floored > int.MaxValue)
            return false;

        count = (int)floored;
        return true;
    }
    public void NotifyPresetChanged()
    {
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }

    private SkillPreset NormalizeLoadedPreset(SkillPreset preset)
    {
        int slotCount = InventoryManager.Instance.saveSkillData.SkillCountByPreset;
        SkillPresetSlot[] normalizedSlots = new SkillPresetSlot[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            SkillPresetSlot slot = preset != null && preset.slots != null && i < preset.slots.Length && preset.slots[i] != null
                ? preset.slots[i]
                : new SkillPresetSlot();

            slot.Normalize();
            if (!slot.IsEmpty && !IsValidEquippedSkill(slot.skillID))
                slot.Clear();

            normalizedSlots[i] = slot;
        }

        return new SkillPreset(normalizedSlots);
    }

    private bool IsValidEquippedSkill(int skillId)
    {
        if (skillId <= 0)
            return false;

        if (!skillDataById.TryGetValue(skillId, out OwnedSkillData skillData))
            return false;

        return skillData.IsEquippable;
    }
    public void SetGoldID()
    {

        if (goldId != 0)
            return;
        else
        {
            foreach (var item in DataManager.Instance.ItemInfoDict)
            {
                if (item.Value.itemType == ItemType.FreeCurrency)
                {
                    goldId = item.Key;
                    break;
                }
            }
        }
    }
}
