using System;
using System.Collections.Generic;

[System.Serializable]
public class EquipListTable : TableBase
{
    public string description;
    public EquipmentType equipmentType;
    public int grade;
    public RarityType rarityType;
    public int equipmentTier;
    public int statType1;
    public int statType2;
    public string iconResource;
    public string equipmentName;
}
