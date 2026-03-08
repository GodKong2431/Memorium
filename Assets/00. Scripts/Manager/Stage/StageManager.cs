using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 스테이지 진행 상태, 보상 세팅, 처치 진행도, 씬 전환 시 진입 상태를 관리한다.
public class StageManager : Singleton<StageManager>
{
    // 현재 진행 스테이지(1부터 시작)
    public int curStage = 1;
    public int maxStage = 1;
    public int curMonsterKillCount = 0;
    public int maxMonsterKillCount = 0;

    // 현재 층(1부터 시작)
    public int curFloor = 1;

    // 현재 StageType에 해당하는 스테이지 키 목록
    public List<int> stageKeyList;

    // 스테이지 보상 기준 데이터(일반/보스)
    public EnemyRewardData normalEnemyReward;
    public EnemyRewardData bossEnemyReward;

    // 씬 객체 참조
    public InfinityMap infinityMap;
    public MonsterSpawner monsterSpawner;

    // 보스 소환 가능 상태
    public bool isReadyToBossSpawn = false;

    // 현재 보스 스테이지 흐름 안에 있는지 여부
    public bool onBossStage = false;

    // 현재 스테이지 타입과 일반 스테이지 진행 값
    [SerializeField] private StageType curStageType;
    public int normalStage = 1;

    // 일반 스테이지 실패 플래그
    public bool onFailedStage = false;

    // 저장 데이터
    public SaveStageData saveStageData;
    public event Action OnStageClearOrFailed;

    // 씬 전환 중 스테이지 진입 요청을 유지하기 위한 버퍼
    private static bool hasPendingStageEntryRequest = false;
    private static StageType pendingStageType = StageType.None;
    private static int pendingStageLevel = 1;

    // 스테이지 키 캐시/해석 서비스
    private StageKeyCatalog stageKeyCatalog;

