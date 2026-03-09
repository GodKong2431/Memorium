using System;
using UnityEngine;

[Serializable]
public struct EquipmentData
{
    //장비 Id
    public int equipmentId;
    //장비 갯수
    public int equipmentValue;
    //장비 강화 수치
    public int equipmentReinforcement;

    public EquipmentData(int id, int value = 1, int reinforecement = 0)
    {
        equipmentId = id;
        equipmentValue = value;
        equipmentReinforcement = reinforecement;
    }
}
