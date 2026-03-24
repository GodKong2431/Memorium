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
    [SerializeField] private RectTransform mergeResultPopupContentRoot;
    [SerializeField] private TextMeshProUGUI mergeResultTitle;
    [SerializeField] private ScrollRect mergeResultScrollView;
    [SerializeField] private RectTransform mergeResultContentRoot;
    [SerializeField] private GameObject mergeResultItemPrefab;
    [SerializeField] private string levelText = "Lv. 0";
    [SerializeField] private string mergeResultTitleText = "합성 결과";
    [SerializeField] private string mergeResultCountFormat = "x{0}";
    //[SerializeField] private 

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
    private Button boundMergeButton;
    private Button boundEquipButton;
    private RectTransform builtRoot;
    private RectTransform builtMergeResultContentRoot;

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

        if (!isBuilt || NeedsRebuild())
            RefreshView();
    }

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;
        BindActionButtons();
        BindInventory();
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;
        UnbindActionButtons();
        UnbindInventory();
    }

    protected override void RefreshView()
    {
        if (!TryPrepare())
            return;

        BindActionButtons();

        if (!isBuilt || NeedsRebuild())
            BuildViews();

        RebindCurrentItemViews();
        RefreshButtons();
        RefreshCurrent();
        RefreshMergeResults();
        ApplyMergeResultVisibility();
    }

    public void ResetForSceneChange()
    {
        handler = null;
        reinforceController = null;
        mergeButton = null;
        equipButton = null;
        root = null;
        mergeResultPanelRoot = null;
        mergeResultPopupContentRoot = null;
        mergeResultTitle = null;
        mergeResultScrollView = null;
        mergeResultContentRoot = null;
        isBuilt = false;
        builtRoot = null;
        builtMergeResultContentRoot = null;
        views.Clear();
        mergeResultViews.Clear();
        UnbindActionButtons();
        UnbindInventory();
        HideMergeResultPopup();
    }

    private bool TryPrepare()
    {
        if (!TryResolveUiReferences())
            return false;

        if (!TryResolveHandler() || !handler.dataLoad)
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

    private bool TryResolveHandler()
    {
        if (handler != null)
            return true;

        handler = Object.FindFirstObjectByType<EquipmentHandler>();
        return handler != null;
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
        builtRoot = root;

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
            views.Add(view);
        }

        isBuilt = true;
    }

    private void RebindCurrentItemViews()
    {
        bool canOpenReinforce = TryResolveReinforceController();

        for (int i = 0; i < views.Count && i < Order.Length; i++)
        {
            EquipItemView view = views[i];
            if (view == null)
                continue;

            if (canOpenReinforce)
            {
                EquipmentType equipmentType = Order[i];
                view.Bind(() => ClickCurrent(equipmentType));
            }
            else if (view.Button != null)
            {
                view.Button.onClick.RemoveAllListeners();
            }
        }
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
            //mergeResultViews[i].SetMergeEffect(); //정리되면서 위치가 어긋난다
        }


        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(mergeResultContentRoot);


        if (mergeResultScrollView != null)
        {
            mergeResultScrollView.StopMovement();
            mergeResultScrollView.verticalNormalizedPosition = 1f;
            mergeResultScrollView.horizontalNormalizedPosition = 0f;
        }

        foreach (EquipItemView view in mergeResultViews)
        {
            view.SetMergeEffect();
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

        if (builtMergeResultContentRoot != mergeResultContentRoot || HasInvalidMergeResultViews())
        {
            mergeResultViews.Clear();
            builtMergeResultContentRoot = mergeResultContentRoot;
        }

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

    private void RefreshCurrent(int equipmentId=0)
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
            views[i].SetDimmed(!TryResolveReinforceController());
            if(equipmentId==itemId)
                views[i].SetEquipEffect();
        }
    }

    private void ClickCurrent(EquipmentType type)
    {
        if (!TryResolveReinforceController())
            return;

        reinforceController.Show(type);
    }

    private bool TryResolveReinforceController()
    {
        if (reinforceController != null)
            return true;

        EquipReinforceUIController[] controllers = Resources.FindObjectsOfTypeAll<EquipReinforceUIController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            EquipReinforceUIController candidate = controllers[i];
            if (candidate == null)
                continue;

            if (!candidate.gameObject.scene.IsValid())
                continue;

            reinforceController = candidate;
            return true;
        }

        return false;
    }

    private bool TryResolveUiReferences()
    {
        RectTransform resolvedRoot = EquipmentUiRuntimeLocator.FindRectTransform("(Panel)CurrentEquipment");
        if (resolvedRoot != null)
            root = resolvedRoot;

        Transform equipmentContentsRoot = root != null ? root.parent : null;

        Button resolvedMergeButton = EquipmentUiRuntimeLocator.FindButton("(Btn)Merge", equipmentContentsRoot);
        if (resolvedMergeButton != null)
            mergeButton = resolvedMergeButton;

        Button resolvedEquipButton = EquipmentUiRuntimeLocator.FindButton("(Btn)Equip", equipmentContentsRoot);
        if (resolvedEquipButton != null)
            equipButton = resolvedEquipButton;

        RectTransform resolvedMergeResultPanel = EquipmentUiRuntimeLocator.FindRectTransform("(Panel)MergeResult", equipmentContentsRoot);
        if (resolvedMergeResultPanel != null)
            mergeResultPanelRoot = resolvedMergeResultPanel;

        if (mergeResultPanelRoot != null)
        {
            TextMeshProUGUI resolvedMergeResultTitle = EquipmentUiRuntimeLocator.FindText("(Text)Result", mergeResultPanelRoot);
            if (resolvedMergeResultTitle != null)
                mergeResultTitle = resolvedMergeResultTitle;

            ScrollRect resolvedMergeResultScrollView = EquipmentUiRuntimeLocator.FindComponent<ScrollRect>("(ScrollView)Result", mergeResultPanelRoot);
            if (resolvedMergeResultScrollView != null)
                mergeResultScrollView = resolvedMergeResultScrollView;
        }

        if (mergeResultScrollView != null && mergeResultScrollView.content != null)
            mergeResultContentRoot = mergeResultScrollView.content;

        if (mergeResultPopupContentRoot == null)
            mergeResultPopupContentRoot = mergeResultPanelRoot;

        return root != null;
    }

    private void BindActionButtons()
    {
        if (mergeButton != boundMergeButton)
        {
            if (boundMergeButton != null)
                boundMergeButton.onClick.RemoveListener(ClickMerge);

            boundMergeButton = mergeButton;

            if (boundMergeButton != null)
            {
                boundMergeButton.onClick.RemoveListener(ClickMerge);
                boundMergeButton.onClick.AddListener(ClickMerge);
            }
        }

        if (equipButton != boundEquipButton)
        {
            if (boundEquipButton != null)
                boundEquipButton.onClick.RemoveListener(ClickEquip);

            boundEquipButton = equipButton;

            if (boundEquipButton != null)
            {
                boundEquipButton.onClick.RemoveListener(ClickEquip);
                boundEquipButton.onClick.AddListener(ClickEquip);
            }
        }
    }

    private void UnbindActionButtons()
    {
        if (boundMergeButton != null)
            boundMergeButton.onClick.RemoveListener(ClickMerge);

        if (boundEquipButton != null)
            boundEquipButton.onClick.RemoveListener(ClickEquip);

        boundMergeButton = null;
        boundEquipButton = null;
    }

    private bool NeedsRebuild()
    {
        if (root == null || builtRoot != root)
            return true;

        if (views.Count != Order.Length)
            return true;

        for (int i = 0; i < views.Count; i++)
        {
            if (views[i] == null || views[i].GameObject == null)
                return true;
        }

        return false;
    }

    private bool HasInvalidMergeResultViews()
    {
        for (int i = 0; i < mergeResultViews.Count; i++)
        {
            if (mergeResultViews[i] == null || mergeResultViews[i].GameObject == null)
                return true;
        }

        return false;
    }

    private void ClickMerge()
    {
        if (!TryResolveHandler())
            return;

        if (handler.TryAutoMerge())
            ShowMergeResultPopup();
        else
            HideMergeResultPopup();

        RefreshButtons();
    }

    private void ClickEquip()
    {
        if (!TryResolveHandler())
            return;

        handler.TryAutoEquip();
        RefreshButtons();
    }

    private void HandleEquippedChanged(EquipmentType type, int itemId)
    {
        RefreshCurrent(itemId);
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
        RectTransform hitRoot = ResolveMergeResultPopupHitRoot();
        if (hitRoot == null)
            return false;

        Canvas parentCanvas = hitRoot.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = parentCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(hitRoot, screenPosition, eventCamera);
    }

    private RectTransform ResolveMergeResultPopupHitRoot()
    {
        if (mergeResultPopupContentRoot != null)
            return mergeResultPopupContentRoot;

        if (mergeResultScrollView != null)
        {
            RectTransform scrollRect = mergeResultScrollView.transform as RectTransform;
            if (scrollRect != null)
            {
                RectTransform parentRect = scrollRect.parent as RectTransform;
                if (parentRect != null && parentRect != mergeResultPanelRoot)
                    return parentRect;

                return scrollRect;
            }
        }

        if (mergeResultTitle != null)
        {
            RectTransform titleRect = mergeResultTitle.transform as RectTransform;
            if (titleRect != null)
            {
                RectTransform parentRect = titleRect.parent as RectTransform;
                if (parentRect != null && parentRect != mergeResultPanelRoot)
                    return parentRect;

                return titleRect;
            }
        }

        return mergeResultContentRoot != null ? mergeResultContentRoot : mergeResultPanelRoot;
    }

    private static Sprite LoadIcon(EquipListTable table)
    {
        string key = string.IsNullOrEmpty(table.iconResource)
            ? table.equipmentName
            : table.iconResource;

        return EquipmentIconResolver.LoadSprite(key);
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
