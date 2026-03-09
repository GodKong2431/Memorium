using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipTabUIController : UIControllerBase
{
    [SerializeField] private EquipmentType tabType = EquipmentType.Weapon;
    [SerializeField] private RectTransform root;
    [SerializeField] private GameObject tierPrefab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private int mergeCount = 3;
    [SerializeField] private string levelText = "Lv. 0";

    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

    private readonly Dictionary<int, EquipItemView> views = new Dictionary<int, EquipItemView>();

    private InventoryManager inventory;
    private EquipmentInventoryModule equipModule;
    private bool isBuilt;

    private void Update()
    {
        if (!isBuilt)
            RefreshView();
    }

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        BindInventory();
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        UnbindInventory();
    }

    protected override void RefreshView()
    {
        if (!TryPrepare())
            return;

        if (!isBuilt)
            BuildViews();

        RefreshAllItems();
    }

    private bool TryPrepare()
    {
        if (root == null || tierPrefab == null || itemPrefab == null || starPrefab == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (!BindInventory())
            return false;

        equipModule = inventory.GetModule<EquipmentInventoryModule>();
        return equipModule != null && equipModule.IsInitialized;
    }

    private bool BindInventory()
    {
        InventoryManager current = InventoryManager.Instance;
        if (current == null)
            return false;

        if (inventory == current)
            return true;

        UnbindInventory();
        inventory = current;
        inventory.OnItemAmountChanged += HandleAmountChanged;
        return true;
    }

    private void UnbindInventory()
    {
        if (inventory == null)
            return;

        inventory.OnItemAmountChanged -= HandleAmountChanged;
        inventory = null;
    }

    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (!views.TryGetValue(item.ItemId, out EquipItemView view))
            return;

        view.RenderCount(ToCount(amount), mergeCount);

        if (equipModule != null)
            view.SetDimmed(!equipModule.IsUnlocked(item.ItemId));
    }

    private void BuildViews()
    {
        views.Clear();
        ClearRoot();

        List<EquipListTable> tables = CollectTables();
        int currentTier = int.MinValue;
        int orderInTier = 0;
        EquipTierUI currentTierUI = null;

        for (int i = 0; i < tables.Count; i++)
        {
            EquipListTable table = tables[i];
            int tier = Mathf.Max(1, table.grade);

            if (tier != currentTier)
            {
                currentTier = tier;
                orderInTier = 0;
                currentTierUI = CreateTier(tier);
            }

            if (currentTierUI != null)
                CreateItem(currentTierUI, table, tier, orderInTier++);
        }

        isBuilt = true;
    }

    private EquipTierUI CreateTier(int tier)
    {
        GameObject tierObject = Instantiate(tierPrefab, root, false);
        tierObject.name = $"Tier_{tier:00}";

        EquipTierUI tierUI = tierObject.GetComponent<EquipTierUI>();
        if (tierUI == null)
            return null;

        tierUI.TierPanel.color = Transparent;

        Color tierColor = RarityColor.TierColorByTier(tier);
        int starCount = GetStarCount(tier);

        for (int i = 0; i < starCount; i++)
        {
            GameObject starObject = Instantiate(starPrefab, tierUI.TierRoot, false);
            starObject.name = $"(Img)TierStar_{i + 1}";

            Image star = starObject.GetComponent<Image>();
            if (star != null)
                star.color = tierColor;
        }

        return tierUI;
    }

    private void CreateItem(EquipTierUI tierUI, EquipListTable table, int tier, int orderInTier)
    {
        GameObject itemObject = Instantiate(itemPrefab, tierUI.ListRoot, false);
        itemObject.name = $"Equipment_{table.ID}";

        EquipItemUI itemUI = itemObject.GetComponent<EquipItemUI>();
        if (itemUI == null)
            return;

        EquipItemView view = new EquipItemView(itemUI);
        int itemId = table.ID;

        view.Bind(() => ClickItem(itemId));
        view.Render(
            LoadIcon(table),
            levelText,
            GetStarCount(tier),
            RarityColor.TierColorByTier(tier));
        view.SetFrameColor(RarityColor.ColorByOrderIndex(orderInTier));
        view.SetDimmed(!equipModule.IsUnlocked(itemId));
        view.RenderCount(ToCount(inventory.GetItemAmount(itemId)), mergeCount);

        views[itemId] = view;
    }

    private void RefreshAllItems()
    {
        if (inventory == null || equipModule == null)
            return;

        foreach (KeyValuePair<int, EquipItemView> pair in views)
        {
            int itemId = pair.Key;
            EquipItemView view = pair.Value;

            view.RenderCount(ToCount(inventory.GetItemAmount(itemId)), mergeCount);
            view.SetDimmed(!equipModule.IsUnlocked(itemId));
        }
    }

    private List<EquipListTable> CollectTables()
    {
        List<EquipListTable> tables = new List<EquipListTable>();

        foreach (KeyValuePair<int, EquipListTable> pair in DataManager.Instance.EquipListDict)
        {
            EquipListTable table = pair.Value;
            if (table.equipmentType == tabType)
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

    private void ClearRoot()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private static Sprite LoadIcon(EquipListTable table)
    {
        string key = string.IsNullOrEmpty(table.iconResource)
            ? table.equipmentName
            : table.iconResource;

        return string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>(key);
    }

    private static int GetStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
    }

    private static int ToCount(BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return 0;

        double value = amount.ToDouble();
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            return 0;

        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }

    private void ClickItem(int itemId)
    {
        Debug.Log($"[EquipTabUIController] 장비 선택 구현 예정: {itemId}");
    }
}
