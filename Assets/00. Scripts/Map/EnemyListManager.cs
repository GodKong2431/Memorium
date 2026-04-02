using System.Collections.Generic;
using UnityEngine;

public class EnemyListManager : Singleton<EnemyListManager>
{
    [SerializeField] private EnemyStatPresenter[] enemyList;

    public Dictionary<int, GameObject> enemyMap;
    public Dictionary<int, EnemyRewardData> enemyRewardMap;

    private bool dataLoad = false;
    public bool DataLoad => dataLoad;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        enemyMap = new Dictionary<int, GameObject>();
        enemyRewardMap = new Dictionary<int, EnemyRewardData>();

        if (enemyList == null)
        {
            dataLoad = true;
            return;
        }

        foreach (EnemyStatPresenter enemy in enemyList)
        {
            if (enemy == null)
                continue;

            enemyMap[enemy.monsterIdFromDataManager] = enemy.gameObject;
            //enemyRewardMap[enemy.monsterIdFromDataManager] = enemy.RewardData;
        }

        dataLoad = true;
    }
}
