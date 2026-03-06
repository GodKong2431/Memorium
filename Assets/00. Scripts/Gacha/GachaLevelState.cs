using System;
using UnityEngine;

/// <summary>
/// 가챠 유형별 레벨/단계 진행 상태.
/// 100회 뽑기=1레벨, 5레벨=1단계, Max Lv 110.
/// 단계별로 나올 수 있는 장비의 전투력 범위가 달라짐.
/// </summary>
[Serializable]
public class GachaLevelState
{
    public GachaType GachaType;
    public int Level = 1;
    /// <summary>현재 레벨에서 뽑은 횟수. 100 도달 시 레벨업.</summary>
    public int DrawCountInCurrentLevel;

    /// <summary>현재 단계 (1~21). 단계별 장비 전투력 범위 결정.</summary>
    public int Stage => GachaConfig.LevelToStage(Level);
    //public int Stage;

    /// <summary>다음 레벨까지 남은 뽑기 횟수</summary>
    public int DrawsUntilNextLevel => Mathf.Max(0, GachaConfig.DrawsPerLevel - DrawCountInCurrentLevel);
    //public int DrawsUntilNextLevel;

    /// <summary>Max 레벨(110) 도달 여부. Max 이상 뽑기 횟수는 카운트 안 됨.</summary>
    public bool IsMaxLevel => Level >= GachaConfig.MaxLevel;

    /// <summary>뽑기 횟수 누적. 100회마다 레벨업.</summary>
    public void AddDraws(int count)
    {
        if (IsMaxLevel) return;

        DrawCountInCurrentLevel += count;
        while (DrawCountInCurrentLevel >= GachaConfig.DrawsPerLevel && Level < GachaConfig.MaxLevel)
        {
            DrawCountInCurrentLevel -= GachaConfig.DrawsPerLevel;
            Level++;
        }
        if (Level >= GachaConfig.MaxLevel)
            DrawCountInCurrentLevel = 0;
    }

    public GachaLevelState(GachaType type, int level, int drawCountInCurrentLevel)
    {
        GachaType = type;
        Level = level;
        DrawCountInCurrentLevel= drawCountInCurrentLevel;
    }
}
