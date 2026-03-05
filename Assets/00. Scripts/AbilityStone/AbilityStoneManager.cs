using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class AbilityStoneManager : Singleton<AbilityStoneManager>
{
    [SerializeField] public AblityStoneSO so;
    
    [SerializeField] private int stoneStatProbabilityID;
    [SerializeField] private int stoneID;
    [SerializeField] private int stoneStatUpID;
    [SerializeField] private int stoneTotalBonusID;
    [SerializeField] public static int ID;
    [SerializeField] public Button resetBnt;
    
    [SerializeField] private int totalCount;
    
    [SerializeField] private StoneGrade grade;

    public event Action<StoneGrade> OnReset;
    
    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        
        ID = stoneStatProbabilityID;
        foreach (var item in so.StoneStatProbabilityDict)
        {
            item.Value.LoadStone(item.Key);
        }
        
        ID = stoneID;
        
        foreach (var item in so.AbilityStoneDict)
        {
            item.Value.LoadStone();
        }
        
        ID = stoneTotalBonusID;
        
        foreach (var item in so.StoneTotalUpBonusDict)
        {
            item.Value.LoadStone(item.Key);
        }
        
        ID = stoneStatUpID;
        
        foreach (var item in so.StoneGradeStatUpDict)
        {
            item.Value.LoadStone(item.Key);
        }
        
        resetBnt.onClick.AddListener(() => ResetStone(grade));
    }

    private void OnDisable()
    {
        foreach (var item in so.AbilityStoneDict)
        {
            item.Value.DisableEvent();
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
        
        Debug.Log($"가중치 {r}");
        
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
        so.AbilityStoneDict[grade].UpStone(slotIndex);
    }
    
    public void ResetUp()
    {
        so.AbilityStoneDict[grade].ResetUp();
    }
    
    public float GetStat(StatType statType)
    {
        float totalStat = 0;
        foreach (var item in so.AbilityStoneDict)
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
        
        foreach(var item in so.AbilityStoneDict)
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
                return item.Value.isUnlock ? (1 + item.Value.increaseStat) : 1f;
            }
        }
        
        return 1f;
    }
}
