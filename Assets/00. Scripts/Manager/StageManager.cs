using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    //[SerializeField] GameObject BossSpawnBtn;
    [SerializeField] Button bossSpawnBtn;
    bool onClickBossSpawnBtn=false;

    //[SerializeField] TextMeshProUGUI curStageAndFloorText;
    //[SerializeField] TextMeshProUGUI curMonsterKillCountText;
    //[SerializeField] Image curMonsterGuage;
    [SerializeField] StageType curStageType;

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
        GameEventManager.OnSummonBossClicked += OnClickBossSummonButtonClick;
        //bossSpawnBtn.onClick.AddListener(() => 
        //{ 
        //    isReadyToBossSpawn = !isReadyToBossSpawn;
        //    //BossSpawnBtn.SetActive(false);
        //    bossSpawnBtn.interactable = false;
        //    onClickBossSpawnBtn = true;
        //});
    }
    private void Init()
    {
        List<int> keyList= DataManager.Instance.StageManageDict.Keys.ToList<int>();
        keyList.Sort();
        stageKeyList= new List<int>();
        foreach (int key in keyList)
        {
            if (DataManager.Instance.StageManageDict[key].stageType == curStageType)
            {
                stageKeyList.Add(key);
            }
        }
        //stageKeyList = DataManager.Instance.StageManageDict.Keys.ToList<int>();
        //stageKeyList.Sort();
    }
    public void OnClickBossSummonButtonClick()
    {
        isReadyToBossSpawn = !isReadyToBossSpawn;
        //BossSpawnBtn.SetActive(false);
        bossSpawnBtn.interactable = false;
        onClickBossSpawnBtn = true;
    }
    public void CheckBossEnemySpawn()
    {
        curMonsterKillCount++;
        //curMonsterGuage.fillAmount = (float)curMonsterKillCount / (float)maxMonsterKillCount;
        //curMonsterKillCountText.text = curMonsterKillCount.ToString();
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
        //여기에 현재 몇 마리 잡았는지 UI로 보여주는 코드도 추가
        //if (maxMonsterKillCount <= EnemyKillRewardDispatcher.TotalKillCount)
        if (maxMonsterKillCount <= curMonsterKillCount)
        {
            //if (!BossSpawnBtn.activeSelf)
            //{
            //    BossSpawnBtn.SetActive(true);
            //}
            if(!onClickBossSpawnBtn)
                bossSpawnBtn.interactable = true;
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
        if (curStage % 20 == 0)
        {
            GameEventManager.OnStageChanged?.Invoke(curFloor, 20);
        }
        else
        {
            GameEventManager.OnStageChanged?.Invoke(curFloor, curStage % 20);
        }
    }
    public void SetKillCount()
    {
        curMonsterKillCount = 0;
        maxMonsterKillCount = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].monsterKillCount;
        //curMonsterKillCountText.text = curMonsterKillCount + "/" + maxMonsterKillCount;
        //curMonsterGuage.fillAmount = 0;
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
    }

    public void StageClear()
    {
        if(stageKeyList.Count<curStage-1)
            curStage++;
        SetReward();
        SetKillCount();
        infinityMap.MapReset();
    }
}
