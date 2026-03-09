using System.Collections.Generic;
using UnityEngine;

public class DungeonUIController : UIControllerBase
{
    private static readonly StageType[] OrderedDungeonTypes =
    {
        StageType.GuardianTaxVault,
        StageType.HallOfTraining,
        StageType.CelestiAlchemyWorkshop,
        StageType.EidosTreasureVault
    };

    [Header("List")]
    [SerializeField] private RectTransform dungeonContents;
    [SerializeField] private GameObject dungeonItemPrefab;

    [Header("Key State")]
    [SerializeField] private int requiredKeyCount = 1;
    [SerializeField] private Color enoughKeyColor = Color.white;
    [SerializeField] private Color notEnoughKeyColor = Color.red;

    [Header("Enter Popup")]
    [SerializeField] private GameObject enterPopupObject;

    private readonly List<DungeonViewData> dungeonViews = new List<DungeonViewData>();
    private readonly Dictionary<StageType, DungeonContentUI> itemsByType = new Dictionary<StageType, DungeonContentUI>();

    private bool isBuilt;
    private StageType selectedStageType = StageType.None;

    private sealed class DungeonViewData
    {
        public StageType stageType;
        public string dungeonName;
        public IReadOnlyList<int> rewardItemIds;
    }

    public StageType SelectedStageType => selectedStageType;

    private void Update()
    {
        if (!isBuilt)
            RefreshView();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        GameEventManager.OnStageChanged += OnStageChanged;
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
        GameEventManager.OnStageChanged -= OnStageChanged;
    }

    protected override void RefreshView()
    {
        if (!TryPrepareData())
            return;

        if (!isBuilt)
            BuildDungeonItems();

        RefreshStateOnly();
    }

    private bool TryPrepareData()
    {
        if (dungeonContents == null || dungeonItemPrefab == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.DungeonReqDict == null)
            return false;

        if (dungeonViews.Count == 0)
            BuildDungeonViews();

        return dungeonViews.Count > 0;
    }

    private void BuildDungeonViews()
    {
        dungeonViews.Clear();
        for (int i = 0; i < OrderedDungeonTypes.Length; i++)
        {
            DungeonViewData data = new DungeonViewData
            {
                stageType = OrderedDungeonTypes[i],
                dungeonName = GetDungeonName(OrderedDungeonTypes[i]),
                rewardItemIds = CollectRewardItemIds(OrderedDungeonTypes[i])
            };
            dungeonViews.Add(data);
        }
    }

    private List<int> CollectRewardItemIds(StageType stageType)
    {
        HashSet<int> rewardSet = new HashSet<int>();
        foreach (KeyValuePair<int, DungeonReqTable> pair in DataManager.Instance.DungeonReqDict)
        {
            DungeonReqTable table = pair.Value;
            if (table.stageType != stageType)
                continue;

            if (table.ItemID > 0)
                rewardSet.Add(table.ItemID);
        }

        List<int> rewards = new List<int>(rewardSet);
        rewards.Sort();
        return rewards;
    }

    private void BuildDungeonItems()
    {
        ClearItems();
        for (int i = 0; i < dungeonViews.Count; i++)
        {
            DungeonViewData viewData = dungeonViews[i];
            GameObject itemObject = Instantiate(dungeonItemPrefab, dungeonContents, false);
            DungeonContentUI item = itemObject.GetComponent<DungeonContentUI>();
            if (item == null)
                item = itemObject.AddComponent<DungeonContentUI>();

            itemObject.name = $"(Img)DungeonBackground_{viewData.stageType}";
            item.SetDungeonName(viewData.dungeonName);
            item.RebuildRewards(viewData.rewardItemIds, LoadRewardIcon);

            StageType cachedType = viewData.stageType;
            item.BindEnter(() => OnClickEnter(cachedType));

            itemsByType[cachedType] = item;
        }
        isBuilt = true;
    }

    private void ClearItems()
    {
        itemsByType.Clear();

        for (int i = dungeonContents.childCount - 1; i >= 0; i--)
            Destroy(dungeonContents.GetChild(i).gameObject);
    }

    private void RefreshStateOnly()
    {
        BigDouble currentKeyAmount = GetCurrentDungeonKeyAmount();
        int requiredCount = Mathf.Max(1, requiredKeyCount);
        BigDouble requiredAmount = new BigDouble(requiredCount);
        bool hasEnoughKey = currentKeyAmount >= requiredAmount;

        for (int i = 0; i < dungeonViews.Count; i++)
        {
            DungeonViewData viewData = dungeonViews[i];
            if (!itemsByType.TryGetValue(viewData.stageType, out DungeonContentUI item))
                continue;

            bool firstStageUnlocked = IsFirstStageUnlocked(viewData.stageType);
            item.SetLocked(!firstStageUnlocked);
            item.SetNeededKeyState(currentKeyAmount, requiredCount, enoughKeyColor, notEnoughKeyColor);
            item.SetEnterInteractable(firstStageUnlocked && hasEnoughKey);
        }
    }

    private void OnClickEnter(StageType stageType)
    {
        if (!IsFirstStageUnlocked(stageType))
            return;

        int requiredCount = Mathf.Max(1, requiredKeyCount);
        if (GetCurrentDungeonKeyAmount() < new BigDouble(requiredCount))
            return;

        selectedStageType = stageType;

        if (enterPopupObject != null)
            enterPopupObject.SetActive(true);
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != CurrencyType.DungeonTicket)
            return;

        if (!isBuilt)
            return;

        RefreshStateOnly();
    }

    private void OnStageChanged(int chapter, int stage)
    {
        if (!isBuilt)
            return;

        RefreshStateOnly();
    }

    private static string GetDungeonName(StageType stageType)
    {
        switch (stageType)
        {
            case StageType.GuardianTaxVault:
                return "Guardian Tax Vault";
            case StageType.HallOfTraining:
                return "Hall Of Training";
            case StageType.CelestiAlchemyWorkshop:
                return "Celesti Alchemy Workshop";
            case StageType.EidosTreasureVault:
                return "Eidos Treasure Vault";
            default:
                return stageType.ToString();
        }
    }

    private static Sprite LoadRewardIcon(int itemId)
    {
        if (DataManager.Instance == null || DataManager.Instance.ItemInfoDict == null)
            return null;

        if (!DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out ItemInfoTable itemInfo))
            return null;

        if (string.IsNullOrEmpty(itemInfo.itemIcon))
            return null;

        return Resources.Load<Sprite>(itemInfo.itemIcon);
    }

    private static bool IsFirstStageUnlocked(StageType stageType)
    {
        if (StageManager.Instance == null)
            return false;

        if (DataManager.Instance == null || DataManager.Instance.DungeonReqDict == null)
            return false;

        return CheckDungeon.HasDungeonAccess(stageType, 1);
    }

    private static BigDouble GetCurrentDungeonKeyAmount()
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        if (currencyModule == null)
            return BigDouble.Zero;

        return currencyModule.GetAmount(CurrencyType.DungeonTicket);
    }
}
