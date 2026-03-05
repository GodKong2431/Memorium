using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class AbilityStoneSlot
{
    public StatType SlotType;
            
    public List<bool> successCounter = new List<bool>();
    
    public float increaseStat;
    
    public float totalStat => successCounter.Count(x => x) * increaseStat;
    
    public event Action<StatType> OnUpdateStat;
        
    public void TypeSetting(StatType type)
    {
        SlotType = type;
    }
    
    public void Up(bool success)
    {
        successCounter.Add(success);
        CallEvent();
    }
    
    public void Reset()
    {
        successCounter.Clear();
        CallEvent();
    }
    
    public void CallEvent()
    {
        OnUpdateStat?.Invoke(SlotType);
    }
}

