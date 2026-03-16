using System;
using System.Collections.Generic;

public sealed class PassiveSkillModule : IInventoryModule
{
    private readonly Dictionary<int, OwnedPassiveData> passiveDict = new Dictionary<int, OwnedPassiveData>();

    /// <summary> 
    /// 주문서 ID / 보유 수량 
    /// </summary>
    private readonly Dictionary<int, int> scrollDict = new Dictionary<int, int>();

    private int goldId = 0;

    private readonly List<PassiveSetTable> activeSetEffects = new List<PassiveSetTable>();

    public event Action OnPassiveInventoryChanged;
    public event Action OnSetEffectChanged;

    public List<PassiveSaveData> saveList = new List<PassiveSaveData>();

    public List<PassiveSaveData> GetSaveList()
    {
        saveList.Clear();
        foreach (var passive in passiveDict.Values)
        {
            saveList.Add(new PassiveSaveData(passive.skillId, passive.grade, passive.level));
        }
        return saveList;
    }

    public void LoadFromList(List<PassiveSaveData> list)
    {
        passiveDict.Clear();
        if (list == null) return;

        foreach (var save in list)
        {
            passiveDict[save.skillId] = new OwnedPassiveData(save);
        }

        RecalcSetEffects();
    }

    #region IInventoryModule

