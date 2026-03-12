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
    public int[] gradeCounts = new int[(int)GemGrade.Count];

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
    private readonly Dictionary<int, OwnedGemData> gemDict = new Dictionary<int, OwnedGemData>();

    private const int MERGE_THRESHOLD = 3;
    public event Action OnGemInventoryChanged;

    public Dictionary<int, int> ItemIdToM4Id { get; private set; }
    public Dictionary<int, int> ItemIdToM5Id { get; private set; }

    public GemInventoryModule()
    {
        BuildGemLookup();
    }

    public void BuildGemLookup()
    {
        ItemIdToM4Id = new Dictionary<int, int>();
        ItemIdToM5Id = new Dictionary<int, int>();

        foreach (var jemId in DataManager.Instance.SkillModule4Dict)
            ItemIdToM4Id[jemId.Value.m4ItemID] = jemId.Key;

        foreach (var jemId in DataManager.Instance.SkillModule5Dict)
            ItemIdToM5Id[jemId.Value.m5ItemID] = jemId.Key;
    }
    //구조체 리스트는 저장 할 수 있다고 이해해서 이렇게 만?들어놨는데 이상하다 싶으시면 다른 방식으로 하셔도 괜찮습니다.

    /// <summary>
    /// 가지고 있는 모든 젬의 id와 등급/ 갯수를 담은 구조체 리스트 반환, UI랑 저장에 쓰시면 될?듯
    /// </summary>
    /// <returns></returns>
    public List<GemSaveData> GetSaveList()
    {
        var list = new List<GemSaveData>();
        foreach (var pair in gemDict)
        {
            for (int i = (int)GemGrade.Common; i < (int)GemGrade.Count; i++)
            {
                if (pair.Value.gradeCounts[i] > 0)
                {
                    list.Add(new GemSaveData(pair.Key, pair.Value.gradeCounts[i], (GemGrade)i));
                }
            }
        }
        return list;
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

        if (!TryGetGemIdByItemId(item.ItemId, item.ItemType, out int gemId)) return false;

        if (!TryConvertAmountToInt(amount, out int addCount)) return false;

        if (!gemDict.TryGetValue(gemId, out var data))
        {
            data = new OwnedGemData(gemId);
            gemDict[gemId] = data;
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
    /// <summary>
    /// 아이템 id=>젬 id 변환
    /// </summary>
    /// <returns></returns>
    private bool TryGetGemIdByItemId(int itemId, ItemType type, out int gemId)
    {
        gemId = -1;
        return type switch
        {
            ItemType.UniqueGem => ItemIdToM4Id.TryGetValue(itemId, out gemId),
            ItemType.ElementGem => ItemIdToM5Id.TryGetValue(itemId, out gemId),
            _ => false
        };
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
