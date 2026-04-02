using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 드랍 설정과 던전 클리어 보상을 함께 관리한다.
/// </summary>
public class RewardManager : Singleton<RewardManager>
{
    private const string ItemDropSettingsResourcePath = "ItemDropSettings";
    private const int GoldItemId = 3810001;
    private const int SkillScrollItemId = 3210001;
    private const int PixiePieceItemId = 3310001;

    private static readonly EquipmentType[] DungeonRewardEquipmentTypes =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Glove,
        EquipmentType.Armor,
        EquipmentType.Boots
    };

    public enum DungeonRewardVisualType
    {
        Item,
        Currency,
        Equipment
    }

    public struct DungeonRewardEntry
    {
        public DungeonRewardVisualType visualType;
        public int itemId;
        public CurrencyType currencyType;
        public int equipmentTier;
        public EquipmentType equipmentType;
        public BigDouble amount;
    }

    public ItemDropSettings DropSettings { get; private set; }
    private readonly List<DungeonRewardEntry> lastGrantedDungeonRewards = new List<DungeonRewardEntry>();
    private StageType lastGrantedDungeonStageType = StageType.None;
    private int lastGrantedDungeonLevel;
    private bool hasLastGrantedDungeonRewards;

    protected override void Awake()
    {
        base.Awake();
        EnsureItemDropSettings();
    }

    private void EnsureItemDropSettings()
    {
        ItemDropSettings loaded = Resources.Load<ItemDropSettings>(ItemDropSettingsResourcePath);
        if (loaded != null)
        {
            DropSettings = loaded;
            return;
        }

        DropSettings = CreateDefaultItemDropSettings();
    }

    public void SetDropTable(ItemDropTable dropTable)
    {
        if (dropTable == null)
        {
            Debug.LogWarning("[RewardManager] dropTable is null.");
            return;
        }

        if (DropSettings == null)
            EnsureItemDropSettings();

        if (DropSettings == null)
            return;

        EquipmentDropTable equipmentDropTable = DataManager.Instance.EquipmentDropDict[dropTable.equipmentDropID];
        DropSettings.equipmentDropRate.Clear();
        DropSettings.dropGold = dropTable.dropGold;
        DropSettings.baseEquipmentTier = equipmentDropTable.BaseEquipmentTier;

        int fullRate = equipmentDropTable.EquipmentTierWeight01
            + equipmentDropTable.EquipmentTierWeight02
            + equipmentDropTable.EquipmentTierWeight03
            + equipmentDropTable.EquipmentTierWeight04;

        if (fullRate > 0)
        {
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight01 / fullRate);
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight02 / fullRate);
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight03 / fullRate);
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight04 / fullRate);
        }

        DropSettings.equipmentChance = (float)(dropTable.equipmentRate);
        DropSettings.pixieFragmentChance = (float)dropTable.fairyPieceRate;
        DropSettings.skillScrollChance = (float)dropTable.scrollRate;
        DropSettings.skillGemChance = (float)dropTable.gemRate;
        DropSettings.dungeonTicketChance = (float)dropTable.keyRate;

        if (DropSettings.bingoLinks.Count > 0)
            DropSettings.bingoLinks.Clear();

        DropSettings.bingoLinks.Add(dropTable.link_0);
        DropSettings.bingoLinks.Add(dropTable.link_1);
        DropSettings.bingoLinks.Add(dropTable.link_2);
        DropSettings.bingoLinks.Add(dropTable.link_3);
        DropSettings.bingoLinks.Add(dropTable.link_4);

        DropSettings.bingoItem_A = dropTable.bingoItem_A;
        DropSettings.bingoItem_B = dropTable.bingoItem_B;

        if (DropSettings.bingoSynergy.Count > 0)
            DropSettings.bingoSynergy.Clear();

        DropSettings.bingoSynergy.Add(dropTable.synergy_0);
        DropSettings.bingoSynergy.Add(dropTable.synergy_1);
        DropSettings.bingoSynergy.Add(dropTable.synergy_2);
        DropSettings.bingoSynergy.Add(dropTable.synergy_3);
        DropSettings.bingoSynergy.Add(dropTable.synergy_4);
    }

    // 던전 레벨에 맞는 보상 미리보기 데이터를 만든다.
    public bool TryGetDungeonRewardPreview(StageType stageType, int dungeonLevel, List<DungeonRewardEntry> rewards)
    {
        if (rewards == null)
            return false;

        rewards.Clear();

        if (!TryGetDungeonRewardStageLevel(stageType, dungeonLevel, out int stageLevel))
            return false;

        switch (stageType)
        {
            case StageType.GuardianTaxVault:
                if (!TryGetGuardianTaxVaultReward(stageLevel, out GuardianTaxVaultRewardTable goldReward))
                    return false;

                AddCurrencyRewardPreview(rewards, CurrencyType.Gold, goldReward.rewardGold);
                break;

            case StageType.HallOfTraining:
                if (!TryGetHallOfTrainingReward(stageLevel, out HallOfTrainingRewardTable expReward))
                    return false;

                AddCurrencyRewardPreview(rewards, CurrencyType.Exp, expReward.rewardExp);
                break;

            case StageType.CelestiAlchemyWorkshop:
                if (!TryGetCelestiAlchemyWorkshopReward(stageLevel, out CelestiAlchemyWorkshopRewardTable alchemyReward))
                    return false;

                AddItemRewardPreview(rewards, PixiePieceItemId, alchemyReward.pixiePieceCount);
                AddItemRewardPreview(rewards, SkillScrollItemId, alchemyReward.skillscrollCount);
                break;

            case StageType.EidosTreasureVault:
                if (!TryGetEidosTreasureVaultReward(stageLevel, out EidosTreasureVaultRewardTable equipmentReward))
                    return false;

                AddEquipmentRewardPreview(rewards, equipmentReward.itemCount, equipmentReward.BaseEquipmentTier);
                break;

            default:
                return false;
        }

        return rewards.Count > 0;
    }

    // 던전 목록 시트에서 보여줄 드랍 종류 요약 데이터를 만든다.
    public bool TryGetDungeonRewardSummary(StageType stageType, List<DungeonRewardEntry> rewards)
    {
        if (rewards == null)
            return false;

        rewards.Clear();

        switch (stageType)
        {
            case StageType.GuardianTaxVault:
                AddCurrencyRewardSummary(rewards, CurrencyType.Gold);
                break;

            case StageType.HallOfTraining:
                AddCurrencyRewardSummary(rewards, CurrencyType.Exp);
                break;

            case StageType.CelestiAlchemyWorkshop:
                AddItemRewardSummary(rewards, PixiePieceItemId);
                AddItemRewardSummary(rewards, SkillScrollItemId);
                break;

            case StageType.EidosTreasureVault:
                int summaryTier = ResolveDungeonSummaryEquipmentTier(stageType);
                for (int i = 0; i < DungeonRewardEquipmentTypes.Length; i++)
                    AddEquipmentRewardSummary(rewards, summaryTier, DungeonRewardEquipmentTypes[i]);
                break;

            default:
                return false;
        }

        return rewards.Count > 0;
    }

    // 던전 클리어 시 실제 보상을 지급한다.
    public bool GrantDungeonClearReward(StageType stageType, int dungeonLevel)
    {
        BeginDungeonRewardCapture(stageType, dungeonLevel);

        if (!TryGetDungeonRewardStageLevel(stageType, dungeonLevel, out int stageLevel))
            return false;

        switch (stageType)
        {
            case StageType.GuardianTaxVault:
                if (!TryGetGuardianTaxVaultReward(stageLevel, out GuardianTaxVaultRewardTable goldReward))
                    return false;

                if (!GrantReward(GoldItemId, goldReward.rewardGold))
                    return false;

                AddGrantedCurrencyReward(CurrencyType.Gold, goldReward.rewardGold);
                return true;

            case StageType.HallOfTraining:
                if (!TryGetHallOfTrainingReward(stageLevel, out HallOfTrainingRewardTable expReward))
                    return false;

                if (!GrantCurrencyReward(CurrencyType.Exp, expReward.rewardExp))
                    return false;

                AddGrantedCurrencyReward(CurrencyType.Exp, expReward.rewardExp);
                return true;

            case StageType.CelestiAlchemyWorkshop:
                if (!TryGetCelestiAlchemyWorkshopReward(stageLevel, out CelestiAlchemyWorkshopRewardTable alchemyReward))
                    return false;

                bool grantedAlchemyReward = false;
                BigDouble pixiePieceCount = new BigDouble(Mathf.Max(0, alchemyReward.pixiePieceCount));
                BigDouble skillScrollCount = new BigDouble(Mathf.Max(0, alchemyReward.skillscrollCount));

                if (GrantReward(PixiePieceItemId, pixiePieceCount))
                {
                    AddGrantedItemReward(PixiePieceItemId, pixiePieceCount);
                    grantedAlchemyReward = true;
                }

                if (GrantReward(SkillScrollItemId, skillScrollCount))
                {
                    AddGrantedItemReward(SkillScrollItemId, skillScrollCount);
                    grantedAlchemyReward = true;
                }

                return grantedAlchemyReward;

            case StageType.EidosTreasureVault:
                if (!TryGetEidosTreasureVaultReward(stageLevel, out EidosTreasureVaultRewardTable equipmentReward))
                    return false;

                return GrantEquipmentDungeonReward(equipmentReward);

            default:
                return false;
        }
    }

    public bool TryGetLastDungeonClearRewards(StageType stageType, int dungeonLevel, List<DungeonRewardEntry> rewards)
    {
        if (rewards == null)
            return false;

        rewards.Clear();

        if (!hasLastGrantedDungeonRewards ||
            lastGrantedDungeonStageType != stageType ||
            lastGrantedDungeonLevel != dungeonLevel ||
            lastGrantedDungeonRewards.Count == 0)
        {
            return false;
        }

        rewards.AddRange(lastGrantedDungeonRewards);
        return true;
    }

    // 보상 타입에 맞는 아이콘을 반환한다.
    public Sprite ResolveDungeonRewardIcon(DungeonRewardEntry reward)
    {
        switch (reward.visualType)
        {
            case DungeonRewardVisualType.Currency:
                return IconManager.GetCurrencyIcon(reward.currencyType);

            case DungeonRewardVisualType.Item:
                if (DataManager.Instance == null ||
                    DataManager.Instance.ItemInfoDict == null ||
                    !DataManager.Instance.ItemInfoDict.TryGetValue(reward.itemId, out ItemInfoTable itemInfo))
                {
                    return null;
                }

                return IconManager.GetItemIcon(itemInfo);

            case DungeonRewardVisualType.Equipment:
                if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
                    return null;

                if (reward.itemId > 0 &&
                    DataManager.Instance.EquipListDict.TryGetValue(reward.itemId, out EquipListTable actualEquipInfo))
                {
                    return IconManager.GetEquipmentIcon(actualEquipInfo);
                }

                int previewEquipmentId = FindPreviewEquipmentId(reward.equipmentTier, reward.equipmentType);
                if (previewEquipmentId <= 0 ||
                    !DataManager.Instance.EquipListDict.TryGetValue(previewEquipmentId, out EquipListTable equipInfo))
                {
                    return null;
                }

                return IconManager.GetEquipmentIcon(equipInfo);

            default:
                return null;
        }
    }

    public bool GrantReward(int itemId, BigDouble count)
    {
        if (itemId <= 0 || count <= BigDouble.Zero)
        {
            Debug.LogWarning($"[RewardManager] invalid reward parameter. itemId={itemId}, count={count}");
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[RewardManager] InventoryManager is missing.");
            return false;
        }

        bool granted = InventoryManager.Instance.AddItem(itemId, count);
        if (!granted)
            Debug.LogWarning($"[RewardManager] reward grant failed. itemId={itemId}, count={count}");

        return granted;
    }

    public bool GrantReward(int itemId, int count)
    {
        return GrantReward(itemId, new BigDouble(count));
    }

    private static bool TryGetDungeonRewardStageLevel(StageType stageType, int dungeonLevel, out int stageLevel)
    {
        stageLevel = 0;

        if (DataManager.Instance == null || DataManager.Instance.StageManageDict == null)
            return false;

        if (!CheckDungeon.TryGetDungeonReq(stageType, dungeonLevel, out int dungeonId, out _))
            return false;

        if (!DataManager.Instance.StageManageDict.TryGetValue(dungeonId, out StageManageTable stageData) || stageData == null)
            return false;

        stageLevel = stageData.stageLevel;
        return stageLevel > 0;
    }

    private static void AddCurrencyRewardPreview(List<DungeonRewardEntry> rewards, CurrencyType currencyType, BigDouble amount)
    {
        if (rewards == null || amount <= BigDouble.Zero)
            return;

        rewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Currency,
            currencyType = currencyType,
            amount = amount
        });
    }

    private static void AddCurrencyRewardSummary(List<DungeonRewardEntry> rewards, CurrencyType currencyType)
    {
        if (rewards == null)
            return;

        rewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Currency,
            currencyType = currencyType,
            amount = BigDouble.Zero
        });
    }

    private static void AddItemRewardPreview(List<DungeonRewardEntry> rewards, int itemId, int amount)
    {
        if (rewards == null || itemId <= 0 || amount <= 0)
            return;

        rewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Item,
            itemId = itemId,
            amount = new BigDouble(amount)
        });
    }

    private static void AddItemRewardSummary(List<DungeonRewardEntry> rewards, int itemId)
    {
        if (rewards == null || itemId <= 0)
            return;

        rewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Item,
            itemId = itemId,
            amount = BigDouble.Zero
        });
    }

    private static void AddEquipmentRewardPreview(List<DungeonRewardEntry> rewards, int itemCount, int equipmentTier, EquipmentType equipmentType = 0)
    {
        if (rewards == null || itemCount <= 0)
            return;

        rewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Equipment,
            equipmentTier = equipmentTier,
            equipmentType = equipmentType,
            amount = new BigDouble(itemCount)
        });
    }

    private static void AddEquipmentRewardSummary(List<DungeonRewardEntry> rewards, int equipmentTier, EquipmentType equipmentType)
    {
        if (rewards == null)
            return;

        rewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Equipment,
            equipmentTier = equipmentTier,
            equipmentType = equipmentType,
            amount = BigDouble.Zero
        });
    }

    private bool GrantCurrencyReward(CurrencyType currencyType, BigDouble amount)
    {
        if (amount <= BigDouble.Zero || InventoryManager.Instance == null)
            return false;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        if (currencyModule == null)
            return false;

        currencyModule.AddCurrency(currencyType, amount);
        InventoryManager.Instance.saveCurrencyData?.Save(currencyType, currencyModule.GetAmount(currencyType));
        return true;
    }

    private bool GrantEquipmentDungeonReward(EidosTreasureVaultRewardTable rewardTable)
    {
        if (rewardTable == null)
            return false;

        bool grantedAny = false;
        int itemCount = Mathf.Max(0, rewardTable.itemCount);
        for (int i = 0; i < itemCount; i++)
        {
            int rewardItemId = RollEquipmentDungeonRewardItemId(rewardTable);
            if (rewardItemId <= 0)
                continue;

            if (!GrantReward(rewardItemId, 1))
                continue;

            AddGrantedEquipmentReward(rewardItemId);
            grantedAny = true;
        }

        return grantedAny;
    }

    private void BeginDungeonRewardCapture(StageType stageType, int dungeonLevel)
    {
        lastGrantedDungeonRewards.Clear();
        lastGrantedDungeonStageType = stageType;
        lastGrantedDungeonLevel = dungeonLevel;
        hasLastGrantedDungeonRewards = true;
    }

    private void AddGrantedCurrencyReward(CurrencyType currencyType, BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return;

        lastGrantedDungeonRewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Currency,
            currencyType = currencyType,
            amount = amount
        });
    }

    private void AddGrantedItemReward(int itemId, BigDouble amount)
    {
        if (itemId <= 0 || amount <= BigDouble.Zero)
            return;

        lastGrantedDungeonRewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Item,
            itemId = itemId,
            amount = amount
        });
    }

    private void AddGrantedEquipmentReward(int equipmentItemId)
    {
        if (equipmentItemId <= 0)
            return;

        EquipmentType equipmentType = 0;
        int equipmentTier = 0;

        if (DataManager.Instance?.EquipListDict != null &&
            DataManager.Instance.EquipListDict.TryGetValue(equipmentItemId, out EquipListTable equipInfo) &&
            equipInfo != null)
        {
            equipmentType = equipInfo.equipmentType;
            equipmentTier = equipInfo.equipmentTier;
        }

        lastGrantedDungeonRewards.Add(new DungeonRewardEntry
        {
            visualType = DungeonRewardVisualType.Equipment,
            itemId = equipmentItemId,
            equipmentType = equipmentType,
            equipmentTier = equipmentTier,
            amount = BigDouble.Zero
        });
    }

    private static int RollEquipmentDungeonRewardItemId(EidosTreasureVaultRewardTable rewardTable)
    {
        if (rewardTable == null)
            return 0;

        int rewardTier = RollEquipmentDungeonRewardTier(rewardTable);
        EquipmentType rewardType = DungeonRewardEquipmentTypes[Random.Range(0, DungeonRewardEquipmentTypes.Length)];
        return FindEquipmentIdByTypeAndTier(rewardType, rewardTier);
    }

    private static int RollEquipmentDungeonRewardTier(EidosTreasureVaultRewardTable rewardTable)
    {
        int[] tierWeights =
        {
            Mathf.Max(0, rewardTable.EquipmentTierWeight01),
            Mathf.Max(0, rewardTable.EquipmentTierWeight02),
            Mathf.Max(0, rewardTable.EquipmentTierWeight03),
            Mathf.Max(0, rewardTable.EquipmentTierWeight04),
            Mathf.Max(0, rewardTable.EquipmentTierWeight05)
        };

        int totalWeight = 0;
        for (int i = 0; i < tierWeights.Length; i++)
            totalWeight += tierWeights[i];

        if (totalWeight <= 0)
            return ResolveExistingEquipmentTier(rewardTable.BaseEquipmentTier);

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < tierWeights.Length; i++)
        {
            if (roll < tierWeights[i])
                return ResolveExistingEquipmentTier(rewardTable.BaseEquipmentTier + i);

            roll -= tierWeights[i];
        }

        return ResolveExistingEquipmentTier(rewardTable.BaseEquipmentTier);
    }

    private static int ResolveDungeonSummaryEquipmentTier(StageType stageType)
    {
        if (!TryGetDungeonRewardStageLevel(stageType, 1, out int stageLevel))
            return 1;

        if (!TryGetEidosTreasureVaultReward(stageLevel, out EidosTreasureVaultRewardTable rewardTable) || rewardTable == null)
            return 1;

        return ResolveExistingEquipmentTier(rewardTable.BaseEquipmentTier);
    }

    private static int FindPreviewEquipmentId(int equipmentTier, EquipmentType equipmentType = 0)
    {
        EquipmentType previewType = equipmentType != 0 ? equipmentType : EquipmentType.Weapon;
        return FindEquipmentIdByTypeAndTier(previewType, equipmentTier);
    }

    private static int FindEquipmentIdByTypeAndTier(EquipmentType equipmentType, int equipmentTier)
    {
        if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return 0;

        int resolvedTier = ResolveExistingEquipmentTier(equipmentTier);

        foreach (EquipListTable equipInfo in DataManager.Instance.EquipListDict.Values)
        {
            if (equipInfo == null)
                continue;

            if (equipInfo.equipmentType == equipmentType && equipInfo.equipmentTier == resolvedTier)
                return equipInfo.ID;
        }

        foreach (EquipListTable equipInfo in DataManager.Instance.EquipListDict.Values)
        {
            if (equipInfo == null)
                continue;

            if (equipInfo.equipmentTier == resolvedTier)
                return equipInfo.ID;
        }

        return 0;
    }

    private static int ResolveExistingEquipmentTier(int requestedTier)
    {
        if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return Mathf.Max(1, requestedTier);

        int bestLowerTier = int.MinValue;
        int bestHigherTier = int.MaxValue;

        foreach (EquipListTable equipInfo in DataManager.Instance.EquipListDict.Values)
        {
            if (equipInfo == null)
                continue;

            int currentTier = equipInfo.equipmentTier;
            if (currentTier == requestedTier)
                return currentTier;

            if (currentTier < requestedTier && currentTier > bestLowerTier)
                bestLowerTier = currentTier;

            if (currentTier > requestedTier && currentTier < bestHigherTier)
                bestHigherTier = currentTier;
        }

        if (bestLowerTier != int.MinValue)
            return bestLowerTier;

        if (bestHigherTier != int.MaxValue)
            return bestHigherTier;

        return Mathf.Max(1, requestedTier);
    }

    private static bool TryGetGuardianTaxVaultReward(int stageLevel, out GuardianTaxVaultRewardTable rewardTable)
    {
        rewardTable = null;
        if (DataManager.Instance?.GuardianTaxVaultRewardDict == null)
            return false;

        foreach (GuardianTaxVaultRewardTable entry in DataManager.Instance.GuardianTaxVaultRewardDict.Values)
        {
            if (entry != null && entry.stageLevel == stageLevel)
            {
                rewardTable = entry;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetHallOfTrainingReward(int stageLevel, out HallOfTrainingRewardTable rewardTable)
    {
        rewardTable = null;
        if (DataManager.Instance?.HallOfTrainingRewardDict == null)
            return false;

        foreach (HallOfTrainingRewardTable entry in DataManager.Instance.HallOfTrainingRewardDict.Values)
        {
            if (entry != null && entry.stageLevel == stageLevel)
            {
                rewardTable = entry;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetCelestiAlchemyWorkshopReward(int stageLevel, out CelestiAlchemyWorkshopRewardTable rewardTable)
    {
        rewardTable = null;
        if (DataManager.Instance?.CelestiAlchemyWorkshopRewardDict == null)
            return false;

        foreach (CelestiAlchemyWorkshopRewardTable entry in DataManager.Instance.CelestiAlchemyWorkshopRewardDict.Values)
        {
            if (entry != null && entry.stageLevel == stageLevel)
            {
                rewardTable = entry;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetEidosTreasureVaultReward(int stageLevel, out EidosTreasureVaultRewardTable rewardTable)
    {
        rewardTable = null;
        if (DataManager.Instance?.EidosTreasureVaultRewardDict == null)
            return false;

        foreach (EidosTreasureVaultRewardTable entry in DataManager.Instance.EidosTreasureVaultRewardDict.Values)
        {
            if (entry != null && entry.stageLevel == stageLevel)
            {
                rewardTable = entry;
                return true;
            }
        }

        return false;
    }

    private static ItemDropSettings CreateDefaultItemDropSettings()
    {
        ItemDropSettings settings = ScriptableObject.CreateInstance<ItemDropSettings>();
        settings.equipmentChance = 0.05f;
        settings.pixieFragmentChance = 0.0001f;
        settings.skillScrollChance = 0.00005f;
        settings.skillGemChance = 0.00001f;
        settings.dungeonTicketChance = 0.00001f;
        settings.stageGap = 3;
        settings.startIP = 100;
        settings.offsetTable = new ItemDropSettings.EquipmentOffsetEntry[]
        {
            new() { offset = 0, weight = 800 },
            new() { offset = 100, weight = 150 },
            new() { offset = 200, weight = 40 },
            new() { offset = 300, weight = 10 }
        };
        settings.equipmentIds = System.Array.Empty<int>();
        settings.pixieFragmentIds = new[] { 3310001 };
        settings.skillScrollIds = new[] { 3210001 };
        settings.skillGemIds = new[] { 3220001 };
        settings.dungeonTicketIds = new[] { 3831001 };
        return settings;
    }
}
