using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : Singleton<StageManager>
{
    //각 스테이지별 키 값은 curStage-1;
    public int curStage = 1;
    public int curMonsterKillCount = 0;
    public int maxMonsterKillCount = 0;

    //각 층별 키 값은 curFloor-1;
    public int curFloor = 1;
    //스테이지 키들 순서대로(0부터) 관리하는 리스트
    public List<int> stageKeyList;

    public EnemyRewardData normalEnemyReward;
    public EnemyRewardData bossEnemyReward;

    public InfinityMap infinityMap;
    public MonsterSpawner monsterSpawner;

    public bool isReadyToBossSpawn=false;

    [SerializeField] GameObject BossSpawnBtn;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        Init();
        //나중에 데이터 연동하면 여기서 내가 진행중인 스테이지 가져와서 그거 기반으로 키 검색하고 진행 현재 스테이지 가져옴

        SetReward();
        SetKillCount();
        //킬 카운트 변경 시 스테이지 매니저의 킬 카운트도 증가 <- 나중에는 그냥 디스패쳐에 있는거 그냥 사용
        EnemyKillRewardDispatcher.OnKillCountChanged += (num) => CheckBossEnemySpawn();
        EnemyKillRewardDispatcher.OnBossKilled += StageClear;
        BossSpawnBtn.GetComponent<Button>().onClick.AddListener(() => 
        { 
            isReadyToBossSpawn = !isReadyToBossSpawn;
            BossSpawnBtn.SetActive(false);
        });
    }
    private void Init()
    {
        stageKeyList = DataManager.Instance.StageManageDict.Keys.ToList<int>();
        stageKeyList.Sort();
    }

    public void CheckBossEnemySpawn()
    {
        curMonsterKillCount++;
        //여기에 현재 몇 마리 잡았는지 UI로 보여주는 코드도 추가
        //if (maxMonsterKillCount <= EnemyKillRewardDispatcher.TotalKillCount)
        if (maxMonsterKillCount <= curMonsterKillCount)
        {
            if (!BossSpawnBtn.activeSelf)
            {
                BossSpawnBtn.SetActive(true);
            }
        }
    }


    //스테이지 증가하면 curstage 증가 후 아래 메서드 다시 호출
    public void SetReward()
    {
        int prevCurFloor=curFloor;
        curFloor = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].floorNumber;
        monsterSpawner.SetMonster();
        normalEnemyReward.expBase = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].commonMonsterExp;
        bossEnemyReward.expBase = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].bossMonsterExp;
    }
    public void SetKillCount()
    {
        curMonsterKillCount = 0;
        maxMonsterKillCount = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].monsterKillCount;
    }

    public void StageClear()
    {
        curStage++;
        SetReward();
        SetKillCount();
        infinityMap.MapReset();
    }
}
