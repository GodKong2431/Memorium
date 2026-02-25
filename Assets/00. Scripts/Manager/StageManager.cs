using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public int normalStage = 1;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
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
        if (stageKeyList == null)
            stageKeyList = new List<int>();
        stageKeyList.Clear();

        foreach (int key in keyList)
        {
            if (DataManager.Instance.StageManageDict[key].stageType == curStageType)
            {
                stageKeyList.Add(key);
            }
        }
        if (monsterSpawner == null)
            monsterSpawner = GameObject.FindFirstObjectByType<MonsterSpawner>();
        if (infinityMap == null)
            infinityMap = GameObject.FindFirstObjectByType<InfinityMap>();
        //stageKeyList = DataManager.Instance.StageManageDict.Keys.ToList<int>();
        //stageKeyList.Sort();
    }
    public void OnClickBossSummonButtonClick()
    {
        Debug.Log("[StageManager] 보스 소환 버튼 클릭");
        isReadyToBossSpawn = !isReadyToBossSpawn;
        //BossSpawnBtn.SetActive(false);
        //bossSpawnBtn.interactable = false;
        onClickBossSpawnBtn = true;
    }
    public void CheckBossEnemySpawn()
    {
        curMonsterKillCount++;
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
        //여기에 현재 몇 마리 잡았는지 UI로 보여주는 코드도 추가
        //if (maxMonsterKillCount <= EnemyKillRewardDispatcher.TotalKillCount)
        //if (maxMonsterKillCount <= curMonsterKillCount)
        //{
        //    if (!onClickBossSpawnBtn)
        //    {
        //        Debug.Log("[StageManager] 보스 소환 버튼 활성화");
        //        bossSpawnBtn.interactable = true;
        //    }
        //}
    }


    //스테이지 증가하면 curstage 증가 후 아래 메서드 다시 호출
    public void SetReward()
    {
        int prevCurFloor=curFloor;
        curFloor = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].floorNumber;
        monsterSpawner.SetMonster();

        //노말, 보스 몬스터 경험치 세팅
        normalEnemyReward.expBase = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].commonMonsterExp;
        bossEnemyReward.expBase = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].bossMonsterExp;

        //스테이지에서 사용할 드롭테이블 인덱스 가져오기
        int dropTableId = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].dropTableID;
        Debug.Log($"[StageManager] dropTableId : {dropTableId}");
        ItemDropTable dropTable = DataManager.Instance.ItemDropDict[dropTableId];

        // RewardManager로 분리했습니다. (ItemDropSettings는 변수(데이터)만 관리)
        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.SetDropTable(dropTable);
        }

        GameEventManager.OnStageChanged?.Invoke(curFloor, DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].sceneNumber);
        //if (curStage % 20 == 0)
        //{
        //    GameEventManager.OnStageChanged?.Invoke(curFloor, 20);
        //}
        //else
        //{
        //    GameEventManager.OnStageChanged?.Invoke(curFloor, curStage % 20);
        //}
    }
    public void SetKillCount()
    {
        curMonsterKillCount = 0;
        maxMonsterKillCount = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].monsterKillCount;
        Debug.Log($"[StageManager] MaxKillCount = {maxMonsterKillCount} 씬 넘버 = {stageKeyList[curStage - 1]}");
        //curMonsterKillCountText.text = curMonsterKillCount + "/" + maxMonsterKillCount;
        //curMonsterGuage.fillAmount = 0;
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
    }

    public void StageClear()
    {
        //if (stageKeyList.Count > curStage - 1)
        //    curStage++;

        if (curStageType == StageType.NormalStage)
        {
            if (stageKeyList.Count > curStage - 1)
                curStage++;
            normalStage = curStage;
            SetReward();
            SetKillCount();
            infinityMap.MapReset();
        }
        //일반 스테이지가 아닌 던전 클리어 시
        //
        else
        {
            SetStageType(StageType.NormalStage, normalStage);
            SceneController.Instance.LoadScene(SceneType.StageScene);
        }
    }

    public void SetStageType(StageType dungeonType, int level)
    {
        monsterSpawner = null;
        curStage = level;
        curStageType = dungeonType;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[StageManager] 씬 넘어감으로 인해 값 초기화");
        //아래 값들은 씬을 넘어간 다음에 작성을 진행해야 할 것
        //스테이지 타입 전용 스테이지 키 리스트 설정
        Init();
        //해당 스테이지에 걸맞는 드롭 및 몬스터 세팅 <- 이거 몬스터 세팅 시점을 잘 설정해야 할 것 같음, 씬 넘어가도 유지되려면 몬스터 스포너를 싱글톤으로 만들거나 해당 정보를 유지할 필요가 있음
        SetReward();
        SetKillCount();
    }
}
