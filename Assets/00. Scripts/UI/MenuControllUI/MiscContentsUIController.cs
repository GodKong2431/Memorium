using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MiscContentsUIController : UIControllerBase
{
    public const string RootObjectName = "MiscContents";

    private const string ScrollViewName = "\uC2A4\uD06C\uB864\uBDF0_\uAE30\uD0C0 \uBAA9\uB85D";
    private const string CategoryPanelName = "\uD328\uB110_\uC138\uBD80 \uCE74\uD14C\uACE0\uB9AC";
    private const string ButtonShowAllName = "\uBC84\uD2BC_\uBAA8\uB450 \uBCF4\uAE30";
    private const string ButtonSkillScrollName = "\uBC84\uD2BC_\uC2A4\uD0AC \uC8FC\uBB38\uC11C";
    private const string ButtonGemName = "\uBC84\uD2BC_\uC7BC";
    private const string ButtonConsumableName = "\uBC84\uD2BC_\uC18C\uBAA8\uD488";
    private const string ItemFrameName = "(Btn)ItemFrame";
    private const string ItemIconName = "\uC774\uBBF8\uC9C0_\uC544\uC774\uD15C \uC544\uC774\uCF58";
    private const string ItemCountName = "\uD14D\uC2A4\uD2B8_\uAC1C\uC218";
    private const string RuntimeTemplateName = "__MiscItemTemplate";
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
        public GameObject GameObject;
        public Button Button;
        public Image IconImage;
        public TMP_Text CountText;
    }

    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private ScrollRect listScrollRect;
    [SerializeField] private RectTransform listContentRoot;
    [SerializeField] private RectTransform categoryRoot;
    [SerializeField] private Button buttonShowAll;
    [SerializeField] private Button buttonSkillScroll;
    [SerializeField] private Button buttonGem;
    [SerializeField] private Button buttonConsumable;
    [SerializeField] private GameObject itemPrefab;

    private readonly List<FilterButtonBinding> filterButtons = new List<FilterButtonBinding>(4);
    private readonly List<MiscItemEntry> workingEntries = new List<MiscItemEntry>();
    private readonly List<RuntimeItemBinding> runtimeItems = new List<RuntimeItemBinding>();

    private InventoryManager inventory;
    private SkillInventoryModule skillModule;
    private GemInventoryModule gemModule;
    private GameObject runtimeTemplateInstance;
    private MiscFilter currentFilter = MiscFilter.All;
    private bool buttonsSubscribed;
    private bool pendingRefresh;
    private bool shouldResetScrollPosition = true;
    private bool missingTemplateLogged;
    private bool forceRebuild = true;

    protected override void Initialize()
    {
        BindReferences();
    }

    protected override void Subscribe()
    {
        currentFilter = MiscFilter.All;
        BindReferences();
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
        BindReferences();
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

    private void BindReferences()
    {
        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (panelRoot == null)
            return;

        if (listScrollRect == null)
            listScrollRect = FindComponentRecursive<ScrollRect>(panelRoot, ScrollViewName);

        if (listContentRoot == null && listScrollRect != null)
            listContentRoot = listScrollRect.content;

        if (categoryRoot == null)
            categoryRoot = FindRectTransformRecursive(panelRoot, CategoryPanelName);

        if (buttonShowAll == null)
            buttonShowAll = FindComponentRecursive<Button>(panelRoot, ButtonShowAllName);

        if (buttonSkillScroll == null)
            buttonSkillScroll = FindComponentRecursive<Button>(panelRoot, ButtonSkillScrollName);

        if (buttonGem == null)
            buttonGem = FindComponentRecursive<Button>(panelRoot, ButtonGemName);

        if (buttonConsumable == null)
            buttonConsumable = FindComponentRecursive<Button>(panelRoot, ButtonConsumableName);

        RebuildFilterButtonBindings();
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

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.ItemInfoDict == null)
            return false;

        if (!BindInventory())
            return false;

        if (ResolveItemPrefab() == null)
        {
            if (!missingTemplateLogged)
            {
                Debug.LogWarning("[MiscContentsUIController] Failed to resolve the misc inventory item prefab.", this);
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

    private GameObject ResolveItemPrefab()
    {
        if (IsItemFramePrefab(itemPrefab))
            return itemPrefab;

        if (IsItemFramePrefab(runtimeTemplateInstance))
            return runtimeTemplateInstance;

        if (listContentRoot == null)
            return null;

        Transform templateSource = FindItemFrameTemplate(listContentRoot);

        if (templateSource == null)
            return null;

        Transform templateParent = panelRoot != null ? panelRoot : transform;
        runtimeTemplateInstance = Instantiate(templateSource.gameObject, templateParent, false);
        runtimeTemplateInstance.name = RuntimeTemplateName;
        runtimeTemplateInstance.SetActive(false);
        return runtimeTemplateInstance;
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
            if (binding == null || binding.GameObject == null || binding.IconImage == null || binding.CountText == null)
                return true;

            if (binding.ItemId != entry.ItemId || binding.ItemType != entry.ItemType)
                return true;
        }

        return false;
    }

    private void RebuildVisibleItems()
    {
        ClearContentChildren();

        GameObject prefab = ResolveItemPrefab();
        if (prefab == null)
            return;

        for (int i = 0; i < workingEntries.Count; i++)
        {
            GameObject itemObject = Instantiate(prefab, listContentRoot, false);
            itemObject.name = string.Format(
                CultureInfo.InvariantCulture,
                "MiscItem_{0:00}_{1}_{2}",
                i + 1,
                workingEntries[i].ItemType,
                workingEntries[i].ItemId);

            if (!TryCreateRuntimeBinding(itemObject, out RuntimeItemBinding binding))
            {
                itemObject.SetActive(false);
                Destroy(itemObject);
                continue;
            }

            PrepareRuntimeItem(binding);
            BindRuntimeItem(binding, workingEntries[i]);
            itemObject.SetActive(true);
            binding.ItemId = workingEntries[i].ItemId;
            binding.ItemType = workingEntries[i].ItemType;
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
            RuntimeItemBinding binding = runtimeItems[i];
            if (binding == null || binding.GameObject == null)
                continue;

            BindRuntimeItem(binding, workingEntries[i]);
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

    private static void PrepareRuntimeItem(RuntimeItemBinding binding)
    {
        if (binding == null)
            return;

        if (binding.Button != null)
        {
            binding.Button.onClick.RemoveAllListeners();
            binding.Button.transition = Selectable.Transition.None;
            binding.Button.interactable = true;
        }

        if (binding.IconImage != null)
            binding.IconImage.preserveAspect = true;

        if (binding.CountText != null)
            binding.CountText.gameObject.SetActive(true);
    }

    private static void BindRuntimeItem(RuntimeItemBinding binding, MiscItemEntry entry)
    {
        if (binding == null || entry == null)
            return;

        if (binding.IconImage != null)
        {
            binding.IconImage.sprite = entry.Icon;
            binding.IconImage.enabled = entry.Icon != null;
        }

        if (binding.CountText != null)
        {
            binding.CountText.gameObject.SetActive(true);
            binding.CountText.text = FormatAmount(entry.Amount);
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
        foreach (KeyValuePair<int, ItemInfoTable> pair in DataManager.Instance.ItemInfoDict)
        {
            ItemInfoTable itemInfo = pair.Value;
            if (itemInfo == null || !IsConsumableType(itemInfo.itemType))
                continue;

            BigDouble amount = inventory.GetItemAmount(pair.Key);
            if (amount <= BigDouble.Zero)
                continue;

            AddEntry(pair.Key, itemInfo.itemType, amount, itemInfo.itemName, LoadItemIcon(itemInfo));
        }
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

    private static float GetSliderValue(BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return 1f;

        double value = amount.ToDouble();
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 1d)
            return 1f;

        return value >= 9999d ? 9999f : (float)value;
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
            sprite = Resources.Load<Sprite>(key.Substring(0, extensionIndex));
            if (sprite != null)
                return sprite;
        }

        const string resourcesToken = "Resources/";
        int resourcesIndex = key.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0)
            return null;

        string relativePath = key.Substring(resourcesIndex + resourcesToken.Length);
        int relativeExtensionIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
        if (relativeExtensionIndex > 0)
            relativePath = relativePath.Substring(0, relativeExtensionIndex);

        return Resources.Load<Sprite>(relativePath);
    }

    private static bool TryCreateRuntimeBinding(GameObject itemObject, out RuntimeItemBinding binding)
    {
        binding = null;
        if (itemObject == null || !TryFindItemFrameBinding(itemObject.transform, out Button button, out Image iconImage, out TMP_Text countText))
            return false;

        binding = new RuntimeItemBinding
        {
            GameObject = itemObject,
            Button = button,
            IconImage = iconImage,
            CountText = countText
        };
        return true;
    }

    private static bool IsItemFramePrefab(GameObject prefab)
    {
        return prefab != null && TryFindItemFrameBinding(prefab.transform, out _, out _, out _);
    }

    private static Transform FindItemFrameTemplate(Transform root)
    {
        if (root == null)
            return null;

        if (TryFindItemFrameBinding(root, out _, out _, out _) &&
            string.Equals(root.name, ItemFrameName, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindItemFrameTemplate(root.GetChild(i));
            if (result != null)
                return result;
        }

        if (TryFindItemFrameBinding(root, out _, out _, out _))
            return root;

        return null;
    }

    private static bool TryFindItemFrameBinding(Transform root, out Button button, out Image iconImage, out TMP_Text countText)
    {
        button = null;
        iconImage = null;
        countText = null;

        if (root == null)
            return false;

        button = root.GetComponent<Button>();
        iconImage = FindComponentRecursive<Image>(root, ItemIconName);
        countText = FindComponentRecursive<TMP_Text>(root, ItemCountName);

        return button != null && iconImage != null && countText != null;
    }

    private static RectTransform FindRectTransformRecursive(RectTransform root, string targetName)
    {
        return FindTransformRecursive(root, targetName) as RectTransform;
    }

    private static T FindComponentRecursive<T>(Transform root, string targetName) where T : Component
    {
        Transform transform = FindTransformRecursive(root, targetName);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static T FindComponentRecursive<T>(RectTransform root, string targetName) where T : Component
    {
        Transform transform = FindTransformRecursive(root, targetName);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static Transform FindTransformRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindTransformRecursive(root.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}

public static class MiscContentsUIBootstrap
{
    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            isRegistered = true;
        }

        AttachControllers();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachControllers();
    }

    private static void AttachControllers()
    {
        RectTransform[] roots = UnityEngine.Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < roots.Length; i++)
        {
            RectTransform root = roots[i];
            if (root == null || root.name != MiscContentsUIController.RootObjectName)
                continue;

            if (root.GetComponent<MiscContentsUIController>() == null)
                root.gameObject.AddComponent<MiscContentsUIController>();
        }
    }
}
