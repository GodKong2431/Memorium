using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Linq;
using Unity.Burst.Intrinsics;

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
    [SerializeField] public List<AbilityStoneSlot> Slots = new List<AbilityStoneSlot>();// 저장 
    
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
    
    public bool isUnlock = false;// 저장
    
    public float CurrentProbability
    {
        get => currentProbability;
        set => currentProbability = Mathf.Clamp(value, minProbability, maxProbability);
    }
    
    public int currentProbabilityCount;

    public bool stoneMult;
    
    public int tier;
    
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
        stoneMult = table.stoneTier;
        Debug.Log("스톤"+table.stoneTier);
        tier = stoneMult ? 1 : 0;
        
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

    public void RestoreLoadedSlots(List<AbilityStoneSlot> loadedSlots)
    {
        if (mgr == null)
        {
            mgr = CharacterStatManager.Instance;
        }

        if (mgr != null)
        {
            foreach (AbilityStoneSlot slot in Slots)
            {
                if (slot != null)
                {
                    slot.OnUpdateStat -= mgr.FinalStat;
                }
            }
        }

        Slots = loadedSlots ?? new List<AbilityStoneSlot>();

        int expectedSlotCount = Mathf.Max(slotUpOpportunitys.Count, 3);
        while (Slots.Count < expectedSlotCount)
        {
            Slots.Add(new AbilityStoneSlot(StatType.None, null));
        }

        if (Slots.Count > expectedSlotCount)
        {
            Slots.RemoveRange(expectedSlotCount, Slots.Count - expectedSlotCount);
        }

        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i] == null)
            {
                Slots[i] = new AbilityStoneSlot(StatType.None, null);
            }

            AbilityStoneSlot slot = Slots[i];
            slot.successCounter ??= new List<bool>();
            slot.increaseStat = ResolveIncreaseStat(slot.SlotType,tier);

            if (mgr != null)
            {
                slot.OnUpdateStat -= mgr.FinalStat;
                slot.OnUpdateStat += mgr.FinalStat;
            }
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
            Slots[i].increaseStat = AbilityStoneManager.Instance.so.StoneGradeStatUpDict[tier][currentType].SetStat(stoneGrade);
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
            SoundManager.Instance?.PlayUiSfx(UiSoundIds.AbilityStoneSuccess);
            return true;
        }
        
        else
        {
            CurrentProbability += 0.1f;
            Slots[slotIndex].Up(false);
            SoundManager.Instance?.PlayUiSfx(UiSoundIds.AbilityStoneFail);
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

    private float ResolveIncreaseStat(StatType statType, int tier)
    {
        if (statType == StatType.None)
        {
            return 0f;
        }

        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager != null
            && abilityStoneManager.so != null
            && abilityStoneManager.so.StoneGradeStatUpDict.TryGetValue(tier, out var tierIndex)
            && tierIndex.TryGetValue(statType, out StoneGradeStatUp statUpData))
        {
            return statUpData.SetStat(stoneGrade);
        }

        return 0f;
    }
}
