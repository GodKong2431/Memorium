using UnityEngine;

[System.Serializable]
public class StatUpgrade
{
    [SerializeField] private int upgradeCount;
    [SerializeField] private float statInCrease;
    [SerializeField] private float baseCost;
    [SerializeField] private float costMultiplyRate;

    public int UpgradeCount { get { return upgradeCount; } }

    public float StatInCrease { get { return statInCrease; } }

    public float BaseCost { get { return baseCost; } }

    public float CostMultiplyRate { get { return costMultiplyRate; } }

    public StatUpgrade(int key)
    {
        DataManager.Instance.StatUpgradeDict.TryGetValue(key, out StatUpgradeTable statUpgradeTable);
        statInCrease = statUpgradeTable.statInCrease;
        baseCost = statUpgradeTable.baseCost;
        costMultiplyRate = statUpgradeTable.costMultiplyRate;
    }

    public void AddCount()
    {
        upgradeCount++;
        baseCost = baseCost * CostMultiplyRate;
    }
}
