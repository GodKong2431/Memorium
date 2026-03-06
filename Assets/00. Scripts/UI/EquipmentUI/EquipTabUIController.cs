using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipTabUIController : UIControllerBase
{
    [Header("Tab Config")]
    [SerializeField] private EquipmentType tabType = EquipmentType.Weapon;

    [SerializeField] private RectTransform root;

    [SerializeField] private GameObject tierPrefab;

    [SerializeField] private GameObject itemPrefab;

    [SerializeField] private GameObject starPrefab;

    [SerializeField] private bool clearOnBuild = true;

    [Header("Display")]
    [SerializeField] private int mergeCount = 3;

    [SerializeField] private string levelText = "Lv. 0";

    private readonly Dictionary<int, EquipItemView> views = new Dictionary<int, EquipItemView>();
    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

    private EquipTierListView tierListView;
    private InventoryManager invManager;
    private EquipmentInventoryModule equipModule;
    private Coroutine readyRoutine;
    private bool isBuilt;

    protected override void Initialize()
    {
        tierListView = new EquipTierListView(root, tierPrefab, itemPrefab, clearOnBuild);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartReadyRoutine();
    }

    protected override void OnDisable()
    {
        if (readyRoutine != null)
        {
            StopCoroutine(readyRoutine);
            readyRoutine = null;
        }

        base.OnDisable();
    }

    protected override void Subscribe()
    {
        BindInventory();
        EquipmentHandler.EquipmentUiRefreshRequested += HandleRefreshRequest;
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= HandleRefreshRequest;
        UnbindInventory();
    }

    protected override void RefreshView()
    {
        BindInventory();

        if (!IsReady())
            return;

        BuildIfNeeded();
        RefreshCounts();
        RefreshLockStates();
    }

    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (!views.TryGetValue(item.ItemId, out EquipItemView view))
            return;

        view.RenderCount(ToCount(amount), mergeCount);
        view.SetDimmed(ShouldDim(item.ItemId));
    }

    private void HandleRefreshRequest()
    {
        RefreshView();
    }

    private void ClickItem(int itemId)
    {
        Debug.Log($"[EquipTabUIController] 장비 선택 구현 예정: {itemId}");
    }

    private void BuildIfNeeded()
    {
        if (isBuilt)
            return;

        // 탭별 장비를 티어 단위로 묶어서 1회 생성한다.
        List<EquipListTable> tables = CollectTables();
        if (tables.Count == 0)
            return;

        Dictionary<int, List<EquipListTable>> byTier = new Dictionary<int, List<EquipListTable>>();
        for (int i = 0; i < tables.Count; i++)
        {
            EquipListTable table = tables[i];
            int tier = Mathf.Max(1, table.grade);
            if (!byTier.TryGetValue(tier, out List<EquipListTable> group))
            {
                group = new List<EquipListTable>();
                byTier[tier] = group;
            }

            group.Add(table);
        }

        views.Clear();
        Dictionary<int, EquipItemView> builtViews = tierListView.Build(
            byTier,
            starPrefab,
            GetTierColor,
            GetOrderColor,
            ShouldDim,
            GetStarCount,
            GetLevelText,
            GetIcon,
            ClickItem);

        foreach (KeyValuePair<int, EquipItemView> pair in builtViews)
            views[pair.Key] = pair.Value;

        isBuilt = true;
    }

    private void RefreshCounts()
    {
        // 인벤토리의 최신 수량을 각 셀에 반영한다.
        foreach (KeyValuePair<int, EquipItemView> pair in views)
        {
            BigDouble amount = invManager != null ? invManager.GetItemAmount(pair.Key) : BigDouble.Zero;

            pair.Value.RenderCount(ToCount(amount), mergeCount);
        }
    }

    private void RefreshLockStates()
    {
        // 해금 여부에 따라 잠금 톤/버튼 상태를 맞춘다.
        foreach (KeyValuePair<int, EquipItemView> pair in views)
            pair.Value.SetDimmed(ShouldDim(pair.Key));
    }

    private List<EquipListTable> CollectTables()
    {
        List<EquipListTable> tables = new List<EquipListTable>();
        foreach (KeyValuePair<int, EquipListTable> pair in DataManager.Instance.EquipListDict)
        {
            EquipListTable table = pair.Value;
            if (table.equipmentType != tabType)
                continue;

            tables.Add(table);
        }

        tables.Sort((lhs, rhs) =>
        {
            int tierCompare = lhs.grade.CompareTo(rhs.grade);
            if (tierCompare != 0)
                return tierCompare;

            int rarityCompare = lhs.rarityType.CompareTo(rhs.rarityType);
            if (rarityCompare != 0)
                return rarityCompare;

            return lhs.equipmentTier.CompareTo(rhs.equipmentTier);
        });

        return tables;
    }

    private Sprite GetIcon(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (iconCache.TryGetValue(key, out Sprite cached))
            return cached;

        Sprite icon = Resources.Load<Sprite>(key);
        iconCache[key] = icon;
        return icon;
    }

    private string GetLevelText(int itemId)
    {
        return levelText;
    }

    private Color GetTierColor(int tier)
    {
        return RarityColor.TierColorByTier(tier);
    }

    private static int GetStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
    }

    private Color GetOrderColor(int order)
    {
        return RarityColor.ColorByOrderIndex(order);
    }

    private static int ToCount(BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return 0;

        double value = amount.ToDouble();
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        double floor = Math.Floor(value);
        if (floor >= int.MaxValue)
            return int.MaxValue;

        return floor <= 0d ? 0 : (int)floor;
    }

    private void StartReadyRoutine()
    {
        if (readyRoutine != null)
            StopCoroutine(readyRoutine);

        readyRoutine = StartCoroutine(WaitReady());
    }

    private IEnumerator WaitReady()
    {
        yield return new WaitUntil(IsReady);
        RefreshView();
        readyRoutine = null;
    }

    private bool IsReady()
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;
        if (InventoryManager.Instance == null)
            return false;

        equipModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        return equipModule != null && equipModule.IsInitialized;
    }

    private bool ShouldDim(int itemId)
    {
        return equipModule != null && !equipModule.IsUnlocked(itemId);
    }

    private void BindInventory()
    {
        InventoryManager manager = InventoryManager.Instance;
        if (manager == null || invManager == manager)
            return;

        UnbindInventory();
        invManager = manager;
        invManager.OnItemAmountChanged += HandleAmountChanged;
    }

    private void UnbindInventory()
    {
        if (invManager == null)
            return;

        invManager.OnItemAmountChanged -= HandleAmountChanged;
        invManager = null;
    }
}

