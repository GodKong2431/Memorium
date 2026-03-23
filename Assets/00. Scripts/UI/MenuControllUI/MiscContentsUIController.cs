using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
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
        public Button Button;
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

    private sealed class RuntimeItemBinding
    {
        public int ItemId;
        public ItemType ItemType;
        public MiscItemFrameUI View;
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
    private readonly List<MiscItemEntry> workingEntries = new();
    private readonly List<RuntimeItemBinding> runtimeItems = new();

    private InventoryManager inventory;
    private SkillInventoryModule skillModule;
    private GemInventoryModule gemModule;
    private MiscFilter currentFilter = MiscFilter.All;
    private bool buttonsSubscribed;
    private bool pendingRefresh;
    private bool shouldResetScrollPosition = true;
    private bool missingTemplateLogged;
    private bool forceRebuild = true;

    protected override void Initialize()
    {
        ResolveSerializedReferences();
        RebuildFilterButtonBindings();
    }

    protected override void Subscribe()
    {
        ResolveSerializedReferences();
        RebuildFilterButtonBindings();
        currentFilter = MiscFilter.All;
        BindInventory();
        SubscribeButtons();
        HideContentChildren();
        shouldResetScrollPosition = true;
        forceRebuild = true;
        pendingRefresh = true;
    }

    protected override void Unsubscribe()
    {
        UnsubscribeButtons();
        UnbindInventory();
        pendingRefresh = false;
    }

    protected override void RefreshView()
    {
        ResolveSerializedReferences();
        if (!CanRender())
            return;

        CollectVisibleEntries();
        RefreshFilterButtonVisuals();

        if (forceRebuild || NeedsRebuild())
            RebuildVisibleItems();
        else
            RefreshVisibleItemCounts();
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled || !pendingRefresh)
            return;

        pendingRefresh = false;
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
            Button = button,
            Image = image,
            BaseColor = baseColor,
            UnselectedAlpha = Mathf.Clamp01(unselectedAlpha)
        });
    }

    private bool CanRender()
    {
        if (panelRoot == null || listContentRoot == null)
            return false;

        if (buttonShowAll == null || buttonSkillScroll == null || buttonGem == null || buttonConsumable == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.ItemInfoDict == null)
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
        InventoryManager current = InventoryManager.Instance;
        if (current == null)
            return false;

        SkillInventoryModule nextSkillModule = current.GetModule<SkillInventoryModule>();
        GemInventoryModule nextGemModule = current.GetModule<GemInventoryModule>();

        if (inventory == current && skillModule == nextSkillModule && gemModule == nextGemModule)
            return true;

        UnbindInventory();

        inventory = current;
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
        currentFilter = nextFilter;
        shouldResetScrollPosition = true;
        forceRebuild = true;
        pendingRefresh = true;
        RefreshFilterButtonVisuals();
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

    private bool NeedsRebuild()
    {
        if (runtimeItems.Count != workingEntries.Count)
            return true;

        for (int i = 0; i < workingEntries.Count; i++)
        {
            RuntimeItemBinding binding = runtimeItems[i];
            MiscItemEntry entry = workingEntries[i];
            if (binding == null || binding.View == null || !binding.View.HasBindings)
                return true;

            if (binding.ItemId != entry.ItemId || binding.ItemType != entry.ItemType)
                return true;
        }

        return false;
    }

    private void RebuildVisibleItems()
    {
        ClearContentChildren();

        if (itemPrefab == null || !itemPrefab.HasBindings)
            return;

        for (int i = 0; i < workingEntries.Count; i++)
        {
            MiscItemFrameUI view = Instantiate(itemPrefab, listContentRoot, false);
            view.gameObject.name = string.Format(
                CultureInfo.InvariantCulture,
                "MiscItem_{0:00}_{1}_{2}",
                i + 1,
                workingEntries[i].ItemType,
                workingEntries[i].ItemId);

            RuntimeItemBinding binding = new RuntimeItemBinding
            {
                ItemId = workingEntries[i].ItemId,
                ItemType = workingEntries[i].ItemType,
                View = view
            };

            PrepareRuntimeItem(view);
            BindRuntimeItem(view, workingEntries[i]);
            runtimeItems.Add(binding);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContentRoot);

        if (shouldResetScrollPosition && listScrollRect != null)
        {
            listScrollRect.StopMovement();
            listScrollRect.verticalNormalizedPosition = 1f;
        }

        shouldResetScrollPosition = false;
        forceRebuild = false;
    }

    private void RefreshVisibleItemCounts()
    {
        for (int i = 0; i < workingEntries.Count && i < runtimeItems.Count; i++)
        {
            MiscItemFrameUI view = runtimeItems[i].View;
            if (view == null)
                continue;

            BindRuntimeItem(view, workingEntries[i]);
        }

        forceRebuild = false;
    }

    private void ClearContentChildren()
    {
        if (listContentRoot == null)
            return;

        for (int i = listContentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = listContentRoot.GetChild(i);
            if (child == null)
                continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        runtimeItems.Clear();
    }

    private static void PrepareRuntimeItem(MiscItemFrameUI view)
    {
        if (view == null)
            return;

        if (view.Button != null)
        {
            view.Button.onClick.RemoveAllListeners();
            view.Button.transition = Selectable.Transition.None;
            view.Button.interactable = true;
        }

        if (view.IconImage != null)
            view.IconImage.preserveAspect = true;

        if (view.CountText != null)
            view.CountText.gameObject.SetActive(true);
    }

    private static void BindRuntimeItem(MiscItemFrameUI view, MiscItemEntry entry)
    {
        if (view == null || entry == null)
            return;

        if (view.IconImage != null)
        {
            view.IconImage.sprite = entry.Icon;
            view.IconImage.enabled = entry.Icon != null;
        }

        if (view.CountText != null)
        {
            view.CountText.gameObject.SetActive(true);
            view.CountText.text = FormatAmount(entry.Amount);
        }
    }

    private void HandleItemAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemType == ItemType.Weapon ||
            item.ItemType == ItemType.Helmet ||
            item.ItemType == ItemType.Glove ||
            item.ItemType == ItemType.Armor ||
            item.ItemType == ItemType.Boots)
            return;

        pendingRefresh = true;
    }

    private void HandleSkillInventoryChanged()
    {
        pendingRefresh = true;
    }

    private void HandleGemInventoryChanged()
    {
        pendingRefresh = true;
    }

    private void CollectVisibleEntries()
    {
        workingEntries.Clear();

        if (inventory == null || DataManager.Instance == null || DataManager.Instance.ItemInfoDict == null)
            return;

        if (currentFilter == MiscFilter.All || currentFilter == MiscFilter.SkillScroll)
            CollectSkillScrollEntries();

        if (currentFilter == MiscFilter.All || currentFilter == MiscFilter.Gem)
            CollectGemEntries();

        if (currentFilter == MiscFilter.All || currentFilter == MiscFilter.Consumable)
            CollectConsumableEntries();

        workingEntries.Sort(CompareEntryOrder);
    }

    private void CollectSkillScrollEntries()
    {
        if (skillModule == null)
            return;

        Dictionary<int, int> scrollCounts = skillModule.GetOwnedScrollItemCounts();
        if (scrollCounts == null)
            return;

        foreach (KeyValuePair<int, int> pair in scrollCounts)
        {
            if (pair.Value <= 0)
                continue;

            if (!DataManager.Instance.ItemInfoDict.TryGetValue(pair.Key, out ItemInfoTable itemInfo))
                continue;

            AddEntry(pair.Key, itemInfo.itemType, new BigDouble(pair.Value), itemInfo.itemName, LoadItemIcon(itemInfo));
        }
    }

    private void CollectGemEntries()
    {
        foreach (KeyValuePair<int, ItemInfoTable> pair in DataManager.Instance.ItemInfoDict)
        {
            ItemInfoTable itemInfo = pair.Value;
            if (itemInfo == null || !IsGemType(itemInfo.itemType))
                continue;

            BigDouble amount = GetGemAmount(pair.Key);
            if (amount <= BigDouble.Zero)
                continue;

            AddEntry(pair.Key, itemInfo.itemType, amount, itemInfo.itemName, LoadItemIcon(itemInfo));
        }
    }

    private void CollectConsumableEntries()
    {
        HashSet<int> addedDisplayItemIds = new();

        foreach (KeyValuePair<int, ItemInfoTable> pair in DataManager.Instance.ItemInfoDict)
        {
            ItemInfoTable itemInfo = pair.Value;
            if (itemInfo == null || !IsConsumableType(itemInfo.itemType))
                continue;

            if (!TryResolveConsumableDisplayInfo(pair.Key, itemInfo, out int displayItemId, out ItemInfoTable displayInfo))
                continue;

            if (!addedDisplayItemIds.Add(displayItemId))
                continue;

            BigDouble amount = inventory.GetItemAmount(displayItemId);
            if (amount <= BigDouble.Zero)
                continue;

            AddEntry(displayItemId, displayInfo.itemType, amount, displayInfo.itemName, LoadItemIcon(displayInfo));
        }
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

    private BigDouble GetGemAmount(int itemId)
    {
        BigDouble resolvedAmount = inventory != null ? inventory.GetItemAmount(itemId) : BigDouble.Zero;
        if (gemModule == null)
            return resolvedAmount;

        foreach (OwnedGemData data in gemModule.GetAllGems())
        {
            if (data == null || data.gemId != itemId)
                continue;

            int totalCount = 0;
            for (int i = (int)GemGrade.Common; i < (int)GemGrade.Count; i++)
                totalCount += Mathf.Max(0, data.gradeCounts[i]);

            BigDouble moduleAmount = new BigDouble(totalCount);
            if (moduleAmount > resolvedAmount)
                resolvedAmount = moduleAmount;
            break;
        }

        return resolvedAmount;
    }

    private void AddEntry(int itemId, ItemType itemType, BigDouble amount, string itemName, Sprite icon)
    {
        if (amount <= BigDouble.Zero)
            return;

        for (int i = 0; i < workingEntries.Count; i++)
        {
            if (workingEntries[i].ItemId != itemId)
                continue;

            if (amount > workingEntries[i].Amount)
                workingEntries[i].Amount = amount;

            return;
        }

        workingEntries.Add(new MiscItemEntry
        {
            ItemId = itemId,
            ItemType = itemType,
            Amount = amount,
            Name = itemName ?? string.Empty,
            Icon = icon
        });
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

    private static Sprite LoadItemIcon(ItemInfoTable itemInfo)
    {
        if (itemInfo == null || string.IsNullOrWhiteSpace(itemInfo.itemIcon))
            return null;

        string key = itemInfo.itemIcon.Trim();
        Sprite sprite = Resources.Load<Sprite>(key);
        if (sprite != null)
            return sprite;

        int extensionIndex = key.LastIndexOf(".", StringComparison.Ordinal);
        if (extensionIndex > 0)
        {
            sprite = Resources.Load<Sprite>(key[..extensionIndex]);
            if (sprite != null)
                return sprite;
        }

        const string resourcesToken = "Resources/";
        int resourcesIndex = key.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0)
            return null;

        string relativePath = key[(resourcesIndex + resourcesToken.Length)..];
        int relativeExtensionIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
        if (relativeExtensionIndex > 0)
            relativePath = relativePath[..relativeExtensionIndex];

        return Resources.Load<Sprite>(relativePath);
    }
}
