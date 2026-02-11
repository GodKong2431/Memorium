using UnityEngine;

/// <summary>
/// 적 처치 시 골드·경험치·아이템을 각각 수식에 따라 계산.
/// 수식이 바뀌면 이 클래스만 수정하면 됨.
/// 
/// 현재는 데이터테이블을 연동하지 않았기에 수치는 대부분 임의로 구현되어 있음.
/// </summary>
public static class EnemyRewardCalculator
{
    /// <summary>
    /// 골드 수식: 기본 골드 × (스테이지당 골드 증가율)^(현재 스테이지 레벨 - 1), 소수점 반올림
    /// </summary>
    public static int CalculateGold(EnemyRewardData data, int stageLevel = 1)
    {
        if (data == null) return 0;
        float gold = data.goldBase * Mathf.Pow(data.goldStageGrowthRate, stageLevel - 1);
        return Mathf.Max(0, Mathf.RoundToInt(gold));
    }

    /// <summary>
    /// 경험치 수식: 몬스터 베이스 경험치 × (증가상수)^(현재 레벨 - 1), 소수점 반올림
    /// </summary>
    public static int CalculateExp(EnemyRewardData data, int level = 1)
    {
        if (data == null) return 0;
        float exp = data.expBase * Mathf.Pow(data.expGrowthRate, level - 1);
        return Mathf.Max(0, Mathf.RoundToInt(exp));
    }

    /// <summary>
    /// 드랍 테이블 기반 아이템 드랍 (확률 롤, 개수는 min~max 랜덤)
    /// </summary>
    public static void RollDrops(EnemyRewardData data, System.Collections.Generic.List<DropResult> results)
    {
        results?.Clear();
        if (data == null || data.dropTable == null) return;

        foreach (var entry in data.dropTable)
        {
            if (string.IsNullOrEmpty(entry.itemId)) continue;
            if (Random.value > entry.dropChance) continue;

            int count = Mathf.Clamp(Random.Range(entry.minCount, entry.maxCount + 1), entry.minCount, entry.maxCount);
            if (count > 0)
                results.Add(new DropResult { itemId = entry.itemId, count = count });
        }
    }

    public struct DropResult
    {
        public string itemId;
        public int count;
    }
}
