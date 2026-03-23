using UnityEngine;

/// <summary>
/// 보상용 ItemDropSettings를 관리하고 현재 스테이지 드랍 테이블을 반영한다.
/// StageManager.SetReward()에서 SetDropTable을 호출하고, EnemyKillRewardDispatcher가 DropSettings를 사용한다.
/// ItemDropSettings 접근은 RewardManager를 통해서만 수행한다.
/// </summary>
//[DefaultExecutionOrder(-100)]
public class RewardManager : Singleton<RewardManager>
{
    private const string ItemDropSettingsResourcePath = "ItemDropSettings";

    /// <summary>현재 사용 중인 ItemDropSettings. 기본적으로 Awake에서 초기화된다.</summary>
    public ItemDropSettings DropSettings { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        EnsureItemDropSettings();
    }

    /// <summary>ItemDropSettings를 로드하고, 없으면 기본값으로 생성한다.</summary>
    private void EnsureItemDropSettings()
    {
        var loaded = Resources.Load<ItemDropSettings>(ItemDropSettingsResourcePath);
        if (loaded != null)
        {
            DropSettings = loaded;
            return;
        }

        DropSettings = CreateDefaultItemDropSettings(); // 리소스가 없을 때만 기본 설정으로 생성

    }

    /// <summary>스테이지별 드랍 테이블을 현재 설정에 반영한다.</summary>
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
        // ItemDropTable CSV 확률은 % 단위이므로 0~1 범위로 변환한다.
        DropSettings.equipmentChance = (float)(dropTable.equipmentRate / 100.0);
        DropSettings.pixieFragmentChance = (float)(dropTable.fairyPieceRate);
        DropSettings.skillScrollChance = (float)(dropTable.scrollRate);
        DropSettings.skillGemChance = (float)(dropTable.gemRate);
        DropSettings.dungeonTicketChance = (float)(dropTable.keyRate);
    }

    /// <summary>
    /// 아이템 ID와 수량을 받아 인벤토리에 보상을 지급한다.
    /// 장비와 재화 모두 InventoryManager를 통해 처리한다.
    /// </summary>
    /// <param name="itemId">EquipListDict 또는 ItemInfoDict 기준 ID</param>
    /// <param name="count">지급할 수량</param>
    /// <returns>지급 성공 여부</returns>
    public bool GrantReward(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0)
        {
            Debug.LogWarning($"[RewardManager] 잘못된 파라미터: itemId={itemId}, count={count}");
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[RewardManager] InventoryManager가 없어 보상을 지급할 수 없습니다.");
            return false;
        }

        bool granted = InventoryManager.Instance.AddItem(itemId, count);
        if (!granted)
            Debug.LogWarning($"[RewardManager] 보상 지급 실패: itemId={itemId}, count={count}");

        return granted;
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


