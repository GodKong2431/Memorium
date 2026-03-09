using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pixie 가챠 뽑기 로직.
/// FairyInfoTable에서 랜덤 1마리 Pixie ID 반환. (등급별 가중치 확장 가능)
/// </summary>
public static class PixieGachaLogic
{
    /// <summary>1회 뽑기: Pixie 1마리 ID 반환. 0이면 테이블 없음/비어있음.</summary>
    public static int DrawPixie()
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad)
            return 0;

        var dict = DataManager.Instance.FairyInfoDict;
        if (dict == null || dict.Count == 0)
            return 0;

        var ids = new List<int>(dict.Keys);
        return ids[Random.Range(0, ids.Count)];
    }
}
