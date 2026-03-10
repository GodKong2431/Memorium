using System;
using UnityEngine;

[System.Serializable]
public class StatUpgrade
{
    [SerializeField] private int id;
    [SerializeField] private string statName;
    [SerializeField] private int upgradeCount;
    [SerializeField] private float statInCrease;
    [SerializeField] private float baseCost;
    [SerializeField] private float costMultiplyRate;
    [SerializeField] private float stat;
    [SerializeField] private BigDouble currentCost;
    [SerializeField] private StatType statType;

    private CharacterStatManager mgr;

    public event Action<StatType> UpgradeStat;

    public int ID {get {return id;}}
    public string StatName {  get { return statName; } }
    public int UpgradeCount { get { return upgradeCount; } }

    public float StatInCrease { get { return statInCrease; } }

    public float BaseCost { get { return baseCost; } }

    public float CostMultiplyRate { get { return costMultiplyRate; } }

    public float Stat { get { return stat; } }

    public BigDouble CurrentCost { get { return currentCost; } }

    public StatType StatType {get {return statType;}}

    public void LoadUpgrade(int upgradeCount = 0, BigDouble? currentCost = null)
    {   
        DataManager.Instance.StatUpgradeDict.TryGetValue(ID, out StatUpgradeTable statUpgradeTable);
        statName = statUpgradeTable.statName;
        statInCrease = statUpgradeTable.statInCrease;
        baseCost = statUpgradeTable.baseCost;
        costMultiplyRate = statUpgradeTable.costMultiplyRate;
        this.upgradeCount = upgradeCount;
        stat = this.upgradeCount * statInCrease;
        this.currentCost = currentCost ?? statUpgradeTable.baseCost;
        mgr = CharacterStatManager.Instance;
        UpgradeStat += mgr.FinalStat;
    }

    public void DisableEvent()
    {
        UpgradeStat -= mgr.FinalStat;
    }

    public void Upgrade()
    {
        var currency = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currency == null)
            return;

        if (!currency.TrySpend(CurrencyType.Gold, currentCost))
        {
            InstanceMessageManager.TryShowInsufficientGold();
            return;
        }

        upgradeCount++;
        currentCost = currentCost * CostMultiplyRate;
        stat = upgradeCount * statInCrease;
        UpgradeStat?.Invoke(statType);
    }

    public bool CheckGold()
    {
        var currency = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currency == null)
            return false;

        var gold = currency.GetAmount(CurrencyType.Gold);

        if (gold < currentCost)
        {
            return false;
        }

        return true;

    }
}
