using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageManager : Singleton<StageManager>
{
    // Current stage index starts at 1.
    public int curStage = 1;
    public int maxStage = 1;
    public int curMonsterKillCount = 0;
    public int maxMonsterKillCount = 0;

    // Current floor index starts at 1.
    public int curFloor = 1;

    // Stage keys filtered by StageType and sorted ascending.
    public List<int> stageKeyList;

    public EnemyRewardData normalEnemyReward;
    public EnemyRewardData bossEnemyReward;

    public InfinityMap infinityMap;
    public MonsterSpawner monsterSpawner;

    public bool isReadyToBossSpawn = false;

    [SerializeField] private Button bossSpawnBtn;
    private bool onClickBossSpawnBtn = false;

    // True while player is in boss stage flow.
    public bool onBossStage = false;

    [SerializeField] private StageType curStageType;
    public int normalStage = 1;

    public bool onFailedStage = false;

    public SaveStageData saveStageData;
    public event Action OnStageClearOrFailed;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);


        saveStageData = JSONService.Load<SaveStageData>();
        (curStage, maxStage, onFailedStage) = saveStageData.InitStageData();
        OnStageClearOrFailed += () =>
        {
            saveStageData.Save(curStage, maxStage, onFailedStage);
        };

        Init();
        SetReward();
        SetKillCount();

        EnemyKillRewardDispatcher.OnKillCountChanged += CheckBossEnemySpawn;
        EnemyKillRewardDispatcher.OnBossKilled += StageClear;
        GameEventManager.OnSummonBossClicked += OnClickBossSummonButtonClick;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        EnemyKillRewardDispatcher.OnKillCountChanged -= CheckBossEnemySpawn;
        EnemyKillRewardDispatcher.OnBossKilled -= StageClear;
        GameEventManager.OnSummonBossClicked -= OnClickBossSummonButtonClick;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void Init()
    {
        if (DataManager.Instance == null || DataManager.Instance.StageManageDict == null)
            return;

        List<int> keyList = DataManager.Instance.StageManageDict.Keys.ToList();
        keyList.Sort();

        if (stageKeyList == null)
            stageKeyList = new List<int>();
        stageKeyList.Clear();

        foreach (int key in keyList)
        {
            if (DataManager.Instance.StageManageDict[key].stageType == curStageType)
                stageKeyList.Add(key);
        }

        if (monsterSpawner == null)
            monsterSpawner = GameObject.FindFirstObjectByType<MonsterSpawner>();
        if (infinityMap == null)
            infinityMap = GameObject.FindFirstObjectByType<InfinityMap>();
    }

    public void OnClickBossSummonButtonClick()
    {
        if (curMonsterKillCount < maxMonsterKillCount)
            return;
        if (isReadyToBossSpawn)
            return;

        Debug.Log("[StageManager] 보스 소환 버튼 클릭");
        isReadyToBossSpawn = true;
        onClickBossSpawnBtn = true;

        if (bossSpawnBtn != null)
            bossSpawnBtn.interactable = false;
    }

    public void CheckBossEnemySpawn(int totalKillCount)
    {
        curMonsterKillCount = Mathf.Clamp(totalKillCount, 0, maxMonsterKillCount);
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
    }

    public void SetReward()
    {
        if (!TryGetCurrentStageData(out StageManageTable stageData))
            return;

        curFloor = stageData.floorNumber;

        if (curStageType == StageType.NormalStage)
            MapManager.Instance.MapSetting(curStageType, curFloor);
        else
            MapManager.Instance.MapSetting(curStageType, curStage);

        if (monsterSpawner == null)
            monsterSpawner = GameObject.FindFirstObjectByType<MonsterSpawner>();

        monsterSpawner?.SetMonster();

        if (normalEnemyReward != null)
            normalEnemyReward.expBase = stageData.commonMonsterExp;
        if (bossEnemyReward != null)
            bossEnemyReward.expBase = stageData.bossMonsterExp;

        int dropTableId = stageData.dropTableID;
        Debug.Log($"[StageManager] dropTableId : {dropTableId}");

        if (DataManager.Instance.ItemDropDict != null && DataManager.Instance.ItemDropDict.TryGetValue(dropTableId, out ItemDropTable dropTable))
        {
            if (RewardManager.Instance != null)
                RewardManager.Instance.SetDropTable(dropTable);
        }

        GameEventManager.OnStageChanged?.Invoke(curFloor, stageData.sceneNumber);
    }

    public void SetKillCount()
    {
        onBossStage = false;
        isReadyToBossSpawn = false;
        onClickBossSpawnBtn = false;

        curMonsterKillCount = 0;

        if (!TryGetCurrentStageData(out StageManageTable stageData))
        {
            maxMonsterKillCount = 0;
            GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
            return;
        }

        maxMonsterKillCount = stageData.monsterKillCount;
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
    }

    public void StageClear()
    {
        if (curStageType == StageType.NormalStage)
        {
            onFailedStage = false;
            if (stageKeyList != null && stageKeyList.Count > curStage - 1)
                curStage++;

            normalStage = curStage;
            SetReward();
            SetKillCount();
            infinityMap?.MapReset();

            OnStageClearOrFailed.Invoke();
        }
        else
        {
            SetStageType(StageType.NormalStage, normalStage);
            SceneController.Instance.LoadScene(SceneType.StageScene);
        }
    }

    public void StageFailed()
    {
        if (curStageType == StageType.NormalStage)
        {
            onFailedStage = true;

            // If failure happened before entering boss stage, move one stage back.
            if (curStage - 2 >= 0 && !onBossStage)
                curStage--;

            normalStage = curStage;
            SetReward();
            SetKillCount();
            infinityMap?.MapReset();

            OnStageClearOrFailed.Invoke();
        }
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
        Init();
        SetReward();
        SetKillCount();
    }

    private bool TryGetCurrentStageData(out StageManageTable stageData)
    {
        stageData = null;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.StageManageDict == null)
            return false;
        if (stageKeyList == null || stageKeyList.Count == 0)
            return false;

        int index = Mathf.Clamp(curStage - 1, 0, stageKeyList.Count - 1);
        int stageKey = stageKeyList[index];

        return DataManager.Instance.StageManageDict.TryGetValue(stageKey, out stageData);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        JSONService.Save(saveStageData);
    }
}
