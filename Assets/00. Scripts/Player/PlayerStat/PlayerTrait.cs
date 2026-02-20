using System;
using UnityEngine;

[System.Serializable]
public class PlayerTrait
{
    public int ID;
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

    public event Action UpgradeTrait;

    public PlayerTrait(int key)
    {
        DataManager.Instance.TraitInfoDict.TryGetValue(key, out var value);

        ID = value.ID;
        TraitTier = value.traitTier;
        TraitName = value.traitName;
        TraitUPStatName = value.traitUPStatName;
        StatUP = value.statUP;
        MinLevel = value.minLevel;
        MaxLevel = value.maxLevel;
        DecreasePoint = value.decreasePoint;
        NeedTrait = value.needTrait;
    }

    public bool Upgrade(ref int point)
    {
        if (!PointCheck(ref point))
        {
            return false;
        }

        if (CurrentLevel >= MaxLevel)
        {
            return false;
        }
        point -= DecreasePoint;
        CurrentLevel++;
        UpgradeTrait?.Invoke();
        return true;
    }

    public bool PointCheck(ref int point)
    {
        return point >= DecreasePoint; // 포인트 생기면 포인트 관련 
    }
}
