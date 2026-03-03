using UnityEngine;

[System.Serializable]
public class AbilityStone
{
    [SerializeField] private int id;

    [SerializeField] private StoneGrade stoneGrade;

    [SerializeField] private int upCost;

    [SerializeField] private int unlockLevel;

    [SerializeField] private int needUp;

    [SerializeField] private int statRerollCost;

    [SerializeField] private int UpResetCost;

    [SerializeField] private float upStartProbability;

    [SerializeField] private float maxProbability;
    [SerializeField] private float minProbability;

    [SerializeField] private int firstUpOpportunity;
    [SerializeField] private int secondUpOpportunity;
    [SerializeField] private int thirdUpOpportunity;

    public void LoadStone()
    {
        id = Test1.ID;
        Test1.ID++;

        DataManager.Instance.StoneDict.TryGetValue(id, out StoneTable table);

        stoneGrade = table.stoneGrade;
        upCost = table.stoneUpCost;
        unlockLevel = table.stoneUnlock;
        needUp = table.stoneNeedUp;
        statRerollCost = table.stoneStatRerollCost;
        UpResetCost = table.stoneUpResetCost;
        upStartProbability = table.stoneUpStartProbability;
        maxProbability = table.stoneMaxProbability;
        minProbability = table.stoneMinProbability;
        firstUpOpportunity = table.stoneFirstUpOpportunity;
        secondUpOpportunity = table.stoneSecondUpOpportunity;
        thirdUpOpportunity = table.stoneThirdUpOpportunity;
    }
}
