using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterStatManager : MonoBehaviour
{
    // 키들
    [SerializeField] private int characterBaseKey;
    [SerializeField] private int characterTableKey;
    [SerializeField] private int level;

    [SerializeField] private CharacterBaseStat baseStat;

    [SerializeField] private StatUpgrade attackStatUpgrade;
    [SerializeField] private StatUpgrade mpStatUpgrade;
    [SerializeField] private StatUpgrade mpRegenStatUpgrade;
    [SerializeField] private StatUpgrade hpStatUpgrade;
    [SerializeField] private StatUpgrade hpRegenStatUpgrade;
    [SerializeField] private StatUpgrade critStatUpgrade;
    [SerializeField] private StatUpgrade critMultStatUpgrade;
    [SerializeField] private StatUpgrade bossDamageStatUpgrade;
    [SerializeField] private StatUpgrade traitStatUpgrade;

    [SerializeField] private PlayerLevel levelBonus;

    [SerializeField] private PlayerSlot playerSlot;

    [SerializeField] private PlayerTrait attackTrait;
    [SerializeField] private PlayerTrait mpTrait;
    [SerializeField] private PlayerTrait hpTrait;
    [SerializeField] private PlayerTrait attackSpeedTrait;
    [SerializeField] private PlayerTrait critTrait;
    [SerializeField] private PlayerTrait critMultTrait;
    [SerializeField] private PlayerTrait bossDamageTrait;
    [SerializeField] private PlayerTrait coolDownTrait;
    [SerializeField] private PlayerTrait damageMultTrait;

    public void LoadTable()
    {
        baseStat = new CharacterBaseStat(characterBaseKey);

        attackStatUpgrade = new StatUpgrade(1010001);
        mpStatUpgrade = new StatUpgrade(1010002);
        mpRegenStatUpgrade = new StatUpgrade(1010003);
        hpStatUpgrade = new StatUpgrade(1010004);
        hpRegenStatUpgrade = new StatUpgrade(1010005);
        critStatUpgrade = new StatUpgrade(1010006);
        critMultStatUpgrade = new StatUpgrade(1010007);
        bossDamageStatUpgrade = new StatUpgrade(1010008);
        traitStatUpgrade = new StatUpgrade(1010009);

        levelBonus = new PlayerLevel(level);

        playerSlot = new PlayerSlot(characterTableKey);

        attackTrait = new PlayerTrait(1040001);
        mpTrait = new PlayerTrait(1040011);
        hpTrait = new PlayerTrait(1040012);
        attackSpeedTrait = new PlayerTrait(1040013);
        critTrait = new PlayerTrait(1040021);
        critMultTrait = new PlayerTrait(1040032);
        bossDamageTrait = new PlayerTrait(1040033); 
        coolDownTrait = new PlayerTrait(1040034);
        damageMultTrait = new PlayerTrait(1040041);
    }


    private void FailLoadTable(TableBase table)
    {
        Debug.Log($"[CharacterStatManager] [{table.GetType().ToString()}] 테이블 불러오기 실패");
    }
}
