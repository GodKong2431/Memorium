using System.Linq;
using UnityEngine;

/// <summary>
/// 보상·아이템 드랍 전역 관리. ItemDropSettings 인스턴스 생성 및 스테이지별 드롭테이블 적용.
/// StageManager.SetReward()에서 SetDropTable 호출. EnemyKillRewardDispatcher는 RewardManager.DropSettings 사용.
/// ItemDropSettings는 RewardManager를 통해서만 접근.
/// </summary>
//[DefaultExecutionOrder(-100)]
public class RewardManager : Singleton<RewardManager>
{
    private const string ItemDropSettingsResourcePath = "ItemDropSettings";

    /// <summary>현재 사용 중인 ItemDropSettings. 항상 Awake에서 초기화됨. (Resources 폴더에 있는 ItemDropSettings을 사용합니다)</summary>
    public ItemDropSettings DropSettings { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        EnsureItemDropSettings();
    }

    /// <summary>ItemDropSettings 인스턴스 생성/로드. RewardManager만 사용.</summary>
    private void EnsureItemDropSettings()
    {
        var loaded = Resources.Load<ItemDropSettings>(ItemDropSettingsResourcePath);
        if (loaded != null)
        {
            DropSettings = loaded;
            return;
        }

        DropSettings = CreateDefaultItemDropSettings(); // 없을시 만드는 건데 나중엔 빼지 않을까 싶네요
        Debug.Log("[RewardManager] ItemDropSettings를 기본값으로 생성했습니다.");
    }

    /// <summary>스테이지별 드롭테이블 적용</summary>
    public void SetDropTable(ItemDropTable dropTable)
    {
        if (dropTable == null)
        {
            Debug.LogWarning("[RewardManager] dropTable이 null입니다.");
            return;
        }

        if (DropSettings == null)
            EnsureItemDropSettings();

        if (DropSettings == null) return;

        var equipmentDropTable = DataManager.Instance.EquipmentDropDict[dropTable.equipmentDropID];
        DropSettings.equipmentDropRate.Clear();
        DropSettings.dropGold = dropTable.dropGold;
        DropSettings.baseEquipmentTier = equipmentDropTable.BaseEquipmentTier;
        int fullRate = equipmentDropTable.EquipmentTierWeight01 + equipmentDropTable.EquipmentTierWeight02
            + equipmentDropTable.EquipmentTierWeight03 + equipmentDropTable.EquipmentTierWeight04;
        if (fullRate > 0)
        {
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight01 / fullRate);
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight02 / fullRate);
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight03 / fullRate);
            DropSettings.equipmentDropRate.Add((float)equipmentDropTable.EquipmentTierWeight04 / fullRate);
        }
        // ItemDropTable CSV 확률은 % 단위 (5=5%, 0.01=0.01%) → 0~1로 변환
        DropSettings.equipmentChance = (float)(dropTable.equipmentRate / 100.0);
        DropSettings.pixieFragmentChance = (float)(dropTable.fairyPieceRate / 100.0);
        DropSettings.skillScrollChance = (float)(dropTable.scrollRate / 100.0 * 20000);
        DropSettings.skillGemChance = (float)(dropTable.gemRate / 100.0);
        DropSettings.dungeonTicketChance = (float)(dropTable.keyRate / 100.0) * 20000; // 테스트를 위해 임시로 확률 증가
    }

    /// <summary>
    /// 아이템 ID와 수량을 받아 타입별로 인벤토리/재화에 보상 지급
    /// (임시로 구현해봤는데 뭔가 썩 마음에 들진 않네요)
    /// </summary>
    /// <param name="itemId">EquipListDict 또는 ItemInfoDict의 ID</param>
    /// <param name="count">지급할 수량</param>
    /// <returns>지급 성공 여부</returns>
    public bool GrantReward(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0)
        {
            Debug.LogWarning($"[RewardManager] 잘못된 파라미터: itemId={itemId}, count={count}");
            return false;
        }

        // 장비: EquipListDict에 있으면 PlayerInventory에 추가
        if (DataManager.Instance?.EquipListDict != null && DataManager.Instance.EquipListDict.ContainsKey(itemId))
        {
            var inv = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            if (inv != null)
            {
                inv.IncreaseEquipment(itemId, count);
                Debug.Log($"[RewardManager] 보상 지급: 장비 ID={itemId} x{count}");
                return true;
            }
            Debug.LogWarning($"[RewardManager] PlayerInventory를 찾을 수 없어 장비 보상을 지급할 수 없습니다.");
            return false;
        }

        // 아이템: ItemInfoDict에서 조회
        if (DataManager.Instance?.ItemInfoDict == null || !DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out var itemInfo))
        {
            Debug.LogError($"[RewardManager] 아이템 ID {itemId}를 찾을 수 없습니다.");
            return false;
        }

        int itemType = (int)itemInfo.itemType;

        // 재화 타입 (Gold=81, Crystal=82, DungeonTicket=83, PixieFragment=31)
        if (itemType == (int)ItemType.FreeCurrency || itemType == (int)ItemType.PaidCurrency ||
            itemType == (int)ItemType.Key || itemType == (int)ItemType.PixiePiece)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrency((CurrencyType)itemType, count);
                Debug.Log($"[RewardManager] 보상 지급: 재화 ID={itemId} x{count}");
                return true;
            }
            return false;
        }

        // 스킬 주문서
        if (itemInfo.itemType == ItemType.SkillScroll)
        {
            if (SkillInventoryManager.Instance != null &&
                SkillInventoryManager.Instance.skillScrollIdToSkillIdDict != null)
            {
                var scrollValues = SkillInventoryManager.Instance.skillScrollIdToSkillIdDict.Values.ToList();
                if (scrollValues.Count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        int skillId = scrollValues[Random.Range(0, scrollValues.Count)];
                        SkillInventoryManager.Instance.AddSkill(skillId);
                    }
                    Debug.Log($"[RewardManager] 보상 지급: 스킬 주문서 x{count}");
                    return true;
                }
            }
            return false;
        }

        Debug.LogWarning($"[RewardManager] 미지원 아이템 타입: ID={itemId}, itemType={itemType}");
        return false;
    }

    private static ItemDropSettings CreateDefaultItemDropSettings()
    {
        var s = ScriptableObject.CreateInstance<ItemDropSettings>();
        s.equipmentChance = 0.05f;
        s.pixieFragmentChance = 0.0001f;
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
        s.equipmentIds = System.Array.Empty<int>();
        s.pixieFragmentIds = new[] { 3310001 };
        s.skillScrollIds = new[] { 3210001 };
        s.skillGemIds = new[] { 3220001 };
        s.dungeonTicketIds = new[] { 3831001 };
        return s;
    }
}
