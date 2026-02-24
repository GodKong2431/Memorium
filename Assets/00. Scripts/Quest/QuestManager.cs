using UnityEngine;
// GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questElimination, 1); 몬스터 죽었을 때 호출
// GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questEquipImprove, 1); 장비 강화했을 때 호출
// GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questSkillImprove, 1); 스킬 강화했을 때 호출


/// <summary>
/// 퀘스트의 진행 상태를 관리하는 매니저 클래스
/// </summary>
public class QuestManager : Singleton<QuestManager>
{
    [Header("현재 진행 상태")]
    public int currentQuestID = 7010001;
    public int currentProgress = 0;

    // 현재 퀘스트
    public LineQuestTable CurrentQuestData
    {
        get
        {
            if (DataManager.Instance.LineQuestDict.TryGetValue(currentQuestID, out var data))
                return data;
            return null;
        }
    }


    // 완료 가능 상태인지
    public bool IsCurrentQuestComplete => CurrentQuestData != null && currentProgress >= CurrentQuestData.reqCount;

    private void Start()
    {
        GameEventManager.OnQuestActionUpdated += HandleQuestAction;
        GameEventManager.OnQuestProgressChanged?.Invoke();
    }

    protected override void OnDestroy()
    {
        GameEventManager.OnQuestActionUpdated -= HandleQuestAction;
        base.OnDestroy();
    }

    /// <summary>
    /// 다른 클래스에서 퀘스트 행동이 발생할 때마다 이 함수를 호출
    /// </summary>
    private void HandleQuestAction(QuestType type, int amount)
    {
        var questData = CurrentQuestData;
        if (questData == null || IsCurrentQuestComplete) return;
        // 발생한 행동이 현재 퀘스트의 요구와 맞으면
        if (questData.questType == type)
        {
            currentProgress += amount;

            // 목표치 달성 시
            if (currentProgress >= questData.reqCount)
            {
                currentProgress = questData.reqCount;
                GameEventManager.OnQuestCompleted?.Invoke();
            }

            GameEventManager.OnQuestProgressChanged?.Invoke();
        }
    }

    /// <summary>
    /// 퀘스트 완료 후 보상 지급과 다음 퀘스트를 로드
    /// </summary>
    public void ClaimReward()
    {
        var questData = CurrentQuestData;
        if (questData == null || !IsCurrentQuestComplete) return;

        GiveReward(questData.rewardGroupID);
        LoadNextQuest();
    }

    /// <summary>
    /// 데이터 매니저에서 보상을 조회하여 지급
    /// </summary>
    private void GiveReward(int rewardGroupID)
    {
        if (DataManager.Instance.QuestRewardsDict.TryGetValue(rewardGroupID, out var reward))
        {
            // 빈 데이터 예외 처리
            if (reward.ItemID == 0 || reward.rewardItemCount == 0)
            {
                Debug.LogWarning($"[QuestManager] 보상 그룹 ID {rewardGroupID}의 아이템 데이터가 비어있습니다.");
                return;
            }

            // TODO: 실제 인벤토리나 재화 매니저에 아이템 추가 로직 연결 필요
            Debug.Log($"보상 획득! 아이템ID: {reward.ItemID}, 개수: {reward.rewardItemCount}");
        }
        else
        {
            Debug.LogError($"[QuestManager] 보상 그룹 ID {rewardGroupID}를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 다음 퀘스트로 이동
    /// </summary>
    private void LoadNextQuest()
    {
        int nextID = currentQuestID + 1;

        // 다음 id 없으면 퀘스트 모두 클리어한 것으로 생각
        if (!DataManager.Instance.LineQuestDict.ContainsKey(nextID))
        {
            Debug.Log("모든 메인 퀘스트를 완료했습니다!");
            currentQuestID = 0;
            return;
        }

        // 퀘스트 데이터 갱신 및 초기화
        currentQuestID = nextID;
        currentProgress = 0;
        GameEventManager.OnQuestProgressChanged?.Invoke();
    }
}