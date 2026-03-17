using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipCurrentUIController : UIControllerBase
{
    [SerializeField] private EquipmentHandler handler;
    [SerializeField] private EquipReinforceUIController reinforceController;
    [SerializeField] private Button mergeButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private RectTransform root;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private RectTransform mergeResultPanelRoot;
    [SerializeField] private TextMeshProUGUI mergeResultTitle;
    [SerializeField] private ScrollRect mergeResultScrollView;
    [SerializeField] private RectTransform mergeResultContentRoot;
    [SerializeField] private GameObject mergeResultItemPrefab;
    [SerializeField] private string levelText = "Lv. 0";
    [SerializeField] private string mergeResultTitleText = "합성 결과";
    [SerializeField] private string mergeResultCountFormat = "x{0}";

    public GameObject ItemPrefab => itemPrefab;

    private static readonly EquipmentType[] Order =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Armor,
        EquipmentType.Glove,
        EquipmentType.Boots
    };

    private readonly List<EquipItemView> views = new List<EquipItemView>();
    private readonly List<EquipItemView> mergeResultViews = new List<EquipItemView>();
    private readonly List<EquipmentInventoryModule.MergeResultEntry> mergeResults = new List<EquipmentInventoryModule.MergeResultEntry>();
    private InventoryManager inventory;
    private bool isBuilt;
    private bool isMergeResultVisible;
    private int ignoreMergeResultCloseUntilFrame = -1;

    protected override void Initialize()
    {
        SetMergeResultVisible(false);
    }

    private void Update()
    {
        if (isMergeResultVisible && ShouldCloseMergeResultPopup())
        {
            HideMergeResultPopup();
            return;
        }

        if (!isBuilt)
            RefreshView();
    }

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;

        if (mergeButton != null)
            mergeButton.onClick.AddListener(ClickMerge);

        if (equipButton != null)
            equipButton.onClick.AddListener(ClickEquip);

        BindInventory();
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;

        if (mergeButton != null)
            mergeButton.onClick.RemoveListener(ClickMerge);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(ClickEquip);

        UnbindInventory();
    }

    protected override void RefreshView()
    {
        if (!TryPrepare())
            return;

        if (!isBuilt)
            BuildViews();

        RefreshButtons();
        RefreshCurrent();
        RefreshMergeResults();
        ApplyMergeResultVisibility();
    }

    private bool TryPrepare()
    {
        if (handler == null || !handler.dataLoad)
            return false;

        if (root == null || itemPrefab == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (!BindInventory())
            return false;

        EquipmentInventoryModule module = inventory.GetModule<EquipmentInventoryModule>();
        return module != null && module.IsInitialized;
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

    private void BuildViews()
    {
        views.Clear();

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        for (int i = 0; i < Order.Length; i++)
        {
            GameObject go = Instantiate(itemPrefab, root, false);
            go.name = $"CurrentEquipment_{Order[i]}";

            EquipItemUI ui = go.GetComponent<EquipItemUI>();
            if (ui == null)
                continue;

            ui.EnsureBindings();

            if (ui.MergeSlider != null)
                ui.MergeSlider.gameObject.SetActive(false);

            EquipItemView view = new EquipItemView(ui);
            if (reinforceController != null)
            {
                EquipmentType equipmentType = Order[i];
                view.Bind(() => ClickCurrent(equipmentType));
                view.SetDimmed(false);
            }
            else if (ui.Button != null)
            {
                ui.Button.onClick.RemoveAllListeners();
                ui.Button.interactable = false;
            }

            views.Add(view);
        }

        isBuilt = true;
    }

    private void RefreshMergeResults()
    {
        if (mergeResultTitle != null)
            mergeResultTitle.text = mergeResultTitleText;

        if (mergeResultContentRoot == null)
            return;

        EquipmentInventoryModule equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null)
            return;

        equipmentModule.CopyMergeResults(mergeResults);
        SyncMergeResultViews(mergeResults.Count);

        for (int i = 0; i < mergeResults.Count; i++)
        {
            EquipmentInventoryModule.MergeResultEntry entry = mergeResults[i];
            if (!DataManager.Instance.EquipListDict.TryGetValue(entry.ItemId, out EquipListTable info))
                continue;

            Sprite icon = LoadIcon(info);
            int starCount = GetStarCount(info.grade);
            string resultCountText = string.Format(mergeResultCountFormat, Mathf.Max(1, entry.Count));

            mergeResultViews[i].Render(icon, resultCountText, starCount, RarityColor.TierColorByTier(info.grade));
            mergeResultViews[i].SetFrameColor(RarityColor.ItemGradeColor(info.rarityType));
            mergeResultViews[i].SetDimmed(false);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(mergeResultContentRoot);

        if (mergeResultScrollView != null)
        {
            mergeResultScrollView.StopMovement();
            mergeResultScrollView.verticalNormalizedPosition = 1f;
            mergeResultScrollView.horizontalNormalizedPosition = 0f;
        }
    }

    private void ApplyMergeResultVisibility()
    {
        bool hasResults = mergeResults.Count > 0;
        if (!hasResults && isMergeResultVisible)
            isMergeResultVisible = false;

        SetMergeResultVisible(isMergeResultVisible && hasResults);
    }

    private void SyncMergeResultViews(int requiredCount)
    {
        GameObject prefab = mergeResultItemPrefab != null ? mergeResultItemPrefab : itemPrefab;
        if (prefab == null || mergeResultContentRoot == null)
            return;

        while (mergeResultViews.Count < requiredCount)
        {
            GameObject go = Instantiate(prefab, mergeResultContentRoot, false);
            go.name = $"MergeResult_{mergeResultViews.Count + 1}";

            EquipItemUI ui = go.GetComponent<EquipItemUI>();
            if (ui == null)
            {
                go.SetActive(false);
                break;
            }

            PrepareMergeResultItem(ui);
            mergeResultViews.Add(new EquipItemView(ui));
        }

        for (int i = 0; i < mergeResultViews.Count; i++)
            mergeResultViews[i].GameObject.SetActive(i < requiredCount);
    }

    private static void PrepareMergeResultItem(EquipItemUI ui)
    {
        if (ui == null)
            return;

        ui.EnsureBindings();

        if (ui.Button != null)
        {
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = false;
        }

        if (ui.MergeSlider != null)
            ui.MergeSlider.gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        if (mergeButton != null)
            mergeButton.interactable = handler.CanAutoMerge();

        if (equipButton != null)
            equipButton.interactable = handler.CanAutoEquip();
    }

    private void RefreshCurrent()
    {
        if (!handler.TryGetPlayerEquipment(out PlayerEquipment player))
            return;

        if (views.Count < Order.Length)
            return;

        EquipmentInventoryModule equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null)
            return;

        for (int i = 0; i < Order.Length; i++)
        {
            int itemId = player.ReturnItemNum(Order[i]);
            if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
                continue;

            Sprite icon = LoadIcon(info);
            int starCount = GetStarCount(info.grade);
            EquipmentData equipmentData = equipmentModule.GetEquipment(itemId);

            if (equipmentData.equipmentId == itemId)
                levelText = "Lv. " + equipmentData.equipmentReinforcement;
            else
                levelText = "Lv. 0";

            views[i].Render(icon, levelText, starCount, RarityColor.TierColorByTier(info.grade));
            views[i].SetFrameColor(RarityColor.ItemGradeColor(info.rarityType));
            views[i].SetDimmed(reinforceController == null);
        }
    }

    private void ClickCurrent(EquipmentType type)
    {
        if (reinforceController == null)
            return;

        reinforceController.Show(type);
    }

    private void ClickMerge()
    {
        if (handler.TryAutoMerge())
            ShowMergeResultPopup();
        else
            HideMergeResultPopup();

        RefreshButtons();
    }

    private void ClickEquip()
    {
        handler.TryAutoEquip();
        RefreshButtons();
    }

    private void HandleEquippedChanged(EquipmentType type, int itemId)
    {
        RefreshCurrent();
    }

    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (IsEquipmentItem(item.ItemType))
            RefreshButtons();
    }

    private void ShowMergeResultPopup()
    {
        isMergeResultVisible = true;
        ignoreMergeResultCloseUntilFrame = Time.frameCount + 1;
        RefreshMergeResults();
        ApplyMergeResultVisibility();
    }

    private void HideMergeResultPopup()
    {
        isMergeResultVisible = false;
        SetMergeResultVisible(false);
    }

    private void SetMergeResultVisible(bool visible)
    {
        if (mergeResultPanelRoot != null && mergeResultPanelRoot.gameObject.activeSelf != visible)
            mergeResultPanelRoot.gameObject.SetActive(visible);
    }

    private bool ShouldCloseMergeResultPopup()
    {
        if (Time.frameCount <= ignoreMergeResultCloseUntilFrame)
            return false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return !IsInsideMergeResultPopup(Touchscreen.current.primaryTouch.position.ReadValue());

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return !IsInsideMergeResultPopup(Mouse.current.position.ReadValue());

        return false;
    }

    private bool IsInsideMergeResultPopup(Vector2 screenPosition)
    {
        if (mergeResultPanelRoot == null)
            return false;

        Canvas parentCanvas = mergeResultPanelRoot.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = parentCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(mergeResultPanelRoot, screenPosition, eventCamera);
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

    private static bool IsEquipmentItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:
            case ItemType.Helmet:
            case ItemType.Armor:
            case ItemType.Glove:
            case ItemType.Boots:
                return true;
            default:
                return false;
        }
    }
}
