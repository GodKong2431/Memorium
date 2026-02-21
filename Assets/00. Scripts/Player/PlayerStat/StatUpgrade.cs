using System;
using UnityEngine;

[System.Serializable]
public class StatUpgrade
{
    [SerializeField] private string statName;
    [SerializeField] private int upgradeCount;
    [SerializeField] private float statInCrease;
    [SerializeField] private float baseCost;
    [SerializeField] private float costMultiplyRate;
    [SerializeField] private float stat;
    [SerializeField] private BigDouble currentCost;
    [SerializeField] private PlayerStatType statType;

    public event Action<PlayerStatType> UpgradeStat;

    public string StatName {  get { return statName; } }
    public int UpgradeCount { get { return upgradeCount; } }

    public float StatInCrease { get { return statInCrease; } }

    public float BaseCost { get { return baseCost; } }

    public float CostMultiplyRate { get { return costMultiplyRate; } }

    public float Stat { get { return stat; } }

    public BigDouble CurrentCost { get { return currentCost; } }

    public StatUpgrade(int key, PlayerStatType type)
    {
        DataManager.Instance.StatUpgradeDict.TryGetValue(key, out StatUpgradeTable statUpgradeTable);
        statName = statUpgradeTable.statName;
        statInCrease = statUpgradeTable.statInCrease;
        baseCost = statUpgradeTable.baseCost;
        costMultiplyRate = statUpgradeTable.costMultiplyRate;
        stat = upgradeCount * statInCrease;
        currentCost = statUpgradeTable.baseCost;
        statType = type;
    }

    public void Upgrade()
    {
        upgradeCount++;
        currentCost = currentCost * CostMultiplyRate;
        stat = upgradeCount * statInCrease;
        UpgradeStat?.Invoke(statType);
    }
}
