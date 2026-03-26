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
    private readonly List<RewardManager.DungeonRewardEntry> rewardPreviewBuffer = new List<RewardManager.DungeonRewardEntry>();

    private bool isBuilt;
    private StageType selectedStageType = StageType.None;
    private DungeonLevelPopupUI enterPopup;

    private sealed class DungeonViewData
    {
        public StageType stageType;
        public string dungeonName;
    }

    public StageType SelectedStageType => selectedStageType;
    public int RequiredKeyCount => Mathf.Max(1, requiredKeyCount);

    protected override void Initialize()
    {
        PrepareEnterPopup();
        HideEnterPopup();
    }

    private void Update()
    {
        if (!isBuilt)
            RefreshView();
    }

    protected override void Subscribe()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAmountChanged += OnItemAmountChanged;
        GameEventManager.OnStageChanged += OnStageChanged;
    }

    protected override void Unsubscribe()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAmountChanged -= OnItemAmountChanged;
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
                dungeonName = GetDungeonName(OrderedDungeonTypes[i], 1)
            };
            dungeonViews.Add(data);
        }
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
        int requiredCount = Mathf.Max(1, requiredKeyCount);
        bool isDungeonInProgress = IsDungeonInProgress();

        if (isDungeonInProgress)
            HideEnterPopup();

        for (int i = 0; i < dungeonViews.Count; i++)
        {
            DungeonViewData viewData = dungeonViews[i];
            if (!itemsByType.TryGetValue(viewData.stageType, out DungeonContentUI item))
                continue;

            bool firstStageUnlocked = IsFirstStageUnlocked(viewData.stageType);
            BigDouble currentKeyAmount = CheckDungeon.GetTicketAmount(viewData.stageType, 1);
            item.SetLocked(!firstStageUnlocked);
            item.SetNeededKeyState(currentKeyAmount, requiredCount, enoughKeyColor, notEnoughKeyColor);
            item.SetEnterInteractable(CheckDungeon.CanEnter(viewData.stageType, 1, requiredCount));

            rewardPreviewBuffer.Clear();
            RewardManager.Instance.TryGetDungeonRewardSummary(viewData.stageType, rewardPreviewBuffer);
            item.RebuildRewards(rewardPreviewBuffer, LoadRewardIcon, false);
        }
    }

    private void OnClickEnter(StageType stageType)
    {
        if (IsDungeonInProgress())
        {
            InstanceMessageManager.TryShowDungeonInProgress();
            return;
        }

        if (!CheckDungeon.CanEnter(stageType, 1, RequiredKeyCount))
            return;

        selectedStageType = stageType;

        PrepareEnterPopup();
        if (enterPopup != null)
        {
            enterPopup.Show(
                stageType,
                GetDungeonName(stageType, 1),
                RequiredKeyCount);
            return;
        }

        if (enterPopupObject != null)
            enterPopupObject.SetActive(true);
    }

    private void OnItemAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemType != ItemType.Key)
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

    private void PrepareEnterPopup()
    {
        if (enterPopupObject == null)
            return;

        if (enterPopup == null)
            enterPopup = enterPopupObject.GetComponent<DungeonLevelPopupUI>();

        if (enterPopup == null)
        {
            Debug.LogWarning("[DungeonUIController] DungeonLevelPopupUI component is missing on the assigned enterPopupObject.");
            return;
        }

        enterPopup.BindHost(this);
    }

    private void HideEnterPopup()
    {
        if (enterPopup != null)
            enterPopup.Hide();
        else if (enterPopupObject != null)
            enterPopupObject.SetActive(false);
    }

    public void ResetForSceneChange()
    {
        selectedStageType = StageType.None;
        HideEnterPopup();
    }

    internal static string GetDungeonName(StageType stageType, int dungeonLevel = 1)
    {
        string tableName = GetDungeonNameFromTable(stageType, dungeonLevel);
        if (!string.IsNullOrWhiteSpace(tableName))
            return tableName;

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

    private static string GetDungeonNameFromTable(StageType stageType, int dungeonLevel)
    {
        if (DataManager.Instance == null ||
            !DataManager.Instance.DataLoad ||
            DataManager.Instance.StageManageDict == null)
        {
            return null;
        }

        if (!CheckDungeon.TryGetDungeonReq(stageType, dungeonLevel, out int dungeonId, out _))
            return null;

        if (!DataManager.Instance.StageManageDict.TryGetValue(dungeonId, out StageManageTable stageData) ||
            stageData == null ||
            string.IsNullOrWhiteSpace(stageData.stageName))
        {
            return null;
        }

        return stageData.stageName.Trim();
    }

    internal static Sprite LoadRewardIcon(RewardManager.DungeonRewardEntry reward)
    {
        return RewardManager.Instance != null
            ? RewardManager.Instance.ResolveDungeonRewardIcon(reward)
            : null;
    }

    private static bool IsFirstStageUnlocked(StageType stageType)
    {
        if (StageManager.Instance == null)
            return false;

        if (DataManager.Instance == null || DataManager.Instance.DungeonReqDict == null)
            return false;

        return CheckDungeon.HasDungeonAccess(stageType, 1);
    }

    private static bool IsDungeonInProgress()
    {
        return StageManager.Instance != null && StageManager.Instance.IsDungeonInProgress;
    }
}
