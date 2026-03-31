using System;
using System.Collections.Generic;
using UnityEngine;

public enum GemGrade
{
    None,
    Common,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Count,
}

[System.Serializable]
public struct GemSaveData
{
    public int gemId;
    public int count; 
    public GemGrade grade; 

    public GemSaveData(int gemId, int count, GemGrade grade)
    {
        this.gemId = gemId;
        this.count = count;
        this.grade = grade;
    }
}
public readonly struct GemDisplayData
{
    public readonly int GemId;
    public readonly GemGrade HighestGrade;
    public readonly int HighestGradeCount;

    public GemDisplayData(int gemId, GemGrade highestGrade, int highestGradeCount)
    {
        GemId = gemId;
        HighestGrade = highestGrade;
        HighestGradeCount = highestGradeCount;
    }
}
public class OwnedGemData
{
    public int gemId;
    public int[] gradeCounts = new int[(int)GemGrade.Count];// 해당 젬id 등급 만큼 갯수 , gradeCounts[1] = Common 갯수, gradeCounts[2] = Rare 갯수

    public OwnedGemData(int gemId)
    {
        this.gemId = gemId;
    }
    public int GetCount(GemGrade grade)
    {
        int index = (int)grade;
        if (index <= 0 || index >= gradeCounts.Length)
            return 0;

        return gradeCounts[index];
    }
}

public sealed class GemInventoryModule : IInventoryModule
{
    public Dictionary<int, int> ItemToM4Dict = new Dictionary<int, int>();
    public Dictionary<int, int[]> SkillToM4Dict = new Dictionary<int, int[]>();
    public Dictionary<int, int> ItemToM5Dict = new Dictionary<int, int>();
    public Dictionary<int, int[]> SkillToM5Dict = new Dictionary<int, int[]>();
    private readonly Dictionary<int, OwnedGemData> gemDict = new Dictionary<int, OwnedGemData>();

    private const int MERGE_THRESHOLD = 3;
    public event Action OnGemInventoryChanged;

    public List<GemSaveData> saveList = new List<GemSaveData>();
    private bool isMappingInitialized;

    //구조체 리스트는 저장 할 수 있다고 이해해서 이렇게 만?들어놨는데 이상하다 싶으시면 다른 방식으로 하셔도 괜찮습니다.

    /// <summary>
    /// 가지고 있는 모든 젬의 id와 등급/ 갯수를 담은 구조체 리스트 반환, UI랑 저장에 쓰시면 될?듯
    /// </summary>
    /// 
    public GemInventoryModule()
    {
        InventoryManager.Instance.saveGemData = JSONService.Load<SaveGemData>();

        //비어있는거 아니면 불러와라
        if (InventoryManager.Instance.saveGemData.InitGemData())
        {
            LoadFromList(InventoryManager.Instance.saveGemData.LoadGemInfoData());
        }

        //데이터 저장
        OnGemInventoryChanged += () => { InventoryManager.Instance.saveGemData.SaveGemInfoData(GetSaveList()); };
    }

    public List<GemSaveData> GetSaveList()
    {
        saveList.Clear();
        foreach (var pair in gemDict)
        {
            for (int i = (int)GemGrade.Common; i < (int)GemGrade.Count; i++)
            {
                if (pair.Value.gradeCounts[i] > 0)
                {
                    saveList.Add(new GemSaveData(pair.Key, pair.Value.gradeCounts[i], (GemGrade)i));
                }
            }
        }
        return saveList;
    }

