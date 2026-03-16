using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Linq;

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
    
    [SerializeField] private int totalUpCount;
    
    private CharacterStatManager mgr;

    private List<int> slotUpOpportunitys = new List<int>();

    public StoneGrade StoneGrade => stoneGrade;
    public int UpCost => upCost;
    public int UnlockLevel => unlockLevel;
    public int NeedUp => needUp;
    public int StatRerollCost => statRerollCost;
    public int UpResetCostValue => UpResetCost;
    public bool IsConfigured => Slots.Exists(slot => slot.SlotType != StatType.None);
    
    public float CurrentProbability
    {
        get => currentProbability;
        set => currentProbability = Mathf.Clamp(value, minProbability, maxProbability);
    }
    
    public int currentProbabilityCount;

    public int GetOpportunityCount(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotUpOpportunitys.Count)
        {
            return 0;
        }

        return slotUpOpportunitys[slotIndex];
    }

    public int GetSuccessCount(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
        {
            return 0;
        }

        return Slots[slotIndex].successCounter.Count(x => x);
    }
    
    public StatType GetStatType(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
        {
            return 0;
        }
        
        return Slots[slotIndex].SlotType;
    }

    public int GetAttemptCount(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
        {
            return 0;
        }

        return Slots[slotIndex].successCounter.Count;
    }

    public bool CanAttemptUpgrade(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
        {
            return false;
        }

        return Slots[slotIndex].SlotType != StatType.None
            && Slots[slotIndex].successCounter.Count < GetOpportunityCount(slotIndex);
    }

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
        
        mgr = CharacterStatManager.Instance;
        
        foreach(var slot in Slots)
        {
            slot.SlotType = StatType.None;
            slot.successCounter.Clear();
            slot.OnUpdateStat += mgr.FinalStat;
        }
        
    }
    public void DisableEvent()
    {
        foreach(var slot in Slots)
        {
            slot.OnUpdateStat -= mgr.FinalStat;
        }
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
            Slots[i].increaseStat = AbilityStoneManager.Instance.so.StoneGradeStatUpDict[currentType].SetStat(stoneGrade);
        }
        
    }
    
    public bool UpStone(int slotIndex)
    {
        
        if (Slots[slotIndex].successCounter.Count >= slotUpOpportunitys[slotIndex]
        || Slots[slotIndex].SlotType == StatType.None)
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
    
    public int GetUpCount()
    {
        totalUpCount = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            if (i == 2)
            {
                continue;
            }
            
            totalUpCount += Slots[i].successCounter.Count(x => x);
        }
        
        return totalUpCount;
    }
}
