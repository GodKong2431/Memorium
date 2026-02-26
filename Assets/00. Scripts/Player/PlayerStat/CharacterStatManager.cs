using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterStatManager : Singleton<CharacterStatManager>
{
    // 키들
    [Tooltip("1000001 ~ 1000003")]
    [Range(1000001, 1000003)]
    [SerializeField] private int characterBaseKey;
    [Tooltip("1030001 ~ 1030003")]
    [Range(1030001, 1030003)]
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
    //[SerializeField] private StatUpgrade traitStatUpgrade;

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
    [SerializeField] private float finalAttribute;

    [SerializeField] private float normalBasicDamage;
    [SerializeField] private float bossBasicDamage;


    [SerializeField] public bool TableLoad = false;

    [SerializeField] private float normalPower;

    [SerializeField] private float expectedCrit;

    IEnumerator Start()
    {
        yield return new WaitUntil(()=>DataManager.Instance!=null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        LoadTable();
        testSaveData = JSONService.Load<TestSavePlayerEquipmentData>();
        testSaveData.InitPlayerEquipmentData();
        //불러온 데이터 플레이어 장착 및 데이터 세팅
        if (equipmentHandler != null)
        {
            equipmentHandler.SetMyEquipOnStart(testSaveData.weaponId, testSaveData.helmetId, testSaveData.gloveId, testSaveData.armorId, testSaveData.bootsId, testSaveData.unlockEquipmentDict);
        }

        //테스트용 골드
        CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, 1000);

        TableLoad = true;
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
    //public StatUpgrade TraitStatUpgrade {  get { return traitStatUpgrade; } }

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

    public float FinalHP => GetFinalStat(PlayerStatType.HP);
    public float FinalHPRegen => GetFinalStat(PlayerStatType.HP_REGEN);
    public float FinalMP => GetFinalStat(PlayerStatType.MP);
    public float FinalMPRegen => GetFinalStat(PlayerStatType.MP_REGEN);
    public float FinalATK => GetFinalStat(PlayerStatType.ATK);
    public float FinalATKSpeed => GetFinalStat(PlayerStatType.ATK_SPEED);
    public float FinalPhysDEF => GetFinalStat(PlayerStatType.PHYS_DEF);
    public float FinalMagicDEF => GetFinalStat(PlayerStatType.MAGIC_DEF);
    public float FinalCritChance => GetFinalStat(PlayerStatType.CRIT_CHANCE);
    public float FinalCritMult => GetFinalStat(PlayerStatType.CRIT_MULT);
    public float FinalMoveSpeed => GetFinalStat(PlayerStatType.MOVE_SPEED);
    public float FinalCoolDownReduce => GetFinalStat(PlayerStatType.COOLDOWN_REDUCE);
    public float FinalGoldGain => GetFinalStat(PlayerStatType.GOLD_GAIN);
    public float FinalExpGain => GetFinalStat(PlayerStatType.EXP_GAIN);
    public float FinalBossDamage => GetFinalStat(PlayerStatType.BOSS_DMG);
    public float FinalNormalDamage => GetFinalStat(PlayerStatType.NORMAL_DMG);
    public float FinalDamageMult => GetFinalStat(PlayerStatType.DMG_MULT);
    public float FinalAttribute => GetFinalStat(PlayerStatType.Attribute);

    /// <summary>버서커 모드 포함한 전투력. normalPower 기반에 버서커 배율 적용.</summary>
    public float NormalPower => GetNormalPowerWithBerserker();

    public event Action StatUpdate;

    public event Action<int, int, int> TraitUpdate;

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
        //traitStatUpgrade.UpgradeStat += FinalStat;

        attackTrait.UpgradeTrait += FinalStat;
        mpTrait.UpgradeTrait += FinalStat;
        hpTrait.UpgradeTrait += FinalStat;
        attackSpeedTrait.UpgradeTrait += FinalStat;
        critTrait.UpgradeTrait += FinalStat;
        critMultTrait.UpgradeTrait += FinalStat;
        bossDamageTrait.UpgradeTrait += FinalStat;
        coolDownTrait.UpgradeTrait += FinalStat;
        damageMultTrait.UpgradeTrait += FinalStat;

        GameEventManager.OnCurrencyChanged += levelBonus.ExpCheck;

        levelBonus.OnLevelUp += AllUpdate;

        playerSlot.OnSlotUpdate += AllUpdate;

        BerserkerModeController.OnBerserkerModeStarted += OnBerserkerModeChanged;
        BerserkerModeController.OnBerserkerModeEnded += OnBerserkerModeChanged;
    }

    private void OnBerserkerModeChanged()
    {
        StatUpdate?.Invoke();
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
        //traitStatUpgrade.UpgradeStat -= FinalStat;

        attackTrait.UpgradeTrait -= FinalStat;
        mpTrait.UpgradeTrait -= FinalStat;
        hpTrait.UpgradeTrait -= FinalStat;
        attackSpeedTrait.UpgradeTrait -= FinalStat;
        critTrait.UpgradeTrait -= FinalStat;
        critMultTrait.UpgradeTrait -= FinalStat;
        bossDamageTrait.UpgradeTrait -= FinalStat;
        coolDownTrait.UpgradeTrait -= FinalStat;
        damageMultTrait.UpgradeTrait -= FinalStat;

        GameEventManager.OnCurrencyChanged -= levelBonus.ExpCheck;

        levelBonus.OnLevelUp -= AllUpdate;

        playerSlot.OnSlotUpdate -= AllUpdate;

        BerserkerModeController.OnBerserkerModeStarted -= OnBerserkerModeChanged;
        BerserkerModeController.OnBerserkerModeEnded -= OnBerserkerModeChanged;
    }

    public void LoadTable()
    {
        baseStat = new CharacterBaseStat(characterBaseKey);

        attackStatUpgrade = new StatUpgrade(1010001,PlayerStatType.ATK);
        mpStatUpgrade = new StatUpgrade(1010002, PlayerStatType.MP);
        mpRegenStatUpgrade = new StatUpgrade(1010003, PlayerStatType.MP_REGEN);
        hpStatUpgrade = new StatUpgrade(1010004, PlayerStatType.HP);
        hpRegenStatUpgrade = new StatUpgrade(1010005, PlayerStatType.HP_REGEN);
        critStatUpgrade = new StatUpgrade(1010006, PlayerStatType.CRIT_CHANCE);
        critMultStatUpgrade = new StatUpgrade(1010007, PlayerStatType.CRIT_MULT);
        bossDamageStatUpgrade = new StatUpgrade(1010008, PlayerStatType.BOSS_DMG);
        //traitStatUpgrade = new StatUpgrade(1010009, PlayerStatType.TRIAT);

        levelBonus = new PlayerLevel(level);

        playerSlot = new PlayerSlot(characterTableKey);

        attackTrait = new PlayerTrait(1040001, PlayerStatType.ATK);
        hpTrait = new PlayerTrait(1040011,PlayerStatType.HP);
        mpTrait = new PlayerTrait(1040012, PlayerStatType.MP);
        attackSpeedTrait = new PlayerTrait(1040013, PlayerStatType.ATK_SPEED);
        critTrait = new PlayerTrait(1040021, PlayerStatType.CRIT_CHANCE);
        critMultTrait = new PlayerTrait(1040032, PlayerStatType.CRIT_MULT);
        bossDamageTrait = new PlayerTrait(1040033, PlayerStatType.BOSS_DMG); 
        coolDownTrait = new PlayerTrait(1040034, PlayerStatType.COOLDOWN_REDUCE);
        damageMultTrait = new PlayerTrait(1040041, PlayerStatType.DMG_MULT);

        EventSet();

        foreach (PlayerStatType playerStat in Enum.GetValues(typeof(PlayerStatType)))
        {
            FinalStat(playerStat);
        }
    }

    public void FinalStat(PlayerStatType playerStatType)
    {
        switch (playerStatType)
        {
            case PlayerStatType.HP:
                finalHP = FinalStatAdd(PlayerStatType.HP, baseStat.HP, hpStatUpgrade.Stat, levelBonus.BonusHP, hpTrait.CurrentStat);
                break;
            case PlayerStatType.MP:
                finalMP = FinalStatAdd(PlayerStatType.MP,baseStat.Mana, mpStatUpgrade.Stat, levelBonus.BonusMP, mpTrait.CurrentStat);
                break;
            case PlayerStatType.HP_REGEN:
                finalHPRegen = FinalStatAdd(PlayerStatType.HP_REGEN, baseStat.HpRegeneration, hpRegenStatUpgrade.Stat, levelBonus.BonusHPRegen, 0);
                break;
            case PlayerStatType.MP_REGEN:
                finalMPRegen = FinalStatAdd(PlayerStatType.MP_REGEN, baseStat.ManaRegeneration, mpRegenStatUpgrade.Stat, levelBonus.BonusMPRegen, 0);
                break;
            case PlayerStatType.ATK:
                finalATK = FinalStatAdd(PlayerStatType.ATK, baseStat.Attack, attackStatUpgrade.Stat, levelBonus.BonusAttack, attackTrait.CurrentStat);
                break;
            case PlayerStatType.ATK_SPEED:
                finalATKSpeed = Math.Min(FinalStatAdd(PlayerStatType.ATK_SPEED, baseStat.AttackSpeed, 0, 0, attackSpeedTrait.CurrentStat), 3);
                break;
            case PlayerStatType.PHYS_DEF:
                finalPhysDEF = FinalStatAdd(PlayerStatType.PHYS_DEF, baseStat.PhysicsResist, 0, 0, 0);
                break;
            case PlayerStatType.MAGIC_DEF:
                finalMagicDEF = FinalStatAdd(PlayerStatType.MAGIC_DEF, baseStat.MagicResist, 0, 0, 0);
                break;
            case PlayerStatType.CRIT_CHANCE:
                finalCritChance = FinalStatAdd(PlayerStatType.CRIT_CHANCE, baseStat.CriticalChance, critStatUpgrade.Stat, 0, critTrait.CurrentStat);
                break;
            case PlayerStatType.CRIT_MULT:
                finalCritMult = FinalStatAdd(PlayerStatType.CRIT_MULT, baseStat.CriticalMultiplier, critMultStatUpgrade.Stat, levelBonus.BonusCriticalDamage, critMultTrait.CurrentStat);
                break;
            case PlayerStatType.MOVE_SPEED:
                finalMoveSpeed = FinalStatAdd(PlayerStatType.MOVE_SPEED, baseStat.MoveSpeed, 0, 0, 0);
                break;
            case PlayerStatType.COOLDOWN_REDUCE:
                finalCoolDownReduce = FinalStatAdd(PlayerStatType.COOLDOWN_REDUCE, baseStat.CoolDown, 0, 0, coolDownTrait.CurrentStat);
                break;
            case PlayerStatType.GOLD_GAIN:
                finalGoldGain = FinalStatAdd(PlayerStatType.GOLD_GAIN, baseStat.GoldGain, 0, 0, 0);
                break;
            case PlayerStatType.EXP_GAIN:
                finalExpGain = FinalStatAdd(PlayerStatType.EXP_GAIN, baseStat.ExpGain, 0, 0, 0);
                break;
            case PlayerStatType.BOSS_DMG:
                finalBossDamage = FinalStatAdd(PlayerStatType.BOSS_DMG, baseStat.BossDamage, bossDamageStatUpgrade.Stat, 0, bossDamageTrait.CurrentStat);
                break;
            case PlayerStatType.NORMAL_DMG:
                finalNormalDamage = FinalStatAdd(PlayerStatType.NORMAL_DMG, baseStat.NormalDamage, 0, 0, 0);
                break;
            case PlayerStatType.DMG_MULT:
                finalDamageMult = FinalStatAdd(PlayerStatType.DMG_MULT, baseStat.DamageMult, 0, 0, DamageMultTrait.CurrentStat);
                break;
            case PlayerStatType.Attribute:
                finalAttribute = FinalStatAdd(PlayerStatType.Attribute, 0, 0, 0, 0);
                break;
        }

        expectedCrit = 1 + (finalCritChance * (finalCritMult - 1));
        normalPower = (finalATK * finalATKSpeed * expectedCrit) * (1 + finalAttribute) * (1 + finalDamageMult) * (1 + finalNormalDamage);
        StatUpdate?.Invoke();
    }

    private float FinalStatAdd(PlayerStatType playerStatType, float baseStat, float upgradeStat, float levelUP, float traitStat)
    {
        float equipStat = playerSlot.GetStat(playerStatType);
        float complate = baseStat + upgradeStat + levelUP + traitStat + equipStat;

        return complate;
    }

    public float GetFinalStat(PlayerStatType statType)
    {
        float baseValue = statType switch
        {
            PlayerStatType.HP => finalHP,
            PlayerStatType.MP => finalMP,
            PlayerStatType.HP_REGEN => finalHPRegen,
            PlayerStatType.MP_REGEN => finalMPRegen,
            PlayerStatType.ATK => finalATK,
            PlayerStatType.ATK_SPEED => finalATKSpeed,
            PlayerStatType.PHYS_DEF => finalPhysDEF,
            PlayerStatType.MAGIC_DEF => finalMagicDEF,
            PlayerStatType.CRIT_CHANCE => finalCritChance,
            PlayerStatType.CRIT_MULT => finalCritMult,
            PlayerStatType.MOVE_SPEED => finalMoveSpeed,
            PlayerStatType.COOLDOWN_REDUCE => finalCoolDownReduce,
            PlayerStatType.GOLD_GAIN => finalGoldGain,
            PlayerStatType.EXP_GAIN => finalExpGain,
            PlayerStatType.BOSS_DMG => finalBossDamage,
            PlayerStatType.NORMAL_DMG => finalNormalDamage,
            PlayerStatType.DMG_MULT => finalDamageMult,
            PlayerStatType.Attribute => finalAttribute,
            _ => 0f
        };
        return ApplyBerserkerMultiplier(statType, baseValue);
    }

    public void AllUpdate()
    {
        foreach(PlayerStatType statType in Enum.GetValues(typeof(PlayerStatType)))
        {
            FinalStat(statType);
        }
    }

    public void Upgrade(StatUpgrade statUpgrade)
    {
        statUpgrade.Upgrade();
    }

    public void TraitUpgrade (PlayerTrait playerTrait)
    {
        playerTrait.Upgrade();
        TraitUpdate?.Invoke(playerTrait.ID, playerTrait.CurrentLevel, playerTrait.MaxLevel);
    }

    public PlayerTrait GetTrait(PlayerStatType playerStatType)
    {
        switch (playerStatType)
        {
            case PlayerStatType.ATK:
                return attackTrait;
            case PlayerStatType.MP:
                return mpTrait;
            case PlayerStatType.HP:
                return hpTrait;
            case PlayerStatType.ATK_SPEED:
                return attackSpeedTrait;
            case PlayerStatType.CRIT_CHANCE:
                return critTrait;
            case PlayerStatType.CRIT_MULT:
                return critMultTrait;
            case PlayerStatType.BOSS_DMG:
                return bossDamageTrait;
            case PlayerStatType.COOLDOWN_REDUCE:
                return coolDownTrait;
            case PlayerStatType.DMG_MULT:
                return damageMultTrait;
            default:
                return null;
        }
    }

    public StatUpgrade GetUpgradeTable(PlayerStatType playerStatType)
    {
        switch (playerStatType)
        {
            case PlayerStatType.ATK:
                return attackStatUpgrade;
            case PlayerStatType.HP:
                return hpStatUpgrade;
            case PlayerStatType.HP_REGEN:
                return hpRegenStatUpgrade;
            case PlayerStatType.MP:
                return mpStatUpgrade;
            case PlayerStatType.MP_REGEN:
                return mpRegenStatUpgrade;
            case PlayerStatType.CRIT_CHANCE:
                return critStatUpgrade;
            case PlayerStatType.CRIT_MULT:
                return critMultStatUpgrade;
            case PlayerStatType.BOSS_DMG:
                return bossDamageStatUpgrade;
            //case PlayerStatType.TRIAT:
            //    return traitStatUpgrade;
            default:
                Debug.Log($"[CharcterStatManager] 현재 {playerStatType} 타입의 업그레이드가 없습니다");
                return null;
        }
    }

    public float GatBasicDamage(float damageMult, float monsterDef)
    {
        return FinalATK * (1 + damageMult) * (1 + FinalDamageMult) * (1 - monsterDef / 100);
    }
    
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        PlayerEquipment playerEquipment = equipmentHandler.playerEquipment;
        if (equipmentHandler.dataLoad)
        {
            testSaveData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
            JSONService.Save(testSaveData);
        }
    }

    #region 버서커 모드 Berserker Mode
    private const float BerserkerStatMultiplier = 2f;

    private float GetNormalPowerWithBerserker()
    {
        float atk = GetFinalStat(PlayerStatType.ATK);
        float atkSpeed = GetFinalStat(PlayerStatType.ATK_SPEED);
        float critChance = GetFinalStat(PlayerStatType.CRIT_CHANCE);
        float critMult = GetFinalStat(PlayerStatType.CRIT_MULT);
        float expectedCrit = 1f + (critChance * (critMult - 1f));
        float attr = GetFinalStat(PlayerStatType.Attribute);
        float dmgMult = GetFinalStat(PlayerStatType.DMG_MULT);
        float normalDmg = GetFinalStat(PlayerStatType.NORMAL_DMG);
        return (atk * atkSpeed * expectedCrit) * (1f + attr) * (1f + dmgMult) * (1f + normalDmg);
    }

    /// <summary>버서커 모드 활성 시 baseValue 2배 반환. 공격속도(ATK_SPEED)는 제외.</summary>
    public float ApplyBerserkerMultiplier(PlayerStatType statType, float baseValue)
    {
        if (statType == PlayerStatType.ATK_SPEED) return baseValue;
        if (BerserkerModeController.Instance != null && BerserkerModeController.Instance.IsActive)
            return baseValue * BerserkerStatMultiplier;
        return baseValue;
    }
    #endregion
}