    private IEnumerator Start()
    {
        // 데이터 테이블 로드 완료까지 대기
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        // 저장 데이터 복원
        saveStageData = JSONService.Load<SaveStageData>();
        var (savedCurStage, savedMaxStage, savedOnFailedStage) = saveStageData.InitStageData();

        normalStage = Mathf.Max(1, savedCurStage);
        maxStage = Mathf.Max(1, savedMaxStage);
        onFailedStage = savedOnFailedStage;

        // 씬 전환 전에 들어온 진입 요청이 있으면 우선 반영
        if (!TryApplyPendingStageEntryRequest())
        {
            if (curStageType == StageType.NormalStage)
                ApplyStageState(StageType.NormalStage, normalStage, updateNormalStage: true);
            else
                ApplyStageState(curStageType, curStage, updateNormalStage: false);
        }

        // 클리어/실패 시 저장
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
        // 이벤트 해제
        EnemyKillRewardDispatcher.OnKillCountChanged -= CheckBossEnemySpawn;
        EnemyKillRewardDispatcher.OnBossKilled -= StageClear;
        GameEventManager.OnSummonBossClicked -= OnClickBossSummonButtonClick;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    // 스테이지 서비스와 씬 의존 객체를 초기화한다.
    private void Init()
    {
        if (!EnsureStageServices())
            return;

        if (stageKeyList == null)
            stageKeyList = new List<int>();
        stageKeyCatalog.TryCopyStageKeys(curStageType, stageKeyList);

        if (monsterSpawner == null)
            monsterSpawner = GameObject.FindFirstObjectByType<MonsterSpawner>();
        if (infinityMap == null)
            infinityMap = GameObject.FindFirstObjectByType<InfinityMap>();
    }

    // 보스 소환 버튼 클릭 이벤트 처리
    public void OnClickBossSummonButtonClick()
    {
        if (curMonsterKillCount < maxMonsterKillCount)
            return;
        if (isReadyToBossSpawn)
            return;

        Debug.Log("[StageManager] Boss summon button clicked.");
        isReadyToBossSpawn = true;
    }

    // 누적 처치 수를 반영하고 스테이지 진행도 UI 이벤트를 발행한다.
    public void CheckBossEnemySpawn(int totalKillCount)
    {
        curMonsterKillCount = Mathf.Clamp(totalKillCount, 0, maxMonsterKillCount);
        GameEventManager.OnStageProgressChanged?.Invoke(curMonsterKillCount, maxMonsterKillCount);
    }

    // 현재 스테이지 데이터 기준으로 맵/몬스터/보상을 갱신한다.
    public void SetReward()
    {
        if (!TryGetCurrentStageData(out StageManageTable stageData))
            return;

        curFloor = stageData.floorNumber;
        MapManager.Instance.MapSetting(curStageType, curFloor);

        if (monsterSpawner == null)
            monsterSpawner = GameObject.FindFirstObjectByType<MonsterSpawner>();

        monsterSpawner?.SetMonster();

        if (normalEnemyReward != null)
            normalEnemyReward.expBase = stageData.commonMonsterExp;
        if (bossEnemyReward != null)
            bossEnemyReward.expBase = stageData.bossMonsterExp;

        int dropTableId = stageData.dropTableID;

        if (DataManager.Instance.ItemDropDict != null &&
            DataManager.Instance.ItemDropDict.TryGetValue(dropTableId, out ItemDropTable dropTable))
        {
            if (RewardManager.Instance != null)
                RewardManager.Instance.SetDropTable(dropTable);
        }

        GameEventManager.OnStageChanged?.Invoke(curFloor, stageData.sceneNumber);
    }

    // 처치 진행 상태를 초기화하고 현재 스테이지 목표 처치 수를 설정한다.
    public void SetKillCount()
    {
        onBossStage = false;
        isReadyToBossSpawn = false;

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

    // 스테이지 클리어 처리
    public void StageClear()
    {
        if (curStageType == StageType.NormalStage)
        {
            onFailedStage = false;
            if (stageKeyList != null && curStage < stageKeyList.Count)
                curStage++;

            normalStage = curStage;
            SetReward();
            SetKillCount();
            infinityMap?.MapReset();

            if (curStage > maxStage)
                maxStage = curStage - 1;

            OnStageClearOrFailed.Invoke();
        }
        else
        {
            SetStageType(StageType.NormalStage, normalStage);
            SceneController.Instance.LoadScene(SceneType.StageScene);
        }
    }

    // 스테이지 실패 처리
    public void StageFailed()
    {
        if (curStageType == StageType.NormalStage)
        {
            onFailedStage = true;

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

    // 스테이지 타입/레벨 변경 요청을 큐에 저장하고 즉시 상태에 반영한다.
    public void SetStageType(StageType dungeonType, int level)
    {
        monsterSpawner = null;

        int requestedLevel = Mathf.Max(1, level);

        QueuePendingStageEntryRequest(dungeonType, requestedLevel);
        ApplyStageState(dungeonType, requestedLevel, updateNormalStage: dungeonType == StageType.NormalStage);
    }

    // 씬 로드 완료 시 스테이지 상태를 다시 반영한다.
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[StageManager] Scene loaded. Refresh stage data.");

        TryApplyPendingStageEntryRequest();

        Init();
        SetReward();
        SetKillCount();
    }

    // 현재 stageType/level 기준으로 StageManageTable을 조회한다.
    private bool TryGetCurrentStageData(out StageManageTable stageData)
    {
        stageData = null;

        if (!EnsureStageServices())
            return false;

        if (!stageKeyCatalog.TryResolve(curStageType, curStage, out stageData, out int resolvedLevel, out _, out List<int> resolvedStageKeyList))
            return false;

        curStage = resolvedLevel;

        if (stageKeyList == null)
            stageKeyList = new List<int>();
        stageKeyList.Clear();
        stageKeyList.AddRange(resolvedStageKeyList);

        return true;
    }

    // 씬 전환 중 유지할 스테이지 진입 요청을 저장한다.
    private static void QueuePendingStageEntryRequest(StageType stageType, int level)
    {
        hasPendingStageEntryRequest = true;
        pendingStageType = stageType;
        pendingStageLevel = Mathf.Max(1, level);
    }

    // 저장된 스테이지 진입 요청이 있으면 현재 상태에 적용한다.
    private bool TryApplyPendingStageEntryRequest()
    {
        if (!hasPendingStageEntryRequest)
            return false;

        ApplyStageState(
            pendingStageType,
            pendingStageLevel,
            updateNormalStage: pendingStageType == StageType.NormalStage);

        hasPendingStageEntryRequest = false;
        return true;
    }

    // stageType/level을 현재 상태로 반영하고 유효 범위로 보정한다.
    private void ApplyStageState(StageType stageType, int level, bool updateNormalStage)
    {
        curStageType = stageType;
        curStage = Mathf.Max(1, level);

        if (EnsureStageServices() &&
            stageKeyCatalog.TryResolve(curStageType, curStage, out _, out int resolvedLevel, out _, out List<int> resolvedStageKeyList))
        {
            curStage = resolvedLevel;

            if (stageKeyList == null)
                stageKeyList = new List<int>();
            stageKeyList.Clear();
            stageKeyList.AddRange(resolvedStageKeyList);
        }
        else if (stageKeyList != null)
        {
            stageKeyList.Clear();
        }

        if (updateNormalStage)
            normalStage = curStage;
    }

    // Stage 관련 서비스 의존성을 준비한다.
    private bool EnsureStageServices()
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.StageManageDict == null)
            return false;

        if (stageKeyCatalog == null)
            stageKeyCatalog = new StageKeyCatalog();

        return true;
    }

    protected override void OnApplicationQuit()
    {
        // 종료 시 저장 데이터 기록
        base.OnApplicationQuit();
        JSONService.Save(saveStageData);
    }
}
