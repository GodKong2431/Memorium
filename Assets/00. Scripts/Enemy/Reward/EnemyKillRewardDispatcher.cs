using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 처치 시 보상을 계산한 뒤 골드/경험치/아이템 이벤트로 전달.
/// 각 아이템 카테고리 독립 롤, 보스는 5배 확률.
/// </summary>
public static class EnemyKillRewardDispatcher
{
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
            OnGoldEarned?.Invoke(gold);
            Debug.Log($"[EnemyKillRewardDispatcher] 골드 +{gold}");
        }

        // 경험치 (스테이지에서 계산된 expBase를 그대로 사용)
        int exp = rewardData.expBase;
        if (exp > 0)
        {
            OnExpEarned?.Invoke(exp);
            Debug.Log($"[EnemyKillRewardDispatcher] 경험치 +{exp}");
        }

        // 아이템: 카테고리별 독립 확률 규칙 (장비·수호요정·스킬주문서·스킬잼·던전입장권)
        var dropSettings = ItemDropSettings.Instance ?? Resources.Load<ItemDropSettings>("ItemDropSettings")
            ?? CreateDefaultItemDropSettings();
        if (dropSettings != null)
        {
            ItemDropLogic.RollAll(dropSettings, stage, isBoss, _itemDropBuffer);
            foreach (var drop in _itemDropBuffer)
            {
                if (drop.category == ItemDropLogic.ItemCategory.Equipment)
                {
                    OnEquipmentDropped?.Invoke(drop.itemId, drop.count, drop.power);
                    Debug.Log($"[EnemyKillRewardDispatcher] 장비 드랍: {drop.itemId} x{drop.count} (파워 {drop.power})");
                }
                else
                {
                    OnItemDropped?.Invoke(drop.itemId, drop.count);
                    Debug.Log($"[EnemyKillRewardDispatcher] 아이템 드랍: {drop.itemId} x{drop.count}");
                }
            }
        }

        // 버서커 오브: 일반 1개, 보스 10개
        int berserkerOrb = isBoss ? 10 : 1;
        OnBerserkerOrbEarned?.Invoke(berserkerOrb);
        Debug.Log($"[EnemyKillRewardDispatcher] 버서커 오브 +{berserkerOrb}");

        if (isBoss)
        {
            ResetKillCount();
            CurrentStageLevel++;
            OnBossKilled?.Invoke();
            Debug.Log($"[EnemyKillRewardDispatcher] 보스 처치! 스테이지 레벨 → {CurrentStageLevel}");
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
        s.equipmentSlotIds = new[] { "equip_weapon", "equip_armor", "equip_helmet", "equip_boots", "equip_gloves" };
        s.fairyShardIds = new[] { "shard_fairy_01" };
        s.skillScrollIds = new[] { "scroll_skill_01" };
        s.skillGemIds = new[] { "gem_skill_01" };
        s.dungeonTicketIds = new[] { "ticket_dungeon_01" };
        return s;
    }
}
