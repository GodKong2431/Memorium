using UnityEngine;

/// <summary>
/// 蹂댁긽쨌?꾩씠???쒕엻 ?꾩뿭 愿由? ItemDropSettings ?몄뒪?댁뒪 ?앹꽦 諛??ㅽ뀒?댁?蹂??쒕∼?뚯씠釉??곸슜.
/// StageManager.SetReward()?먯꽌 SetDropTable ?몄텧. EnemyKillRewardDispatcher??RewardManager.DropSettings ?ъ슜.
/// ItemDropSettings??RewardManager瑜??듯빐?쒕쭔 ?묎렐.
/// </summary>
//[DefaultExecutionOrder(-100)]
public class RewardManager : Singleton<RewardManager>
{
    private const string ItemDropSettingsResourcePath = "ItemDropSettings";

    /// <summary>?꾩옱 ?ъ슜 以묒씤 ItemDropSettings. ??긽 Awake?먯꽌 珥덇린?붾맖. (Resources ?대뜑???덈뒗 ItemDropSettings???ъ슜?⑸땲??</summary>
    public ItemDropSettings DropSettings { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        EnsureItemDropSettings();
    }

    /// <summary>ItemDropSettings ?몄뒪?댁뒪 ?앹꽦/濡쒕뱶. RewardManager留??ъ슜.</summary>
    private void EnsureItemDropSettings()
    {
        var loaded = Resources.Load<ItemDropSettings>(ItemDropSettingsResourcePath);
        if (loaded != null)
        {
            DropSettings = loaded;
            return;
        }

        DropSettings = CreateDefaultItemDropSettings(); // ?놁쓣??留뚮뱶??嫄대뜲 ?섏쨷??鍮쇱? ?딆쓣源??띕꽕??
        Debug.Log("[RewardManager] ItemDropSettings瑜?湲곕낯媛믪쑝濡??앹꽦?덉뒿?덈떎.");
    }

    /// <summary>?ㅽ뀒?댁?蹂??쒕∼?뚯씠釉??곸슜</summary>
    public void SetDropTable(ItemDropTable dropTable)
    {
        if (dropTable == null)
        {
            Debug.LogWarning("[RewardManager] dropTable??null?낅땲??");
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
        // ItemDropTable CSV ?뺣쪧? % ?⑥쐞 (5=5%, 0.01=0.01%) ??0~1濡?蹂??
        DropSettings.equipmentChance = (float)(dropTable.equipmentRate / 100.0);
        DropSettings.pixieFragmentChance = (float)(dropTable.fairyPieceRate / 100.0);
        DropSettings.skillScrollChance = (float)(dropTable.scrollRate / 100.0 * 20000);
        DropSettings.skillGemChance = (float)(dropTable.gemRate / 100.0);
        DropSettings.dungeonTicketChance = (float)(dropTable.keyRate / 100.0) * 20000; // ?뚯뒪?몃? ?꾪빐 ?꾩떆濡??뺣쪧 利앷?
    }

    /// <summary>
    /// ?꾩씠??ID? ?섎웾??諛쏆븘 ??낅퀎濡??몃깽?좊━/?ы솕??蹂댁긽 吏湲?
    /// (?꾩떆濡?援ы쁽?대뇬?붾뜲 萸붽? ??留덉쓬???ㅼ쭊 ?딅꽕??
    /// </summary>
    /// <param name="itemId">EquipListDict ?먮뒗 ItemInfoDict??ID</param>
    /// <param name="count">吏湲됲븷 ?섎웾</param>
    /// <returns>吏湲??깃났 ?щ?</returns>
    public bool GrantReward(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0)
        {
            Debug.LogWarning($"[RewardManager] ?섎せ???뚮씪誘명꽣: itemId={itemId}, count={count}");
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



