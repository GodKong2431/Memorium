using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static PlayerStatView;

[System.Serializable]
public class SavePlayerData
{
    //현재 플레이어의 레벨, 골드 강화, 특성을 저장하는 cs
    //플레이어 레벨
    public int playerLevel;
    
    //골드 업그레이드
    public List<int> playerStatType_GoldUpgrades;
    public List<int> statUpgradeCount_GoldUpgrades;
    public List<BigDouble> statUpgradeCost_GoldUpgrades;

    //특성 업그레이드
    public List<int> playerStatType_Traits;
    public List<int> currentLevel_Traits;

    //플레이어 골드 업그레이드와 특성 업그레이드를 저장하는 SO
    private CharacterStatSO characterStatSO;
    private Dictionary<int, StatUpgrade> upgrades=new Dictionary<int, StatUpgrade>();
    private Dictionary<int, PlayerTrait> traits =new Dictionary<int, PlayerTrait>();

    //특성은 CurrentLevel만

    public SavePlayerData() { }

    public void InitPlayerData(CharacterStatSO so)
    {
        characterStatSO = so;
        if (playerLevel <= 0)
        {
            Debug.Log($"[SavePlaterData] {playerLevel}이므로 값 초기화");
            //골드 업그레이드 초기화
            playerStatType_GoldUpgrades = new List<int>();
            statUpgradeCount_GoldUpgrades = new List<int>();
            statUpgradeCost_GoldUpgrades = new List<BigDouble>();

            foreach (var statUpgrade in characterStatSO.Upgrades)
            {
                playerStatType_GoldUpgrades.Add((int)statUpgrade.Key);
                statUpgradeCount_GoldUpgrades.Add(statUpgrade.Value.UpgradeCount);
                statUpgradeCost_GoldUpgrades.Add(statUpgrade.Value.CurrentCost);
                upgrades[(int)statUpgrade.Key] = statUpgrade.Value;
            }

            //특성 업그레이드 초기화
            playerStatType_Traits = new List<int>();
            currentLevel_Traits = new List<int>();
            foreach (var traitUpgrade in characterStatSO.Traits)
            {
                playerStatType_Traits.Add((int)traitUpgrade.Key);
                currentLevel_Traits.Add(traitUpgrade.Value.CurrentLevel);
                traits[(int)traitUpgrade.Key] = traitUpgrade.Value;
            }
        }
        //나중에 스탯 종류 갯수가 증가한다면? <- 해당 코드 메서드로 분리할 예정
        //타입 갯수 혹은 종류 비교 후 없으면 new로 만들던가 하자
        else
        {
            Debug.Log($"[SavePlaterData] {playerLevel}이므로 값 불러오기");
            for (int i = 0; i < playerStatType_GoldUpgrades.Count; i++)
            {
                characterStatSO.Upgrades[(StatType)playerStatType_GoldUpgrades[i]].LoadUpgrade(statUpgradeCount_GoldUpgrades[i], statUpgradeCost_GoldUpgrades[i]);
                upgrades[playerStatType_GoldUpgrades[i]] = characterStatSO.Upgrades[(StatType)playerStatType_GoldUpgrades[i]];
            }

            for (int i = 0; i < playerStatType_Traits.Count; i++)
            {
                characterStatSO.Traits[(StatType)playerStatType_Traits[i]].LoadTrait(currentLevel_Traits[i]);
                traits[playerStatType_Traits[i]] = characterStatSO.Traits[(StatType)playerStatType_Traits[i]];
            }
        }
    }

    public int GetLevel()
    {
        if (playerLevel <= 0)
        {
            playerLevel = 1;
            return 1;
        }
        else
            return playerLevel;
    }

    public void SaveLevel()
    {
        Debug.Log($"[SavePlayerData] 레벨 저장 : {CharacterStatManager.Instance.LevelBonus.CurrentLevel}");
        playerLevel = CharacterStatManager.Instance.LevelBonus.CurrentLevel;
        Debug.Log($"[SavePlayerData] 레벨 저장 성공 : {playerLevel}");
    }

    public void Save()
    {
        for (int i = 0; i < playerStatType_GoldUpgrades.Count; i++)
        {
            statUpgradeCount_GoldUpgrades[i] = upgrades[playerStatType_GoldUpgrades[i]].UpgradeCount;
            statUpgradeCost_GoldUpgrades[i] = upgrades[playerStatType_GoldUpgrades[i]].CurrentCost;
        }

        for (int i = 0; i < playerStatType_Traits.Count; i++)
        {
            currentLevel_Traits[i] = traits[playerStatType_Traits[i]].CurrentLevel;
        }
    }
}
