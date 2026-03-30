using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterStatManager : Singleton<CharacterStatManager>
{
    public static Transform playerTransform;
    
    [SerializeField] CharacterStatSO characterStatSO;
    // 키들
    [Tooltip("1000001 ~ 1000003")]
    [Range(1000001, 1000003)]
    [SerializeField] private int characterBaseKey;
    [Tooltip("1030001 ~ 1030003")]
    [Range(1030001, 1030003)]
    [SerializeField] private int characterTableKey;
    [SerializeField] private int level;

    public SaveEquipmentData saveEquipmentData;
    [SerializeField] EquipmentHandler equipmentHandler;
    public SavePlayerData savePlayerData;

    [SerializeField] private CharacterBaseStat baseStat;

    [SerializeField] private PlayerLevel levelBonus;

    [SerializeField] private PlayerSlot playerSlot;

    public SerializedDictionary<StatType, StatUpgrade> Upgrades = new SerializedDictionary<StatType, StatUpgrade>();
    public SerializedDictionary<StatType, PlayerTrait> Traits = new SerializedDictionary<StatType, PlayerTrait>();
    public SerializedDictionary<StatType, FinalStat> FinalStats = new SerializedDictionary<StatType, FinalStat>();


    [SerializeField] private float normalBasicDamage;
    [SerializeField] private float bossBasicDamage;


    [SerializeField] public bool TableLoad = false;

    [SerializeField] private float attackPoint;
    [SerializeField] private float defensePoint;
    [SerializeField] private float normalPower;

    [SerializeField] private float expectedCrit;

    private EffectController _playerEffectController;
    private bool _isBatchUpdatingStats;
    
    public bool isBerserker;
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
        yield return new WaitUntil(() => equipmentHandler.dataLoad);
        yield return new WaitUntil(() => InventoryManager.Instance != null);
        InventoryManager.Instance.OnItemAmountChanged += saveEquipmentData.SaveEquipment;
        EquipmentInventoryModule equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        equipmentModule.OnEquipmentInfoChanged += saveEquipmentData.SaveEquipmentReinforcement;

        TableLoad = true;

        //SceneManager.sceneLoaded += OnSceneChanged;
    }

    public CharacterBaseStat BaseStat { get { return baseStat; } }

    public PlayerLevel LevelBonus { get { return levelBonus; } }

    public PlayerSlot PlayerSlot { get { return playerSlot; } }

    /// <summary>버서커 모드 포함한 전투력. normalPower 기반에 버서커 배율 적용.</summary>
    public float NormalPower => normalPower;

    public event Action StatUpdate;

    public event Action<int, int, int> TraitUpdate;

    public void EventSet()
    {

        GameEventManager.OnCurrencyChanged += levelBonus.ExpCheck;

        levelBonus.OnLevelUp += AllStatUpdate;
        levelBonus.OnLevelUp += savePlayerData.SaveLevel;

        playerSlot.OnSlotUpdate += AllStatUpdate;

        StatUpdate += savePlayerData.Save;

        BerserkerModeController.OnBerserkerModeChanged += OnBerserkerModeChanged;
    }

    private void OnBerserkerModeChanged(bool changeState)
    {
        if (!TableLoad || !isActiveAndEnabled)
            return;

        isBerserker = changeState;
        AllStatUpdate();
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

        levelBonus.OnLevelUp -= AllStatUpdate;

        playerSlot.OnSlotUpdate -= AllStatUpdate;

        BerserkerModeController.OnBerserkerModeChanged -= OnBerserkerModeChanged;
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

        AllStatUpdate();
    }

    public void FinalStat(StatType playerStatType)
    {
        if (playerStatType == StatType.None)
            return;

        // 초기화 이전 호출(예: 저장 데이터 복원 시점)에서는 계산을 건너뛴다.
        if (FinalStats == null || FinalStats.Count == 0)
            return;

        if (!FinalStats.TryGetValue(playerStatType, out var finalStat) || finalStat == null)
            return;

        finalStat.FinalStatCalculate();

        if (_isBatchUpdatingStats)
            return;

        NormalPowerCalculate();

        StatUpdate?.Invoke();
    }

    public void NormalPowerCalculate()
    {
        FinalStats.TryGetValue(StatType.CRIT_CHANCE, out var crit);
        FinalStats.TryGetValue(StatType.CRIT_MULT, out var critMult);

        expectedCrit = 1 + (crit.finalStat * (critMult.finalStat - 1f));
        
        // 공격 점수
        FinalStats.TryGetValue(StatType.ATK, out var atk);
        FinalStats.TryGetValue(StatType.ATK_SPEED, out var atkSPD);
        FinalStats.TryGetValue(StatType.DMG_MULT, out var dmgMult);
        FinalStats.TryGetValue(StatType.NORMAL_DMG, out var normalDmg);
        FinalStats.TryGetValue(StatType.BOSS_DMG, out var bossDmg);
        
        // 방어 점수
        FinalStats.TryGetValue(StatType.HP, out var hp);
        FinalStats.TryGetValue(StatType.PHYS_DEF, out var physDef);
        FinalStats.TryGetValue(StatType.MAGIC_DEF, out var magicDef);
        FinalStats.TryGetValue(StatType.HP_REGEN, out var hpRegen);
        
        //
        FinalStats.TryGetValue(StatType.MP, out var mp);
        FinalStats.TryGetValue(StatType.MP_REGEN, out var mpRegen);
        
        attackPoint = (atk.finalStat * atkSPD.finalStat * expectedCrit) * (1 + dmgMult.finalStat) * (1 + (normalDmg.finalStat + bossDmg.finalStat) / 2);
        defensePoint = (hp.finalStat * (1 + physDef.finalStat + magicDef.finalStat) / 100) * (hpRegen.finalStat * 0.2f);
        normalPower = (attackPoint * defensePoint) + (mp.finalStat * 0.1f) + (mpRegen.finalStat * 0.2f);
    }

    public float GetFinalStat(StatType statType)
    {
        float baseValue = FinalStats.TryGetValue(statType, out var finalStat) ? finalStat.finalStat : 0f;
        // PlayerStateMachine에서 EffectController를 등록하기 전에도 전투력 계산이 먼저 호출될 수 있다.
        if (_playerEffectController != null)
            baseValue = _playerEffectController.GetModifiedStat(statType, baseValue);
        return baseValue;
    }

    public float GetPreviewFinalStat(StatType statType, float additionalTraitValue = 0f)
    {
        if (statType == StatType.None)
            return 0f;

        float baseStatValue = BaseStat != null && BaseStat.baseStatValues.TryGetValue(statType, out var baseValue) ? baseValue : 0f;
        float upgradeStatValue = Upgrades.TryGetValue(statType, out var upgrade) ? upgrade?.Stat ?? 0f : 0f;
        float levelBonusValue = LevelBonus != null && LevelBonus.BonusValues.TryGetValue(statType, out var levelValue) ? levelValue : 0f;

        float traitStatValue = 0f;
        if (Traits.TryGetValue(statType, out var trait) && trait != null)
            traitStatValue = trait.CurrentStat;

        float equipStatValue = PlayerSlot != null ? PlayerSlot.GetStat(statType) : 0f;

        float abilityStoneStat = AbilityStoneManager.Instance != null && AbilityStoneManager.Instance.LoadStone
            ? AbilityStoneManager.Instance.GetStat(statType, 0)
            : 0f;
        float abilityStoneMultStat = AbilityStoneManager.Instance != null && AbilityStoneManager.Instance.LoadStone
            ? AbilityStoneManager.Instance.GetStat(statType, 1)
            : 0f;
        float abilityStoneBonusStat = AbilityStoneManager.Instance != null && AbilityStoneManager.Instance.LoadStone
            ? AbilityStoneManager.Instance.GetBonusStat(statType)
            : 0f;
        float bingoSynergyStat = BingoBoardManager.Instance != null && BingoBoardManager.Instance.LoadBingo
            ? BingoBoardManager.Instance.GetSynergyStat(statType)
            : 0f;

        float previewValue =
            (baseStatValue + upgradeStatValue + levelBonusValue + traitStatValue + additionalTraitValue + equipStatValue + abilityStoneStat) *
            (1 + abilityStoneBonusStat + bingoSynergyStat + abilityStoneMultStat);

        if (isBerserker && BerserkerModeController.Instance != null)
        {
            float berserkerValue = BerserkerModeController.Instance._berserkModeSo.BserserkMultStatSo.TryGetValue(statType, out var value) ? value : 1f;
            previewValue *= berserkerValue;
        }

        previewValue = ApplyBerserkerMultiplier(statType, previewValue);

        if (_playerEffectController != null)
            previewValue = _playerEffectController.GetModifiedStat(statType, previewValue);

        return previewValue;
    }

    public void AllStatUpdate()
    {
        _isBatchUpdatingStats = true;

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            FinalStat(statType);
        }

        _isBatchUpdatingStats = false;
        NormalPowerCalculate();
        StatUpdate?.Invoke();
    }

    public void Upgrade(StatUpgrade statUpgrade)
    {
        statUpgrade.Upgrade();
    }

    public bool TraitUpgrade(PlayerTrait playerTrait)
    {
        if (playerTrait == null)
            return false;

        if (!playerTrait.Upgrade())
            return false;

        TraitUpdate?.Invoke(playerTrait.ID, playerTrait.CurrentLevel, playerTrait.MaxLevel);
        return true;
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
    public void SaveOnEquip(int itemId, EquipmentType type)
    {
        saveEquipmentData.SaveOnEquip(itemId, type);
    }

    //protected override void OnApplicationQuit()
    //{
    //    base.OnApplicationQuit();
    //    if (equipmentHandler == null || !equipmentHandler.dataLoad)
    //        return;
    //    if (!equipmentHandler.TryGetPlayerEquipment(out var playerEquipment))
    //        return;
    //    if (saveEquipmentData == null)
    //        return;

    //    saveEquipmentData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
    //    JSONService.Save(saveEquipmentData);


    //    savePlayerData.Save();
    //    JSONService.Save(savePlayerData);
    //}

    //public void OnSceneChanged(Scene scene, LoadSceneMode mode)
    //{
    //    if(TableLoad)
    //        equipmentHandler.RefreshMyEquip();
    //}
    #region 버서커 모드 Berserker Mode
    private const float BerserkerStatMultiplier = 2f;

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
