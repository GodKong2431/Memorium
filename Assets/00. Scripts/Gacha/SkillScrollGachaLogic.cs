using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스킬 주문서 가챠 뽑기 로직.
/// GachaSkillScrollTable의 뽑기 레벨별 가중치(1/2/3/4/5/10/20/30개 등장)로 개수를 정한 뒤,
/// 해당 개수만큼 스킬 주문서 아이템 ID를 랜덤 반환.
/// </summary>
public static class SkillScrollGachaLogic
{
    private static readonly int[] CountOptions = { 1, 2, 3, 4, 5, 10, 20, 30 };

    /// <summary>1회 뽑기: 레벨에 맞는 테이블 행에서 개수 가중치 롤 → 그 개수만큼 스킬 주문서 ID 리스트 반환.</summary>
    public static List<int> DrawSkillScrolls(int gachaLevel)
    {
        var result = new List<int>();
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad)
            return result;

        var table = DataManager.Instance.GachaSkillScrollDict;
        if (table == null || table.Count == 0)
            return result;

        GachaSkillScrollTable row = table.Values.FirstOrDefault(r => r.gachaLevel == gachaLevel);
        if (row == null)
            row = table.Values.OrderByDescending(r => r.gachaLevel).FirstOrDefault();
        if (row == null)
            return result;

        int count = RollCount(row);
        if (count <= 0)
            return result;

        var scrollIds = GetSkillScrollItemIds();
        if (scrollIds == null || scrollIds.Count == 0)
            return result;

        for (int i = 0; i < count; i++)
        {
            int id = scrollIds[Random.Range(0, scrollIds.Count)];
            result.Add(id);
        }

        return result;
    }

    /// <summary>테이블 행의 weight1~weight30으로 등장 개수(1/2/3/4/5/10/20/30) 롤.</summary>
    private static int RollCount(GachaSkillScrollTable row)
    {
        int w1 = row.weight1;
        int w2 = row.weight2;
        int w3 = row.weight3;
        int w4 = row.weight4;
        int w5 = row.weight5;
        int w10 = row.weight10;
        int w20 = row.weight20;
        int w30 = row.weight30;

        int total = w1 + w2 + w3 + w4 + w5 + w10 + w20 + w30;
        if (total <= 0) return 1;

        int r = Random.Range(0, total);
        if (r < w1) return 1;
        r -= w1;
        if (r < w2) return 2;
        r -= w2;
        if (r < w3) return 3;
        r -= w3;
        if (r < w4) return 4;
        r -= w4;
        if (r < w5) return 5;
        r -= w5;
        if (r < w10) return 10;
        r -= w10;
        if (r < w20) return 20;
        return 30;
    }

    private static List<int> GetSkillScrollItemIds()
    {
        if (DataManager.Instance?.ItemInfoDict == null)
            return null;

        var list = new List<int>();
        foreach (var kv in DataManager.Instance.ItemInfoDict)
        {
            if (kv.Value.itemType == ItemType.SkillScroll)
                list.Add(kv.Key);
        }
        return list;
    }
}
