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

    public PlayerStatType statType;

    public event Action<PlayerStatType> UpgradeTrait;

    public float CurrentStat;

    private CharacterStatManager mgr;

    public void LoadTrait()
    {
        DataManager.Instance.TraitInfoDict.TryGetValue(ID, out var value);

        TraitTier = value.traitTier;
        TraitName = value.traitName;
        TraitUPStatName = value.traitUPStatName;
        StatUP = value.statUP;
        MinLevel = value.minLevel;
        MaxLevel = value.maxLevel;
        DecreasePoint = value.decreasePoint;
        NeedTrait = value.needTrait;

        mgr = CharacterStatManager.Instance;

        UpgradeTrait += mgr.FinalStat;
    }

    public void DisableEvent()
    {
        UpgradeTrait -= mgr.FinalStat;
    }

    public bool Upgrade()
    {
        if (!CurrencyManager.Instance.TrySpend(CurrencyType.TraitPoint, DecreasePoint))
        {
            return false;
        }

        if (CurrentLevel >= MaxLevel)
        {
            return false;
        }
        CurrentLevel++;
        CurrentStat = CurrentLevel * StatUP;
        UpgradeTrait?.Invoke(statType);
        return true;
    }

    public bool PointCheck(ref int point)
    {
        return point >= DecreasePoint; // 포인트 생기면 포인트 관련 
    }
}
