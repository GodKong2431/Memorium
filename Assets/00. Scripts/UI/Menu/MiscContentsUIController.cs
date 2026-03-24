using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MiscContentsUIController : UIControllerBase
{
    private const float DefaultUnselectedButtonAlpha = 0.39215687f;
    private const float SelectedButtonAlpha = 1f;

    private enum MiscFilter
    {
        All,
        SkillScroll,
        Gem,
        Consumable
    }

    private sealed class FilterButtonBinding
    {
        public MiscFilter Filter;
        public Image Image;
        public Color BaseColor;
        public float UnselectedAlpha;
    }

    private sealed class MiscItemEntry
    {
        public int ItemId;
        public ItemType ItemType;
        public BigDouble Amount;
        public string Name;
        public Sprite Icon;
    }

    [Header("Root")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private ScrollRect listScrollRect;
    [SerializeField] private RectTransform listContentRoot;

    [Header("Filter Buttons")]
    [SerializeField] private Button buttonShowAll;
    [SerializeField] private Button buttonSkillScroll;
    [SerializeField] private Button buttonGem;
    [SerializeField] private Button buttonConsumable;

    [Header("Item Template")]
    [SerializeField] private MiscItemFrameUI itemPrefab;

    private readonly List<FilterButtonBinding> filterButtons = new(4);
    private readonly List<MiscItemEntry> visibleEntries = new();
    private readonly List<MiscItemFrameUI> itemViews = new();
    private readonly HashSet<int> visibleConsumableIds = new();
    private readonly HashSet<int> visibleSkillScrollIds = new();

    private InventoryManager inventory;
    private SkillInventoryModule skillModule;
    private GemInventoryModule gemModule;
    private MiscFilter currentFilter = MiscFilter.All;
    private bool buttonsSubscribed;
    private bool isDirty;
    private bool shouldResetScrollPosition = true;
    private bool missingTemplateLogged;

    protected override void Initialize()
    {
        ResolveSerializedReferences();
        RebuildFilterButtonBindings();
        HideContentChildren();
    }

    protected override void Subscribe()
    {
        ResolveSerializedReferences();
        RebuildFilterButtonBindings();
        currentFilter = MiscFilter.All;
        SubscribeButtons();
        shouldResetScrollPosition = true;
        MarkDirty();
    }

    protected override void Unsubscribe()
    {
        UnsubscribeButtons();
        UnbindInventory();
        isDirty = false;
    }

    protected override void RefreshView()
    {
        ResolveSerializedReferences();
        if (!TryPrepareRuntime())
        {
            isDirty = true;
            return;
        }

        CollectVisibleEntries();
        RefreshFilterButtonVisuals();
        RebuildItemViews();
        isDirty = false;
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled || !isDirty)
            return;

        RefreshView();
    }

    private void ResolveSerializedReferences()
    {
        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (listContentRoot == null && listScrollRect != null)
            listContentRoot = listScrollRect.content;
    }

    private void RebuildFilterButtonBindings()
    {
        filterButtons.Clear();
        AddFilterButton(MiscFilter.All, buttonShowAll);
        AddFilterButton(MiscFilter.SkillScroll, buttonSkillScroll);
        AddFilterButton(MiscFilter.Gem, buttonGem);
        AddFilterButton(MiscFilter.Consumable, buttonConsumable);
    }

    private void AddFilterButton(MiscFilter filter, Button button)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();

        Color baseColor = image != null ? image.color : Color.white;
        float unselectedAlpha = image != null && image.color.a < SelectedButtonAlpha
            ? image.color.a
            : DefaultUnselectedButtonAlpha;

        filterButtons.Add(new FilterButtonBinding
        {
            Filter = filter,
            Image = image,
            BaseColor = baseColor,
            UnselectedAlpha = Mathf.Clamp01(unselectedAlpha)
        });
    }

    private bool TryPrepareRuntime()
    {
        if (panelRoot == null || listContentRoot == null)
            return false;

        if (buttonShowAll == null || buttonSkillScroll == null || buttonGem == null || buttonConsumable == null)
            return false;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || !dataManager.DataLoad || dataManager.ItemInfoDict == null)
            return false;

        if (!BindInventory())
            return false;

        if (itemPrefab == null || !itemPrefab.HasBindings)
        {
            if (!missingTemplateLogged)
            {
                Debug.LogWarning("[MiscContentsUIController] Misc item prefab binding is missing.", this);
                missingTemplateLogged = true;
            }

            return false;
        }

        missingTemplateLogged = false;
        return true;
    }

    private bool BindInventory()
    {
        InventoryManager currentInventory = InventoryManager.Instance;
        if (currentInventory == null)
            return false;

        SkillInventoryModule nextSkillModule = currentInventory.GetModule<SkillInventoryModule>();
        GemInventoryModule nextGemModule = currentInventory.GetModule<GemInventoryModule>();

        if (inventory == currentInventory && skillModule == nextSkillModule && gemModule == nextGemModule)
            return true;

        UnbindInventory();

        inventory = currentInventory;
        skillModule = nextSkillModule;
        gemModule = nextGemModule;

        inventory.OnItemAmountChanged += HandleItemAmountChanged;

        if (skillModule != null)
            skillModule.OnInventoryChanged += HandleSkillInventoryChanged;

        if (gemModule != null)
            gemModule.OnGemInventoryChanged += HandleGemInventoryChanged;

        return true;
    }

    private void UnbindInventory()
    {
        if (inventory != null)
            inventory.OnItemAmountChanged -= HandleItemAmountChanged;

        if (skillModule != null)
            skillModule.OnInventoryChanged -= HandleSkillInventoryChanged;

        if (gemModule != null)
            gemModule.OnGemInventoryChanged -= HandleGemInventoryChanged;

        inventory = null;
        skillModule = null;
        gemModule = null;
    }

    private void SubscribeButtons()
    {
        if (buttonsSubscribed)
            return;

        if (buttonShowAll != null)
            buttonShowAll.onClick.AddListener(HandleShowAllClicked);

        if (buttonSkillScroll != null)
            buttonSkillScroll.onClick.AddListener(HandleSkillScrollClicked);

        if (buttonGem != null)
            buttonGem.onClick.AddListener(HandleGemClicked);

        if (buttonConsumable != null)
            buttonConsumable.onClick.AddListener(HandleConsumableClicked);

        buttonsSubscribed = true;
    }

    private void UnsubscribeButtons()
    {
        if (!buttonsSubscribed)
            return;

        if (buttonShowAll != null)
            buttonShowAll.onClick.RemoveListener(HandleShowAllClicked);

        if (buttonSkillScroll != null)
            buttonSkillScroll.onClick.RemoveListener(HandleSkillScrollClicked);

        if (buttonGem != null)
            buttonGem.onClick.RemoveListener(HandleGemClicked);

        if (buttonConsumable != null)
            buttonConsumable.onClick.RemoveListener(HandleConsumableClicked);

        buttonsSubscribed = false;
    }

    private void HandleShowAllClicked()
    {
        SetFilter(MiscFilter.All);
    }

    private void HandleSkillScrollClicked()
    {
        SetFilter(MiscFilter.SkillScroll);
    }

    private void HandleGemClicked()
    {
        SetFilter(MiscFilter.Gem);
    }

    private void HandleConsumableClicked()
    {
        SetFilter(MiscFilter.Consumable);
    }

    private void SetFilter(MiscFilter nextFilter)
    {
        if (currentFilter == nextFilter)
            return;

        currentFilter = nextFilter;
        shouldResetScrollPosition = true;
        RefreshFilterButtonVisuals();
        MarkDirty();
    }

    private void RefreshFilterButtonVisuals()
    {
        for (int i = 0; i < filterButtons.Count; i++)
        {
            FilterButtonBinding binding = filterButtons[i];
            if (binding.Image == null)
                continue;

            Color color = binding.BaseColor;
            color.a = binding.Filter == currentFilter ? SelectedButtonAlpha : binding.UnselectedAlpha;
            binding.Image.color = color;
        }
    }

    private void MarkDirty()
    {
        isDirty = true;
    }

    private void HideContentChildren()
    {
        if (listContentRoot == null)
            return;

        for (int i = 0; i < listContentRoot.childCount; i++)
        {
            Transform child = listContentRoot.GetChild(i);
            if (child != null)
                child.gameObject.SetActive(false);
        }
    }

    private void RebuildItemViews()
    {
        ClearRuntimeItems();

        for (int i = 0; i < visibleEntries.Count; i++)
        {
            MiscItemEntry entry = visibleEntries[i];
            MiscItemFrameUI view = Instantiate(itemPrefab, listContentRoot, false);
            if (view == null)
                continue;

            view.PrepareForRuntime();
            view.gameObject.name = string.Format(
                CultureInfo.InvariantCulture,
                "MiscItem_{0:00}_{1}_{2}",
                i + 1,
                entry.ItemType,
                entry.ItemId);

            view.Bind(entry.Icon, FormatAmount(entry.Amount));
            itemViews.Add(view);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContentRoot);
        ResetScrollPositionIfNeeded();
    }

    private void ClearRuntimeItems()
    {
        for (int i = 0; i < itemViews.Count; i++)
        {
            MiscItemFrameUI view = itemViews[i];
            if (view != null)
                Destroy(view.gameObject);
        }

        itemViews.Clear();
    }

    private void ResetScrollPositionIfNeeded()
    {
        if (!shouldResetScrollPosition || listScrollRect == null)
            return;

        listScrollRect.StopMovement();
        listScrollRect.verticalNormalizedPosition = 1f;
        shouldResetScrollPosition = false;
    }

    private void HandleItemAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (InventoryTypeMapper.TryToEquipmentType(item.ItemType, out _))
            return;

        if (item.ItemType == ItemType.Pixie)
            return;

        MarkDirty();
    }

    private void HandleSkillInventoryChanged()
    {
        MarkDirty();
    }

    private void HandleGemInventoryChanged()
    {
        MarkDirty();
    }

    private void CollectVisibleEntries()
    {
        visibleEntries.Clear();
        visibleConsumableIds.Clear();
        visibleSkillScrollIds.Clear();

        DataManager dataManager = DataManager.Instance;
        if (inventory == null || dataManager == null || dataManager.ItemInfoDict == null)
            return;

        if (currentFilter == MiscFilter.All || currentFilter == MiscFilter.SkillScroll)
            CollectSkillScrollEntries(dataManager);

        if (currentFilter == MiscFilter.All || currentFilter == MiscFilter.Gem)
            CollectGemEntries(dataManager);

        if (currentFilter == MiscFilter.All || currentFilter == MiscFilter.Consumable)
            CollectConsumableEntries(dataManager);

        visibleEntries.Sort(CompareEntryOrder);
    }

    private void CollectSkillScrollEntries(DataManager dataManager)
    {
        if (skillModule == null || dataManager.SkillInfoDict == null)
            return;

        foreach (KeyValuePair<int, SkillInfoTable> pair in dataManager.SkillInfoDict)
        {
            SkillInfoTable skillInfo = pair.Value;
            if (skillInfo == null || skillInfo.skillScrollID <= 0)
                continue;

            int scrollId = skillInfo.skillScrollID;
            if (!visibleSkillScrollIds.Add(scrollId))
                continue;

            BigDouble count = inventory.GetItemAmount(scrollId);
            if (count <= BigDouble.Zero)
                continue;

            if (!dataManager.ItemInfoDict.TryGetValue(scrollId, out ItemInfoTable itemInfo) || itemInfo == null)
                continue;

            AddOrUpdateEntry(scrollId, itemInfo.itemType, count, itemInfo.itemName, ResolveItemIcon(itemInfo));
        }
    }

    private void CollectGemEntries(DataManager dataManager)
    {
        foreach (KeyValuePair<int, ItemInfoTable> pair in dataManager.ItemInfoDict)
        {
            ItemInfoTable itemInfo = pair.Value;
            if (itemInfo == null || !IsGemType(itemInfo.itemType))
                continue;

            BigDouble amount = GetGemAmount(pair.Key);
            if (amount <= BigDouble.Zero)
                continue;

            AddOrUpdateEntry(pair.Key, itemInfo.itemType, amount, itemInfo.itemName, ResolveItemIcon(itemInfo));
        }
    }

    private void CollectConsumableEntries(DataManager dataManager)
    {
        foreach (KeyValuePair<int, ItemInfoTable> pair in dataManager.ItemInfoDict)
        {
            ItemInfoTable itemInfo = pair.Value;
            if (itemInfo == null || !IsConsumableType(itemInfo.itemType))
                continue;

            if (!TryResolveConsumableDisplayInfo(pair.Key, itemInfo, out int displayItemId, out ItemInfoTable displayInfo))
                continue;

            if (!visibleConsumableIds.Add(displayItemId))
                continue;

            BigDouble amount = inventory.GetItemAmount(displayItemId);
            if (amount <= BigDouble.Zero)
                continue;

            AddOrUpdateEntry(displayItemId, displayInfo.itemType, amount, displayInfo.itemName, ResolveItemIcon(displayInfo));
        }
    }

    private void AddOrUpdateEntry(int itemId, ItemType itemType, BigDouble amount, string itemName, Sprite icon)
    {
        if (amount <= BigDouble.Zero)
            return;

        for (int i = 0; i < visibleEntries.Count; i++)
        {
            MiscItemEntry entry = visibleEntries[i];
            if (entry.ItemId != itemId)
                continue;

            if (amount > entry.Amount)
                entry.Amount = amount;

            if (entry.Icon == null && icon != null)
                entry.Icon = icon;

            if (string.IsNullOrEmpty(entry.Name) && !string.IsNullOrEmpty(itemName))
                entry.Name = itemName;

            return;
        }

        visibleEntries.Add(new MiscItemEntry
        {
            ItemId = itemId,
            ItemType = itemType,
            Amount = amount,
            Name = itemName ?? string.Empty,
            Icon = icon
        });
    }

    private BigDouble GetGemAmount(int itemId)
    {
        BigDouble amount = inventory != null ? inventory.GetItemAmount(itemId) : BigDouble.Zero;
        if (gemModule == null)
            return amount;

        foreach (OwnedGemData gemData in gemModule.GetAllGems())
        {
            if (gemData == null || gemData.gemId != itemId)
                continue;

            int totalCount = 0;
            for (int i = (int)GemGrade.Common; i < (int)GemGrade.Count; i++)
                totalCount += Mathf.Max(0, gemData.gradeCounts[i]);

            BigDouble moduleAmount = new BigDouble(totalCount);
            if (moduleAmount > amount)
                amount = moduleAmount;

            break;
        }

        return amount;
    }

    private static bool TryResolveConsumableDisplayInfo(int itemId, ItemInfoTable itemInfo, out int displayItemId, out ItemInfoTable displayInfo)
    {
        displayItemId = itemId;
        displayInfo = itemInfo;

        if (itemInfo == null || DataManager.Instance?.ItemInfoDict == null)
            return false;

        if (!ShouldCollapseCurrencyBackedConsumable(itemInfo.itemType))
            return true;

        displayItemId = GetPrimaryItemIdByType(itemInfo.itemType);
        if (displayItemId == 0)
            return false;

        return DataManager.Instance.ItemInfoDict.TryGetValue(displayItemId, out displayInfo) && displayInfo != null;
    }

    private static bool ShouldCollapseCurrencyBackedConsumable(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.FreeCurrency:
            case ItemType.PaidCurrency:
            case ItemType.Key:
                return true;
            default:
                return false;
        }
    }

    private static int GetPrimaryItemIdByType(ItemType itemType)
    {
        if (DataManager.Instance?.ItemInfoDict == null)
            return 0;

        foreach (KeyValuePair<int, ItemInfoTable> pair in DataManager.Instance.ItemInfoDict)
        {
            if (pair.Value == null || pair.Value.itemType != itemType)
                continue;

            return pair.Key;
        }

        return 0;
    }

    private static int CompareEntryOrder(MiscItemEntry lhs, MiscItemEntry rhs)
    {
        int categoryCompare = GetCategoryOrder(lhs.ItemType).CompareTo(GetCategoryOrder(rhs.ItemType));
        if (categoryCompare != 0)
            return categoryCompare;

        int nameCompare = string.Compare(lhs.Name, rhs.Name, StringComparison.Ordinal);
        if (nameCompare != 0)
            return nameCompare;

        return lhs.ItemId.CompareTo(rhs.ItemId);
    }

    private static int GetCategoryOrder(ItemType itemType)
    {
        if (itemType == ItemType.SkillScroll)
            return 0;

        if (IsGemType(itemType))
            return 1;

        return 2;
    }

    private static bool IsGemType(ItemType itemType)
    {
        return itemType == ItemType.ElementGem || itemType == ItemType.UniqueGem;
    }

    private static bool IsConsumableType(ItemType itemType)
    {
        if (InventoryTypeMapper.TryToEquipmentType(itemType, out _))
            return false;

        if (itemType == ItemType.SkillScroll || IsGemType(itemType))
            return false;

        if (itemType == ItemType.Pixie)
            return false;

        return true;
    }

    private static string FormatAmount(BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return "0";

        return amount.ToString();
    }

    private static Sprite ResolveItemIcon(ItemInfoTable itemInfo)
    {
        return IconManager.GetItemIcon(itemInfo);
    }
}