    public void LoadFromList(List<GemSaveData> saveList)
    {
        gemDict.Clear();
        if (saveList == null) return;

        foreach (var saveData in saveList)
        {
            if (!gemDict.TryGetValue(saveData.gemId, out var data))
            {
                data = new OwnedGemData(saveData.gemId);
                gemDict[saveData.gemId] = data;
            }
            data.gradeCounts[(int)saveData.grade] = saveData.count;
        }
    }
    private void EnsureMappingData()
    {
        if (isMappingInitialized)
            return;

        InitGemMappingData();
    }
    #region IInventoryModule
    public bool CanHandle(ItemType itemType)
    {
        return itemType == ItemType.ElementGem
            || itemType == ItemType.UniqueGem;
    }

    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType)) return false;


        if (!TryConvertAmountToInt(amount, out int addCount)) return false;

        if (!gemDict.TryGetValue(item.ItemId, out var data))
        {
            data = new OwnedGemData(item.ItemId);
            gemDict[item.ItemId] = data;
        }

        data.gradeCounts[(int)GemGrade.Common] += addCount;
        TryMerge(data);

        OnGemInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        return false;
    }
    public BigDouble GetAmount(InventoryItemContext item)
    {
        return BigDouble.Zero;
    }
    #endregion

    #region UI 표시용 



    /// <summary>
    /// 해당 스킬이 프리셋에 장착되어있는지 반환 (젬 슬롯 활성화 판단용)
    /// </summary>
    public bool IsSkillEquipped(int skillId)
    {
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return false;
        return skillModule.IsEquippedInCurrentPreset(skillId);
    }

    /// <summary>
    /// 스킬 ID로 해당 스킬의 M4 슬롯 해금 여부를 반환 / 마지막칸
    /// </summary>
    public bool IsM4SlotUnlocked(int skillId)
    {
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        var skillData = skillModule?.GetSkillData(skillId);
        
        return skillData?.IsM4JemSlotOpen ?? false;
    }

    /// <summary>
    /// 스킬 ID로 해당 스킬의 M5 슬롯 해금 여부를 반환 / m5SlotIndex=0 첫번째칸 10 제한 /m5SlotIndex=1 두번째칸 100제한
    /// </summary>
    public bool IsM5SlotUnlocked(int skillId, int m5SlotIndex)
    {
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        var skillData = skillModule?.GetSkillData(skillId);

        return skillData?.IsM5JemSlotOpen(m5SlotIndex) ?? false;
    }

    /// <summary>
    /// 보유한 젬 목록중 장착 가능한 m4 모듈 젬 리스트 반환
    /// </summary>
    /// <param name="skillId"></param>
    /// <returns></returns>
    public List<OwnedGemData> GetEquippableOwnedM4Gems(int skillId)
    {
        EnsureMappingData();
        GetEquippedGemIds(skillId, out int eqM4, out int eqM5_0, out int eqM5_1);
        List<OwnedGemData> result = new List<OwnedGemData>();

        if (SkillToM4Dict.TryGetValue(skillId, out int[] m4Array))
        {
            for (int i = 0; i < m4Array.Length; i++)
            {
                int m4Id = m4Array[i];
                if (m4Id == 0) continue; 

                int itemId = GetItemIdByM4Id(m4Id);
                if (itemId == 0) continue;

                if (gemDict.TryGetValue(itemId, out var ownedData))
                {
                    if (itemId == eqM4) continue;
                    result.Add(ownedData);
                }
            }
        }
        return result;
    }
    /// <summary>
    /// 보유한 젬 목록중 장착 가능한 m5 모듈 젬 리스트 반환
    /// </summary>
    public List<OwnedGemData> GetEquippableOwnedM5Gems(int skillId)
    {
        EnsureMappingData(); 
        GetEquippedGemIds(skillId, out int eqM4, out int eqM5_0, out int eqM5_1);

        List<OwnedGemData> result = new List<OwnedGemData>();

        if (SkillToM5Dict.TryGetValue(skillId, out int[] m5Array))
        {
            for (int i = 0; i < m5Array.Length; i++)
            {
                int m5Id = m5Array[i];
                if (m5Id == 0) continue;

                int itemId = GetItemIdByM5Id(m5Id);
                if (itemId == 0) continue;

                if (gemDict.TryGetValue(itemId, out var ownedData))
                {
                    if (itemId == eqM5_0 || itemId == eqM5_1) continue;
                    result.Add(ownedData);
                }
            }
        }
        return result;
    }
    public List<GemDisplayData> GetEquippableM4GemDisplayList(int skillId)
    {
        return ConvertToDisplayData(GetEquippableOwnedM4Gems(skillId));
    }

    public List<GemDisplayData> GetEquippableM5GemDisplayList(int skillId)
    {
        return ConvertToDisplayData(GetEquippableOwnedM5Gems(skillId));
    }

    private List<GemDisplayData> ConvertToDisplayData(List<OwnedGemData> ownedGems)
    {
        List<GemDisplayData> result = new List<GemDisplayData>();
        if (ownedGems == null)
            return result;

        for (int i = 0; i < ownedGems.Count; i++)
        {
            OwnedGemData data = ownedGems[i];
            if (data == null)
                continue;

            GemGrade highestGrade = GetHighestGrade(data.gemId);
            if (highestGrade == GemGrade.None)
                continue;

            int count = data.GetCount(highestGrade);
            if (count <= 0)
                continue;

            result.Add(new GemDisplayData(data.gemId, highestGrade, count));
        }

        return result;
    }
    #endregion

    #region UI 버튼용


    /// <summary>
    /// 스킬이 현재 프리셋에 있다면 해당 스킬에 M4 젬을 장착
    /// </summary>
    public bool TryEquipM4GemBySkillId(int skillId, int gemId)
    {
        int presetSlotIndex = GetPresetSlotIndexBySkillId(skillId);

        if (presetSlotIndex != -1)
            return TryEquipM4Gem(presetSlotIndex, gemId);
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return false;

        skillModule.SetGemDirect(skillId, 0, gemId, true);
        return true;
    }

    /// <summary>
    /// 스킬이 현재 프리셋에 있다면 해당 스킬에 M5 젬을 장착
    /// </summary>
    public bool TryEquipM5GemBySkillId(int skillId, int m5SlotIndex, int gemId)
    {
        int presetSlotIndex = GetPresetSlotIndexBySkillId(skillId);

        if (presetSlotIndex != -1)
            return TryEquipM5Gem(presetSlotIndex, m5SlotIndex, gemId);

        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return false;

        skillModule.SetGemDirect(skillId, m5SlotIndex, gemId, false);
        return true;
    }
    #region Private
    /// <summary>
    /// 현재 프리셋에서 특정 스킬 ID가 몇 번째 슬롯에 있는지 확인
    /// </summary>
    private int GetPresetSlotIndexBySkillId(int skillId)
    {
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return -1;

        var currentPreset = skillModule.GetCurrentPreset();
        for (int i = 0; i < currentPreset.slots.Length; i++)
        {
            if (currentPreset.slots[i].skillID == skillId)
                return i;
        }

        return -1;
    }
    /// <summary>
    /// 특정 프리셋 슬롯의 스킬에 M4 젬 장착을 요청
    /// </summary>
    private bool TryEquipM4Gem(int presetSlotIndex, int gemId)
    {
        if (gemId == -1) return CallSetM4Jem(presetSlotIndex, -1);

        if (!gemDict.ContainsKey(gemId)) return false;

        if (!ItemToM4Dict.ContainsKey(gemId)) return false;
        if (GetHighestGrade(gemId) == GemGrade.None) return false;


        return CallSetM4Jem(presetSlotIndex, gemId);
    }

    /// <summary>
    /// 특정 프리셋 슬롯의 스킬에 M5 젬 장착을 요청
    /// </summary>
    private bool TryEquipM5Gem(int presetSlotIndex, int m5SlotIndex, int gemId)
    {
        if (gemId == -1)
        {
            return CallSetM5Jem(presetSlotIndex, m5SlotIndex, -1);
        }

        if (!gemDict.ContainsKey(gemId)) return false;

        if (!ItemToM5Dict.ContainsKey(gemId)) return false;
        if (GetHighestGrade(gemId) == GemGrade.None) return false;

        return CallSetM5Jem(presetSlotIndex, m5SlotIndex, gemId);
    }

    private bool CallSetM4Jem(int presetSlotIndex, int gemId)
    {
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return false;

        return skillModule.SetM4Jem(presetSlotIndex, gemId);
    }

    private bool CallSetM5Jem(int presetSlotIndex, int m5SlotIndex, int gemId)
    {
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return false;

        return skillModule.SetM5Jem(presetSlotIndex, m5SlotIndex, gemId);
    }
    #endregion
    #endregion

    #region 매핑
    public void InitGemMappingData()
    {
        BuildM4Mapping();
        BuildM5Mapping();
    }

    private void BuildM4Mapping()
    {
        ItemToM4Dict.Clear();
        SkillToM4Dict.Clear();

        var m4Dict = DataManager.Instance.SkillModule4Dict;
        if (m4Dict == null) return;

        foreach (var m4Data in m4Dict.Values)
        {
            if (m4Data.m4ItemID != 0)
            {
                ItemToM4Dict[m4Data.m4ItemID] = m4Data.ID; 
            }

            if (m4Data.skillID != 0)
            {
                if (!SkillToM4Dict.TryGetValue(m4Data.skillID, out int[] m4Array))
                {
                    m4Array = new int[4]; 
                    SkillToM4Dict[m4Data.skillID] = m4Array;
                }

                int typeIndex = (int)m4Data.m4Type;

                if (typeIndex >= 0 && typeIndex < m4Array.Length)
                {
                    m4Array[typeIndex] = m4Data.ID;
                }
            }
        }
    }

    private void BuildM5Mapping()
    {
        ItemToM5Dict.Clear();
        SkillToM5Dict.Clear();

        var m5Dict = DataManager.Instance.SkillModule5Dict;
        if (m5Dict == null) return;

        foreach (var m5Data in m5Dict.Values)
        {
            if (m5Data.m5ItemID != 0)
            {
                ItemToM5Dict[m5Data.m5ItemID] = m5Data.ID;
            }

            if (m5Data.skillID != 0)
            {
                if (!SkillToM5Dict.TryGetValue(m5Data.skillID, out int[] m5Array))
                {
                    m5Array = new int[3];
                    SkillToM5Dict[m5Data.skillID] = m5Array;
                }

                int typeIndex = (int)m5Data.m5Type;

                if (typeIndex >= 0 && typeIndex < m5Array.Length)
                {
                    m5Array[typeIndex] = m5Data.ID;
                }
            }
        }
    }
    #endregion

    #region M4 변환 Getter 

    public int GetM4IdByItemId(int itemId)
    {
        EnsureMappingData();
        if (ItemToM4Dict.TryGetValue(itemId, out int m4Id))
            return m4Id;
        return 0;
    }
    public int GetItemIdByM4Id(int m4Id)
    {
        EnsureMappingData();
        if (DataManager.Instance.SkillModule4Dict.TryGetValue(m4Id, out var m4Data))
            return m4Data.m4ItemID;
        return 0;
    }
    public int GetM4IdBySkillId(int skillId, int m4Type)
    {
        EnsureMappingData();
        if (SkillToM4Dict.TryGetValue(skillId, out int[] m4Array))
        {
            if (m4Type >= 0 && m4Type < m4Array.Length)
                return m4Array[m4Type];
        }
        return 0;
    }

    public int GetSkillIdByM4Id(int m4Id)
    {
        EnsureMappingData();
        if (DataManager.Instance.SkillModule4Dict.TryGetValue(m4Id, out var m4Data))
            return m4Data.skillID;
        return 0;
    }

    public int GetSkillIdByM4ItemId(int itemId)
    {
        EnsureMappingData();
        int m4Id = GetM4IdByItemId(itemId);
        if (m4Id != 0)
            return GetSkillIdByM4Id(m4Id);
        return 0;
    }

    public int GetM4ItemIdBySkillId(int skillId, int m4Type)
    {
        EnsureMappingData();
        int m4Id = GetM4IdBySkillId(skillId, m4Type);
        if (m4Id != 0)
            return GetItemIdByM4Id(m4Id);
        return 0;
    }

    #endregion

    #region M5 변환 Getter
    public int GetM5IdByItemId(int itemId)
    {
        EnsureMappingData();
        if (ItemToM5Dict.TryGetValue(itemId, out int m5Id))
            return m5Id;
        return 0;
    }
    
    public int GetItemIdByM5Id(int m5Id)
    {
        EnsureMappingData();
        if (DataManager.Instance.SkillModule5Dict.TryGetValue(m5Id, out var m5Data))
            return m5Data.m5ItemID;
        return 0;
    }

    public int GetM5IdBySkillId(int skillId, int m5Type)
    {
        EnsureMappingData();
        if (SkillToM5Dict.TryGetValue(skillId, out int[] m5Array))
        {
            if (m5Type >= 0 && m5Type < m5Array.Length)
                return m5Array[m5Type];
        }
        return 0;
    }

    public int GetSkillIdByM5Id(int m5Id)
    {
        EnsureMappingData();
        if (DataManager.Instance.SkillModule5Dict.TryGetValue(m5Id, out var m5Data))
            return m5Data.skillID;
        return 0;
    }

    public int GetSkillIdByM5ItemId(int itemId)
    {
        EnsureMappingData();
        int m5Id = GetM5IdByItemId(itemId);
        if (m5Id != 0)
            return GetSkillIdByM5Id(m5Id);
        return 0;
    }

    public int GetM5ItemIdBySkillId(int skillId, int m5Type)
    {
        EnsureMappingData();
        int m5Id = GetM5IdBySkillId(skillId, m5Type);
        if (m5Id != 0)
            return GetItemIdByM5Id(m5Id);
        return 0;
    }

    #endregion
    public IEnumerable<OwnedGemData> GetAllGems()
    {
        return gemDict.Values;
    }

    #region 자동 합성

    private void TryMerge(OwnedGemData data)
    {
        for (int i = (int)GemGrade.Common; i < (int)GemGrade.Mythic; i++)
        {
            int count = data.gradeCounts[i] / MERGE_THRESHOLD;
            if (count > 0)
            {
                data.gradeCounts[i] -= MERGE_THRESHOLD*count;
                data.gradeCounts[i + 1] += count;

            }
        }
    }
    #endregion

    /// <summary>
    /// 스킬 데미지 계산용
    /// </summary>
    public GemGrade GetHighestGrade(int gemId)
    {
        if (!gemDict.TryGetValue(gemId, out var data))
            return GemGrade.None;

        for (int i = (int)GemGrade.Mythic; i >= (int)GemGrade.Common; i--)
        {
            if (data.gradeCounts[i] > 0) 
                return (GemGrade)i;
        }
        return GemGrade.None;
    }
    private void GetEquippedGemIds(int skillId, out int m4Id, out int m5Id0, out int m5Id1)
    {
        m4Id = 0; m5Id0 = 0; m5Id1 = 0;

        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null) return;

        var gemSlot = skillModule.GetGemSlotData(skillId);
        if (gemSlot == null) return;

        m4Id = gemSlot.m4JemID;
        m5Id0 = gemSlot.m5JemIDs != null && gemSlot.m5JemIDs.Length > 0 ? gemSlot.m5JemIDs[0] : 0;
        m5Id1 = gemSlot.m5JemIDs != null && gemSlot.m5JemIDs.Length > 1 ? gemSlot.m5JemIDs[1] : 0;
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
    public void DebugPrintSkillGemStatus(int skillId, int slotIndex)
    {
        EnsureMappingData();
        bool isM4 = (slotIndex == 2);
        string typeStr = isM4 ? "M4" : "M5";

        Debug.Log($"[디버그] 스킬 ID: {skillId}, 슬롯 인덱스: {slotIndex} ({typeStr} 젬 확인 시작)");

        if (isM4)
        {
            if (SkillToM4Dict.TryGetValue(skillId, out int[] m4Array))
                PrintGemDebugInfo(m4Array, true);
            else
                Debug.Log($" -> SkillToM4Dict에 매핑 데이터가 없습니다.");
        }
        else
        {
            if (SkillToM5Dict.TryGetValue(skillId, out int[] m5Array))
                PrintGemDebugInfo(m5Array, false);
            else
                Debug.Log($" -> SkillToM5Dict에 매핑 데이터가 없습니다.");
        }
    }

    private void PrintGemDebugInfo(int[] array, bool isM4)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int mappingId = array[i];
            if (mappingId == 0) continue;

            int itemId = isM4 ? GetItemIdByM4Id(mappingId) : GetItemIdByM5Id(mappingId);
            Debug.Log($"인덱스[{i}] 매핑됨: id={mappingId}, 필요 itemId={itemId}");

            if (itemId != 0)
            {
                bool hasGem = gemDict.TryGetValue(itemId, out var data);
                Debug.Log($" -> 인벤토리 보유 여부: {hasGem}");

                if (hasGem)
                {
                    GemGrade highestGrade = GetHighestGrade(itemId);
                    Debug.Log($" -> 최고 등급: {highestGrade}, 해당 등급 개수: {data.GetCount(highestGrade)}");
                }
            }
        }
    }
}