    public bool CanHandle(ItemType itemType)
    {
        // TODO: ItemType에 패시브 주문서 타입 추가 후 매칭
        return false;
    }

    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType)) return false;

        int addCount = (int)Math.Floor(amount.ToDouble());
        if (addCount <= 0) return false;

        int itemId = item.ItemId;
        if (!scrollDict.ContainsKey(itemId))
            scrollDict[itemId] = 0;

        scrollDict[itemId] += addCount;
        return true;
    }

    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType)) return false;

        int removeCount = (int)Math.Floor(amount.ToDouble());
        if (removeCount <= 0) return false;

        int itemId = item.ItemId;
        if (!scrollDict.TryGetValue(itemId, out int current)) return false;
        if (current < removeCount) return false;

        scrollDict[itemId] = current - removeCount;
        return true;
    }

    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!CanHandle(item.ItemType)) return BigDouble.Zero;
        if (scrollDict.TryGetValue(item.ItemId, out int count))
            return new BigDouble(count);
        return BigDouble.Zero;
    }

    #endregion

    #region UI 표시용

    public OwnedPassiveData GetOwnedPassiveData(int skillId)
    {
        passiveDict.TryGetValue(skillId, out var data);
        return data;
    }

    public IEnumerable<OwnedPassiveData> GetAllOwnedPassives()
    {
        return passiveDict.Values;
    }

    public bool IsOwned(int skillId)
    {
        return passiveDict.ContainsKey(skillId);
    }

    /// <summary>
    /// 골드 보유량 및 레벨 체크하여 레벨업 가능 여부 반환
    /// </summary>
    public bool CanLevelUpPassive(int skillId)
    {
        if (!passiveDict.TryGetValue(skillId, out var passive)) return false;
        if (!passive.CanLevelUp()) return false;

        SetGoldID();
        BigDouble cost = passive.GetLevelUpCost();
        return InventoryManager.Instance.HasEnoughItem(goldId, cost);
    }

    public BigDouble GetLevelUpCost(int skillId)
    {
        if (!passiveDict.TryGetValue(skillId, out var passive)) return BigDouble.Zero;
        return passive.GetLevelUpCost();
    }

    /// <summary>
    /// 승급 가능 여부 반환
    /// </summary>
    public bool CanEvolvePassive(int skillId)
    {
        if (!passiveDict.TryGetValue(skillId, out var passive)) return false;
        if (!passive.CanEvolve()) return false;

        int reqItemId = passive.GetEvolveItemId();
        if (reqItemId == 0) return false;

        int reqCount = passive.GetEvolveScrollCount();
        return HasEnoughScroll(reqItemId, reqCount);
    }

    /// <summary>
    /// 특정 주문서의 보유 수량 반환 (UI 표시용)
    /// </summary>
    public int GetScrollCount(int itemId)
    {
        if (scrollDict.TryGetValue(itemId, out int count))
            return count;
        return 0;
    }

    #endregion

    #region UI 버튼용

    /// <summary>
    /// 모든 업그레이드(레벨업/승급) 호출, 하나라도 성공시 종료 => true 반환
    /// </summary>
    public bool TryUpgradePassive(int skillId)
    {
        if (TryEvolvePassive(skillId)) return true;
        if (TryLevelUpPassive(skillId)) return true;
        return false;
    }

    public bool TryLevelUpPassive(int skillId)
    {
        if (!CanLevelUpPassive(skillId)) return false;

        var passive = passiveDict[skillId];
        InventoryManager.Instance.RemoveItem(goldId, passive.GetLevelUpCost());
        passive.ExecuteLevelUp();

        OnPassiveInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEvolvePassive(int skillId)
    {
        if (!CanEvolvePassive(skillId)) return false;

        var passive = passiveDict[skillId];
        RemoveScroll(passive.GetEvolveItemId(), passive.GetEvolveScrollCount());
        passive.ExecuteEvolve();

        RecalcSetEffects();
        OnPassiveInventoryChanged?.Invoke();
        return true;
    }

    #endregion

    #region 세트 효과

    /// <summary>
    /// 신화 등급 패시브 수를 세서 2/3/5세트 달성 여부 갱신
    /// </summary>
    public void RecalcSetEffects()
    {
        int prevCount = activeSetEffects.Count;

        int mythicCount = 0;
        foreach (var p in passiveDict.Values)
        {
            if (p.grade >= 5) mythicCount++;
        }

        activeSetEffects.Clear();

        var setDict = DataManager.Instance.PassiveSetDict;
        if (setDict == null) return;

        foreach (var set in setDict.Values)
        {
            if (mythicCount >= set.reqCount)
                activeSetEffects.Add(set);
        }

        if (activeSetEffects.Count != prevCount)
            OnSetEffectChanged?.Invoke();
    }

    public List<PassiveSetTable> GetActiveSetEffects() => activeSetEffects;

    public List<string> GetSetVfxPaths()
    {
        var paths = new List<string>();
        foreach (var set in activeSetEffects)
            paths.Add(set.vfxPath);
        return paths;
    }

    #endregion

    #region 스탯 제공

    /// <summary>
    /// 해당 StatType에 대한 패시브 스탯 + 세트효과 합산값 반환
    /// </summary>
    public float GetPassiveStat(StatType statType)
    {
        float total = 0f;

        foreach (var passive in passiveDict.Values)
        {
            if (passive.passiveInfoTable == null) continue;
            if (passive.GetStatType() != statType) continue;

            float value = passive.GetCurrentStatValue();
            float max = passive.GetMaxValue();
            if (max > 0f && value > max) value = max;
            total += value;
        }

        foreach (var set in activeSetEffects)
        {
            if (set.effectType == statType)
                total += set.effectValue;
            if (set.effectType2 == statType)
                total += set.effectValue2;
        }

        return total;
    }

    #endregion

    #region Private

    private bool HasEnoughScroll(int itemId, int required)
    {
        if (scrollDict.TryGetValue(itemId, out int count))
            return count >= required;
        return false;
    }

    private bool RemoveScroll(int itemId, int removeCount)
    {
        if (!scrollDict.TryGetValue(itemId, out int count)) return false;
        if (count < removeCount) return false;
        scrollDict[itemId] = count - removeCount;
        return true;
    }

    private void SetGoldID()
    {
        if (goldId != 0) return;

        foreach (var item in DataManager.Instance.ItemInfoDict)
        {
            if (item.Value.itemType == ItemType.FreeCurrency)
            {
                goldId = item.Key;
                break;
            }
        }
    }

    #endregion
}