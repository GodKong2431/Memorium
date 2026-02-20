using System;
using UnityEngine;

[System.Serializable]
public class StatUpgrade
{
    [SerializeField] private int upgradeCount;
    [SerializeField] private float statInCrease;
    [SerializeField] private float baseCost;
    [SerializeField] private float costMultiplyRate;
    [SerializeField] private float stat;
    [SerializeField] private float currentCost;

    public event Action UpgradeStat;

    public int UpgradeCount { get { return upgradeCount; } }

    public float StatInCrease { get { return statInCrease; } }

    public float BaseCost { get { return baseCost; } }

    public float CostMultiplyRate { get { return costMultiplyRate; } }

    public float Stat { get { return stat; } }

    public float CurrentCost { get { return currentCost; } }

    public StatUpgrade(int key)
    {
        DataManager.Instance.StatUpgradeDict.TryGetValue(key, out StatUpgradeTable statUpgradeTable);
        statInCrease = statUpgradeTable.statInCrease;
        baseCost = statUpgradeTable.baseCost;
        costMultiplyRate = statUpgradeTable.costMultiplyRate;
        stat = upgradeCount * statInCrease;
        currentCost = statUpgradeTable.baseCost;
    }

    public void Upgrade()
    {
        upgradeCount++;
        currentCost = currentCost * CostMultiplyRate;
        stat = upgradeCount * statInCrease;
        UpgradeStat?.Invoke();
    }
}
