using System.Collections.Generic;
using UnityEngine;

public static class CheckDungeon
{
    // 던전 타입별 리퀘스트 ID 목록 캐시
    private static readonly Dictionary<StageType, List<int>> DungeonIdsByType = new Dictionary<StageType, List<int>>();
    // 1레벨 던전 해금에 필요한 일반 스테이지 레벨 캐시
    private static readonly Dictionary<StageType, int> FirstAccessStageLevelByType = new Dictionary<StageType, int>();

    private static bool isCacheBuilt;

    // 테이블이 다시 로드될 때 던전 캐시를 비운다
    public static void InvalidateCache()
    {
        isCacheBuilt = false;
        DungeonIdsByType.Clear();
        FirstAccessStageLevelByType.Clear();
    }

    // 해당 던전 타입의 총 레벨 수를 반환한다
    public static int GetMaxLevelCount(StageType type)
    {
        return TryGetDungeonIds(type, out List<int> dungeonIds)
            ? dungeonIds.Count
            : 0;
    }

    // 요청한 레벨을 실제 존재하는 던전 레벨 범위로 보정한다
    public static int ClampLevel(StageType type, int level)
    {
        int maxLevelCount = GetMaxLevelCount(type);
        if (maxLevelCount <= 0)
            return 1;

        return Mathf.Clamp(level, 1, maxLevelCount);
    }

    // 현재 저장 진행도 기준으로 입장 가능한 최고 레벨을 찾는다
    public static int GetMaxUnlockedLevel(StageType type)
    {
        int maxLevelCount = GetMaxLevelCount(type);
        if (maxLevelCount <= 0)
            return 1;

        int highestUnlockedLevel = 0;
        for (int level = 1; level <= maxLevelCount; level++)
        {
            if (!HasDungeonAccess(type, level))
                break;

            highestUnlockedLevel = level;
        }

        return Mathf.Clamp(highestUnlockedLevel == 0 ? 1 : highestUnlockedLevel, 1, maxLevelCount);
    }

    // 던전 레벨 입장 가능 여부를 진행도 기준으로 판정한다
    public static bool HasDungeonAccess(StageType type, int level)
    {
        if (!IsDungeonStage(type))
            return false;

        if (StageManager.Instance == null)
            return false;

        int maxLevelCount = GetMaxLevelCount(type);
        if (maxLevelCount <= 0)
            return false;

        int resolvedLevel = Mathf.Clamp(level, 1, maxLevelCount);
        if (resolvedLevel > 1)
        {
            if (!TryGetDungeonProgressIndex(type, out int index))
                return false;

            List<int> maxStage = StageManager.Instance.maxStage;
            if (maxStage == null || index < 0 || index >= maxStage.Count)
                return false;

            return maxStage[index] >= resolvedLevel - 1;
        }

        if (!TryGetFirstAccessStageLevel(type, out int requiredStageLevel))
            return false;

        List<int> currentMaxStage = StageManager.Instance.maxStage;
        return currentMaxStage != null &&
               currentMaxStage.Count > 0 &&
               currentMaxStage[0] >= requiredStageLevel;
    }

    // 해금 조건과 티켓 보유량을 함께 확인한다
    public static bool CanEnter(StageType type, int level, int requiredCount)
    {
        int resolvedLevel = ClampLevel(type, level);
        return HasDungeonAccess(type, resolvedLevel) &&
               HasEnoughTicket(type, resolvedLevel, requiredCount);
    }

    // 던전 타입과 레벨로 실제 던전 리퀘스트 행을 찾는다
    public static bool TryGetDungeonReq(StageType type, int level, out int dungeonId, out DungeonReqTable dungeonReq)
    {
        dungeonId = 0;
        dungeonReq = null;

        if (!TryGetDungeonIds(type, out List<int> dungeonIds))
            return false;

        int resolvedLevel = Mathf.Clamp(level, 1, dungeonIds.Count);
        dungeonId = dungeonIds[resolvedLevel - 1];
        return DataManager.Instance.DungeonReqDict.TryGetValue(dungeonId, out dungeonReq);
    }

    // 현재 던전 레벨이 요구하는 티켓 아이템 ID를 반환한다
    public static int GetTicketItemId(StageType type, int level)
    {
        return TryGetDungeonReq(type, level, out _, out DungeonReqTable dungeonReq)
            ? dungeonReq.ItemID
            : 0;
    }

