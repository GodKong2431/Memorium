using System;
using System.Collections.Generic;

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


    //구조체 리스트는 저장 할 수 있다고 이해해서 이렇게 만?들어놨는데 이상하다 싶으시면 다른 방식으로 하셔도 괜찮습니다.

    /// <summary>
    /// 가지고 있는 모든 젬의 id와 등급/ 갯수를 담은 구조체 리스트 반환, UI랑 저장에 쓰시면 될?듯
    /// </summary>
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
                    result.Add(ownedData);
                }
            }
        }
        return result;
    }
    /// <summary>
    /// 보유한 젬 목록중 장착 가능한 m5 모듈 젬 리스트 반환
    /// </summary>
    public List<OwnedGemData> GetEquippableOwnedM5Gems()
    {
        List<OwnedGemData> result = new List<OwnedGemData>();

        foreach (var gem in gemDict)
        {
            int gemId = gem.Key;

            if (ItemToM5Dict.ContainsKey(gemId))
            {
                result.Add(gem.Value);
            }
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

        if (presetSlotIndex == -1) return false;

        return TryEquipM4Gem(presetSlotIndex, gemId);
    }

    /// <summary>
    /// 스킬이 현재 프리셋에 있다면 해당 스킬에 M5 젬을 장착
    /// </summary>
    public bool TryEquipM5GemBySkillId(int skillId, int m5SlotIndex, int gemId)
    {
        int presetSlotIndex = GetPresetSlotIndexBySkillId(skillId);

        if (presetSlotIndex == -1) return false;

        return TryEquipM5Gem(presetSlotIndex, m5SlotIndex, gemId);
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
        if (ItemToM4Dict.TryGetValue(itemId, out int m4Id))
            return m4Id;
        return 0;
    }
    public int GetItemIdByM4Id(int m4Id)
    {
        if (DataManager.Instance.SkillModule4Dict.TryGetValue(m4Id, out var m4Data))
            return m4Data.m4ItemID;
        return 0;
    }
    public int GetM4IdBySkillId(int skillId, int m4Type)
    {
        if (SkillToM4Dict.TryGetValue(skillId, out int[] m4Array))
        {
            if (m4Type >= 0 && m4Type < m4Array.Length)
                return m4Array[m4Type];
        }
        return 0;
    }

    public int GetSkillIdByM4Id(int m4Id)
    {
        if (DataManager.Instance.SkillModule4Dict.TryGetValue(m4Id, out var m4Data))
            return m4Data.skillID;
        return 0;
    }

    public int GetSkillIdByM4ItemId(int itemId)
    {
        int m4Id = GetM4IdByItemId(itemId);
        if (m4Id != 0)
            return GetSkillIdByM4Id(m4Id);
        return 0;
    }

    public int GetM4ItemIdBySkillId(int skillId, int m4Type)
    {
        int m4Id = GetM4IdBySkillId(skillId, m4Type);
        if (m4Id != 0)
            return GetItemIdByM4Id(m4Id);
        return 0;
    }

    #endregion

    #region M5 변환 Getter
    public int GetM5IdByItemId(int itemId)
    {
        if (ItemToM5Dict.TryGetValue(itemId, out int m5Id))
            return m5Id;
        return 0;
    }
    
    public int GetItemIdByM5Id(int m5Id)
    {
        if (DataManager.Instance.SkillModule5Dict.TryGetValue(m5Id, out var m5Data))
            return m5Data.m5ItemID;
        return 0;
    }

    public int GetM5IdBySkillId(int skillId, int m5Type)
    {
        if (SkillToM5Dict.TryGetValue(skillId, out int[] m5Array))
        {
            if (m5Type >= 0 && m5Type < m5Array.Length)
                return m5Array[m5Type];
        }
        return 0;
    }

    public int GetSkillIdByM5Id(int m5Id)
    {
        if (DataManager.Instance.SkillModule5Dict.TryGetValue(m5Id, out var m5Data))
            return m5Data.skillID;
        return 0;
    }

    public int GetSkillIdByM5ItemId(int itemId)
    {
        int m5Id = GetM5IdByItemId(itemId);
        if (m5Id != 0)
            return GetSkillIdByM5Id(m5Id);
        return 0;
    }

    public int GetM5ItemIdBySkillId(int skillId, int m5Type)
    {
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

}
