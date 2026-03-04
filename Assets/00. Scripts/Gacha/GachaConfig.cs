using System;
using UnityEngine;

/// <summary>
/// 가챠 시스템 정적 설정값.
/// DB 생기면 연동 예정.
/// </summary>
public static class GachaConfig
{
    /// <summary>뽑기 레벨 증가에 필요한 횟수</summary>
    public const int DrawsPerLevel = 100;
    
    /// <summary>단계별 레벨 증가에 필요한 횟수</summary>
    public const int LevelsPerStage = 5;

    public const int MaxLevel = 110;
    public const int TotalStages = 21; // Lv 1~5: 1단계 ... Lv 101~110: 21단계

    /// <summary>오프셋 (0~400). 오프셋이 낮을 수록 등장 확률이 높음</summary>
    public static readonly int[] Offsets = { 0, 100, 200, 300, 400 };

    /// <summary>각 오프셋 등장 확률 (낮은 전투력일수록 높음). 합계 100%</summary>
    public static readonly float[] OffsetProbabilities = { 0.60f, 0.25f, 0.10f, 0.04f, 0.01f };

    /// <summary>1회 뽑기당 뽑기권 수. N회 뽑기 = N장. 뽑기권 구매 시 1장 단위.</summary>
    public const int TicketCostPerDraw = 1;
    /// <summary>1회 뽑기당 크리스탈 수. N회 뽑기 = N*10개. 뽑기권 1장 구매 = 10개.</summary>
    public const int CrystalCostPerDraw = 10;

    /// <summary>단계별 기준 전투력 (최소값). 1단계=100, 21단계=2100.</summary>
    public static int GetBaseCombatPower(int stage) => Mathf.Clamp(stage, 1, TotalStages) * 100;

    /// <summary>레벨 → 단계 매핑. Lv 1~5 → 1, Lv 6~10 → 2, ... Lv 101~110 → 21.</summary>
    public static int LevelToStage(int level)
    {
        if (level <= 0) return 1;
        return Mathf.Min((level - 1) / LevelsPerStage + 1, TotalStages);
    }

    /// <summary>전투력 → equipmentTier (1~25). combatPower 100→1, 200→2, ... 2500→25.</summary>
    public static int CombatPowerToTier(int combatPower)
    {
        int tier = 1 + (combatPower - 100) / 100;
        return Mathf.Clamp(tier, 1, 25);
    }
}
