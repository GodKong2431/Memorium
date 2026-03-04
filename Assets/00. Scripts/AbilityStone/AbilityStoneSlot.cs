using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class AbilityStoneSlot
{
    public StatType SlotType;
            
    public List<bool> successCounter = new List<bool>();

    public void TypeSetting(StatType type)
    {
        SlotType = type;
        
        Debug.Log(type);
    }
    
    public void Up(bool success)
    {
        successCounter.Add(success);
    }
    
    public void Reset()
    {
        successCounter.Clear();
    }
}

