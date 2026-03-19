using System;
using System.Collections.Generic;

public sealed class SkillInventoryModule : IInventoryModule
{
    public int goldId = 0;
    private const int PresetCount = 3; // 프리셋 슬롯 그룹 개수.

    private readonly Dictionary<int, OwnedSkillData> skillDataById = new Dictionary<int, OwnedSkillData>(); // 스킬 ID별 보유 정보.
    private readonly Dictionary<int, List<int>> skillIdsByScrollId = new Dictionary<int, List<int>>(); // 스크롤 ID별 스킬 후보 목록.

    private readonly SkillPreset[] presets = new SkillPreset[PresetCount]; // 장착 프리셋 배열.
    private readonly SkillMergeHandler mergeHandler; // 스킬 합성 로직 전담 객체.
    private readonly SkillPresetHandler presetHandler; // 프리셋 조작 로직 전담 객체.

    private bool isScrollMapReady; // 스크롤 매핑 초기화 여부.

    public event Action OnInventoryChanged; // 스킬 인벤토리 변경 이벤트.
    public event Action<OwnedSkillData> OnInventoryChagedByData; // 스킬 인벤토리 변경 이벤트(데이터를 매개변수로 함)
    public event Action<int> OnPresetChanged; // 프리셋 변경 이벤트.
    
    public int GetPresstNum => presets.Length;

    public int CurrentPresetIndex => presetHandler.CurrentPresetIndex; // 현재 선택된 프리셋 인덱스.


    private Dictionary<int, BigDouble> skillUpCostByLevel;



    public SkillInventoryModule()
    {
        InventoryManager.Instance.saveSkillData = JSONService.Load<SaveSkillData>();
        InventoryManager.Instance.saveSkillData.InitSkillData();

        List<OwnedSkillData> ownedSkillDatas = InventoryManager.Instance.saveSkillData.LoadSkillData();
        if (ownedSkillDatas.Count > 0)
        {
            foreach (OwnedSkillData data in ownedSkillDatas)
            {
                skillDataById[data.skillID] = data;
            }
        }

        for (int i = 0; i < PresetCount; i++)
            presets[i] = NormalizeLoadedPreset(InventoryManager.Instance.saveSkillData.LoadSkillPreset(i));

        mergeHandler = new SkillMergeHandler(skillDataById);
        presetHandler = new SkillPresetHandler(skillDataById, presets);

        //프리셋 데이터 혹은 프리셋 변경 시 이벤트
        OnPresetChanged += (ctx) => { 
            InventoryManager.Instance.saveSkillData.SaveSkillPreset(ctx, GetPreset(ctx));
        };

        //프리셋에 저장된 데이터 불러오기
        OnPresetChanged += InventoryManager.Instance.saveSkillData.SavePresetNum;
        //저장된 프리셋 번호 불러오기
        SwitchPreset(InventoryManager.Instance.saveSkillData.SavedPresetNum);

        OnInventoryChagedByData += InventoryManager.Instance.saveSkillData.SaveSkillInfoData;
        OnInventoryChanged?.Invoke();
    }

    #region IInventoryModule
    // 스킬 모듈은 스킬 주문서 타입만 허브 라우팅 대상으로 처리한다.
    public bool CanHandle(ItemType itemType)
    {
        return itemType == ItemType.SkillScroll;
    }

