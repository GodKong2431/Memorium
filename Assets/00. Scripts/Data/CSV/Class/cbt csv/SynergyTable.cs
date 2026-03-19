using System;
using System.Collections.Generic;

[System.Serializable]
public class SynergyTable : TableBase
{
    public string synergyType;
    public RarityType synergyRarity;
    public StatType providedStat1;
    public StatType providedStat2;
    public float statUp1;
    public float statUp2;
    public string synergyName;
    public string synergyDescription;
}
