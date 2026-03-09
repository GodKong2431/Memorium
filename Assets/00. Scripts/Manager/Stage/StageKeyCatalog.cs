using System.Collections.Generic;
using UnityEngine;

// StageType별 스테이지 키를 캐싱하고 요청 레벨을 실제 스테이지 데이터로 해석한다.
public sealed class StageKeyCatalog
{
    private readonly Dictionary<StageType, List<int>> stageKeysByType = new Dictionary<StageType, List<int>>();
    private bool isCacheBuilt;

    // 지정한 StageType의 키 목록을 destination으로 복사한다.
    public bool TryCopyStageKeys(StageType stageType, List<int> destination)
    {
        if (destination == null)
            return false;

        destination.Clear();
        if (!TryGetStageKeys(stageType, out List<int> source))
            return false;

        destination.AddRange(source);
        return true;
    }

    // 지정한 StageType의 키 목록을 반환한다.
    public bool TryGetStageKeys(StageType stageType, out List<int> stageKeys)
    {
        stageKeys = null;

        if (!EnsureCacheBuilt())
            return false;

        if (!stageKeysByType.TryGetValue(stageType, out List<int> keys) || keys == null || keys.Count == 0)
            return false;

        stageKeys = keys;
        return true;
    }

    // 캐시를 무효화한다. 다음 조회 시 다시 빌드된다.
    public void Invalidate()
    {
        isCacheBuilt = false;
    }

    // 요청한 stageType/level을 유효 범위로 보정하고 실제 스테이지 데이터를 반환한다.
    public bool TryResolve(
        StageType stageType,
        int requestedLevel,
        out StageManageTable stageData,
        out int resolvedLevel,
        out int stageKey,
        out List<int> stageKeys)
    {
        stageData = null;
        resolvedLevel = 1;
        stageKey = 0;
        stageKeys = null;

        if (!TryGetStageKeys(stageType, out stageKeys))
            return false;

        resolvedLevel = Mathf.Clamp(requestedLevel, 1, stageKeys.Count);
        stageKey = stageKeys[resolvedLevel - 1];

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.StageManageDict == null)
            return false;

        return DataManager.Instance.StageManageDict.TryGetValue(stageKey, out stageData);
    }

    // StageManageDict를 읽어 타입별 키 캐시를 만든다.
    private bool EnsureCacheBuilt()
    {
        if (isCacheBuilt)
            return true;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.StageManageDict == null)
            return false;

        stageKeysByType.Clear();

        foreach (var pair in DataManager.Instance.StageManageDict)
        {
            StageType type = pair.Value.stageType;
            if (!stageKeysByType.TryGetValue(type, out List<int> keyList))
            {
                keyList = new List<int>();
                stageKeysByType[type] = keyList;
            }

            keyList.Add(pair.Key);
        }

        foreach (var keyList in stageKeysByType.Values)
            keyList.Sort();

        isCacheBuilt = true;
        return true;
    }
}
