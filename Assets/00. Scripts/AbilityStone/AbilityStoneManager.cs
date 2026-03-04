using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class AbilityStoneManager : Singleton<AbilityStoneManager>
{
    [SerializeField] private AblityStoneSO so;
    
    [SerializeField] private int stoneStatProbabilityID;
    [SerializeField] private int stoneID;
    
    [SerializeField] public static int ID;
    [SerializeField] public Button resetBnt;
    
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
        
        resetBnt.onClick.AddListener(() => ResetStone(grade));
        
        // foreach (var item in DataManager.Instance.StoneTotalUpBonusDict)
        // {
        //     DataManager.Instance.StoneTotalUpBonusDict.TryGetValue(item.Key, out var table);
        //     so.StoneTotalUpBonusDict.Add(table.stoneTotalUP, new StoneTotalUpBonus(table));
        // }
        
        
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

}