    // 스킬 주문서 추가 요청을 랜덤 스킬 지급으로 변환한다.
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemType != ItemType.SkillScroll)
            return false;

        if (!TryConvertAmountToInt(amount, out int count))
            return false;

        int skillIdData = 0;
        for (int i = 0; i < count; i++)
        {
            if (!TryGetRandomSkillIdByScroll(item.ItemId, out int skillId))
                return false;

            AddSkill(skillId, SkillGrade.Scroll, 1, notify: false);
            skillIdData = skillId;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    // 스킬 주문서 차감은 별도 요구사항이 없어 기본적으로 지원하지 않는다.
    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        return false;
    }

    // 스킬 주문서 보유량은 별도 저장하지 않으므로 0을 반환한다.
    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!TryGetRandomSkillIdByScroll(item.ItemId, out int skillId))
            return BigDouble.Zero;

        if (!skillDataById.TryGetValue(skillId, out OwnedSkillData skillData))
        {
            return BigDouble.Zero;
        }
        BigDouble count = skillData.GetCount(SkillGrade.Scroll);
        return count;
    }
    #endregion
    private void BuildSkillUpCostMapping()
    {
        if (skillUpCostByLevel != null) return;
        skillUpCostByLevel = new Dictionary<int, BigDouble>();

        foreach (var pair in DataManager.Instance.SkillUpDict)
        {
            skillUpCostByLevel[pair.Value.skillLevel] = pair.Value.reqGold;
        }
    }
 
    // 특정 스킬의 보유 데이터를 반환한다.
    public OwnedSkillData GetSkillData(int skillId)
    {
        skillDataById.TryGetValue(skillId, out var data);
        return data;
    }

    // 스킬 보유량/등급 수량을 증가시킨다.
    public void AddSkill(int skillId, SkillGrade grade = SkillGrade.Scroll, int amount = 1, bool notify = true)
    {
        if (!skillDataById.TryGetValue(skillId, out var data))
        {
            data = new OwnedSkillData
            {
                skillID = skillId,
                level = 0
            };
            skillDataById[skillId] = data;
        }

        data.AddCount(grade, amount);
        if (notify)
            OnInventoryChanged?.Invoke();
        OnInventoryChagedByData?.Invoke(skillDataById[skillId]);

    }

    // 스킬 레벨업에 필요한 골드 비용을 계산한다.
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
    public bool TryLevelUpSkill(int skillId)
    {
        if (!TryGetLevelUpCost(skillId, out BigDouble cost))
            return false;

        SetGoldID();
        if (!InventoryManager.Instance.HasEnoughItem(goldId, cost))
            return false;

        InventoryManager.Instance.RemoveItem(goldId, cost);
        skillDataById[skillId].level++;

        OnInventoryChanged?.Invoke();
        OnInventoryChagedByData?.Invoke(skillDataById[skillId]);
        return true;
    }

    // 지정 스킬 체인을 가능한 만큼 합성한다.
    public int MergeChain(int skillId, bool notify = true)
    {
        int total = mergeHandler.MergeChain(skillId, (id, grade, amount) => AddSkill(id, grade, amount, notify: false));
        if (total > 0 && notify)
            OnInventoryChanged?.Invoke();

        return total;
    }

    // 전체 스킬을 일괄 합성한다.
    public int MergeAllSkills()
    {
        int total = mergeHandler.MergeAllSkills((id, grade, amount) => AddSkill(id, grade, amount, notify: false));
        if (total > 0)
            OnInventoryChanged?.Invoke();

        return total;
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
        presetHandler.SwitchPreset(index);
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }

    // 프리셋 슬롯에 스킬을 장착한다.
    public bool SetPresetSlot(int slotIndex, int skillId)
    {
        if (!presetHandler.SetPresetSlot(slotIndex, skillId))
            return false;

        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    // 프리셋 슬롯을 비운다.
    public void ClearPresetSlot(int slotIndex)
    {
        presetHandler.ClearPresetSlot(slotIndex);
        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
    }

    // 지정 프리셋 슬롯의 M4 젬을 설정한다.
    public bool SetM4Jem(int presetSlotIndex, int jemId)
    {
        if (!presetHandler.SetM4Jem(presetSlotIndex, jemId))
            return false;

        OnPresetChanged?.Invoke(presetHandler.CurrentPresetIndex);
        return true;
    }

    // 지정 프리셋 슬롯의 M5 젬을 설정한다.
    public bool SetM5Jem(int presetSlotIndex, int m5SlotIndex, int jemId)
    {
        if (!presetHandler.SetM5Jem(presetSlotIndex, m5SlotIndex, jemId))
            return false;

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

            int scrollCount = skillData.GetCount(SkillGrade.Scroll);
            if (scrollCount <= 0)
                continue;

            if (countsByScrollId.ContainsKey(scrollItemId))
                countsByScrollId[scrollItemId] += scrollCount;
            else
                countsByScrollId[scrollItemId] = scrollCount;
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
