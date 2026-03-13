using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 처치 시 보상 계산. 아이템은 Resources/ItemDrop 프리팹 스폰 → 3초 후 끌어당김 → 회수 시 이벤트 발송.
/// </summary>
public static class EnemyKillRewardDispatcher
{
    private const string DropItemPrefabPath = "ItemDrop";

    public static event Action<int> OnGoldEarned;
    public static event Action<int> OnExpEarned;
    public static event Action<string, int> OnItemDropped;
    public static event Action<string, int, int> OnEquipmentDropped;
    public static event Action<int> OnBerserkerOrbEarned;
    public static event Action OnBossKilled;
    public static event Action OnNormalEnemyKilled;

    /// <summary>전역 몬스터 처치 카운트 (일반 몬스터만).</summary>
    public static int TotalKillCount { get; private set; }

    /// <summary>처치 카운트 변경 시 (새 총합).</summary>
    public static event Action<int> OnKillCountChanged;

    private static readonly List<ItemDropLogic.ItemDropResult> _itemDropBuffer = new List<ItemDropLogic.ItemDropResult>();

    /// <summary>현재 스테이지 레벨. StageManager 등에서 설정.</summary>
    public static int CurrentStageLevel { get; set; } = 1;

    /// <summary>드랍 회수 시 호출 (DropItemController에서 사용).</summary>
    public static void RaiseItemCollected(int itemId, int count, bool isEquipment)
    {
        var idStr = itemId.ToString();
        if (isEquipment)
            OnEquipmentDropped?.Invoke(idStr, count, 0);
        else
            OnItemDropped?.Invoke(idStr, count);
    }

    /// <summary>아이템 획득 시 디버그 로그 (프리팹 없이 이벤트만 발송할 때 사용).</summary>
    internal static void LogItemAcquired(int itemId, int count, ItemDropLogic.ItemCategory category)
    {
        string name = null;
        if (DataManager.Instance != null && DataManager.Instance.DataLoad)
        {
            if (category == ItemDropLogic.ItemCategory.Equipment && DataManager.Instance.EquipListDict?.TryGetValue(itemId, out var e) == true)
                name = e.equipmentName;
            else if (DataManager.Instance.ItemInfoDict?.TryGetValue(itemId, out var i) == true)
                name = i.itemName;
        }

    }

    /// <summary>전역 처치 카운트 초기화 (씬 전환 등에서 호출).</summary>
    public static void ResetKillCount()
    {
        TotalKillCount = 0;
        OnKillCountChanged?.Invoke(0);
    }

    /// <summary>한 번에 골드·경험치·아이템 계산 후 이벤트 발송. 골드/경험치는 StageManager.SetReward()에서 세팅한 값 사용.</summary>
    public static void GrantRewards(EnemyRewardData rewardData, bool isBoss = false, int stageLevel = -1, Vector3 worldPosition = default)
    {
        if (rewardData == null) return;

        int stage = stageLevel >= 1 ? stageLevel : CurrentStageLevel;

        // 골드: RewardManager.DropSettings.dropGold (StageManager.SetReward → SetDropTable에서 세팅)
        var dropSettings = RewardManager.Instance?.DropSettings;
        if (dropSettings != null)
        {
            var baseGold = dropSettings.dropGold;
            if (baseGold > 0)
            {
                var goldMult = 1.0 + (double)CharacterStatManager.Instance.FinalStats[StatType.GOLD_GAIN].finalStat;
                var finalGold = baseGold * goldMult;
                //var currencyModule = InventoryManager.Instance != null
                //    ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
                //    : null;
                //currencyModule?.AddCurrency(CurrencyType.Gold, finalGold);
                InventoryManager.Instance.AddItem(TypeToId.ConvertTypeToId(ItemType.FreeCurrency), finalGold);

            }
        }

        // 경험치: StageManager가 expBase에 세팅한 값 사용
        int exp = rewardData.expBase;
        if (StageManager.Instance != null)
        {
            var reward = isBoss ? StageManager.Instance.bossEnemyReward : StageManager.Instance.normalEnemyReward;
            if (reward != null) exp = reward.expBase;
        }
        if (exp > 0)
        {
            var finalExp = new BigDouble(exp * (1 + CharacterStatManager.Instance.FinalStats[StatType.EXP_GAIN].finalStat));
            var currencyModule = InventoryManager.Instance != null
                ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
                : null;
            currencyModule?.AddCurrency(CurrencyType.Exp, finalExp);



            //경험치 저장
            InventoryManager.Instance.saveCurrencyData.Save(CurrencyType.Exp,
                currencyModule.GetAmount(CurrencyType.Exp));
        }

        // 아이템: RewardManager를 통해서만 ItemDropSettings 사용
        if (dropSettings != null)
        {
            ItemDropLogic.RollAll(dropSettings, stage, isBoss, _itemDropBuffer);
            var prefab = Resources.Load<GameObject>(DropItemPrefabPath);
            foreach (var drop in _itemDropBuffer)
            {
            if (prefab != null)
            {
                    var go = UnityEngine.Object.Instantiate(prefab, worldPosition + Vector3.up * 0.5f, Quaternion.identity);
                    // 아이템 드랍 이펙트 추가 예정 (드랍 시 파티클 등)
                    // 아이템 드랍 효과음 추가 예정
                    var ctrl = go.GetComponent<DropItemController>();
                    if (ctrl == null) ctrl = go.AddComponent<DropItemController>();
                    ctrl.Initialize(drop.itemId, drop.count, drop.category);

                }
                else
                {
                    // 프리팹 없으면 기존처럼 이벤트만 발송 (PlayerData 구독)
                    LogItemAcquired(drop.itemId, drop.count, drop.category);
                    if (drop.category == ItemDropLogic.ItemCategory.Equipment)
                        OnEquipmentDropped?.Invoke(drop.itemId.ToString(), drop.count, 0);
                    else
                        OnItemDropped?.Invoke(drop.itemId.ToString(), drop.count);
                }
            }
        }

        // 버서커 오브: 일반 1개, 보스 10개 (PlayerBerserkerOrb 구독). 버서커 모드 중에는 수집 안 함.
        if (BerserkerModeController.Instance == null || !BerserkerModeController.Instance.IsActive)
        {
            int berserkerOrb = isBoss ? PlayerBerserkerOrb.BossBerserkerOrb : PlayerBerserkerOrb.NormalBerserkerOrb;
            OnBerserkerOrbEarned?.Invoke(berserkerOrb);
        }

        if (isBoss)
        {
            CurrentStageLevel++;
            OnBossKilled?.Invoke();

            // CurrentStageLevel은 임시로 사용중입니다
        }
        else
        {
            // 전역 몬스터 처치 카운트 (일반 몬스터만)
            TotalKillCount++;
            OnKillCountChanged?.Invoke(TotalKillCount);
            OnNormalEnemyKilled?.Invoke();

            GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questElimination, 1); //몬스터 죽었을 때 호출
        }
    }

    public static void TotalKillCountUp(int count)
    {
        TotalKillCount = count;
        OnKillCountChanged?.Invoke(count);
        OnNormalEnemyKilled?.Invoke();
    }
}