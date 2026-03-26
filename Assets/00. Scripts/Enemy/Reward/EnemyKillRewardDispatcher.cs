using System;
using System.Collections.Generic;
using UnityEngine;

public static class EnemyKillRewardDispatcher
{
    private const string DropItemPrefabPath = "ItemDrop";

    public static event Action<int> OnGoldEarned;
    public static event Action<int> OnExpEarned;
    public static event Action<string, int> OnItemDropped;
    public static event Action<string, int, int> OnEquipmentDropped;
    public static event Action<Vector3, int> OnBerserkerOrb;
    public static event Action OnBossKilled;
    public static event Action OnNormalEnemyKilled;
    public static event Action<int> OnKillCountChanged;

    private static readonly List<ItemDropLogic.ItemDropResult> ItemDropBuffer = new List<ItemDropLogic.ItemDropResult>();

    public static int TotalKillCount { get; private set; }
    public static int CurrentStageLevel { get; set; } = 1;

    public static void RaiseItemCollected(int itemId, int count, bool isEquipment)
    {
        string idStr = itemId.ToString();
        if (isEquipment)
            OnEquipmentDropped?.Invoke(idStr, count, 0);
        else
            OnItemDropped?.Invoke(idStr, count);
    }

    internal static void LogItemAcquired(int itemId, int count, ItemDropLogic.ItemCategory category)
    {
        string name = null;
        if (DataManager.Instance != null && DataManager.Instance.DataLoad)
        {
            if (category == ItemDropLogic.ItemCategory.Equipment && DataManager.Instance.EquipListDict?.TryGetValue(itemId, out EquipListTable equipment) == true)
                name = equipment.equipmentName;
            else if (DataManager.Instance.ItemInfoDict?.TryGetValue(itemId, out ItemInfoTable itemInfo) == true)
                name = itemInfo.itemName;
        }
    }

    public static void ResetKillCount()
    {
        TotalKillCount = 0;
        OnKillCountChanged?.Invoke(0);
    }

    public static void GrantRewards(EnemyRewardData rewardData, bool isBoss = false, int stageLevel = -1, Vector3 worldPosition = default)
    {
        if (rewardData == null)
            return;

        int stage = stageLevel >= 1 ? stageLevel : CurrentStageLevel;
        ItemDropSettings dropSettings = RewardManager.Instance?.DropSettings;

        if (dropSettings != null)
        {
            BigDouble baseGold = dropSettings.dropGold;
            if (baseGold > 0)
            {
                double goldMultiplier = 1.0 + (double)CharacterStatManager.Instance.FinalStats[StatType.GOLD_GAIN].finalStat;
                BigDouble finalGold = baseGold * goldMultiplier;
                InventoryManager.Instance.AddItem(TypeToId.ConvertTypeToId(ItemType.FreeCurrency), finalGold);
            }
        }

        int exp = rewardData.expBase;
        if (StageManager.Instance != null)
        {
            EnemyRewardData reward = isBoss ? StageManager.Instance.bossEnemyReward : StageManager.Instance.normalEnemyReward;
            if (reward != null)
                exp = reward.expBase;
        }

        if (exp > 0)
        {
            BigDouble finalExp = new BigDouble(exp * (1 + CharacterStatManager.Instance.FinalStats[StatType.EXP_GAIN].finalStat));
            CurrencyInventoryModule currencyModule = InventoryManager.Instance != null
                ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
                : null;
            currencyModule?.AddCurrency(CurrencyType.Exp, finalExp);
            InventoryManager.Instance.saveCurrencyData.Save(CurrencyType.Exp, currencyModule.GetAmount(CurrencyType.Exp));
        }

        if (dropSettings != null)
        {
            ItemDropLogic.RollAll(dropSettings, stage, isBoss, ItemDropBuffer);

            bool hasRareDrop = false;
            for (int i = 0; i < ItemDropBuffer.Count; i++)
            {
                if (!ItemDropBuffer[i].isRareDrop)
                    continue;

                hasRareDrop = true;
                break;
            }

            if (hasRareDrop && SoundManager.Instance != null)
                SoundManager.Instance.PlayUiSfx(UiSoundIds.RareDrop);

            GameObject prefab = Resources.Load<GameObject>(DropItemPrefabPath);
            foreach (ItemDropLogic.ItemDropResult drop in ItemDropBuffer)
            {
                if (prefab != null)
                {
                    GameObject go = UnityEngine.Object.Instantiate(prefab, worldPosition + Vector3.up, Quaternion.identity);
                    DropItemController controller = go.GetComponent<DropItemController>();
                    if (controller == null)
                        controller = go.AddComponent<DropItemController>();

                    controller.Initialize(drop.itemId, drop.count, drop.category);
                    continue;
                }

                LogItemAcquired(drop.itemId, drop.count, drop.category);
                if (drop.category == ItemDropLogic.ItemCategory.Equipment)
                    OnEquipmentDropped?.Invoke(drop.itemId.ToString(), drop.count, 0);
                else
                    OnItemDropped?.Invoke(drop.itemId.ToString(), drop.count);
            }
        }

        if (BerserkerModeController.Instance != null && !BerserkerModeController.Instance.IsActive)
        {
            int berserkerOrb = isBoss ? PlayerBerserkerOrb.BossBerserkerOrb : PlayerBerserkerOrb.NormalBerserkerOrb;
            OnBerserkerOrb?.Invoke(worldPosition + Vector3.up * 0.5f, berserkerOrb);
        }

        if (isBoss)
        {
            CurrentStageLevel++;
            OnBossKilled?.Invoke();
        }
        else
        {
            TotalKillCount++;
            OnKillCountChanged?.Invoke(TotalKillCount);
            OnNormalEnemyKilled?.Invoke();
            GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questElimination, 1);
        }
    }

    public static void TotalKillCountUp(int count)
    {
        TotalKillCount = count;
        OnKillCountChanged?.Invoke(count);
        OnNormalEnemyKilled?.Invoke();
    }
}
