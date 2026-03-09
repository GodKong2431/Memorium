using UnityEngine;

[System.Serializable]
public class OwnedFairyData
{
    public int fairyID;
    public int level;

    public OwnedFairyData(int fairyID)
    {
        this.fairyID = fairyID;
        this.level = 1;
    }
}