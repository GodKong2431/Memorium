using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AbilityStone
{
    [SerializeField] private int id;

    [SerializeField] private StoneGrade stoneGrade;

    [SerializeField] private int upCost;

    [SerializeField] private int unlockLevel;

    [SerializeField] private int needUp;

    [SerializeField] private int statRerollCost;

    [SerializeField] private int UpResetCost;

    [SerializeField] private float upStartProbability;

    [SerializeField] private float maxProbability;
    [SerializeField] private float minProbability;

    [SerializeField] private int firstUpOpportunity;
    [SerializeField] private int secondUpOpportunity;
    [SerializeField] private int thirdUpOpportunity;
    [SerializeField] public List<AbilityStoneSlot> Slots = new List<AbilityStoneSlot>();
    
    [SerializeField] private float currentProbability;
    
    private List<int> slotUpOpportunitys = new List<int>();
    
    public float CurrentProbability
    {
        get => currentProbability;
        set => currentProbability = Mathf.Clamp(value, minProbability, maxProbability);
    }
    
    public int currentProbabilityCount;

    public void LoadStone()
    {
        id = AbilityStoneManager.ID;
        AbilityStoneManager.ID++;

        DataManager.Instance.StoneDict.TryGetValue(id, out StoneTable table);

        stoneGrade = table.stoneGrade;
        upCost = table.stoneUpCost;
        unlockLevel = table.stoneUnlock;
        needUp = table.stoneNeedUp;
        statRerollCost = table.stoneStatRerollCost;
        UpResetCost = table.stoneUpResetCost;
        upStartProbability = table.stoneUpStartProbability;
        maxProbability = table.stoneMaxProbability;
        minProbability = table.stoneMinProbability;
        firstUpOpportunity = table.stoneFirstUpOpportunity;
        secondUpOpportunity = table.stoneSecondUpOpportunity;
        thirdUpOpportunity = table.stoneThirdUpOpportunity;
        
        AbilityStoneManager.Instance.OnReset += Reset;
        
        CurrentProbability = upStartProbability;
        
        slotUpOpportunitys.Clear();
        
        slotUpOpportunitys.Add(firstUpOpportunity);
        slotUpOpportunitys.Add(secondUpOpportunity);
        slotUpOpportunitys.Add(thirdUpOpportunity);
    }
    
    public void Reset(StoneGrade grade)
    {
        if (stoneGrade != grade)
        {
            return;
        }
        
        ResetUp();
        
        for (int i = 0; i < Slots.Count; i++)
        {
            var currentType = AbilityStoneManager.Instance.SetProbAblility(i+1);
            
            if (i == 2)
            {
                while (Slots[0].SlotType == currentType || Slots[1].SlotType == currentType)
                {
                    currentType = AbilityStoneManager.Instance.SetProbAblility(i+1);
                }
            }
            
            Slots[i].TypeSetting(currentType);
        }
    }
    
    public bool UpStone(int slotIndex)
    {
        
        if (Slots[slotIndex].successCounter.Count >= slotUpOpportunitys[slotIndex])
        {
            return false;
        }
        float r = Random.Range(0f,1f);
        
        if (r < CurrentProbability)
        {
            CurrentProbability -= 0.1f;
            Slots[slotIndex].Up(true);
            return true;
        }
        
        else
        {
            CurrentProbability += 0.1f;
            Slots[slotIndex].Up(false);
            return false;
        }
    }
    
    public void ResetUp()
    {
        CurrentProbability = upStartProbability;
        
        foreach(var slot in Slots)
        {
            slot.Reset();
        }
    }
    
}
