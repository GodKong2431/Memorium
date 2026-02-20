using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterStatManager : Singleton<CharacterStatManager>
{
    // 키들
    [SerializeField] private int characterBaseKey;
    [SerializeField] private int characterTableKey;
    [SerializeField] private int level;

    [SerializeField] TestSavePlayerEquipmentData testSaveData;
    [SerializeField] EquipmentHandler equipmentHandler;

    [SerializeField] private TraitManager traitManager;

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

    [SerializeField] private float finalHP;
    [SerializeField] private float finalHPRegen;
    [SerializeField] private float finalMP;
    [SerializeField] private float finalMPRegen;
    [SerializeField] private float finalATK;
    [SerializeField] private float finalATKSpeed;
    [SerializeField] private float finalPhysDEF;
    [SerializeField] private float finalMagicDEF;
    [SerializeField] private float finalCritChance;
    [SerializeField] private float finalCritMult;
    [SerializeField] private float finalMoveSpeed;
    [SerializeField] private float finalCoolDownReduce;
    [SerializeField] private float finalGoldGain;
    [SerializeField] private float finalExpGain;
    [SerializeField] private float finalBossDamage;
    [SerializeField] private float finalNormalDamage;
    [SerializeField] private float finalDamageMult;

    IEnumerator Start()
    {
        yield return new WaitUntil(()=>DataManager.Instance!=null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        LoadTable();
        testSaveData = JSONService.Load<TestSavePlayerEquipmentData>();
        testSaveData.InitPlayerEquipmentData();
        //불러온 데이터 플레이어 장착 및 데이터 세팅
        equipmentHandler.SetMyEquipOnStart(testSaveData.weaponId, testSaveData.helmetId, testSaveData.gloveId, testSaveData.armorId, testSaveData.bootsId, testSaveData.unlockEquipmentDict);
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

    public float FinalHP { get { return finalHP; } }
    public float FinalHPRegen { get { return finalHPRegen; } }
    public float FinalMP { get { return finalMP; } }
    public float FinalMPRegen {  get { return finalMPRegen; } }
    public float FinalATK {get { return finalATK; } }
    public float FinalATKSpeed {  get { return finalATKSpeed; } }
    public float FinalPhysDEF {  get { return finalPhysDEF; } }
    public float FinalMagicDEF {  get { return finalMagicDEF; } }
    public float FinalCritChance {  get { return finalCritChance; } }
    public float FinalCritMult {  get { return finalCritMult; } }
    public float FinalMoveSpeed {  get { return finalMoveSpeed; } }
    public float FinalCoolDownReduce {  get { return finalCoolDownReduce; } }
    public float FinalGoldGain {  get { return finalGoldGain; } }
    public float FinalExpGain {  get { return finalExpGain; } }
    public float FinalBossDamage {  get { return finalBossDamage; } }
    public float FinalNormalDamage {  get { return finalNormalDamage; } }
    public float FinalDamageMult {  get { return finalDamageMult; } }

    public event Action<PlayerStatType, float> StatUpdate;

    public event Action<int, int, int> TraitUpdate;

    private void OnEnable()
    {
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

        attackTrait.UpgradeTrait -= FinalStat;
        mpTrait.UpgradeTrait -= FinalStat;
        hpTrait.UpgradeTrait -= FinalStat;
        attackSpeedTrait.UpgradeTrait -= FinalStat;
        critTrait.UpgradeTrait -= FinalStat;
        critMultTrait.UpgradeTrait -= FinalStat;
        bossDamageTrait.UpgradeTrait -= FinalStat;
        coolDownTrait.UpgradeTrait -= FinalStat;
        damageMultTrait.UpgradeTrait -= FinalStat;
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

        EventSet();

        FinalStat();
    }

    public void FinalStat()
    {
        finalHP = FinalStatAdd(PlayerStatType.HP,baseStat.HP, hpStatUpgrade.Stat, levelBonus.BonusHP, hpTrait.CurrentLevel, hpTrait.StatUP);
        finalHPRegen = FinalStatAdd(PlayerStatType.HP_REGEN,baseStat.HpRegeneration, hpRegenStatUpgrade.Stat, levelBonus.BonusHPRegen, 0, 0);
        finalMP = FinalStatAdd(PlayerStatType.MP,baseStat.Mana, mpStatUpgrade.Stat, levelBonus.BonusMP, mpTrait.CurrentLevel, mpTrait.StatUP);
        finalMPRegen = FinalStatAdd(PlayerStatType.MP_REGEN, baseStat.ManaRegeneration, mpRegenStatUpgrade.Stat, levelBonus.BonusMPRegen, 0, 0);
        finalATK = FinalStatAdd(PlayerStatType.ATK, baseStat.Attack, attackStatUpgrade.Stat, levelBonus.BonusAttack, attackTrait.CurrentLevel, attackTrait.StatUP);
        finalATKSpeed = FinalStatAdd(PlayerStatType.ATK_SPEED, baseStat.AttackSpeed, 0, 0, attackSpeedTrait.CurrentLevel, attackSpeedTrait.StatUP);
        finalPhysDEF = FinalStatAdd(PlayerStatType.PHYS_DEF, baseStat.PhysicsResist, 0, 0, 0, 0);
        finalMagicDEF = FinalStatAdd(PlayerStatType.MAGIC_DEF, baseStat.MagicResist, 0, 0, 0, 0);
        finalCritChance = FinalStatAdd(PlayerStatType.CRIT_CHANCE, baseStat.CriticalChance, critStatUpgrade.Stat, 0, critTrait.CurrentLevel, critTrait.StatUP);
        finalCritMult = FinalStatAdd(PlayerStatType.CRIT_MULT, baseStat.CriticalMultiplier, critMultStatUpgrade.Stat, levelBonus.BonusCriticalDamage, critMultTrait.CurrentLevel, critMultTrait.StatUP);
        finalMoveSpeed = FinalStatAdd(PlayerStatType.MOVE_SPEED, baseStat.MoveSpeed, 0, 0, 0, 0);
        finalCoolDownReduce = FinalStatAdd(PlayerStatType.COOLDOWN_REDUCE, baseStat.CoolDown, 0, 0, coolDownTrait.CurrentLevel, coolDownTrait.StatUP);
        finalGoldGain = FinalStatAdd(PlayerStatType.GOLD_GAIN, baseStat.GoldGain, 0, 0, 0, 0);
        finalExpGain = FinalStatAdd(PlayerStatType.EXP_GAIN, baseStat.ExpGain, 0, 0, 0, 0);
        finalBossDamage = FinalStatAdd(PlayerStatType.BOSS_DMG, baseStat.BossDamage, bossDamageStatUpgrade.Stat, 0, bossDamageTrait.CurrentLevel, bossDamageTrait.StatUP);
        finalNormalDamage = FinalStatAdd(PlayerStatType.NORMAL_DMG, baseStat.NormalDamage, 0, 0, 0, 0);
        finalDamageMult = FinalStatAdd(PlayerStatType.DMG_MULT, baseStat.DamageMult, 0, 0, 0, 0);
    }

    private float FinalStatAdd(PlayerStatType playerStatType ,float baseStat, float upgradeStat, float levelHP, int traitLevel, float traitStat)
    {
        float equipStat = playerSlot.GetStat(playerStatType);
        float complate = baseStat + upgradeStat + levelHP + (traitLevel * traitStat) + equipStat;

        StatUpdate?.Invoke(playerStatType, complate);

        return complate;
    }

    public void Upgrade(StatUpgrade statUpgrade)
    {
        statUpgrade.Upgrade();
    }

    public void TraitUpgrade (PlayerTrait playerTrait)
    {
        playerTrait.Upgrade(ref traitManager.StatPoints);
        TraitUpdate?.Invoke(playerTrait.ID, playerTrait.CurrentLevel, playerTrait.MaxLevel);
    }


    public void EventSet()
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

        attackTrait.UpgradeTrait += FinalStat;
        mpTrait.UpgradeTrait += FinalStat;
        hpTrait.UpgradeTrait += FinalStat;
        attackSpeedTrait.UpgradeTrait += FinalStat;
        critTrait.UpgradeTrait += FinalStat;
        critMultTrait.UpgradeTrait += FinalStat;
        bossDamageTrait.UpgradeTrait += FinalStat;
        coolDownTrait.UpgradeTrait += FinalStat;
        damageMultTrait.UpgradeTrait += FinalStat;
    }
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        PlayerEquipment playerEquipment = equipmentHandler.playerEquipment;
        testSaveData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
        JSONService.Save(testSaveData);
    }
}