    // 현재 던전 레벨 티켓의 보유량을 반환한다
    public static BigDouble GetTicketAmount(StageType type, int level)
    {
        int ticketItemId = GetTicketItemId(type, level);
        if (ticketItemId <= 0 || InventoryManager.Instance == null)
            return BigDouble.Zero;

        return InventoryManager.Instance.GetItemAmount(ticketItemId);
    }

    // 요구 수량 이상 티켓을 가지고 있는지 확인한다
    public static bool HasEnoughTicket(StageType type, int level, int requiredCount)
    {
        return GetTicketAmount(type, level) >= new BigDouble(Mathf.Max(1, requiredCount));
    }

    // 해당 던전 레벨의 티켓을 실제로 소모한다
    public static bool TrySpendTicket(StageType type, int level, int requiredCount)
    {
        int ticketItemId = GetTicketItemId(type, level);
        if (ticketItemId <= 0 || InventoryManager.Instance == null)
            return false;

        return InventoryManager.Instance.RemoveItem(ticketItemId, new BigDouble(Mathf.Max(1, requiredCount)));
    }

    // 던전 타입별 ID 목록 캐시를 가져온다
    private static bool TryGetDungeonIds(StageType type, out List<int> dungeonIds)
    {
        dungeonIds = null;

        if (!EnsureCacheBuilt())
            return false;

        return DungeonIdsByType.TryGetValue(type, out dungeonIds) &&
               dungeonIds != null &&
               dungeonIds.Count > 0;
    }

    // 1레벨 던전 입장에 필요한 일반 스테이지 레벨을 가져온다
    private static bool TryGetFirstAccessStageLevel(StageType type, out int requiredStageLevel)
    {
        requiredStageLevel = 0;

        if (!EnsureCacheBuilt())
            return false;

        return FirstAccessStageLevelByType.TryGetValue(type, out requiredStageLevel) &&
               requiredStageLevel > 0;
    }

    // DungeonReqTable과 StageManageTable을 바탕으로 캐시를 구성한다
    private static bool EnsureCacheBuilt()
    {
        if (isCacheBuilt)
            return true;

        if (DataManager.Instance == null ||
            !DataManager.Instance.DataLoad ||
            DataManager.Instance.DungeonReqDict == null ||
            DataManager.Instance.StageManageDict == null)
        {
            return false;
        }

        DungeonIdsByType.Clear();
        FirstAccessStageLevelByType.Clear();

        foreach (KeyValuePair<int, DungeonReqTable> pair in DataManager.Instance.DungeonReqDict)
        {
            DungeonReqTable dungeonReq = pair.Value;
            if (dungeonReq == null || !IsDungeonStage(dungeonReq.stageType))
                continue;

            if (!DungeonIdsByType.TryGetValue(dungeonReq.stageType, out List<int> dungeonIds))
            {
                dungeonIds = new List<int>();
                DungeonIdsByType[dungeonReq.stageType] = dungeonIds;
            }

            dungeonIds.Add(pair.Key);

            if (dungeonReq.stageID01 <= 0)
                continue;

            if (!DataManager.Instance.StageManageDict.TryGetValue(dungeonReq.stageID01, out StageManageTable requiredStageData) ||
                requiredStageData == null)
            {
                continue;
            }

            int requiredStageLevel = requiredStageData.stageLevel;
            if (!FirstAccessStageLevelByType.TryGetValue(dungeonReq.stageType, out int currentRequiredStageLevel) ||
                requiredStageLevel < currentRequiredStageLevel)
            {
                FirstAccessStageLevelByType[dungeonReq.stageType] = requiredStageLevel;
            }
        }

        foreach (List<int> dungeonIds in DungeonIdsByType.Values)
            dungeonIds.Sort();

        isCacheBuilt = true;
        return true;
    }

    // 던전 진행도 저장 리스트에서 사용할 인덱스를 계산한다
    private static bool TryGetDungeonProgressIndex(StageType type, out int index)
    {
        index = (int)type - (int)StageType.NormalStage;
        return IsDungeonStage(type) && index >= 0;
    }

    // 일반 스테이지를 제외한 실제 던전 타입인지 확인한다
    private static bool IsDungeonStage(StageType type)
    {
        return type != StageType.None && type != StageType.NormalStage;
    }
}
