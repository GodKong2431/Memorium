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

    [SerializeField] SaveEquipmentData saveEquipmentData;
    [SerializeField] EquipmentHandler equipmentHandler;
    [SerializeField] SavePlayerData savePlayerData;

    [SerializeField] private TraitManager traitManager;

    [SerializeField] private CharacterBaseStat baseStat;

    [SerializeField] private PlayerLevel levelBonus;

    [SerializeField] private PlayerSlot playerSlot;

    public SerializedDictionary<StatType, StatUpgrade> Upgrades = new SerializedDictionary<StatType, StatUpgrade>();
    public SerializedDictionary<StatType, PlayerTrait> Traits = new SerializedDictionary<StatType, PlayerTrait>();
    public SerializedDictionary<StatType, FinalStat> FinalStats = new SerializedDictionary<StatType, FinalStat>();


    [SerializeField] private float normalBasicDamage;
    [SerializeField] private float bossBasicDamage;


    [SerializeField] public bool TableLoad = false;

    [SerializeField] private float normalPower;

    [SerializeField] private float expectedCrit;

    private EffectController _playerEffectController;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        Upgrades = characterStatSO.Upgrades;
        Traits = characterStatSO.Traits;


        savePlayerData = JSONService.Load<SavePlayerData>();

        LoadTable();
        saveEquipmentData = JSONService.Load<SaveEquipmentData>();
        saveEquipmentData.InitPlayerEquipmentData();


        //불러온 데이터 플레이어 장착 및 데이터 세팅
        if (equipmentHandler != null)
        {
            ReinforecementEquipmentStat.InitReinforcement(saveEquipmentData.unlockEquipmentDict);
            ReinforecementEquipmentStat.InitBonusStat(saveEquipmentData.unlockEquipmentDict);
            equipmentHandler.SetMyEquipOnStart(saveEquipmentData.weaponId, saveEquipmentData.helmetId, saveEquipmentData.gloveId, saveEquipmentData.armorId, saveEquipmentData.bootsId, 
                saveEquipmentData.unlockEquipmentDict);
        }

        yield return new WaitUntil(() => InventoryManager.Instance != null);
        InventoryManager.Instance.OnItemAmountChanged += saveEquipmentData.SaveEquipment;
        EquipmentInventoryModule equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        equipmentModule.OnEquipmentInfoChanged += saveEquipmentData.SaveEquipmentReinforcement;

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
        levelBonus.OnLevelUp += savePlayerData.SaveLevel;

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

        //levelBonus = new PlayerLevel(level);
        levelBonus = new PlayerLevel(savePlayerData.GetLevel());

        playerSlot = new PlayerSlot(characterTableKey);

        foreach (var trait in Traits.Values)
        {
            trait.LoadTrait();
        }

        savePlayerData.InitPlayerData(characterStatSO);

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            FinalStats.Add(statType, new FinalStat(statType));
        }


        EventSet();

        foreach (StatType playerStat in Enum.GetValues(typeof(StatType)))
        {
            FinalStat(playerStat);
        }
    }

    public void FinalStat(StatType playerStatType)
    {
        FinalStats.TryGetValue(playerStatType, out var finalStat);
        finalStat.FinalStatCalculate();

        NormalPowerCalculate();

        StatUpdate?.Invoke();
    }

    public void NormalPowerCalculate()
    {
        FinalStats.TryGetValue(StatType.CRIT_CHANCE, out var crit);
        FinalStats.TryGetValue(StatType.CRIT_MULT, out var critMult);

        expectedCrit = 1 + (crit.finalStat * (critMult.finalStat - 1f));

        FinalStats.TryGetValue(StatType.ATK, out var atk);
        FinalStats.TryGetValue(StatType.ATK_SPEED, out var atkSPD);
        FinalStats.TryGetValue(StatType.DMG_MULT, out var dmgMult);
        FinalStats.TryGetValue(StatType.NORMAL_DMG, out var normalDmg);

        normalPower = (atk.finalStat * atkSPD.finalStat * expectedCrit) * (1 + dmgMult.finalStat) * (1 + normalDmg.finalStat);
    }

    public float GetFinalStat(StatType statType)
    {
        float baseValue = FinalStats.TryGetValue(statType, out var finalStat) ? finalStat.finalStat : 0f;
        baseValue = ApplyBerserkerMultiplier(statType, baseValue);
        baseValue = _playerEffectController.GetModifiedStat(statType, baseValue);
        return baseValue;
    }

    public void AllUpdate()
    {
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
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

    public PlayerTrait GetTrait(StatType playerStatType)
    {
        return Traits.TryGetValue(playerStatType, out var trait) ? trait : null;
    }

    public StatUpgrade GetUpgradeTable(StatType playerStatType)
    {
        return Upgrades.TryGetValue(playerStatType, out var upgrade) ? upgrade : null;
    }

    public float GatBasicDamage(float damageMult, float monsterDef)
    {
        var FinalATK = FinalStats.TryGetValue(StatType.ATK, out var atk) ? atk.finalStat : 0f;
        var FinalDamageMult = FinalStats.TryGetValue(StatType.DMG_MULT, out var dmgMult) ? dmgMult.finalStat : 0f;
        return FinalATK * (1 + damageMult) * (1 + FinalDamageMult) * (1 - monsterDef / 100);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        if (equipmentHandler == null || !equipmentHandler.dataLoad)
            return;
        if (!equipmentHandler.TryGetPlayerEquipment(out var playerEquipment))
            return;
        if (saveEquipmentData == null)
            return;

        saveEquipmentData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
        JSONService.Save(saveEquipmentData);


        savePlayerData.Save();
        JSONService.Save(savePlayerData);
    }

    #region 버서커 모드 Berserker Mode
    private const float BerserkerStatMultiplier = 2f;

    private float GetNormalPowerWithBerserker()
    {
        float atk = GetFinalStat(StatType.ATK);
        float atkSpeed = GetFinalStat(StatType.ATK_SPEED);
        float critChance = GetFinalStat(StatType.CRIT_CHANCE);
        float critMult = GetFinalStat(StatType.CRIT_MULT);
        float expectedCrit = 1f + (critChance * (critMult - 1f));
        float dmgMult = GetFinalStat(StatType.DMG_MULT);
        float normalDmg = GetFinalStat(StatType.NORMAL_DMG);
        return (atk * atkSpeed * expectedCrit) * (1f + dmgMult) * (1f + normalDmg);
    }

    /// <summary>버서커 모드 활성 시 baseValue 2배 반환. 공격속도(ATK_SPEED)는 제외.</summary>
    public float ApplyBerserkerMultiplier(StatType statType, float baseValue)
    {
        if (statType == StatType.ATK_SPEED) return baseValue;
        if (BerserkerModeController.Instance != null && BerserkerModeController.Instance.IsActive)
            return baseValue * BerserkerStatMultiplier;
        return baseValue;
    }
    #endregion

    #region 버프 컨트롤러 등록
    public void RegisterEffectController(EffectController effectController)
    {
        _playerEffectController = effectController;
    }
    #endregion
}
