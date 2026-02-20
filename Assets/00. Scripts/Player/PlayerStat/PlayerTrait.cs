using UnityEngine;

[System.Serializable]
public class PlayerTrait
{
    public string TraitTier;
    public string TraitName;
    public string TraitUPStatName;
    public float StatUP;
    public int MinLevel;
    public int MaxLevel;
    public int DecreasePoint;
    public string NeedTrait;

    public int CurrentLevel;

    public bool unlock = false;

    public PlayerTrait(int key)
    {
        DataManager.Instance.TraitInfoDict.TryGetValue(key, out var value);

        TraitTier = value.traitTier;
        TraitName = value.traitName;
        TraitUPStatName = value.traitUPStatName;
        StatUP = value.statUP;
        MinLevel = value.minLevel;
        MaxLevel = value.maxLevel;
        DecreasePoint = value.decreasePoint;
        NeedTrait = value.needTrait;
    }

    public bool Upgrade()
    {
        if (!PointCheck())
        {
            return false;
        }

        if (CurrentLevel >= MaxLevel)
        {
            return false;
        }
        CurrentLevel++;

        return true;
    }

    public bool PointCheck()
    {
        return false; // 포인트 생기면 포인트 관련 
    }
}
