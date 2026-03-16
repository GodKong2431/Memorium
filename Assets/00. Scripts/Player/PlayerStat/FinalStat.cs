using UnityEngine;

[System.Serializable]
public class FinalStat
{
    public float finalStat;

    public StatType playerStatType;
    
    public FinalStat(StatType statType)
    {
        playerStatType = statType;
    }

    public float FinalStatCalculate()
    {
        // 베이스 스탯
        float baseStatValue = CharacterStatManager.Instance.BaseStat.baseStatValues.TryGetValue(playerStatType, out var baseValue) ? baseValue : 0f;

        // 업그레이드 스탯
        var upgradeStat = CharacterStatManager.Instance.Upgrades.TryGetValue(playerStatType, out var upgrade) ? upgrade : null;
        float upgradeStatValue = upgradeStat?.Stat ?? 0f;

        // 레벨업 스탯
        float levelBonus = CharacterStatManager.Instance.LevelBonus.BonusValues.TryGetValue(playerStatType, out var levelBonusValue) ? levelBonusValue : 0f;

        // 특성 스탯
        var traitStat = CharacterStatManager.Instance.Traits.TryGetValue(playerStatType, out var trait) ? trait : null;
        float traitValue = traitStat?.CurrentStat ?? 0f;

        // 장비 스탯
        float equipStat = CharacterStatManager.Instance.PlayerSlot.GetStat(playerStatType);
        
        float abilityStoneStat = AbilityStoneManager.Instance.LoadStone ? AbilityStoneManager.Instance.GetStat(playerStatType) : 0f; 
        
        float ablityStoneBonusStat = AbilityStoneManager.Instance.LoadStone ? AbilityStoneManager.Instance.GetBonusStat(playerStatType) : 0f;
        
        float bingoSynergyStat = BingoBoard.Instance.LoadBingo ? BingoBoard.Instance.GetSynergyStat(playerStatType) : 0f;
        
        float passiveStat = InventoryManager.Instance.DataLoad ? InventoryManager.Instance.GetModule<PassiveSkillModule>().GetPassiveStat(playerStatType) : 0f;
        
                
        finalStat = (baseStatValue + upgradeStatValue + levelBonus + traitValue + equipStat + abilityStoneStat + passiveStat) * (1 + ablityStoneBonusStat + bingoSynergyStat);
        
        if (CharacterStatManager.Instance.isBerserker)
        {
            float value = BerserkerModeController.Instance._berserkModeSo.BserserkMultStatSo.TryGetValue(playerStatType, out var v) ? v : 1f;
            finalStat = finalStat * value;
        }
        
        return finalStat;
    }

}
