using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class AbilityStoneManager : Singleton<AbilityStoneManager>
{
    [SerializeField] public AbilityStoneSO so;
    
    [SerializeField] private int stoneStatProbabilityID;
    [SerializeField] private int stoneID;
    [SerializeField] private int stoneStatUpID;
    [SerializeField] private int stoneTotalBonusID;
    [SerializeField] public static int ID;
    [SerializeField] public Button resetBnt;
    
    [SerializeField] private int totalCount;
    
    [SerializeField] private StoneGrade grade;
    [SerializeField] private int tier;

    public event Action<StoneGrade> OnReset;
    
    public bool LoadStone = false;

    public SaveAbilityStoneData saveAbilityStoneData;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => CharacterStatManager.Instance != null);
        yield return new WaitUntil(() => CharacterStatManager.Instance.TableLoad);
        
        so = Resources.Load<AbilityStoneSO>("AbilityStoneSO");

        stoneStatProbabilityID = so.stoneStatProbabilityID;
        stoneID = so.stoneID;
        stoneStatUpID = so.stoneStatUpID;
        stoneTotalBonusID = so.stoneTotalBonusID;
        
        ID = stoneStatProbabilityID;
        foreach (var item in so.StoneStatProbabilityDict)
        {
            item.Value.LoadStone(item.Key);
        }
        
        ID = stoneID;
        
        foreach (var index in so.AbilityStoneDict)
        {
            foreach (var item in index.Value)
            {
                item.Value.LoadStone();
            }
        }

        ID = stoneTotalBonusID;
        
        foreach (var item in so.StoneTotalUpBonusDict)
        {
            item.Value.LoadStone(item.Key);
        }
        
        ID = stoneStatUpID;
        
        foreach (var tier in so.StoneGradeStatUpDict)
        {
            foreach (var item in tier.Value)
            {
                item.Value.LoadStone(item.Key);
            }
        }
        
        
        if (resetBnt != null)
        {
            resetBnt.onClick.AddListener(() => ResetStone(grade));
        }
        
        totalCount = 0;

        saveAbilityStoneData = JSONService.Load<SaveAbilityStoneData>();
        saveAbilityStoneData.InitAblityStoneData();
        saveAbilityStoneData.LoadAbilityStoneData(so);

        LoadStone = true;
        CharacterStatManager.Instance.AllStatUpdate();
    }

    private void OnDisable()
    {
        foreach (var index in so.AbilityStoneDict)
        {
            foreach (var item in index.Value)
        {
            item.Value.DisableEvent();
        }
        }
        
    }
    public void ResetStone(StoneGrade grade)
    {
        OnReset?.Invoke(grade);
    }
    
    // 가중치를 이용하여 타입한개 뽑기
    public StatType SetProbAblility(int slotIndex)
    {        
        float total = 0f;
        
        foreach (var weight in so.StoneStatProbabilityDict)
        {
            float w = GetWeight(weight.Value, slotIndex);
            
            if (w > 0f)
            {
                total += w;
            }
        }
        
        float r = UnityEngine.Random.value * total;
        float acc = 0f;
        

        
        foreach (var weight in so.StoneStatProbabilityDict)
        {
            float w = GetWeight(weight.Value, slotIndex);
            
            if (w <= 0f)
            {
                continue;
            }
            
            acc += w;
            
            if (r <= acc)
            {
                return weight.Key;
            }
        }
        
        foreach (var weight in so.StoneStatProbabilityDict)
        {
            if (GetWeight(weight.Value, slotIndex) > 0f)
            {
                return weight.Key;
            }
        }
        
        throw new InvalidOperationException("스톤의 스탯 설정이 안되었습니다");
    }
    
    private float GetWeight(StoneStatProbability p, int slotIndex)
    {
        return slotIndex switch
        {
            1 => p.FirstSlotProbability,
            2 => p.SecondSlotProbability,
            3 => p.ThirdSlotProbability,
            _ => throw new ArgumentOutOfRangeException(nameof(slotIndex), "범위 벗아남")
        };
    }
    
    public void UpStone(int slotIndex)
    {
        so.AbilityStoneDict[tier][grade].UpStone(slotIndex);
    }
    
    public void ResetUp()
    {
        so.AbilityStoneDict[tier][grade].ResetUp();
    }
    
    public float GetStat(StatType statType, int tier)
    {
        float totalStat = 0;
        foreach (var item in so.AbilityStoneDict[tier])
        {
            for (int i = 0; i < item.Value.Slots.Count; i++)
            {
                if (statType == item.Value.Slots[i].SlotType)
                {
                    totalStat += i == 2 ? -(item.Value.Slots[i].totalStat) : item.Value.Slots[i].totalStat;
                }
            }
        }
        
        return totalStat;
    }
    
    public void CheckTotalUpCount()
    {
        totalCount = 0;
        
        foreach(var item in so.AbilityStoneDict[tier])
        {
            totalCount += item.Value.GetUpCount();
        }
        
        foreach(var item in so.StoneTotalUpBonusDict)
        {
            if(item.Value.totalUpCount <= totalCount)
            {
                item.Value.Unlock(true);
            }
            
            else
            {
                item.Value.Unlock(false);
            }
        }
    }
    
    public float GetBonusStat(StatType statType)
    {
        CheckTotalUpCount();
        
        foreach (var item in so.StoneTotalUpBonusDict)
        {
            if (item.Value.statType == statType)
            {
                return item.Value.isUnlock ? item.Value.increaseStat : 0f;
            }
        }
        
        return 0f;
    }


    public void SaveAbilityStoneInfo()
    {
        saveAbilityStoneData.SaveAbilityStoneDataBySO(so);
    }
}
