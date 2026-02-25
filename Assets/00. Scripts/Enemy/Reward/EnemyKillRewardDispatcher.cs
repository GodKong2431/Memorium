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
        Debug.Log($"[아이템 획득] ID={itemId} x{count} ({category}) {(string.IsNullOrEmpty(name) ? "" : $"- {name}")}");
    }

    /// <summary>전역 처치 카운트 초기화 (씬 전환 등에서 호출).</summary>
    public static void ResetKillCount()
    {
        TotalKillCount = 0;
        OnKillCountChanged?.Invoke(0);
    }

    /// <summary>한 번에 골드·경험치·아이템 계산 후 이벤트 발송.</summary>
    public static void GrantRewards(EnemyRewardData rewardData, bool isBoss = false, int stageLevel = -1, Vector3 worldPosition = default)
    {
        if (rewardData == null) return;

        int stage = stageLevel >= 1 ? stageLevel : CurrentStageLevel;

        // 골드 100%
        int gold = EnemyRewardCalculator.CalculateGold(rewardData, stage);
        if (gold > 0)
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, gold);
            OnGoldEarned?.Invoke(gold);
            Debug.Log($"[EnemyKillRewardDispatcher] 골드 +{gold}");
        }

        // 경험치 (스테이지에서 계산된 expBase를 그대로 사용)
        int exp = rewardData.expBase;
        if (exp > 0)
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCurrency(CurrencyType.Exp, exp);
            OnExpEarned?.Invoke(exp);
            Debug.Log($"[EnemyKillRewardDispatcher] 경험치 +{exp}");
        }

        // 아이템: 카테고리별 독립 확률 규칙 (장비·수호요정·스킬주문서·스킬잼·던전입장권)
        var dropSettings = ItemDropSettings.Instance ?? Resources.Load<ItemDropSettings>("ItemDropSettings")
            ?? CreateDefaultItemDropSettings();
        if (dropSettings != null)
        {
            ItemDropLogic.RollAll(dropSettings, stage, isBoss, _itemDropBuffer);
            var prefab = Resources.Load<GameObject>(DropItemPrefabPath);
            foreach (var drop in _itemDropBuffer)
            {
                if (prefab != null)
                {
                    var go = UnityEngine.Object.Instantiate(prefab, worldPosition + Vector3.up * 0.5f, Quaternion.identity);
                    var ctrl = go.GetComponent<DropItemController>();
                    if (ctrl == null) ctrl = go.AddComponent<DropItemController>();
                    ctrl.Initialize(drop.itemId, drop.count, drop.category);
                    Debug.Log($"[EnemyKillRewardDispatcher] 드랍 스폰: itemId={drop.itemId} x{drop.count}");
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

        // 버서커 오브: 일반 1개, 보스 10개
        int berserkerOrb = isBoss ? 10 : 1;
        OnBerserkerOrbEarned?.Invoke(berserkerOrb);
        Debug.Log($"[EnemyKillRewardDispatcher] 버서커 오브 +{berserkerOrb}");

        if (isBoss)
        {
            CurrentStageLevel++;
            OnBossKilled?.Invoke();
            Debug.Log($"[EnemyKillRewardDispatcher] 보스 처치! 스테이지 레벨 → {CurrentStageLevel}");
            // CurrentStageLevel은 임시로 사용중입니다
        }
        else
        {
            // 전역 몬스터 처치 카운트 (일반 몬스터만)
            //TotalKillCount++;
            //OnKillCountChanged?.Invoke(TotalKillCount);
            //OnNormalEnemyKilled?.Invoke();

            TotalKillCountUp(TotalKillCount++);

            GameEventManager.OnQuestActionUpdated?.Invoke(QuestType.questElimination, 1); //몬스터 죽었을 때 호출
        }
    }

    public static void TotalKillCountUp(int count)
    {
        TotalKillCount = count;
        OnKillCountChanged?.Invoke(TotalKillCount);
        OnNormalEnemyKilled?.Invoke();
    }

    private static ItemDropSettings CreateDefaultItemDropSettings()
    {
        var s = ScriptableObject.CreateInstance<ItemDropSettings>();
        s.equipmentChance = 0.05f;
        s.fairyShardChance = 0.0001f;
        s.skillScrollChance = 0.00005f;
        s.skillGemChance = 0.00001f;
        s.dungeonTicketChance = 0.00001f;
        s.stageGap = 3;
        s.startIP = 100;
        s.offsetTable = new ItemDropSettings.EquipmentOffsetEntry[]
        {
            new() { offset = 0, weight = 800 },
            new() { offset = 100, weight = 150 },
            new() { offset = 200, weight = 40 },
            new() { offset = 300, weight = 10 }
        };
        s.equipmentIds = Array.Empty<int>();
        s.fairyShardIds = new[] { 3310001 };
        s.skillScrollIds = new[] { 3210001 };
        s.skillGemIds = new[] { 3220001 };
        s.dungeonTicketIds = new[] { 3831001 };
        return s;
    }
}
