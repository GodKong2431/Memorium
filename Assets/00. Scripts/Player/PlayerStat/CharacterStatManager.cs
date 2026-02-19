using System.Collections;
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

    IEnumerator Start()
    {
        yield return new WaitUntil(()=>DataManager.Instance!=null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        LoadTable();
    }
    public CharacterBaseStat BaseStat { get { return baseStat; } }

    public StatUpgrade AttackStatUpgrade { get { return attackStatUpgrade; } }
    public StatUpgrade MpStatUpgrade { get { return mpStatUpgrade; } }
    public StatUpgrade MpRegenStatUpgrade { get { return mpRegenStatUpgrade; } }
    public StatUpgrade HpStatUpgrade { get { return hpStatUpgrade; } }
    public StatUpgrade HpRegenStatUpgrade { get { return hpRegenStatUpgrade; } }
    public StatUpgrade CritStatUpgrade {  get { return critStatUpgrade; } }
    public StatUpgrade CritMultStatUpgrade { get { return critMultStatUpgrade; } }
    public StatUpgrade BossDamageStatUpgrade { get { return bossDamageStatUpgrade; } }
    public StatUpgrade TraitStatUpgrade {  get { return traitStatUpgrade; } }

    public PlayerLevel LevelBonus { get { return levelBonus; } }

    public PlayerSlot PlayerSlot { get { return playerSlot; } }

    public PlayerTrait AttackTrait { get { return attackTrait; } }
    public PlayerTrait MpTrait { get { return mpTrait; } }
    public PlayerTrait HpTrait { get { return hpTrait; } }
    public PlayerTrait AttackSpeedTrait { get { return attackSpeedTrait; } }
    public PlayerTrait CritTrait { get { return critTrait; } }
    public PlayerTrait CritMultTrait { get { return critMultTrait; } }
    public PlayerTrait BossDamageTrait { get { return bossDamageTrait; } }
    public PlayerTrait CoolDownTrait { get { return coolDownTrait; } }
    public PlayerTrait DamageMultTrait { get { return damageMultTrait; } }

    public float FinalHP { get; private set; }
    public float FinalHPRegen { get; private set; }
    public float FinalMP { get; private set; }
    public float FinalMPRegen { get; private set; }
    public float FinalATK { get; private set; }
    public float FinalATKSpeed { get; private set; }
    public float FinalPhysDEF { get; private set; }
    public float FinalMagicDEF { get; private set; }
    public float FinalCritChance { get; private set; }
    public float FinalCritMult { get; private set; }
    public float FinalMoveSpeed { get; private set; }
    public float FinalCoolDownReduce { get; private set; }
    public float FinalGoldGain { get; private set; }
    public float FinalExpGain { get; private set; }
    public float FinalBossDamage { get; private set; }
    public float FinalNormalDamage { get; private set; }
    public float FinalDamageMult { get; private set; }

    private void OnEnable()
    {
        attackStatUpgrade.UpgradeStat += FinalStat;
        mpStatUpgrade.UpgradeStat += FinalStat;
        mpRegenStatUpgrade.UpgradeStat += FinalStat;
        hpStatUpgrade.UpgradeStat += FinalStat;
        hpRegenStatUpgrade.UpgradeStat += FinalStat;
        critStatUpgrade.UpgradeStat += FinalStat;
        critMultStatUpgrade.UpgradeStat += FinalStat;
        bossDamageStatUpgrade.UpgradeStat += FinalStat;
        traitStatUpgrade.UpgradeStat += FinalStat;
    }

    private void OnDisable()
    {
        attackStatUpgrade.UpgradeStat -= FinalStat;
        mpStatUpgrade.UpgradeStat -= FinalStat;
        mpRegenStatUpgrade.UpgradeStat -= FinalStat;
        hpStatUpgrade.UpgradeStat -= FinalStat;
        hpRegenStatUpgrade.UpgradeStat -= FinalStat;
        critStatUpgrade.UpgradeStat -= FinalStat;
        critMultStatUpgrade.UpgradeStat -= FinalStat;
        bossDamageStatUpgrade.UpgradeStat -= FinalStat;
        traitStatUpgrade.UpgradeStat -= FinalStat;
    }


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

        FinalStat();
    }

    public void FinalStat()
    {
        FinalHP = FinalStatAdd(baseStat.HP, hpStatUpgrade.Stat, levelBonus.BonusHP, hpTrait.CurrentLevel, hpTrait.StatUP);
        FinalHPRegen = FinalStatAdd(baseStat.HpRegeneration, hpRegenStatUpgrade.Stat, levelBonus.BonusHPRegen, 0, 0);
        FinalMP = FinalStatAdd(baseStat.Mana, mpStatUpgrade.Stat, levelBonus.BonusMP, mpTrait.CurrentLevel, mpTrait.StatUP);
        FinalMPRegen = FinalStatAdd(baseStat.ManaRegeneration, mpRegenStatUpgrade.Stat, levelBonus.BonusMPRegen, 0, 0);
        FinalATK = FinalStatAdd(baseStat.Attack, attackStatUpgrade.Stat, levelBonus.BonusAttack, attackTrait.CurrentLevel, attackTrait.StatUP);
        FinalATKSpeed = FinalStatAdd(baseStat.AttackSpeed, 0, 0, attackSpeedTrait.CurrentLevel, attackSpeedTrait.StatUP);
        FinalPhysDEF = FinalStatAdd(baseStat.PhysicsResist, 0, 0, 0, 0);
        FinalMagicDEF = FinalStatAdd(baseStat.MagicResist, 0, 0, 0, 0);
        FinalCritChance = FinalStatAdd(baseStat.CriticalChance, critStatUpgrade.Stat, 0, critTrait.CurrentLevel, critTrait.StatUP);
        FinalCritMult = FinalStatAdd(baseStat.CriticalMultiplier, critMultStatUpgrade.Stat, levelBonus.BonusCriticalDamage, critMultTrait.CurrentLevel, critMultTrait.StatUP);
        FinalMoveSpeed = FinalStatAdd(baseStat.MoveSpeed, 0, 0, 0, 0);
        FinalCoolDownReduce = FinalStatAdd(baseStat.CoolDown, 0, 0, coolDownTrait.CurrentLevel, coolDownTrait.StatUP);
        FinalGoldGain = FinalStatAdd(baseStat.GoldGain, 0, 0, 0, 0);
        FinalExpGain = FinalStatAdd(baseStat.ExpGain, 0, 0, 0, 0);
        FinalBossDamage = FinalStatAdd(baseStat.BossDamage, 0, 0, 0, 0);
        FinalNormalDamage = FinalStatAdd(baseStat.NormalDamage, 0, 0, 0, 0);
        FinalDamageMult = FinalStatAdd(baseStat.DamageMult, 0, 0, 0, 0);
    }

    private float FinalStatAdd(float baseStat, float upgradeStat, float levelHP, int traitLevel, float traitStat)
    {
        return baseStat + upgradeStat + levelHP + (traitLevel * traitStat);
    }

    public void Upgrade(ref StatUpgrade statUpgrade)
    {

    }

    private void FailLoadTable(TableBase table)
    {
        Debug.Log($"[CharacterStatManager] [{table.GetType().ToString()}] 테이블 불러오기 실패");
    }
}
