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
        DropSettings.fairyShardChance = (float)(dropTable.fairyPieceRate / 100.0);
        DropSettings.skillScrollChance = (float)(dropTable.scrollRate / 100.0);
        DropSettings.skillGemChance = (float)(dropTable.gemRate / 100.0);
        DropSettings.dungeonTicketChance = (float)(dropTable.keyRate / 100.0);
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
        s.equipmentIds = System.Array.Empty<int>();
        s.fairyShardIds = new[] { 3310001 };
        s.skillScrollIds = new[] { 3210001 };
        s.skillGemIds = new[] { 3220001 };
        s.dungeonTicketIds = new[] { 3831001 };
        return s;
    }
}
