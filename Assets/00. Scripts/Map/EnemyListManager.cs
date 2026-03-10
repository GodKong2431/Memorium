using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyListManager : Singleton<EnemyListManager>
{
    [SerializeField] EnemyStatPresenter[] enemyList;
    public Dictionary<int, GameObject> enemyMap;
    public Dictionary<int, EnemyRewardData> enemyRewardMap;
    private bool dataLoad=false;
    public bool DataLoad => dataLoad;

    protected override void Awake()
    {
        base.Awake();
        enemyMap = new Dictionary<int, GameObject>();
        foreach (var enemy in enemyList)
        {

            //Debug.Log($"[EnemyListManager] 몬스터 ID {enemy.monsterIdFromDataManager}");
            enemyMap[enemy.monsterIdFromDataManager]=enemy.gameObject;
            //enemyRewardMap[enemy.monsterIdFromDataManager] = enemy.RewardData;
        }
        dataLoad = true;
    }
}
