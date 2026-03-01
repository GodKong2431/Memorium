using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterStatManager : Singleton<CharacterStatManager>
{
    [SerializeField] CharacterStatSO characterStatSO;
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

    [SerializeField] private PlayerLevel levelBonus;

    [SerializeField] private PlayerSlot playerSlot;

    public SerializedDictionary<PlayerStatType, StatUpgrade> Upgrades = new SerializedDictionary<PlayerStatType, StatUpgrade>();
    public SerializedDictionary<PlayerStatType, PlayerTrait> Traits = new SerializedDictionary<PlayerStatType, PlayerTrait>();
    public SerializedDictionary<PlayerStatType, FinalStat> FinalStats = new SerializedDictionary<PlayerStatType, FinalStat>();


    [SerializeField] private float normalBasicDamage;
    [SerializeField] private float bossBasicDamage;


    [SerializeField] public bool TableLoad = false;

    [SerializeField] private float normalPower;

    [SerializeField] private float expectedCrit;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        Upgrades = characterStatSO.Upgrades;
        Traits = characterStatSO.Traits;
        
        LoadTable();
        testSaveData = JSONService.Load<TestSavePlayerEquipmentData>();
        testSaveData.InitPlayerEquipmentData();

        //불러온 데이터 플레이어 장착 및 데이터 세팅
        if (equipmentHandler != null)
        {
            equipmentHandler.SetMyEquipOnStart(testSaveData.weaponId, testSaveData.helmetId, testSaveData.gloveId, testSaveData.armorId, testSaveData.bootsId, testSaveData.unlockEquipmentDict);
        }

        

        TableLoad = true;
    }

    public CharacterBaseStat BaseStat { get { return baseStat; } }

    public PlayerLevel LevelBonus { get { return levelBonus; } }

    public PlayerSlot PlayerSlot { get { return playerSlot; } }

    /// <summary>버서커 모드 포함한 전투력. normalPower 기반에 버서커 배율 적용.</summary>
    public float NormalPower => GetNormalPowerWithBerserker();

    public event Action StatUpdate;

    public event Action<int, int, int> TraitUpdate;

    public void EventSet()
    {

        GameEventManager.OnCurrencyChanged += levelBonus.ExpCheck;

        levelBonus.OnLevelUp += AllUpdate;

        playerSlot.OnSlotUpdate += AllUpdate;

        BerserkerModeController.OnBerserkerModeStarted += OnBerserkerModeChanged;
        BerserkerModeController.OnBerserkerModeEnded += OnBerserkerModeChanged;
    }

    private void OnBerserkerModeChanged()
    {
        if (!TableLoad || !isActiveAndEnabled)
            return;

        StatUpdate?.Invoke();
    }

    private void OnDisable()
    {
        foreach (var upgrade in Upgrades.Values)
        {
            upgrade.DisableEvent();
        }

        foreach (var trait in Traits.Values)
        {
            trait.DisableEvent();
        }

        GameEventManager.OnCurrencyChanged -= levelBonus.ExpCheck;

        levelBonus.OnLevelUp -= AllUpdate;

        playerSlot.OnSlotUpdate -= AllUpdate;

        BerserkerModeController.OnBerserkerModeStarted -= OnBerserkerModeChanged;
        BerserkerModeController.OnBerserkerModeEnded -= OnBerserkerModeChanged;
    }

    public void LoadTable()
    {
        baseStat = new CharacterBaseStat(characterBaseKey);

        foreach (var upgrade in Upgrades.Values)
        {
            upgrade.LoadUpgrade();
        }

        levelBonus = new PlayerLevel(level);

        playerSlot = new PlayerSlot(characterTableKey);

        foreach (var trait in Traits.Values)
        {
            trait.LoadTrait();
        }

        foreach (PlayerStatType statType in Enum.GetValues(typeof(PlayerStatType)))
        {
            FinalStats.Add(statType, new FinalStat(statType));
        }

        EventSet();

        foreach (PlayerStatType playerStat in Enum.GetValues(typeof(PlayerStatType)))
        {
            FinalStat(playerStat);
        }
    }

    public void FinalStat(PlayerStatType playerStatType)
    {
        FinalStats.TryGetValue(playerStatType, out var finalStat);
        finalStat.FinalStatCalculate();

        NormalPowerCalculate();

        StatUpdate?.Invoke();
    }

    public void NormalPowerCalculate()
    {
        FinalStats.TryGetValue(PlayerStatType.CRIT_CHANCE, out var crit);
        FinalStats.TryGetValue(PlayerStatType.CRIT_MULT, out var critMult);

        expectedCrit = 1 + (crit.finalStat * (critMult.finalStat - 1f));

        FinalStats.TryGetValue(PlayerStatType.ATK, out var atk);
        FinalStats.TryGetValue(PlayerStatType.ATK_SPEED, out var atkSPD);
        FinalStats.TryGetValue(PlayerStatType.DMG_MULT, out var dmgMult);
        FinalStats.TryGetValue(PlayerStatType.NORMAL_DMG, out var normalDmg);

        normalPower = (atk.finalStat * atkSPD.finalStat * expectedCrit) * (1 + dmgMult.finalStat) * (1 + normalDmg.finalStat);
    }

    public float GetFinalStat(PlayerStatType statType)
    {
        float baseValue = FinalStats.TryGetValue(statType, out var finalStat) ? finalStat.finalStat : 0f;
        return ApplyBerserkerMultiplier(statType, baseValue);
    }

    public void AllUpdate()
    {
        foreach (PlayerStatType statType in Enum.GetValues(typeof(PlayerStatType)))
        {
            FinalStat(statType);
        }
    }

    public void Upgrade(StatUpgrade statUpgrade)
    {
        statUpgrade.Upgrade();
    }

    public void TraitUpgrade(PlayerTrait playerTrait)
    {
        playerTrait.Upgrade();
        TraitUpdate?.Invoke(playerTrait.ID, playerTrait.CurrentLevel, playerTrait.MaxLevel);
    }

    public PlayerTrait GetTrait(PlayerStatType playerStatType)
    {
        return Traits.TryGetValue(playerStatType, out var trait) ? trait : null;
    }

    public StatUpgrade GetUpgradeTable(PlayerStatType playerStatType)
    {
        return Upgrades.TryGetValue(playerStatType, out var upgrade) ? upgrade : null;
    }

    public float GatBasicDamage(float damageMult, float monsterDef)
    {
        var FinalATK = FinalStats.TryGetValue(PlayerStatType.ATK, out var atk) ? atk.finalStat : 0f;
        var FinalDamageMult = FinalStats.TryGetValue(PlayerStatType.DMG_MULT, out var dmgMult) ? dmgMult.finalStat : 0f;
        return FinalATK * (1 + damageMult) * (1 + FinalDamageMult) * (1 - monsterDef / 100);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        if (equipmentHandler == null || !equipmentHandler.dataLoad)
            return;
        if (!equipmentHandler.TryGetPlayerEquipment(out var playerEquipment))
            return;
        if (testSaveData == null)
            return;

        testSaveData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
        JSONService.Save(testSaveData);
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
        float dmgMult = GetFinalStat(PlayerStatType.DMG_MULT);
        float normalDmg = GetFinalStat(PlayerStatType.NORMAL_DMG);
        return (atk * atkSpeed * expectedCrit) * (1f + dmgMult) * (1f + normalDmg);
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
