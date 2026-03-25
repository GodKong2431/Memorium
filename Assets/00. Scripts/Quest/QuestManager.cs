using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questElimination, 1);
// GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questEquipImprove, 1);
// GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questSkillImprove, 1);

/// <summary>
/// Manages quest progress and rewards.
/// </summary>
public class QuestManager : Singleton<QuestManager>
{
    [Header("Current Progress")]
    public int currentQuestID = 7010001;
    public int currentProgress = 0;
    public int completedQuestCount = 0;

    public SaveQuestData saveQuestData;

    public bool DataLoad = false;

    // Current quest row.
    public LineQuestTable CurrentQuestData
    {
        get
        {
            if (!IsQuestDataReady || currentQuestID <= 0)
                return null;

            return DataManager.Instance.LineQuestDict.TryGetValue(currentQuestID, out var data) ? data : null;
        }
    }

    // True when the current quest requirement is satisfied.
    public bool IsCurrentQuestComplete
    {
        get
        {
            LineQuestTable current = CurrentQuestData;
            return current != null && currentProgress >= current.reqCount;
        }
    }

    public int CurrentDisplayQuestNumber => Mathf.Max(1, completedQuestCount + 1);

    private IEnumerator Start()
    {
        GameEventManager.OnQuestActionUpdated += HandleQuestAction;
        GameEventManager.OnQuestProgressChanged += HandleQuestProgressChanged;

        //JSON Data Load
        saveQuestData = JSONService.Load<SaveQuestData>();
        (currentQuestID, currentProgress, completedQuestCount) = saveQuestData.InitQuestData();

        // Delay first UI broadcast until quest table is loaded.
        yield return new WaitUntil(() => IsQuestDataReady);
        NormalizeCurrentQuestState();
        GameEventManager.OnQuestProgressChanged?.Invoke();
        DataLoad = true;
    }

    protected override void OnDestroy()
    {
        GameEventManager.OnQuestActionUpdated -= HandleQuestAction;
        GameEventManager.OnQuestProgressChanged -= HandleQuestProgressChanged;
        base.OnDestroy();
    }

    private void HandleQuestAction(QuestType type, int amount)
    {
        LineQuestTable questData = CurrentQuestData;
        if (questData == null || IsCurrentQuestComplete)
            return;
        if (questData.questType != type)
            return;

        currentProgress += amount;

        if (currentProgress >= questData.reqCount)
        {
            currentProgress = questData.reqCount;
            GameEventManager.OnQuestCompleted?.Invoke();
        }

        GameEventManager.OnQuestProgressChanged?.Invoke();
    }

    public void ClaimReward()
    {
        LineQuestTable questData = CurrentQuestData;
        if (questData == null || !IsCurrentQuestComplete)
            return;

        GiveReward(questData.rewardGroupID);
        LoadNextQuest();
    }

    private void GiveReward(int rewardGroupID)
    {
        if (DataManager.Instance == null || DataManager.Instance.QuestRewardsDict == null)
            return;

        if (!DataManager.Instance.QuestRewardsDict.TryGetValue(rewardGroupID, out var reward))
        {
            Debug.LogError($"[QuestManager] Reward group not found: {rewardGroupID}");
            return;
        }

        if (reward.ItemID == 0 || reward.rewardItemCount == 0)
        {
            Debug.LogWarning($"[QuestManager] Reward payload is empty: {rewardGroupID}");
            return;
        }

        if (RewardManager.Instance != null)
            RewardManager.Instance.GrantReward(reward.ItemID, reward.rewardItemCount);
    }

    private void LoadNextQuest()
    {
        if (!IsQuestDataReady)
            return;

        List<int> orderedQuestIds = GetOrderedQuestIds();
        if (orderedQuestIds.Count == 0)
        {
            currentQuestID = 0;
            currentProgress = 0;
            completedQuestCount = 0;
            GameEventManager.OnQuestProgressChanged?.Invoke();
            return;
        }

        completedQuestCount++;

        int currentIndex = orderedQuestIds.IndexOf(currentQuestID);
        if (currentIndex < 0)
        {
            currentQuestID = orderedQuestIds[0];
        }
        else
        {
            int nextIndex = (currentIndex + 1) % orderedQuestIds.Count;
            currentQuestID = orderedQuestIds[nextIndex];
        }

        currentProgress = 0;
        GameEventManager.OnQuestProgressChanged?.Invoke();
    }

    private void HandleQuestProgressChanged()
    {
        saveQuestData?.Save(currentQuestID, currentProgress, completedQuestCount);
    }

    private void NormalizeCurrentQuestState()
    {
        List<int> orderedQuestIds = GetOrderedQuestIds();
        if (orderedQuestIds.Count == 0)
        {
            currentQuestID = 0;
            currentProgress = 0;
            completedQuestCount = 0;
            return;
        }

        completedQuestCount = Mathf.Max(0, completedQuestCount);

        if (!orderedQuestIds.Contains(currentQuestID))
        {
            currentQuestID = orderedQuestIds[0];
            currentProgress = 0;
        }

        LineQuestTable currentQuestData = CurrentQuestData;
        if (currentQuestData == null)
        {
            currentQuestID = orderedQuestIds[0];
            currentProgress = 0;
            return;
        }

        int currentQuestIndex = orderedQuestIds.IndexOf(currentQuestID);
        if (currentQuestIndex >= 0)
            completedQuestCount = Mathf.Max(completedQuestCount, currentQuestIndex);

        currentProgress = Mathf.Clamp(currentProgress, 0, Mathf.Max(0, currentQuestData.reqCount));
    }

    private List<int> GetOrderedQuestIds()
    {
        if (!IsQuestDataReady)
            return new List<int>();

        return DataManager.Instance.LineQuestDict
            .Where(pair => pair.Value != null)
            .OrderBy(pair => pair.Value.questNum)
            .ThenBy(pair => pair.Key)
            .Select(pair => pair.Key)
            .ToList();
    }

    private static bool IsQuestDataReady =>
        DataManager.Instance != null &&
        DataManager.Instance.DataLoad &&
        DataManager.Instance.LineQuestDict != null;


    //protected override void OnApplicationQuit()
    //{
    //    base.OnApplicationQuit();
    //    saveQuestData.Save(currentQuestID, currentProgress);
    //    JSONService.Save(saveQuestData);
    //}

    //public async Task AutoSaveTask()
    //{
    //    Debug.Log("[QuestManager] 퀘스트 변경사항 확인 및 데이터 저장");
    //    await JSONService.SaveFileOnAsync(saveQuestData);
    //    saveQuestData.ClearDirty();
    //}
}
