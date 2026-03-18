using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PixieContentsUIController : UIControllerBase
{
    private const int PixiePreviewLayer = 3;
    private const string PreviewSurfaceObjectName = "__PixiePreviewSurface";
    private const string PreviewStageObjectName = "__PixiePreviewStage";
    private const string PreviewCameraObjectName = "__PixiePreviewCamera";
    private const string MenuPreviewSurfaceObjectName = "__PixieMenuPreviewSurface";
    private const string LockedLevelLabel = "미보유";
    private const string SummonLabel = "소환";
    private const string SummonedLabel = "소환중";
    private const string GrowthLabel = "성장";
    private const string UnlockLabel = "해금";
    private const string EvolveLabel = "진화";
    private const string MaxLabel = "최대";
    private const string AttackTypeLabel = "공격형";
    private const string DefenseTypeLabel = "방어형";
    private const string UtilityTypeLabel = "유틸형";
    private const string UnknownTypeLabel = "픽시";
    private const int PixieUnlockCost = 50;
    private const int MenuPreviewTextureSize = 128;
    private static readonly Vector3 MenuPreviewFocusOffset = new Vector3(0f, 0.05f, 0f);
    private const float MenuPreviewDistanceMultiplier = 2.2f;
    private const string FragmentCostIconObjectName = "__PixieFragmentCostIcon";
    private const string FragmentCostTextObjectName = "__PixieFragmentCostText";
    private static readonly Vector2 MultiLineCostIconSize = new Vector2(24f, 24f);
    private const float MultiLineGrowthCostFontSize = 24f;
    private const float MultiLineCostLineSpacing = 32f;

    private sealed class MenuPreviewRuntime
    {
        public RawImage surface;
        public RenderTexture texture;
        public Transform stageTransform;
        public Camera camera;
        public GameObject instance;
        public AsyncOperationHandle<GameObject>? loadHandle;
        public int previewPixieId;
        public int requestVersion;
    }

    [Header("Root")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private ScrollRect pixieScrollRect;
    [SerializeField] private RectTransform pixieListContentRoot;
    [SerializeField] private PixieMenuItemUI pixieItemTemplate;

    [Header("Details")]
    [SerializeField] private RectTransform pixieModelPanel;
    [SerializeField] private TMP_Text pixieNameText;
    [SerializeField] private TMP_Text pixieLevelText;
    [SerializeField] private TMP_Text pixieTypeText;
    [SerializeField] private PixieStatSlotUI[] buffSlots;
    [SerializeField] private PixieStatSlotUI[] debuffSlots;

    [Header("Actions")]
    [SerializeField] private Button growthButton;
    [SerializeField] private TMP_Text growthLabelText;
    [SerializeField] private TMP_Text growthCostText;
    [SerializeField] private Image growthGoldCostIcon;
    [SerializeField] private Button summonButton;
    [SerializeField] private TMP_Text summonButtonText;

    [Header("List Visuals")]
    [SerializeField] private Color selectedLabelColor = Color.white;
    [SerializeField] private Color ownedLabelColor = Color.white;
    [SerializeField] private Color lockedLabelColor = new Color(0.58f, 0.58f, 0.58f, 1f);
    [SerializeField] private float selectedItemScale = 1f;
    [SerializeField] private float unselectedItemScale = 0.92f;
    [SerializeField] private float lockedItemAlpha = 0.55f;

    [Header("Type Colors")]
    [SerializeField] private Color attackTypeColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color defenseTypeColor = new Color(0.45f, 0.82f, 1f, 1f);
    [SerializeField] private Color utilityTypeColor = new Color(0.5f, 1f, 0.68f, 1f);
    [SerializeField] private Color unknownTypeColor = Color.white;

    [Header("Preview")]
    [SerializeField] private Vector3 previewModelEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] private float previewModelScale = 1f;
    [SerializeField] private Vector3 previewFocusOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private float previewDistanceMultiplier = 3.2f;
    [SerializeField] private int previewTextureSize = 512;
    [SerializeField] private Color previewBackgroundColor = new Color(0f, 0f, 0f, 0f);

    private readonly Dictionary<int, PixieMenuItemUI> runtimeItemByPixieId = new Dictionary<int, PixieMenuItemUI>();
    private readonly List<int> runtimePixieIds = new List<int>();
    private readonly List<int> rootPixieIds = new List<int>();
    private readonly List<FairyStatTable> workingBuffStats = new List<FairyStatTable>(3);
    private readonly List<FairyStatTable> workingDebuffStats = new List<FairyStatTable>(3);
    private readonly Dictionary<string, string> localizedTextByKey = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, Sprite> pixieIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Sprite> itemIconByItemId = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, int> rootPixieIdByPixieId = new Dictionary<int, int>();
    private readonly Dictionary<int, MenuPreviewRuntime> menuPreviewByRootPixieId = new Dictionary<int, MenuPreviewRuntime>();

    private InventoryManager inventory;
    private PixieInventoryModule pixieModule;
    private bool buttonsSubscribed;
    private bool pendingRefresh;
    private bool listBuilt;
    private bool shouldResetScrollPosition = true;
    private int selectedPixieId;
    private int previewPixieId;
    private int previewRequestVersion;
    private RawImage previewSurface;
    private RenderTexture previewTexture;
    private Transform previewStageTransform;
    private Camera previewCamera;
    private GameObject previewInstance;
    private AsyncOperationHandle<GameObject>? previewLoadHandle;
    private Image growthFragmentCostIcon;
    private TMP_Text growthFragmentCostText;
    private bool growthCostStyleCached;
    private float defaultGrowthCostFontSize;
    private TextAlignmentOptions defaultGrowthCostAlignment;
    private Vector2 defaultGrowthCostSize;
    private Vector2 defaultGrowthCostAnchoredPosition;
    private Vector2 defaultGrowthGoldIconAnchoredPosition;
    private Vector2 defaultGrowthGoldIconSize;

    protected override void Initialize()
    {
        ResolveReferences();
        ClearSlots(buffSlots);
        ClearSlots(debuffSlots);
        SetPreviewVisible(false);
    }

    protected override void Subscribe()
    {
        ResolveReferences();
        SubscribeButtons();
        BindInventory();
        shouldResetScrollPosition = true;
        pendingRefresh = true;
    }

    protected override void Unsubscribe()
    {
        UnsubscribeButtons();
        UnbindInventory();
        pendingRefresh = false;
        SetPreviewVisible(false);
        ClearPreviewInstance();
        ClearAllMenuPreviews();
    }

    protected override void RefreshView()
    {
        ResolveReferences();
        if (!CanRender())
        {
            pendingRefresh = true;
            return;
        }

        EnsureLocalizedTextCache();
        EnsureRootPixieIds();
        EnsureListBuilt();
        EnsureValidSelection();
        RefreshPixieList();
        RefreshSelectedPixieDetail();
        ResetScrollPositionIfNeeded();
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
            return;

        if (pendingRefresh)
        {
            pendingRefresh = false;
            RefreshView();
        }

        if (previewCamera != null && previewSurface != null && previewTexture != null && previewInstance != null)
            previewCamera.Render();

        foreach (MenuPreviewRuntime runtime in menuPreviewByRootPixieId.Values)
        {
            if (runtime == null ||
                runtime.camera == null ||
                runtime.surface == null ||
                runtime.texture == null ||
                runtime.instance == null)
            {
                continue;
            }

            runtime.camera.Render();
        }
    }

    private void OnDestroy()
    {
        ClearPreviewInstance();
        ClearAllMenuPreviews();
        ReleasePreviewTexture();

        if (previewStageTransform != null)
            Destroy(previewStageTransform.gameObject);
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (pixieListContentRoot == null && pixieScrollRect != null)
            pixieListContentRoot = pixieScrollRect.content;

        EnsureGrowthCostVisuals();
    }

    private bool CanRender()
    {
        if (panelRoot == null ||
            pixieScrollRect == null ||
            pixieListContentRoot == null ||
            pixieItemTemplate == null ||
            pixieModelPanel == null ||
            pixieNameText == null ||
            pixieLevelText == null ||
            pixieTypeText == null ||
            growthButton == null ||
            growthLabelText == null ||
            growthCostText == null ||
            summonButton == null ||
            summonButtonText == null)
        {
            return false;
        }

        if (!pixieItemTemplate.HasBindings)
            return false;

        if (!AreSlotsValid(buffSlots) || !AreSlotsValid(debuffSlots))
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.FairyInfoDict == null)
            return false;

        return BindInventory();
    }

    private bool BindInventory()
    {
        InventoryManager currentInventory = InventoryManager.Instance;
        PixieInventoryModule currentPixieModule = currentInventory != null ? currentInventory.GetModule<PixieInventoryModule>() : null;
        if (currentInventory == null || currentPixieModule == null)
            return false;

        if (inventory == currentInventory && pixieModule == currentPixieModule)
            return true;

        UnbindInventory();

        inventory = currentInventory;
        pixieModule = currentPixieModule;
        inventory.OnItemAmountChanged += HandleInventoryAmountChanged;
        pixieModule.OnPixieInventoryChanged += HandlePixieInventoryChanged;
        pixieModule.OnPixieEquipped += HandlePixieEquipped;
        return true;
    }

    private void UnbindInventory()
    {
        if (inventory != null)
            inventory.OnItemAmountChanged -= HandleInventoryAmountChanged;

        if (pixieModule != null)
        {
            pixieModule.OnPixieInventoryChanged -= HandlePixieInventoryChanged;
            pixieModule.OnPixieEquipped -= HandlePixieEquipped;
        }

        inventory = null;
        pixieModule = null;
    }

    private void SubscribeButtons()
    {
        if (buttonsSubscribed)
            return;

        growthButton.onClick.AddListener(HandleGrowthClicked);
        summonButton.onClick.AddListener(HandleSummonClicked);
        buttonsSubscribed = true;
    }

    private void UnsubscribeButtons()
    {
        if (!buttonsSubscribed)
            return;

        growthButton.onClick.RemoveListener(HandleGrowthClicked);
        summonButton.onClick.RemoveListener(HandleSummonClicked);
        buttonsSubscribed = false;
    }

    private void EnsureLocalizedTextCache()
    {
        localizedTextByKey.Clear();

        if (DataManager.Instance?.StringDict == null)
            return;

        foreach (StringTable entry in DataManager.Instance.StringDict.Values)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.stringKey) || string.IsNullOrWhiteSpace(entry.Kor))
                continue;

            localizedTextByKey[entry.stringKey] = entry.Kor;
        }
    }

    private void EnsureRootPixieIds()
    {
        if (DataManager.Instance?.FairyInfoDict == null)
            return;

        if (rootPixieIds.Count > 0 && rootPixieIdByPixieId.Count == DataManager.Instance.FairyInfoDict.Count)
            return;

        rootPixieIds.Clear();
        rootPixieIdByPixieId.Clear();

        HashSet<int> evolvedPixieIds = new HashSet<int>();
        foreach (FairyInfoTable fairyInfo in DataManager.Instance.FairyInfoDict.Values)
        {
            if (fairyInfo != null && fairyInfo.nextID != 0)
                evolvedPixieIds.Add(fairyInfo.nextID);
        }

        List<int> sortedPixieIds = new List<int>(DataManager.Instance.FairyInfoDict.Keys);
        sortedPixieIds.Sort();

        for (int i = 0; i < sortedPixieIds.Count; i++)
        {
            int rootPixieId = sortedPixieIds[i];
            if (evolvedPixieIds.Contains(rootPixieId))
                continue;

            rootPixieIds.Add(rootPixieId);
            CacheRootPixieLine(rootPixieId);
        }
    }

    private void CacheRootPixieLine(int rootPixieId)
    {
        int currentPixieId = rootPixieId;
        for (int i = 0; i < 16; i++)
        {
            if (!rootPixieIdByPixieId.ContainsKey(currentPixieId))
                rootPixieIdByPixieId[currentPixieId] = rootPixieId;

            if (!DataManager.Instance.FairyInfoDict.TryGetValue(currentPixieId, out FairyInfoTable fairyInfo) ||
                fairyInfo == null ||
                fairyInfo.nextID == 0)
            {
                break;
            }

            currentPixieId = fairyInfo.nextID;
        }
    }

    private int ResolveRootPixieId(int pixieId)
    {
        EnsureRootPixieIds();

        if (pixieId == 0)
            return 0;

        if (pixieId != 0 && rootPixieIdByPixieId.TryGetValue(pixieId, out int rootPixieId))
            return rootPixieId;

        return rootPixieIds.Count > 0 ? rootPixieIds[0] : 0;
    }

    private int ResolveDisplayedPixieId(int rootPixieId)
    {
        if (rootPixieId == 0 || DataManager.Instance?.FairyInfoDict == null)
            return 0;

        int currentPixieId = rootPixieId;
        int displayedPixieId = 0;

        for (int i = 0; i < 16; i++)
        {
            if (pixieModule != null && pixieModule.IsOwned(currentPixieId))
                displayedPixieId = currentPixieId;

            if (!DataManager.Instance.FairyInfoDict.TryGetValue(currentPixieId, out FairyInfoTable fairyInfo) ||
                fairyInfo == null ||
                fairyInfo.nextID == 0)
            {
                break;
            }

            currentPixieId = fairyInfo.nextID;
        }

        return displayedPixieId != 0 ? displayedPixieId : rootPixieId;
    }

    private void EnsureListBuilt()
    {
        if (listBuilt && runtimeItemByPixieId.Count == rootPixieIds.Count)
            return;

        ClearRuntimeList();
        runtimePixieIds.Clear();

        for (int i = pixieListContentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = pixieListContentRoot.GetChild(i);
            if (child == pixieItemTemplate.transform)
                continue;

            Destroy(child.gameObject);
        }

        pixieItemTemplate.gameObject.SetActive(false);

        for (int i = 0; i < rootPixieIds.Count; i++)
        {
            int pixieId = rootPixieIds[i];
            PixieMenuItemUI view = Instantiate(pixieItemTemplate, pixieListContentRoot);
            view.gameObject.name = $"PixieItem_{pixieId}";
            view.gameObject.SetActive(true);

            int capturedPixieId = pixieId;
            view.Button.onClick.AddListener(() => HandlePixieClicked(capturedPixieId));

            runtimeItemByPixieId[pixieId] = view;
            runtimePixieIds.Add(pixieId);
        }

        listBuilt = true;
    }

    private void ClearRuntimeList()
    {
        ClearAllMenuPreviews();

        foreach (KeyValuePair<int, PixieMenuItemUI> pair in runtimeItemByPixieId)
        {
            if (pair.Value == null)
                continue;

            pair.Value.Button.onClick.RemoveAllListeners();
            Destroy(pair.Value.gameObject);
        }

        runtimeItemByPixieId.Clear();
    }

    private void EnsureValidSelection()
    {
        if (selectedPixieId != 0)
        {
            int selectedRootPixieId = ResolveRootPixieId(selectedPixieId);
            int displayedPixieId = ResolveDisplayedPixieId(selectedRootPixieId);
            if (displayedPixieId != 0)
            {
                selectedPixieId = displayedPixieId;
                return;
            }
        }

        int equippedPixieId = pixieModule != null ? pixieModule.EquippedPixiedID() : 0;
        if (equippedPixieId != 0 && DataManager.Instance.FairyInfoDict.ContainsKey(equippedPixieId))
        {
            selectedPixieId = equippedPixieId;
            return;
        }

        foreach (int pixieId in runtimePixieIds)
        {
            int displayedPixieId = ResolveDisplayedPixieId(pixieId);
            if (pixieModule != null && pixieModule.IsOwned(displayedPixieId))
            {
                selectedPixieId = displayedPixieId;
                return;
            }
        }

        selectedPixieId = runtimePixieIds.Count > 0 ? ResolveDisplayedPixieId(runtimePixieIds[0]) : 0;
    }

    private void RefreshPixieList()
    {
        int equippedPixieId = pixieModule != null ? pixieModule.EquippedPixiedID() : 0;
        int selectedRootPixieId = ResolveRootPixieId(selectedPixieId);
        int equippedRootPixieId = ResolveRootPixieId(equippedPixieId);

        for (int i = 0; i < runtimePixieIds.Count; i++)
        {
            int rootPixieId = runtimePixieIds[i];
            int pixieId = ResolveDisplayedPixieId(rootPixieId);

            if (!runtimeItemByPixieId.TryGetValue(rootPixieId, out PixieMenuItemUI view) || view == null)
                continue;

            if (!DataManager.Instance.FairyInfoDict.TryGetValue(pixieId, out FairyInfoTable fairyInfo))
                continue;

            bool isOwned = pixieModule != null && pixieModule.IsOwned(pixieId);
            bool isSelected = rootPixieId == selectedRootPixieId;
            bool isSummoned = rootPixieId == equippedRootPixieId;

            view.LabelText.text = BuildListLabel(fairyInfo, isOwned, isSummoned);
            RefreshMenuPreview(rootPixieId, view, fairyInfo, i);

            ApplyGradeVisuals(view.GradeImages, fairyInfo);
            ApplyListVisuals(view, isSelected, isOwned);
        }
    }

    private void RefreshSelectedPixieDetail()
    {
        if (selectedPixieId == 0 || !DataManager.Instance.FairyInfoDict.TryGetValue(selectedPixieId, out FairyInfoTable fairyInfo))
        {
            ClearDetailView();
            return;
        }

        OwnedPixieData ownedPixie = pixieModule != null ? pixieModule.GetOwnedPixieData(selectedPixieId) : null;
        pixieNameText.text = ResolvePixieName(fairyInfo);
        pixieLevelText.text = ownedPixie != null ? $"Lv. {Mathf.Max(1, ownedPixie.level)}" : LockedLevelLabel;
        ApplyPixieType(fairyInfo);
        RefreshStatSlots(fairyInfo, ownedPixie);
        RefreshActionButtons(fairyInfo, ownedPixie);
        RefreshPreview(fairyInfo);
    }

    private void ClearDetailView()
    {
        pixieNameText.text = string.Empty;
        pixieLevelText.text = LockedLevelLabel;
        pixieTypeText.text = UnknownTypeLabel;
        pixieTypeText.color = unknownTypeColor;
        growthLabelText.text = GrowthLabel;
        growthCostText.text = string.Empty;
        ApplyGrowthCostVisuals(null, null, false, false, false);
        growthButton.interactable = false;
        summonButtonText.text = SummonLabel;
        summonButton.interactable = false;
        ClearSlots(buffSlots);
        ClearSlots(debuffSlots);
        SetPreviewVisible(false);
        ClearPreviewInstance();
    }

    private void RefreshStatSlots(FairyInfoTable fairyInfo, OwnedPixieData ownedPixie)
    {
        workingBuffStats.Clear();
        workingDebuffStats.Clear();

        AddStatToWorkingSet(fairyInfo.statID1, ownedPixie);
        AddStatToWorkingSet(fairyInfo.statID2, ownedPixie);
        AddStatToWorkingSet(fairyInfo.statID3, ownedPixie);

        ApplyStatSlots(buffSlots, workingBuffStats, ownedPixie);
        ApplyStatSlots(debuffSlots, workingDebuffStats, ownedPixie);
    }

    private void AddStatToWorkingSet(int statId, OwnedPixieData ownedPixie)
    {
        if (statId == 0 || DataManager.Instance.FairyStatDict == null)
            return;

        if (!DataManager.Instance.FairyStatDict.TryGetValue(statId, out FairyStatTable stat) || stat == null)
            return;

        float value = CalculateStatValue(stat, ownedPixie);
        if (value > 0f)
            workingBuffStats.Add(stat);
        else if (value < 0f)
            workingDebuffStats.Add(stat);
    }

    private void ApplyStatSlots(PixieStatSlotUI[] slots, List<FairyStatTable> stats, OwnedPixieData ownedPixie)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            PixieStatSlotUI slot = slots[i];
            if (slot == null)
                continue;

            if (i >= stats.Count)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            FairyStatTable stat = stats[i];
            slot.gameObject.SetActive(true);
            slot.ApplyResponsiveLayout();

            if (slot.Button != null)
                slot.Button.interactable = false;

            if (slot.IconImage != null)
                slot.IconImage.sprite = IconManager.GetIcon(stat.statType);

            if (slot.LabelText != null)
                slot.LabelText.text = BuildStatLabel(stat, ownedPixie);
        }
    }

    private void RefreshActionButtons(FairyInfoTable fairyInfo, OwnedPixieData ownedPixie)
    {
        string actionLabel = GrowthLabel;
        string actionCost = string.Empty;
        bool canGrow = false;

        if (ownedPixie == null)
        {
            actionLabel = UnlockLabel;
            actionCost = new BigDouble(PixieUnlockCost).ToString("F2");
            canGrow = pixieModule != null && pixieModule.CanUnlockPixie(fairyInfo.ID);
        }
        else if (ownedPixie.CanEvolve() && fairyInfo.nextID != 0)
        {
            actionLabel = EvolveLabel;
            actionCost = string.Empty;
            canGrow = true;
        }
        else if (!ownedPixie.IsMaxLevel())
        {
            actionLabel = GrowthLabel;
            actionCost = BuildLevelUpCostLabel(ownedPixie);
            canGrow = pixieModule != null && pixieModule.CanLevelUp(fairyInfo.ID);
        }
        else
        {
            actionLabel = MaxLabel;
            actionCost = string.Empty;
            canGrow = false;
        }

        growthLabelText.text = actionLabel;
        growthCostText.text = actionCost;
        ApplyGrowthCostVisuals(fairyInfo, ownedPixie, ownedPixie == null, actionLabel == GrowthLabel, !string.IsNullOrEmpty(actionCost));
        growthButton.interactable = canGrow;

        bool isOwned = ownedPixie != null;
        bool isSummoned = pixieModule != null && pixieModule.EquippedPixiedID() == fairyInfo.ID;
        summonButtonText.text = isSummoned ? SummonedLabel : SummonLabel;
        summonButton.interactable = isOwned && !isSummoned;
    }

    private string BuildLevelUpCostLabel(OwnedPixieData ownedPixie)
    {
        if (ownedPixie == null)
            return string.Empty;

        string goldCost = ownedPixie.GetLevelUpCost().ToString("F2");
        if (ownedPixie.gradeTable != null && ownedPixie.gradeTable.fairyGrade == FairyGrade.Mythic)
        {
            return $"{goldCost}\n{FormatPixieFragmentCost(ownedPixie.GetFragmentCost())}";
        }

        return goldCost;
    }

    private static string FormatPixieFragmentCost(int fragmentCost)
    {
        if (fragmentCost <= 0)
            return "0";

        return fragmentCost >= 1000
            ? new BigDouble(fragmentCost).ToString("F0")
            : fragmentCost.ToString(CultureInfo.InvariantCulture);
    }

    private void EnsureGrowthCostVisuals()
    {
        if (growthCostText == null)
            return;

        if (growthGoldCostIcon == null)
        {
            Image[] childImages = growthCostText.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < childImages.Length; i++)
            {
                Image image = childImages[i];
                if (image == null || image.transform == growthCostText.transform)
                    continue;

                growthGoldCostIcon = image;
                break;
            }
        }

        CacheGrowthCostDefaults();

        if (growthFragmentCostIcon != null)
        {
            EnsureGrowthFragmentCostText();
            if (growthFragmentCostText != null && growthFragmentCostIcon.transform.parent != growthFragmentCostText.transform)
                growthFragmentCostIcon.rectTransform.SetParent(growthFragmentCostText.transform, false);
            return;
        }

        EnsureGrowthFragmentCostText();

        Transform existing = growthCostText.transform.Find(FragmentCostIconObjectName);
        if (existing != null)
        {
            growthFragmentCostIcon = existing.GetComponent<Image>();
            if (growthFragmentCostText != null && growthFragmentCostIcon != null && growthFragmentCostIcon.transform.parent != growthFragmentCostText.transform)
                growthFragmentCostIcon.rectTransform.SetParent(growthFragmentCostText.transform, false);
            return;
        }

        GameObject iconObject = new GameObject(FragmentCostIconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(growthFragmentCostText != null ? growthFragmentCostText.transform : growthCostText.transform, false);
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = defaultGrowthGoldIconAnchoredPosition;
        iconRect.sizeDelta = defaultGrowthGoldIconSize;

        growthFragmentCostIcon = iconObject.GetComponent<Image>();
        growthFragmentCostIcon.raycastTarget = false;
        growthFragmentCostIcon.enabled = false;
    }

    private void CacheGrowthCostDefaults()
    {
        if (growthCostStyleCached || growthCostText == null)
            return;

        growthCostStyleCached = true;
        defaultGrowthCostFontSize = growthCostText.fontSize;
        defaultGrowthCostAlignment = growthCostText.alignment;
        defaultGrowthCostSize = growthCostText.rectTransform.sizeDelta;
        defaultGrowthCostAnchoredPosition = growthCostText.rectTransform.anchoredPosition;

        if (growthGoldCostIcon != null)
        {
            RectTransform iconRect = growthGoldCostIcon.rectTransform;
            defaultGrowthGoldIconAnchoredPosition = iconRect.anchoredPosition;
            defaultGrowthGoldIconSize = iconRect.sizeDelta;
        }
        else
        {
            defaultGrowthGoldIconAnchoredPosition = new Vector2(-20f, 0f);
            defaultGrowthGoldIconSize = new Vector2(50f, 50f);
        }
    }

    private void EnsureGrowthFragmentCostText()
    {
        if (growthCostText == null)
            return;

        if (growthFragmentCostText != null)
            return;

        Transform existing = growthCostText.transform.parent != null
            ? growthCostText.transform.parent.Find(FragmentCostTextObjectName)
            : null;
        if (existing != null)
        {
            growthFragmentCostText = existing.GetComponent<TMP_Text>();
            return;
        }

        GameObject textObject = new GameObject(FragmentCostTextObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(growthCostText.transform.parent, false);
        textRect.anchorMin = growthCostText.rectTransform.anchorMin;
        textRect.anchorMax = growthCostText.rectTransform.anchorMax;
        textRect.pivot = growthCostText.rectTransform.pivot;
        textRect.anchoredPosition = growthCostText.rectTransform.anchoredPosition;
        textRect.sizeDelta = growthCostText.rectTransform.sizeDelta;

        TextMeshProUGUI fragmentText = textObject.GetComponent<TextMeshProUGUI>();
        fragmentText.font = growthCostText.font;
        fragmentText.fontSharedMaterial = growthCostText.fontSharedMaterial;
        fragmentText.fontSize = growthCostText.fontSize;
        fragmentText.fontStyle = growthCostText.fontStyle;
        fragmentText.color = growthCostText.color;
        fragmentText.alignment = growthCostText.alignment;
        fragmentText.enableAutoSizing = growthCostText.enableAutoSizing;
        fragmentText.fontSizeMin = growthCostText.fontSizeMin;
        fragmentText.fontSizeMax = growthCostText.fontSizeMax;
        fragmentText.textWrappingMode = growthCostText.textWrappingMode;
        fragmentText.overflowMode = growthCostText.overflowMode;
        fragmentText.raycastTarget = false;
        fragmentText.text = string.Empty;
        fragmentText.gameObject.SetActive(false);

        growthFragmentCostText = fragmentText;
    }

    private void ApplyGrowthCostVisuals(FairyInfoTable fairyInfo, OwnedPixieData ownedPixie, bool isUnlockCost, bool isGrowthCost, bool hasCost)
    {
        EnsureGrowthCostVisuals();

        if (growthCostText == null)
            return;

        growthCostText.fontSize = defaultGrowthCostFontSize;
        growthCostText.alignment = defaultGrowthCostAlignment;
        growthCostText.rectTransform.sizeDelta = defaultGrowthCostSize;
        growthCostText.rectTransform.anchoredPosition = defaultGrowthCostAnchoredPosition;

        if (growthFragmentCostText != null)
        {
            growthFragmentCostText.text = string.Empty;
            growthFragmentCostText.fontSize = defaultGrowthCostFontSize;
            growthFragmentCostText.alignment = defaultGrowthCostAlignment;
            growthFragmentCostText.rectTransform.sizeDelta = defaultGrowthCostSize;
            growthFragmentCostText.rectTransform.anchoredPosition = defaultGrowthCostAnchoredPosition;
            growthFragmentCostText.gameObject.SetActive(false);
        }

        if (growthGoldCostIcon != null)
        {
            growthGoldCostIcon.enabled = false;
            growthGoldCostIcon.rectTransform.anchoredPosition = defaultGrowthGoldIconAnchoredPosition;
            growthGoldCostIcon.rectTransform.sizeDelta = defaultGrowthGoldIconSize;
        }

        if (growthFragmentCostIcon != null)
        {
            growthFragmentCostIcon.enabled = false;
            growthFragmentCostIcon.rectTransform.anchoredPosition = defaultGrowthGoldIconAnchoredPosition;
            growthFragmentCostIcon.rectTransform.sizeDelta = defaultGrowthGoldIconSize;
        }

        if (!hasCost)
            return;

        bool showFragmentCost = isUnlockCost || (ownedPixie != null &&
            ownedPixie.gradeTable != null &&
            ownedPixie.gradeTable.fairyGrade == FairyGrade.Mythic &&
            isGrowthCost &&
            !ownedPixie.CanEvolve() &&
            !ownedPixie.IsMaxLevel());

        if (showFragmentCost && fairyInfo != null && growthFragmentCostIcon != null)
        {
            growthFragmentCostIcon.sprite = LoadItemIcon(fairyInfo.fragmentItemID);
            growthFragmentCostIcon.enabled = growthFragmentCostIcon.sprite != null;
        }

        if (ownedPixie != null &&
            ownedPixie.gradeTable != null &&
            ownedPixie.gradeTable.fairyGrade == FairyGrade.Mythic &&
            isGrowthCost &&
            !ownedPixie.CanEvolve() &&
            !ownedPixie.IsMaxLevel())
        {
            string[] costLines = growthCostText.text.Split('\n');
            string goldCost = costLines.Length > 0 ? costLines[0] : string.Empty;
            string fragmentCost = costLines.Length > 1 ? costLines[1] : string.Empty;

            growthCostText.fontSize = MultiLineGrowthCostFontSize;
            growthCostText.alignment = TextAlignmentOptions.TopLeft;
            growthCostText.rectTransform.sizeDelta = defaultGrowthCostSize;
            growthCostText.rectTransform.anchoredPosition = defaultGrowthCostAnchoredPosition;
            growthCostText.text = goldCost;

            if (growthGoldCostIcon != null)
            {
                growthGoldCostIcon.enabled = true;
                growthGoldCostIcon.rectTransform.anchoredPosition = defaultGrowthGoldIconAnchoredPosition;
                growthGoldCostIcon.rectTransform.sizeDelta = MultiLineCostIconSize;
            }

            if (growthFragmentCostText != null)
            {
                growthFragmentCostText.fontSize = MultiLineGrowthCostFontSize;
                growthFragmentCostText.alignment = TextAlignmentOptions.TopLeft;
                growthFragmentCostText.rectTransform.sizeDelta = defaultGrowthCostSize;
                growthFragmentCostText.rectTransform.anchoredPosition = defaultGrowthCostAnchoredPosition + new Vector2(0f, -MultiLineCostLineSpacing);
                growthFragmentCostText.text = fragmentCost;
                growthFragmentCostText.gameObject.SetActive(true);
            }

            if (growthFragmentCostIcon != null && growthFragmentCostText != null)
            {
                growthFragmentCostIcon.enabled = growthFragmentCostIcon.sprite != null;
                growthFragmentCostIcon.rectTransform.anchoredPosition = defaultGrowthGoldIconAnchoredPosition;
                growthFragmentCostIcon.rectTransform.sizeDelta = MultiLineCostIconSize;
            }

            return;
        }

        if (isUnlockCost)
        {
            if (growthFragmentCostIcon != null)
                growthFragmentCostIcon.enabled = growthFragmentCostIcon.sprite != null;
            return;
        }

        if (growthGoldCostIcon != null)
            growthGoldCostIcon.enabled = true;
    }

    private Sprite LoadItemIcon(int itemId)
    {
        if (itemId == 0 || DataManager.Instance?.ItemInfoDict == null)
            return null;

        if (itemIconByItemId.TryGetValue(itemId, out Sprite cachedIcon))
            return cachedIcon;

        if (!DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out ItemInfoTable itemInfo) ||
            itemInfo == null ||
            string.IsNullOrWhiteSpace(itemInfo.itemIcon))
        {
            itemIconByItemId[itemId] = null;
            return null;
        }

        string key = itemInfo.itemIcon.Trim();
        Sprite sprite = Resources.Load<Sprite>(key);
        if (sprite == null)
        {
            int extensionIndex = key.LastIndexOf(".", StringComparison.Ordinal);
            if (extensionIndex > 0)
                sprite = Resources.Load<Sprite>(key[..extensionIndex]);
        }

        if (sprite == null)
        {
            const string resourcesToken = "Resources/";
            int resourcesIndex = key.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0)
            {
                string relativePath = key[(resourcesIndex + resourcesToken.Length)..];
                int extensionIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
                if (extensionIndex > 0)
                    relativePath = relativePath[..extensionIndex];

                sprite = Resources.Load<Sprite>(relativePath);
            }
        }

        itemIconByItemId[itemId] = sprite;
        return sprite;
    }

    private void ApplyPixieType(FairyInfoTable fairyInfo)
    {
        string typeLabel = ResolvePixieTypeLabel(fairyInfo, out Color typeColor);
        pixieTypeText.text = typeLabel;
        pixieTypeText.color = typeColor;
    }

    private string ResolvePixieTypeLabel(FairyInfoTable fairyInfo, out Color typeColor)
    {
        string prefabPath = fairyInfo != null ? fairyInfo.prefabPath ?? string.Empty : string.Empty;
        if (prefabPath.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            typeColor = attackTypeColor;
            return AttackTypeLabel;
        }

        if (prefabPath.IndexOf("Defensive", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            typeColor = defenseTypeColor;
            return DefenseTypeLabel;
        }

        if (prefabPath.IndexOf("Utill", StringComparison.OrdinalIgnoreCase) >= 0 ||
            prefabPath.IndexOf("Utility", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            typeColor = utilityTypeColor;
            return UtilityTypeLabel;
        }

        typeColor = unknownTypeColor;
        return UnknownTypeLabel;
    }

    private void HandlePixieClicked(int pixieId)
    {
        int displayedPixieId = ResolveDisplayedPixieId(pixieId);
        if (selectedPixieId == displayedPixieId)
            return;

        selectedPixieId = displayedPixieId;
        RequestRefresh();
    }

    private void HandleGrowthClicked()
    {
        if (pixieModule == null || !DataManager.Instance.FairyInfoDict.TryGetValue(selectedPixieId, out FairyInfoTable fairyInfo))
            return;

        int selectedRootPixieId = ResolveRootPixieId(selectedPixieId);

        if (!pixieModule.TryUpgradePixie(selectedPixieId))
            return;

        int displayedPixieId = ResolveDisplayedPixieId(selectedRootPixieId);
        if (displayedPixieId != 0)
            selectedPixieId = displayedPixieId;
        else if (fairyInfo.nextID != 0 && pixieModule.IsOwned(fairyInfo.nextID))
            selectedPixieId = fairyInfo.nextID;

        RequestRefresh();
    }

    private void HandleSummonClicked()
    {
        if (pixieModule == null)
            return;

        OwnedPixieData ownedPixie = pixieModule.GetOwnedPixieData(selectedPixieId);
        if (ownedPixie == null || pixieModule.EquippedPixiedID() == selectedPixieId)
            return;

        pixieModule.EquipPixie(selectedPixieId);
        RequestRefresh();
    }

    private void HandleInventoryAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        RequestRefresh();
    }

    private void HandlePixieInventoryChanged()
    {
        RequestRefresh();
    }

    private void HandlePixieEquipped(OwnedPixieData data)
    {
        RequestRefresh();
    }

    private void RequestRefresh()
    {
        pendingRefresh = true;
    }

    private void ResetScrollPositionIfNeeded()
    {
        if (!shouldResetScrollPosition || pixieScrollRect == null)
            return;

        shouldResetScrollPosition = false;
        Canvas.ForceUpdateCanvases();
        pixieScrollRect.horizontalNormalizedPosition = 0f;
    }

    private string BuildListLabel(FairyInfoTable fairyInfo, bool isOwned, bool isSummoned)
    {
        string name = ResolvePixieName(fairyInfo);
        if (isSummoned)
            return $"{name}\n[{SummonedLabel}]";

        if (!isOwned)
            return $"{name}\n[{LockedLevelLabel}]";

        OwnedPixieData ownedPixie = pixieModule != null ? pixieModule.GetOwnedPixieData(fairyInfo.ID) : null;
        return ownedPixie != null ? $"{name}\nLv. {Mathf.Max(1, ownedPixie.level)}" : name;
    }

    private void ApplyGradeVisuals(Image[] gradeImages, FairyInfoTable fairyInfo)
    {
        if (gradeImages == null || fairyInfo == null || DataManager.Instance.FairyGradeDict == null)
            return;

        int activeCount = 1;
        if (DataManager.Instance.FairyGradeDict.TryGetValue(fairyInfo.gradeID, out FairyGradeTable gradeTable) && gradeTable != null)
            activeCount = Mathf.Clamp((int)gradeTable.fairyGrade + 1, 1, gradeImages.Length);

        for (int i = 0; i < gradeImages.Length; i++)
        {
            if (gradeImages[i] == null)
                continue;

            gradeImages[i].gameObject.SetActive(i < activeCount);
        }
    }

    private void ApplyListVisuals(PixieMenuItemUI view, bool isSelected, bool isOwned)
    {
        float alpha = isOwned ? 1f : lockedItemAlpha;
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(view.gameObject);
        canvasGroup.alpha = alpha;
        view.transform.localScale = Vector3.one * (isSelected ? selectedItemScale : unselectedItemScale);
        view.LabelText.color = isSelected ? selectedLabelColor : (isOwned ? ownedLabelColor : lockedLabelColor);
        view.Button.interactable = true;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    private void RefreshMenuPreview(int rootPixieId, PixieMenuItemUI view, FairyInfoTable fairyInfo, int slotIndex)
    {
        if (view == null || view.IconImage == null)
            return;

        MenuPreviewRuntime runtime = GetOrCreateMenuPreviewRuntime(rootPixieId, view, slotIndex);
        if (runtime == null || runtime.surface == null)
            return;

        view.IconImage.enabled = false;

        if (fairyInfo == null || string.IsNullOrWhiteSpace(fairyInfo.prefabPath))
        {
            SetMenuPreviewVisible(runtime, false);
            ClearMenuPreviewInstance(runtime);
            runtime.previewPixieId = 0;
            return;
        }

        if (runtime.previewPixieId == fairyInfo.ID && runtime.instance != null)
        {
            SetMenuPreviewVisible(runtime, true);
            return;
        }

        runtime.previewPixieId = fairyInfo.ID;
        ClearMenuPreviewInstance(runtime);

        int requestVersion = ++runtime.requestVersion;
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(fairyInfo.prefabPath);
        runtime.loadHandle = handle;

        handle.Completed += operation =>
        {
            if (runtime.loadHandle.HasValue && runtime.loadHandle.Value.Equals(operation))
                runtime.loadHandle = null;

            if (requestVersion != runtime.requestVersion || this == null || runtime.stageTransform == null)
            {
                if (operation.IsValid())
                    Addressables.Release(operation);
                return;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
            {
                if (operation.IsValid())
                    Addressables.Release(operation);
                SetMenuPreviewVisible(runtime, false);
                return;
            }

            runtime.instance = Instantiate(operation.Result, runtime.stageTransform);
            if (operation.IsValid())
                Addressables.Release(operation);

            if (runtime.instance == null)
            {
                SetMenuPreviewVisible(runtime, false);
                return;
            }

            runtime.instance.name = $"PixieMenuPreview_{fairyInfo.ID}";
            runtime.instance.transform.localPosition = Vector3.zero;
            runtime.instance.transform.localRotation = Quaternion.Euler(previewModelEuler);
            runtime.instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, previewModelScale);
            SetLayerRecursively(runtime.instance, PixiePreviewLayer);
            FramePreviewInstance(runtime.instance, runtime.stageTransform, runtime.camera, MenuPreviewFocusOffset, MenuPreviewDistanceMultiplier, 0.75f, 10f);
            SetMenuPreviewVisible(runtime, true);
        };
    }

    private MenuPreviewRuntime GetOrCreateMenuPreviewRuntime(int rootPixieId, PixieMenuItemUI view, int slotIndex)
    {
        if (menuPreviewByRootPixieId.TryGetValue(rootPixieId, out MenuPreviewRuntime cachedRuntime) && cachedRuntime != null)
            return cachedRuntime;

        RectTransform previewRoot = view.IconImage.rectTransform;
        if (previewRoot == null)
            return null;

        MenuPreviewRuntime runtime = new MenuPreviewRuntime();

        GameObject previewSurfaceObject = new GameObject(MenuPreviewSurfaceObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        RectTransform previewRect = previewSurfaceObject.GetComponent<RectTransform>();
        previewRect.SetParent(previewRoot, false);
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;

        runtime.surface = previewSurfaceObject.GetComponent<RawImage>();
        runtime.surface.raycastTarget = false;
        runtime.surface.color = new Color(1f, 1f, 1f, 0f);

        runtime.texture = new RenderTexture(MenuPreviewTextureSize, MenuPreviewTextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = $"PixieMenuPreviewTexture_{rootPixieId}"
        };
        runtime.surface.texture = runtime.texture;

        GameObject stageObject = new GameObject($"{PreviewStageObjectName}_{rootPixieId}");
        runtime.stageTransform = stageObject.transform;
        runtime.stageTransform.position = new Vector3(20000f + (slotIndex * 500f), 10000f, 20000f);

        GameObject cameraObject = new GameObject($"{PreviewCameraObjectName}_{rootPixieId}");
        runtime.camera = cameraObject.AddComponent<Camera>();
        runtime.camera.transform.SetParent(runtime.stageTransform, false);
        runtime.camera.enabled = false;
        runtime.camera.clearFlags = CameraClearFlags.SolidColor;
        runtime.camera.backgroundColor = previewBackgroundColor;
        runtime.camera.cullingMask = 1 << PixiePreviewLayer;
        runtime.camera.nearClipPlane = 0.01f;
        runtime.camera.farClipPlane = 30f;
        runtime.camera.allowHDR = false;
        runtime.camera.allowMSAA = false;
        runtime.camera.targetTexture = runtime.texture;

        menuPreviewByRootPixieId[rootPixieId] = runtime;
        return runtime;
    }

    private void ClearAllMenuPreviews()
    {
        foreach (MenuPreviewRuntime runtime in menuPreviewByRootPixieId.Values)
            ReleaseMenuPreviewRuntime(runtime);

        menuPreviewByRootPixieId.Clear();
    }

    private static void ReleaseMenuPreviewRuntime(MenuPreviewRuntime runtime)
    {
        if (runtime == null)
            return;

        runtime.requestVersion++;
        ClearMenuPreviewInstance(runtime);

        if (runtime.stageTransform != null)
            Destroy(runtime.stageTransform.gameObject);

        if (runtime.texture != null)
        {
            runtime.texture.Release();
            Destroy(runtime.texture);
            runtime.texture = null;
        }

        if (runtime.surface != null)
        {
            runtime.surface.texture = null;
            Destroy(runtime.surface.gameObject);
            runtime.surface = null;
        }
    }

    private static void ClearMenuPreviewInstance(MenuPreviewRuntime runtime)
    {
        if (runtime == null)
            return;

        if (runtime.instance != null)
        {
            Destroy(runtime.instance);
            runtime.instance = null;
        }

        if (runtime.loadHandle.HasValue && runtime.loadHandle.Value.IsValid())
        {
            Addressables.Release(runtime.loadHandle.Value);
            runtime.loadHandle = null;
        }
    }

    private static void SetMenuPreviewVisible(MenuPreviewRuntime runtime, bool isVisible)
    {
        if (runtime?.surface == null)
            return;

        runtime.surface.color = isVisible ? Color.white : new Color(1f, 1f, 1f, 0f);
    }

    private string ResolvePixieName(FairyInfoTable fairyInfo)
    {
        if (fairyInfo == null)
            return UnknownTypeLabel;

        if (!string.IsNullOrWhiteSpace(fairyInfo.nameKey) &&
            localizedTextByKey.TryGetValue(fairyInfo.nameKey, out string localizedName) &&
            !string.IsNullOrWhiteSpace(localizedName))
        {
            return localizedName;
        }

        if (!string.IsNullOrWhiteSpace(fairyInfo.nameKey))
        {
            const string fairyNamePrefix = "FAIRY_NAME_";
            if (fairyInfo.nameKey.StartsWith(fairyNamePrefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(fairyInfo.nameKey.Substring(fairyNamePrefix.Length), out int parsedId))
            {
                return $"픽시 {parsedId}";
            }

            return fairyInfo.nameKey;
        }

        return $"픽시 {fairyInfo.ID}";
    }

    private Sprite ResolvePixieIcon(FairyInfoTable fairyInfo)
    {
        if (fairyInfo == null || string.IsNullOrWhiteSpace(fairyInfo.iconPath))
            return null;

        string resourcePath = NormalizeResourcePath(fairyInfo.iconPath);
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        if (pixieIconCache.TryGetValue(resourcePath, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        pixieIconCache[resourcePath] = sprite;
        return sprite;
    }

    private static string NormalizeResourcePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return string.Empty;

        string normalized = rawPath.Replace('\\', '/').Trim();
        int resourcesIndex = normalized.IndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
            normalized = normalized.Substring(resourcesIndex + "Resources/".Length);

        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("Assets/".Length);

        int extensionIndex = normalized.LastIndexOf('.');
        if (extensionIndex > 0)
            normalized = normalized.Substring(0, extensionIndex);

        return normalized;
    }

    private void RefreshPreview(FairyInfoTable fairyInfo)
    {
        EnsurePreviewRuntime();
        if (previewSurface == null)
            return;

        if (fairyInfo == null || string.IsNullOrWhiteSpace(fairyInfo.prefabPath))
        {
            SetPreviewVisible(false);
            ClearPreviewInstance();
            previewPixieId = 0;
            return;
        }

        if (previewPixieId == fairyInfo.ID && previewInstance != null)
        {
            SetPreviewVisible(true);
            return;
        }

        previewPixieId = fairyInfo.ID;
        ClearPreviewInstance();

        int requestVersion = ++previewRequestVersion;
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(fairyInfo.prefabPath);
        previewLoadHandle = handle;

        handle.Completed += operation =>
        {
            if (previewLoadHandle.HasValue && previewLoadHandle.Value.Equals(operation))
                previewLoadHandle = null;

            if (requestVersion != previewRequestVersion || this == null || previewStageTransform == null)
            {
                if (operation.IsValid())
                    Addressables.Release(operation);
                return;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
            {
                if (operation.IsValid())
                    Addressables.Release(operation);
                SetPreviewVisible(false);
                return;
            }

            previewInstance = Instantiate(operation.Result, previewStageTransform);
            if (operation.IsValid())
                Addressables.Release(operation);

            if (previewInstance == null)
            {
                SetPreviewVisible(false);
                return;
            }

            previewInstance.name = $"PixiePreview_{fairyInfo.ID}";
            previewInstance.transform.localPosition = Vector3.zero;
            previewInstance.transform.localRotation = Quaternion.Euler(previewModelEuler);
            previewInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, previewModelScale);
            SetLayerRecursively(previewInstance, PixiePreviewLayer);
            FramePreviewInstance(previewInstance);
            SetPreviewVisible(true);
        };
    }

    private void EnsurePreviewRuntime()
    {
        if (pixieModelPanel == null)
            return;

        if (previewSurface == null)
        {
            Transform existing = pixieModelPanel.Find(PreviewSurfaceObjectName);
            if (existing != null)
                previewSurface = existing.GetComponent<RawImage>();

            if (previewSurface == null)
            {
                GameObject previewSurfaceObject = new GameObject(PreviewSurfaceObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                RectTransform previewRect = previewSurfaceObject.GetComponent<RectTransform>();
                previewRect.SetParent(pixieModelPanel, false);
                previewRect.anchorMin = Vector2.zero;
                previewRect.anchorMax = Vector2.one;
                previewRect.offsetMin = Vector2.zero;
                previewRect.offsetMax = Vector2.zero;
                previewRect.SetSiblingIndex(0);

                previewSurface = previewSurfaceObject.GetComponent<RawImage>();
                previewSurface.raycastTarget = false;
                previewSurface.color = Color.white;
            }
        }

        if (previewTexture == null)
        {
            int textureSize = Mathf.Max(128, previewTextureSize);
            previewTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "PixiePreviewTexture"
            };
        }

        if (previewStageTransform == null)
        {
            GameObject stageObject = new GameObject(PreviewStageObjectName);
            previewStageTransform = stageObject.transform;
            previewStageTransform.position = new Vector3(10000f, 10000f, 10000f);
        }

        if (previewCamera == null)
        {
            GameObject cameraObject = new GameObject(PreviewCameraObjectName);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.transform.SetParent(previewStageTransform, false);
            previewCamera.enabled = false;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = previewBackgroundColor;
            previewCamera.cullingMask = 1 << PixiePreviewLayer;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 100f;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.targetTexture = previewTexture;
        }

        if (previewSurface.texture != previewTexture)
            previewSurface.texture = previewTexture;
    }

    private void FramePreviewInstance(GameObject instance)
    {
        if (previewStageTransform == null || previewCamera == null || instance == null)
            return;

        FramePreviewInstance(instance, previewStageTransform, previewCamera, previewFocusOffset, previewDistanceMultiplier, 1.5f, 12f);
    }

    private static void FramePreviewInstance(
        GameObject instance,
        Transform stageTransform,
        Camera targetCamera,
        Vector3 focusOffset,
        float distanceMultiplier,
        float minDistance,
        float farClipMultiplier)
    {
        if (stageTransform == null || targetCamera == null || instance == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        Vector3 focusPoint = stageTransform.position + focusOffset;
        float distance = 3f;

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            focusPoint = bounds.center + focusOffset;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            distance = Mathf.Max(minDistance, radius * Mathf.Max(minDistance, distanceMultiplier));
            targetCamera.nearClipPlane = Mathf.Max(0.01f, distance * 0.05f);
            targetCamera.farClipPlane = Mathf.Max(25f, distance * farClipMultiplier);
        }

        Vector3 cameraDirection = Quaternion.Euler(12f, 0f, 0f) * Vector3.forward;
        targetCamera.transform.position = focusPoint - cameraDirection.normalized * distance;
        targetCamera.transform.rotation = Quaternion.LookRotation(focusPoint - targetCamera.transform.position, Vector3.up);
    }

    private void ClearPreviewInstance()
    {
        previewRequestVersion++;

        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }

        if (previewLoadHandle.HasValue && previewLoadHandle.Value.IsValid())
        {
            Addressables.Release(previewLoadHandle.Value);
            previewLoadHandle = null;
        }
    }

    private void ReleasePreviewTexture()
    {
        if (previewTexture == null)
            return;

        previewTexture.Release();
        Destroy(previewTexture);
        previewTexture = null;
    }

    private void SetPreviewVisible(bool isVisible)
    {
        if (previewSurface == null)
            return;

        previewSurface.color = isVisible ? Color.white : new Color(1f, 1f, 1f, 0f);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        Transform transform = target.transform;
        for (int i = 0; i < transform.childCount; i++)
            SetLayerRecursively(transform.GetChild(i).gameObject, layer);
    }

    private static bool AreSlotsValid(PixieStatSlotUI[] slots)
    {
        if (slots == null || slots.Length == 0)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || !slots[i].HasBindings)
                return false;
        }

        return true;
    }

    private static void ClearSlots(PixieStatSlotUI[] slots)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].gameObject.SetActive(false);
            if (slots[i].LabelText != null)
                slots[i].LabelText.text = string.Empty;
        }
    }

    private static float CalculateStatValue(FairyStatTable stat, OwnedPixieData ownedPixie)
    {
        if (stat == null)
            return 0f;

        int level = ownedPixie != null ? Mathf.Max(1, ownedPixie.level) : 1;
        int gradeBonus = ownedPixie != null && ownedPixie.gradeTable != null ? (int)ownedPixie.gradeTable.fairyGrade : 0;
        return stat.baseValue + (level * stat.lvGrowth) + (gradeBonus * stat.grdGrowth);
    }

    private static string BuildStatLabel(FairyStatTable stat, OwnedPixieData ownedPixie)
    {
        float value = CalculateStatValue(stat, ownedPixie);
        string prefix = value > 0f ? "+" : string.Empty;
        return $"{GetStatDisplayName(stat.statType)} {prefix}{FormatStatValue(value, stat.statType)}";
    }

    private static string GetStatDisplayName(StatType statType)
    {
        switch (statType)
        {
            case StatType.HP:
                return "체력";
            case StatType.HP_REGEN:
                return "체력 재생";
            case StatType.MP:
                return "마나";
            case StatType.MP_REGEN:
                return "마나 재생";
            case StatType.ATK:
                return "공격력";
            case StatType.ATK_SPEED:
                return "공격 속도";
            case StatType.PHYS_DEF:
                return "물리 방어";
            case StatType.MAGIC_DEF:
                return "마법 방어";
            case StatType.CRIT_CHANCE:
                return "치명타 확률";
            case StatType.CRIT_MULT:
                return "치명타 피해";
            case StatType.MOVE_SPEED:
                return "이동 속도";
            case StatType.COOLDOWN_REDUCE:
                return "쿨타임 감소";
            case StatType.GOLD_GAIN:
                return "골드 획득";
            case StatType.EXP_GAIN:
                return "경험치 획득";
            case StatType.BOSS_DMG:
                return "보스 피해";
            case StatType.NORMAL_DMG:
                return "일반 적 피해";
            case StatType.DMG_MULT:
                return "최종 피해";
            case StatType.ALL_STAT:
                return "전체 능력치";
            default:
                return statType.ToString();
        }
    }

    private static string FormatStatValue(float value, StatType statType)
    {
        if (StatGroups.MultTypes.Contains(statType))
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}%", value * 100f);

        if (Mathf.Abs(value) >= 1000f)
            return new BigDouble(value).ToString("F2");

        if (Mathf.Abs(value - Mathf.Round(value)) <= 0.01f)
            return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
