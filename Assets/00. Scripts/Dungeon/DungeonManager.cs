using UnityEngine;

/// <summary>
/// 던전의 입장 정보와 현재 플레이 중인 던전의 상태를 관리하는 매니저
/// </summary>
public class DungeonManager : Singleton<DungeonManager>
{
    [Header("현재 입장한 던전 정보")]
    public int currentDungeonID = 0;

    /// <summary>
    /// 현재 입장한 던전의 테이블 데이터를 DataManager에서 즉시 가져옴
    /// </summary>
    public DungeonReqTable CurrentDungeonData
    {
        get
        {
            if (currentDungeonID != 0 && DataManager.Instance.DungeonReqDict.TryGetValue(currentDungeonID, out var data))
            {
                return data;
            }
            return null;
        }
    }

    /// <summary>
    /// 던전 씬에 들어왔을 때, 이 던전이 올바른 데이터인지 확인
    /// </summary>
    public bool IsValidDungeon()
    {
        return CurrentDungeonData != null;
    }

    // 던전 클리어 로직
}