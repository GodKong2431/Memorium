using System.Data;
using UnityEngine;
using UnityEngine.UIElements;

public class StoneTotalUpBonus
{
    private StatType statType;
    private float increaseStat;
    
    public StoneTotalUpBonus(StoneTotalUpBonusTable table)
    {
        increaseStat = table.stoneBonusStat;
    }
}
