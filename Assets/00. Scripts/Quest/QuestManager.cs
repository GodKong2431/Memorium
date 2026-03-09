using System.Collections;
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

    public SaveQuestData saveQuestData;

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

    private IEnumerator Start()
    {
        GameEventManager.OnQuestActionUpdated += HandleQuestAction;

        //JSON Data Load
        saveQuestData = JSONService.Load<SaveQuestData>();
        (currentQuestID, currentProgress) = saveQuestData.InitQuestData();

        // Delay first UI broadcast until quest table is loaded.
        yield return new WaitUntil(() => IsQuestDataReady);
        GameEventManager.OnQuestProgressChanged?.Invoke();
    }

    protected override void OnDestroy()
    {
        GameEventManager.OnQuestActionUpdated -= HandleQuestAction;
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

        int nextID = currentQuestID + 1;
        if (!DataManager.Instance.LineQuestDict.ContainsKey(nextID))
        {

            currentQuestID = 0;
            return;
        }

        currentQuestID = nextID;
        currentProgress = 0;
        GameEventManager.OnQuestProgressChanged?.Invoke();

        saveQuestData.SaveID(currentQuestID);
    }

    private static bool IsQuestDataReady =>
        DataManager.Instance != null &&
        DataManager.Instance.DataLoad &&
        DataManager.Instance.LineQuestDict != null;


    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        saveQuestData.Save(currentQuestID, currentProgress);
        JSONService.Save(saveQuestData);

    }
}