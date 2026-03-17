using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CharacterInfoPanelController : UIControllerBase
{
    private const string CharacterRenderSurfaceObjectName = "__CharacterRenderSurface";
    private const string CharacterEquipmentItemObjectName = "__CharacterEquipmentItem";
    private const string LegacyUtilitySectionTitle = "\uAE30\uD0C0";
    private const string NormalizedUtilitySectionTitle = "\uC720\uD2F8\uB9AC\uD2F0";

    private sealed class EquipmentSlotBinding
    {
        public EquipmentType EquipmentType;
        public RectTransform Root;
        public Button Button;
        public Image Icon;
        public Image Background;
        public TMP_Text NameText;
        public TMP_Text LevelText;
        public EquipItemUI RuntimeItemUi;
        public EquipItemView RuntimeItemView;
    }

    private sealed class PreviewTransformBinding
    {
        public Transform Source;
        public Transform Clone;
    }

    private sealed class StatSectionBinding
    {
        public RectTransform Root;
        public TMP_Text HeaderText;
        public RectTransform NameColumn;
        public RectTransform ValueColumn;
        public TMP_Text NameTemplate;
        public TMP_Text ValueTemplate;
        public readonly List<TMP_Text> NameTexts = new List<TMP_Text>();
        public readonly List<TMP_Text> ValueTexts = new List<TMP_Text>();
        public float TopOffset;
        public float BottomPadding;
        public float RowHeight;
    }

    private static readonly StatType[] AttackStatOrder =
    {
        StatType.ATK,
        StatType.ATK_SPEED,
        StatType.CRIT_CHANCE,
        StatType.CRIT_MULT,
        StatType.DMG_MULT,
        StatType.NORMAL_DMG,
        StatType.BOSS_DMG
    };

    private static readonly StatType[] DefenseStatOrder =
    {
        StatType.HP,
        StatType.HP_REGEN,
        StatType.MP,
        StatType.MP_REGEN,
        StatType.PHYS_DEF,
        StatType.MAGIC_DEF
    };

    private static readonly StatType[] UtilityStatOrder =
    {
        StatType.MOVE_SPEED,
        StatType.COOLDOWN_REDUCE,
        StatType.GOLD_GAIN,
        StatType.EXP_GAIN,
        StatType.ALL_STAT
    };

    [Header("Root")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform characterPreviewRoot;
    [SerializeField] private RectTransform leftEquipmentGroup;
    [SerializeField] private RectTransform rightEquipmentGroup;
    [SerializeField] private TMP_Text combatPowerLabel;
    [SerializeField] private ScrollRect statListScrollRect;
    [SerializeField] private RectTransform statListContentRoot;
    [SerializeField] private GameObject equipmentSlotItemPrefab;

    [Header("Preview")]
    [SerializeField] private Vector3 previewStagePosition = new Vector3(5000f, 5000f, 5000f);
    [SerializeField] private Vector3 previewCameraOffset = new Vector3(0f, 1.15f, -3.6f);
    [SerializeField] private Vector3 previewLookOffset = new Vector3(0f, 0.95f, 0f);
    [SerializeField] private Vector3 previewModelEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] private Color previewBackgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private int previewTextureSize = 512;
    [SerializeField] private int previewLayer = 30;

    [Header("Text")]
    [SerializeField] private string combatPowerFormat = "전투력: {0}";
    [SerializeField] private string emptyEquipmentName = "-";
    [SerializeField] private string equipmentLevelFormat = "Lv. {0}";
    [SerializeField] private string attackSectionTitle = "공격";
    [SerializeField] private string defenseSectionTitle = "방어";
    [SerializeField] private string utilitySectionTitle = "\uC720\uD2F8\uB9AC\uD2F0";

    private readonly Dictionary<EquipmentType, EquipmentSlotBinding> equipmentSlotMap = new Dictionary<EquipmentType, EquipmentSlotBinding>();
    private readonly List<GameObject> generatedStatObjects = new List<GameObject>();
    private readonly List<PreviewTransformBinding> previewTransformBindings = new List<PreviewTransformBinding>();
    private readonly List<Renderer> previewRenderers = new List<Renderer>();
    private readonly HashSet<StatType> categorizedStats = new HashSet<StatType>();
    private readonly List<StatSectionBinding> cachedStatSections = new List<StatSectionBinding>(3);

    private CharacterStatManager statManager;
    private EquipmentHandler equipmentHandler;
    private PlayerEquipment playerEquipment;
    private EquipCurrentUIController equipmentUiController;
    private Transform playerTransform;
    private RawImage characterPreviewImage;
    private Transform previewStageTransform;
    private Transform previewCharacterTransform;
    private Camera previewCamera;
    private RenderTexture previewRenderTexture;
    private bool statSubscribed;
    private bool statContentClaimed;
    private bool shouldResetStatScrollPosition = true;
    private bool loggedSkinnedMeshWarning;
    private GameObject resolvedEquipmentItemPrefab;

    protected override void Initialize()
    {
        CacheCategorizedStats();
        RefreshLayoutBindings();
        SetPreviewVisible(false);
    }

    protected override void Subscribe()
    {
        shouldResetStatScrollPosition = true;
        GameEventManager.OnPlayerSpawned += OnPlayerSpawned;
        PlayerEquipment.EquippedItemChanged += OnEquippedItemChanged;
        BindStatManager();
        TryBindCurrentPlayer();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnPlayerSpawned -= OnPlayerSpawned;
        PlayerEquipment.EquippedItemChanged -= OnEquippedItemChanged;
        UnbindStatManager();
        SetPreviewVisible(false);
    }

    protected override void RefreshView()
    {
        RefreshLayoutBindings();

        if (!TryPrepareRuntime())
            return;

        RefreshCombatPower();
        RefreshEquipmentSlots();
        RebuildStatList();
        EnsurePreviewClone();
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
            return;

        if (previewCharacterTransform == null || previewCamera == null || previewRenderTexture == null)
            return;

        SyncPreviewPose();
        UpdatePreviewCamera();
        previewCamera.Render();
    }

    private void OnDestroy()
    {
        ReleasePreviewTexture();

        if (previewStageTransform != null)
            Destroy(previewStageTransform.gameObject);
    }

    public void ResetForSceneChange()
    {
        shouldResetStatScrollPosition = true;
        playerTransform = null;
        playerEquipment = null;
        equipmentHandler = null;
        equipmentUiController = null;
        resolvedEquipmentItemPrefab = null;
        ClearPreviewClone();
        SetPreviewVisible(false);
        ClearEquipmentSlots();
    }

    private void OnPlayerSpawned(Transform playerTransform)
    {
        shouldResetStatScrollPosition = true;
        this.playerTransform = playerTransform;
        ResolvePlayerEquipment();
        ClearPreviewClone();
        RefreshView();
    }

    private void OnEquippedItemChanged(EquipmentType equipmentType, int itemId)
    {
        if (!equipmentSlotMap.ContainsKey(equipmentType))
            return;

        RefreshEquipmentSlots();
    }

    private void OnStatUpdated()
    {
        RefreshCombatPower();
        RebuildStatList();
    }

    private void RefreshLayoutBindings()
    {
        BindReferences();
        NormalizeDisplayLabels();
        RefreshEquipmentSlotMap();
        CacheStatSections();
        EnsureCharacterRenderSurface();
    }

    private void BindReferences()
    {
        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (characterPreviewRoot == null)
            characterPreviewRoot = FindRectTransformRecursive(panelRoot, "(Img)CharRender");

        if (leftEquipmentGroup == null)
            leftEquipmentGroup = FindRectTransformRecursive(panelRoot, "(Panel)LeftBtnGroup");

        if (rightEquipmentGroup == null)
            rightEquipmentGroup = FindRectTransformRecursive(panelRoot, "(Panel)RightBtnGroup");

        if (combatPowerLabel == null)
            combatPowerLabel = FindTextRecursive(panelRoot, "텍스트_스탯");

        if (statListScrollRect == null)
            statListScrollRect = FindComponentRecursive<ScrollRect>(panelRoot, "스크롤 뷰_스탯");

        if (statListContentRoot == null && statListScrollRect != null)
            statListContentRoot = statListScrollRect.content;
    }

    private void NormalizeDisplayLabels()
    {
        if (string.IsNullOrWhiteSpace(utilitySectionTitle) || string.Equals(utilitySectionTitle, LegacyUtilitySectionTitle, StringComparison.Ordinal))
            utilitySectionTitle = NormalizedUtilitySectionTitle;
    }

    private bool TryPrepareRuntime()
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad)
            return false;

        BindStatManager();
        ResolvePlayerEquipment();

        return statManager != null && statManager.TableLoad;
    }

    private void BindStatManager()
    {
        CharacterStatManager current = CharacterStatManager.Instance;
        if (current == statManager && statSubscribed)
            return;

        UnbindStatManager();
        statManager = current;
        if (statManager == null)
            return;

        statManager.StatUpdate += OnStatUpdated;
        statSubscribed = true;
    }

    private void UnbindStatManager()
    {
        if (!statSubscribed || statManager == null)
            return;

        statManager.StatUpdate -= OnStatUpdated;
        statSubscribed = false;
    }

    private void TryBindCurrentPlayer()
    {
        if (playerTransform != null)
            return;

        if (ScenePlayerLocator.TryGetPlayerTransform(out Transform currentPlayerTransform))
            playerTransform = currentPlayerTransform;
    }

    private void ResolvePlayerEquipment()
    {
        if (playerTransform != null)
        {
            PlayerEquipment boundEquipment = playerTransform.GetComponentInChildren<PlayerEquipment>(true);
            if (boundEquipment != null)
            {
                playerEquipment = boundEquipment;
                if (equipmentHandler == null)
                    equipmentHandler = boundEquipment.equipmentHandler;

                return;
            }
        }

        if (equipmentHandler == null)
            equipmentHandler = UnityEngine.Object.FindFirstObjectByType<EquipmentHandler>();

        if (equipmentHandler != null && equipmentHandler.TryGetPlayerEquipment(out PlayerEquipment currentEquipment))
        {
            playerEquipment = currentEquipment;
            return;
        }

        playerEquipment = UnityEngine.Object.FindFirstObjectByType<PlayerEquipment>();
    }

    private void CacheCategorizedStats()
    {
        categorizedStats.Clear();

        for (int i = 0; i < AttackStatOrder.Length; i++)
            categorizedStats.Add(AttackStatOrder[i]);

        for (int i = 0; i < DefenseStatOrder.Length; i++)
            categorizedStats.Add(DefenseStatOrder[i]);

        for (int i = 0; i < UtilityStatOrder.Length; i++)
            categorizedStats.Add(UtilityStatOrder[i]);
    }

    private void RefreshEquipmentSlotMap()
    {
        equipmentSlotMap.Clear();
        CacheEquipmentSlotsInGroup(leftEquipmentGroup);
        CacheEquipmentSlotsInGroup(rightEquipmentGroup);
        EnsureEquipmentSlotViews();
    }

    private void CacheEquipmentSlotsInGroup(RectTransform groupRoot)
    {
        if (groupRoot == null)
            return;

        for (int i = 0; i < groupRoot.childCount; i++)
        {
            RectTransform child = groupRoot.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            if (!TryParseEquipmentType(child.name, out EquipmentType equipmentType))
                continue;

            EquipmentSlotBinding slot = new EquipmentSlotBinding
            {
                EquipmentType = equipmentType,
                Root = child,
                Button = child.GetComponent<Button>(),
                Icon = ResolveSlotIcon(child),
                Background = ResolveSlotBackground(child),
                NameText = ResolveSlotNameText(child),
                LevelText = ResolveSlotLevelText(child)
            };

            equipmentSlotMap[equipmentType] = slot;
        }
    }

    private void EnsureEquipmentSlotViews()
    {
        foreach (KeyValuePair<EquipmentType, EquipmentSlotBinding> pair in equipmentSlotMap)
            EnsureEquipmentSlotView(pair.Value);
    }

    private void EnsureEquipmentSlotView(EquipmentSlotBinding slot)
    {
        if (slot == null || slot.Root == null)
            return;

        if (slot.RuntimeItemUi == null)
        {
            Transform existing = slot.Root.Find(CharacterEquipmentItemObjectName);
            if (existing != null)
                slot.RuntimeItemUi = existing.GetComponent<EquipItemUI>();
        }

        if (slot.RuntimeItemUi == null)
        {
            GameObject prefab = ResolveEquipmentItemPrefab();
            if (prefab == null)
                return;

            GameObject instance = Instantiate(prefab, slot.Root, false);
            instance.name = CharacterEquipmentItemObjectName;

            RectTransform instanceRect = instance.transform as RectTransform;
            if (instanceRect != null)
            {
                instanceRect.anchorMin = Vector2.zero;
                instanceRect.anchorMax = Vector2.one;
                instanceRect.offsetMin = Vector2.zero;
                instanceRect.offsetMax = Vector2.zero;
                instanceRect.localScale = Vector3.one;
                instanceRect.localRotation = Quaternion.identity;
            }

            slot.RuntimeItemUi = instance.GetComponent<EquipItemUI>();
        }

        if (slot.RuntimeItemUi == null)
            return;

        PrepareRuntimeEquipmentItem(slot.RuntimeItemUi);

        if (slot.RuntimeItemView == null)
            slot.RuntimeItemView = new EquipItemView(slot.RuntimeItemUi);

        HideManualSlotContent(slot.Root, slot.RuntimeItemUi.transform);
    }

    private void RefreshCombatPower()
    {
        if (combatPowerLabel == null || statManager == null)
            return;

        BigDouble power = statManager.NormalPower;
        combatPowerLabel.text = string.Format(CultureInfo.InvariantCulture, combatPowerFormat, power.ToString());
    }

    private void RefreshEquipmentSlots()
    {
        foreach (KeyValuePair<EquipmentType, EquipmentSlotBinding> pair in equipmentSlotMap)
        {
            if (!TryGetEquippedItemInfo(pair.Key, out EquipListTable equipInfo, out int reinforceLevel))
            {
                ApplyEmptySlot(pair.Value);
                continue;
            }

            ApplyEquipmentToSlot(pair.Value, equipInfo, reinforceLevel);
        }
    }

    private void ClearEquipmentSlots()
    {
        foreach (KeyValuePair<EquipmentType, EquipmentSlotBinding> pair in equipmentSlotMap)
            ApplyEmptySlot(pair.Value);
    }

    private bool TryGetEquippedItemInfo(EquipmentType equipmentType, out EquipListTable equipInfo, out int reinforceLevel)
    {
        equipInfo = null;
        reinforceLevel = 0;

        if (playerEquipment == null || DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return false;

        int itemId = playerEquipment.ReturnItemNum(equipmentType);
        if (itemId == 0)
            return false;

        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out equipInfo))
            return false;

        reinforceLevel = GetEquipmentReinforcementLevel(itemId);
        return true;
    }

    private int GetEquipmentReinforcementLevel(int itemId)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
            return 0;

        EquipmentInventoryModule equipmentModule = inventory.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null || !equipmentModule.IsInitialized)
            return 0;

        EquipmentData equipmentData = equipmentModule.GetEquipment(itemId);
        return equipmentData.equipmentId == itemId ? equipmentData.equipmentReinforcement : 0;
    }

    private void ApplyEquipmentToSlot(EquipmentSlotBinding slot, EquipListTable equipInfo, int reinforceLevel)
    {
        if (slot == null)
            return;

        if (slot.RuntimeItemView != null && slot.RuntimeItemUi != null)
        {
            Sprite icon = LoadEquipmentIcon(equipInfo);
            slot.RuntimeItemUi.gameObject.SetActive(true);
            slot.RuntimeItemView.Render(
                icon,
                string.Format(CultureInfo.InvariantCulture, equipmentLevelFormat, reinforceLevel),
                GetEquipmentStarCount(equipInfo.grade),
                RarityColor.TierColorByTier(equipInfo.grade));
            slot.RuntimeItemView.SetFrameColor(RarityColor.ItemGradeColor(equipInfo.rarityType));
            slot.RuntimeItemView.SetDimmed(false);

            if (slot.RuntimeItemUi.Icon != null)
            {
                slot.RuntimeItemUi.Icon.enabled = icon != null;
                slot.RuntimeItemUi.Icon.color = Color.white;
                slot.RuntimeItemUi.Icon.preserveAspect = true;
            }

            return;
        }

        if (slot.Icon != null)
        {
            slot.Icon.sprite = LoadEquipmentIcon(equipInfo);
            slot.Icon.enabled = slot.Icon.sprite != null;
            slot.Icon.preserveAspect = true;
            slot.Icon.color = Color.white;
        }

        if (slot.NameText != null)
            slot.NameText.text = string.IsNullOrWhiteSpace(equipInfo.equipmentName) ? equipInfo.equipmentType.ToString() : equipInfo.equipmentName;

        if (slot.LevelText != null)
            slot.LevelText.text = string.Format(CultureInfo.InvariantCulture, equipmentLevelFormat, reinforceLevel);

        if (slot.Background != null)
        {
            Color rarityColor = RarityColor.ItemGradeColor(equipInfo.rarityType);
            float alpha = Mathf.Max(0.2f, slot.Background.color.a);
            slot.Background.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, alpha);
        }
    }

    private void ApplyEmptySlot(EquipmentSlotBinding slot)
    {
        if (slot == null)
            return;

        if (slot.RuntimeItemView != null && slot.RuntimeItemUi != null)
        {
            slot.RuntimeItemUi.gameObject.SetActive(true);
            slot.RuntimeItemView.Render(null, string.Empty, 1, Color.clear);
            slot.RuntimeItemView.SetFrameColor(new Color(1f, 1f, 1f, 0.22f));
            slot.RuntimeItemView.SetDimmed(true);

            if (slot.RuntimeItemUi.Icon != null)
            {
                slot.RuntimeItemUi.Icon.sprite = null;
                slot.RuntimeItemUi.Icon.enabled = false;
            }

            return;
        }

        if (slot.Icon != null)
        {
            slot.Icon.sprite = null;
            slot.Icon.enabled = false;
        }

        if (slot.NameText != null)
            slot.NameText.text = emptyEquipmentName;

        if (slot.LevelText != null)
            slot.LevelText.text = string.Empty;
    }

    private void RebuildStatList()
    {
        if (statManager == null || statListContentRoot == null)
            return;

        CacheStatSections();
        if (TryRebuildStatListWithExistingLayout())
        {
            ResetStatScrollPositionIfNeeded();
            return;
        }

        ClaimStatContentRoot();
        ClearGeneratedStatObjects();

        AddStatSection(attackSectionTitle, AttackStatOrder, includeZeroValues: true);
        AddStatSection(defenseSectionTitle, DefenseStatOrder, includeZeroValues: true);

        List<StatType> utilityStats = CollectUtilityStats();
        if (utilityStats.Count > 0)
            AddStatSection(utilitySectionTitle, utilityStats, includeZeroValues: false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(statListContentRoot);

        ResetStatScrollPositionIfNeeded();
    }

    private bool TryRebuildStatListWithExistingLayout()
    {
        if (cachedStatSections.Count < 3)
            return false;

        List<StatType> attackStats = CollectVisibleStats(AttackStatOrder, includeZeroValues: true);
        List<StatType> defenseStats = CollectVisibleStats(DefenseStatOrder, includeZeroValues: true);
        List<StatType> utilityStats = CollectUtilityStats();

        bool rebuiltAttack = RebuildExistingSection(cachedStatSections[0], attackSectionTitle, attackStats);
        bool rebuiltDefense = RebuildExistingSection(cachedStatSections[1], defenseSectionTitle, defenseStats);
        bool rebuiltUtility = RebuildExistingSection(cachedStatSections[2], utilitySectionTitle, utilityStats);

        if (!rebuiltAttack || !rebuiltDefense || !rebuiltUtility)
            return false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(statListContentRoot);
        return true;
    }

    private void CacheStatSections()
    {
        cachedStatSections.Clear();
        if (statListContentRoot == null)
            return;

        for (int i = 0; i < statListContentRoot.childCount; i++)
        {
            RectTransform child = statListContentRoot.GetChild(i) as RectTransform;
            StatSectionBinding binding = BuildStatSectionBinding(child);
            if (binding != null)
                cachedStatSections.Add(binding);
        }
    }

    private static StatSectionBinding BuildStatSectionBinding(RectTransform root)
    {
        if (root == null)
            return null;

        TMP_Text headerText = null;
        List<RectTransform> textColumns = new List<RectTransform>(2);

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform child = root.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            TMP_Text directText = child.GetComponent<TMP_Text>();
            if (directText != null)
            {
                if (headerText == null)
                    headerText = directText;
                continue;
            }

            if (child.GetComponent<VerticalLayoutGroup>() == null)
                continue;

            if (GetDirectChildTexts(child).Count == 0)
                continue;

            textColumns.Add(child);
        }

        if (headerText == null || textColumns.Count < 2)
            return null;

        List<TMP_Text> nameTexts = GetDirectChildTexts(textColumns[0]);
        List<TMP_Text> valueTexts = GetDirectChildTexts(textColumns[1]);
        if (nameTexts.Count == 0 || valueTexts.Count == 0)
            return null;

        EnsureColumnLayout(textColumns[0]);
        EnsureColumnLayout(textColumns[1]);

        float rowHeight = Mathf.Max(nameTexts[0].rectTransform.sizeDelta.y, valueTexts[0].rectTransform.sizeDelta.y);
        if (rowHeight <= 0f)
            rowHeight = 50f;

        float topOffset = Mathf.Abs(textColumns[0].anchoredPosition.y);
        float bottomPadding = root.sizeDelta.y - topOffset - rowHeight * Mathf.Max(nameTexts.Count, valueTexts.Count);
        if (bottomPadding < 0f)
            bottomPadding = 0f;

        StatSectionBinding binding = new StatSectionBinding
        {
            Root = root,
            HeaderText = headerText,
            NameColumn = textColumns[0],
            ValueColumn = textColumns[1],
            NameTemplate = nameTexts[0],
            ValueTemplate = valueTexts[0],
            TopOffset = topOffset,
            BottomPadding = bottomPadding,
            RowHeight = rowHeight
        };

        binding.NameTexts.AddRange(nameTexts);
        binding.ValueTexts.AddRange(valueTexts);
        return binding;
    }

    private static List<TMP_Text> GetDirectChildTexts(RectTransform parent)
    {
        List<TMP_Text> result = new List<TMP_Text>();
        if (parent == null)
            return result;

        for (int i = 0; i < parent.childCount; i++)
        {
            TMP_Text text = parent.GetChild(i).GetComponent<TMP_Text>();
            if (text != null)
                result.Add(text);
        }

        return result;
    }

    private static void EnsureColumnLayout(RectTransform columnRoot)
    {
        if (columnRoot == null)
            return;

        ContentSizeFitter fitter = columnRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = columnRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private List<StatType> CollectVisibleStats(IReadOnlyList<StatType> statTypes, bool includeZeroValues)
    {
        List<StatType> visibleStats = new List<StatType>(statTypes.Count);
        for (int i = 0; i < statTypes.Count; i++)
        {
            StatType statType = statTypes[i];
            if (statType == StatType.None)
                continue;

            float value = statManager.GetFinalStat(statType);
            if (!includeZeroValues && Mathf.Abs(value) <= 0.0001f)
                continue;

            visibleStats.Add(statType);
        }

        return visibleStats;
    }

    private bool RebuildExistingSection(StatSectionBinding binding, string sectionTitle, IReadOnlyList<StatType> visibleStats)
    {
        if (binding == null || binding.Root == null || binding.NameColumn == null || binding.ValueColumn == null)
            return false;

        if (binding.HeaderText != null)
            binding.HeaderText.text = sectionTitle;

        int rowCount = visibleStats != null ? visibleStats.Count : 0;
        binding.Root.gameObject.SetActive(rowCount > 0);
        if (rowCount <= 0)
            return true;

        EnsureSectionTextPool(binding.NameColumn, binding.NameTexts, binding.NameTemplate, rowCount);
        EnsureSectionTextPool(binding.ValueColumn, binding.ValueTexts, binding.ValueTemplate, rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            StatType statType = visibleStats[i];

            TMP_Text nameText = binding.NameTexts[i];
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = GetStatDisplayName(statType);
            }

            TMP_Text valueText = binding.ValueTexts[i];
            if (valueText != null)
            {
                valueText.gameObject.SetActive(true);
                valueText.text = FormatStatValue(statManager.GetFinalStat(statType), statType);
            }
        }

        SetInactiveFrom(binding.NameTexts, rowCount);
        SetInactiveFrom(binding.ValueTexts, rowCount);

        float contentHeight = binding.RowHeight * rowCount;
        SetRectHeight(binding.NameColumn, contentHeight);
        SetRectHeight(binding.ValueColumn, contentHeight);
        SetRectHeight(binding.Root, binding.TopOffset + contentHeight + binding.BottomPadding);

        LayoutElement layoutElement = binding.Root.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = binding.Root.gameObject.AddComponent<LayoutElement>();

        layoutElement.minHeight = binding.Root.sizeDelta.y;
        layoutElement.preferredHeight = binding.Root.sizeDelta.y;

        LayoutRebuilder.ForceRebuildLayoutImmediate(binding.NameColumn);
        LayoutRebuilder.ForceRebuildLayoutImmediate(binding.ValueColumn);
        LayoutRebuilder.ForceRebuildLayoutImmediate(binding.Root);
        return true;
    }

    private static void EnsureSectionTextPool(RectTransform columnRoot, List<TMP_Text> pool, TMP_Text template, int requiredCount)
    {
        if (columnRoot == null || template == null)
            return;

        while (pool.Count < requiredCount)
        {
            TMP_Text clone = UnityEngine.Object.Instantiate(template, columnRoot);
            clone.name = template.name;
            clone.gameObject.SetActive(true);
            pool.Add(clone);
        }
    }

    private static void SetInactiveFrom(List<TMP_Text> texts, int startIndex)
    {
        for (int i = startIndex; i < texts.Count; i++)
        {
            if (texts[i] != null)
                texts[i].gameObject.SetActive(false);
        }
    }

    private static void SetRectHeight(RectTransform rectTransform, float height)
    {
        if (rectTransform == null)
            return;

        Vector2 sizeDelta = rectTransform.sizeDelta;
        sizeDelta.y = height;
        rectTransform.sizeDelta = sizeDelta;
    }

    private void ResetStatScrollPositionIfNeeded()
    {
        if (!shouldResetStatScrollPosition || statListScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        statListScrollRect.StopMovement();
        statListScrollRect.verticalNormalizedPosition = 1f;
        shouldResetStatScrollPosition = false;
    }

    private void ClaimStatContentRoot()
    {
        if (statContentClaimed || statListContentRoot == null)
            return;

        for (int i = statListContentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = statListContentRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        statContentClaimed = true;
    }

    private void ClearGeneratedStatObjects()
    {
        for (int i = 0; i < generatedStatObjects.Count; i++)
        {
            if (generatedStatObjects[i] != null)
                Destroy(generatedStatObjects[i]);
        }

        generatedStatObjects.Clear();
    }

    private void AddStatSection(string sectionTitle, IReadOnlyList<StatType> statTypes, bool includeZeroValues)
    {
        if (statListContentRoot == null || statTypes == null || statTypes.Count == 0)
            return;

        List<StatType> visibleStats = new List<StatType>(statTypes.Count);
        for (int i = 0; i < statTypes.Count; i++)
        {
            StatType statType = statTypes[i];
            if (statType == StatType.None)
                continue;

            float value = statManager.GetFinalStat(statType);
            if (!includeZeroValues && Mathf.Abs(value) <= 0.0001f)
                continue;

            visibleStats.Add(statType);
        }

        if (visibleStats.Count == 0)
            return;

        GameObject sectionObject = CreateUiObject($"(Section){sectionTitle}", statListContentRoot);
        generatedStatObjects.Add(sectionObject);

        Image sectionBackground = sectionObject.AddComponent<Image>();
        sectionBackground.color = new Color(1f, 1f, 1f, 0.045f);

        VerticalLayoutGroup sectionLayout = sectionObject.AddComponent<VerticalLayoutGroup>();
        sectionLayout.childAlignment = TextAnchor.UpperCenter;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.spacing = 12f;
        sectionLayout.padding = new RectOffset(20, 20, 18, 18);

        ContentSizeFitter sectionFitter = sectionObject.AddComponent<ContentSizeFitter>();
        sectionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement sectionElement = sectionObject.AddComponent<LayoutElement>();
        sectionElement.minWidth = 900f;
        sectionElement.preferredWidth = 900f;

        CreateSectionHeader(sectionObject.transform as RectTransform, sectionTitle);

        for (int i = 0; i < visibleStats.Count; i++)
            CreateStatRow(sectionObject.transform as RectTransform, visibleStats[i]);
    }

    private List<StatType> CollectUtilityStats()
    {
        List<StatType> utilityStats = CollectVisibleStats(UtilityStatOrder, includeZeroValues: true);

        Array values = Enum.GetValues(typeof(StatType));
        for (int i = 0; i < values.Length; i++)
        {
            StatType statType = (StatType)values.GetValue(i);
            if (statType == StatType.None || categorizedStats.Contains(statType) || utilityStats.Contains(statType))
                continue;

            if (Mathf.Abs(statManager.GetFinalStat(statType)) <= 0.0001f)
                continue;

            utilityStats.Add(statType);
        }

        return utilityStats;
    }

    private static bool TryParseEquipmentType(string rawName, out EquipmentType equipmentType)
    {
        equipmentType = default(EquipmentType);
        if (string.IsNullOrWhiteSpace(rawName))
            return false;

        string normalized = rawName.Trim();
        int closingParenIndex = normalized.LastIndexOf(')');
        if (closingParenIndex >= 0 && closingParenIndex + 1 < normalized.Length)
            normalized = normalized.Substring(closingParenIndex + 1);

        if (normalized.EndsWith("Tap", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(0, normalized.Length - 3);

        switch (normalized.Trim().ToLowerInvariant())
        {
            case "weapon":
                equipmentType = EquipmentType.Weapon;
                return true;

            case "helmet":
            case "headpiece":
            case "head":
                equipmentType = EquipmentType.Helmet;
                return true;

            case "glove":
            case "gloves":
                equipmentType = EquipmentType.Glove;
                return true;

            case "armor":
                equipmentType = EquipmentType.Armor;
                return true;

            case "boot":
            case "boots":
                equipmentType = EquipmentType.Boots;
                return true;
        }

        return Enum.TryParse(normalized, true, out equipmentType);
    }

    private GameObject ResolveEquipmentItemPrefab()
    {
        if (equipmentSlotItemPrefab != null)
            return equipmentSlotItemPrefab;

        if (resolvedEquipmentItemPrefab != null)
            return resolvedEquipmentItemPrefab;

        if (equipmentUiController == null)
        {
            Transform searchRoot = panelRoot != null ? panelRoot.root : transform.root;
            if (searchRoot != null)
            {
                EquipCurrentUIController[] controllers = searchRoot.GetComponentsInChildren<EquipCurrentUIController>(true);
                if (controllers != null && controllers.Length > 0)
                    equipmentUiController = controllers[0];
            }
        }

        if (equipmentUiController == null)
            equipmentUiController = UnityEngine.Object.FindFirstObjectByType<EquipCurrentUIController>();

        if (equipmentUiController != null)
            resolvedEquipmentItemPrefab = equipmentUiController.ItemPrefab;

        return resolvedEquipmentItemPrefab;
    }

    private static void PrepareRuntimeEquipmentItem(EquipItemUI ui)
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

        CanvasGroup canvasGroup = ui.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = ui.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f;
    }

    private static void HideManualSlotContent(RectTransform slotRoot, Transform activeContent)
    {
        if (slotRoot == null)
            return;

        for (int i = 0; i < slotRoot.childCount; i++)
        {
            Transform child = slotRoot.GetChild(i);
            if (child == activeContent)
                continue;

            child.gameObject.SetActive(false);
        }
    }

    private void CreateSectionHeader(RectTransform parent, string title)
    {
        GameObject headerObject = CreateUiObject($"(Text){title}", parent);
        generatedStatObjects.Add(headerObject);

        LayoutElement headerLayout = headerObject.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 52f;

        TMP_Text headerText = headerObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(headerText, 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        headerText.color = new Color(1f, 0.92f, 0.54f, 1f);
        headerText.text = title;
    }

    private void CreateStatRow(RectTransform parent, StatType statType)
    {
        GameObject rowObject = CreateUiObject($"(Stat){statType}", parent);
        generatedStatObjects.Add(rowObject);

        Image rowBackground = rowObject.AddComponent<Image>();
        rowBackground.color = new Color(1f, 1f, 1f, 0.08f);

        HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.spacing = 16f;
        rowLayout.padding = new RectOffset(20, 20, 12, 12);

        LayoutElement rowElement = rowObject.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 64f;

        Sprite iconSprite = ResolveStatIcon(statType);
        if (iconSprite != null)
            CreateStatIcon(rowObject.transform as RectTransform, iconSprite);

        TMP_Text nameText = CreateRowText(rowObject.transform as RectTransform, $"(Text)Name_{statType}", TextAlignmentOptions.Left);
        nameText.text = GetStatDisplayName(statType);
        AddFlexibleLayout(nameText.gameObject, 1f, -1f);

        TMP_Text valueText = CreateRowText(rowObject.transform as RectTransform, $"(Text)Value_{statType}", TextAlignmentOptions.Right);
        valueText.text = FormatStatValue(statManager.GetFinalStat(statType), statType);
        valueText.color = new Color(0.95f, 0.97f, 1f, 1f);
        AddFlexibleLayout(valueText.gameObject, 0f, 230f);
    }

    private void CreateStatIcon(RectTransform parent, Sprite iconSprite)
    {
        GameObject iconObject = CreateUiObject("(Img)StatIcon", parent);
        generatedStatObjects.Add(iconObject);

        LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 42f;
        iconLayout.preferredHeight = 42f;

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.color = Color.white;
    }

    private TMP_Text CreateRowText(RectTransform parent, string objectName, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        generatedStatObjects.Add(textObject);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(text, 28f, FontStyles.Normal, alignment);
        text.text = string.Empty;
        return text;
    }

    private void ApplyTextStyle(TMP_Text text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        TMP_FontAsset fontAsset = combatPowerLabel != null ? combatPowerLabel.font : TMP_Settings.defaultFontAsset;
        Material fontMaterial = combatPowerLabel != null ? combatPowerLabel.fontSharedMaterial : null;

        text.font = fontAsset;
        if (fontMaterial != null)
            text.fontSharedMaterial = fontMaterial;

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.color = combatPowerLabel != null ? combatPowerLabel.color : Color.white;
    }

    private static void AddFlexibleLayout(GameObject target, float flexibleWidth, float preferredWidth)
    {
        if (target == null)
            return;

        LayoutElement layoutElement = target.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = flexibleWidth;
        if (preferredWidth > 0f)
            layoutElement.preferredWidth = preferredWidth;
    }

    private void EnsureCharacterRenderSurface()
    {
        if (characterPreviewRoot == null)
            return;

        Transform surfaceTransform = characterPreviewRoot.Find(CharacterRenderSurfaceObjectName);
        if (surfaceTransform == null)
        {
            GameObject surfaceObject = CreateUiObject(CharacterRenderSurfaceObjectName, characterPreviewRoot);
            RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
            surfaceRect.anchorMin = Vector2.zero;
            surfaceRect.anchorMax = Vector2.one;
            surfaceRect.offsetMin = Vector2.zero;
            surfaceRect.offsetMax = Vector2.zero;
            surfaceRect.SetAsLastSibling();

            surfaceObject.AddComponent<CanvasRenderer>();
            characterPreviewImage = surfaceObject.AddComponent<RawImage>();
        }
        else
        {
            characterPreviewImage = surfaceTransform.GetComponent<RawImage>();
            if (characterPreviewImage == null)
                characterPreviewImage = surfaceTransform.gameObject.AddComponent<RawImage>();
        }

        if (characterPreviewImage == null)
            return;

        characterPreviewImage.raycastTarget = false;
        characterPreviewImage.texture = previewRenderTexture;
    }

    private void EnsurePreviewInfrastructure()
    {
        if (previewStageTransform == null)
        {
            GameObject stageObject = new GameObject("__CharacterInfoPreviewStage");
            stageObject.transform.SetParent(transform, false);
            stageObject.transform.position = previewStagePosition;
            previewStageTransform = stageObject.transform;
        }

        if (previewCamera == null)
        {
            GameObject cameraObject = new GameObject("CharacterInfoPreviewCamera");
            cameraObject.transform.SetParent(previewStageTransform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = previewBackgroundColor;
            previewCamera.cullingMask = 1 << Mathf.Clamp(previewLayer, 0, 31);
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 50f;
            previewCamera.fieldOfView = 28f;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.enabled = false;
        }

        EnsurePreviewTexture();
    }

    private void EnsurePreviewTexture()
    {
        int size = Mathf.Max(256, previewTextureSize);

        if (characterPreviewRoot != null)
        {
            int rectSize = Mathf.CeilToInt(Mathf.Max(characterPreviewRoot.rect.width, characterPreviewRoot.rect.height));
            if (rectSize > 0)
                size = Mathf.Max(size, Mathf.NextPowerOfTwo(rectSize));
        }

        if (previewRenderTexture != null && previewRenderTexture.width == size && previewRenderTexture.height == size)
            return;

        ReleasePreviewTexture();

        previewRenderTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32)
        {
            name = "CharacterInfoPreviewRT",
            antiAliasing = 1
        };

        previewRenderTexture.Create();

        if (previewCamera != null)
            previewCamera.targetTexture = previewRenderTexture;

        if (characterPreviewImage != null)
            characterPreviewImage.texture = previewRenderTexture;
    }

    private void ReleasePreviewTexture()
    {
        if (previewCamera != null)
            previewCamera.targetTexture = null;

        if (characterPreviewImage != null)
            characterPreviewImage.texture = null;

        if (previewRenderTexture == null)
            return;

        previewRenderTexture.Release();
        Destroy(previewRenderTexture);
        previewRenderTexture = null;
    }

    private void EnsurePreviewClone()
    {
        if (playerTransform == null || characterPreviewImage == null)
        {
            SetPreviewVisible(false);
            return;
        }

        EnsurePreviewInfrastructure();

        if (previewCharacterTransform != null)
        {
            SetPreviewVisible(true);
            return;
        }

        previewCharacterTransform = new GameObject("CharacterInfoPreviewModel").transform;
        previewCharacterTransform.SetParent(previewStageTransform, false);
        previewCharacterTransform.localPosition = Vector3.zero;
        previewCharacterTransform.localRotation = Quaternion.Euler(previewModelEuler);
        previewCharacterTransform.localScale = playerTransform.localScale;

        BuildPreviewCloneHierarchy(playerTransform, previewCharacterTransform, isRoot: true);
        SetLayerRecursively(previewCharacterTransform.gameObject, previewLayer);
        SetPreviewVisible(previewRenderers.Count > 0);
    }

    private void ClearPreviewClone()
    {
        previewTransformBindings.Clear();
        previewRenderers.Clear();

        if (previewCharacterTransform != null)
            Destroy(previewCharacterTransform.gameObject);

        previewCharacterTransform = null;
    }

    private void BuildPreviewCloneHierarchy(Transform source, Transform clone, bool isRoot)
    {
        if (source == null || clone == null)
            return;

        if (!isRoot)
        {
            clone.localPosition = source.localPosition;
            clone.localRotation = source.localRotation;
            clone.localScale = source.localScale;

            previewTransformBindings.Add(new PreviewTransformBinding
            {
                Source = source,
                Clone = clone
            });
        }

        CopyRenderableComponents(source, clone);

        for (int i = 0; i < source.childCount; i++)
        {
            Transform sourceChild = source.GetChild(i);
            GameObject cloneChildObject = new GameObject(sourceChild.name);
            Transform cloneChild = cloneChildObject.transform;
            cloneChild.SetParent(clone, false);
            BuildPreviewCloneHierarchy(sourceChild, cloneChild, isRoot: false);
        }
    }

    private void CopyRenderableComponents(Transform source, Transform clone)
    {
        MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceMeshRenderer = source.GetComponent<MeshRenderer>();
        if (sourceMeshFilter != null && sourceMeshRenderer != null)
        {
            MeshFilter cloneMeshFilter = clone.gameObject.AddComponent<MeshFilter>();
            cloneMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer cloneMeshRenderer = clone.gameObject.AddComponent<MeshRenderer>();
            cloneMeshRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
            cloneMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            cloneMeshRenderer.receiveShadows = false;
            cloneMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
            cloneMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            previewRenderers.Add(cloneMeshRenderer);
        }

        SpriteRenderer sourceSpriteRenderer = source.GetComponent<SpriteRenderer>();
        if (sourceSpriteRenderer != null)
        {
            SpriteRenderer cloneSpriteRenderer = clone.gameObject.AddComponent<SpriteRenderer>();
            cloneSpriteRenderer.sprite = sourceSpriteRenderer.sprite;
            cloneSpriteRenderer.color = sourceSpriteRenderer.color;
            cloneSpriteRenderer.flipX = sourceSpriteRenderer.flipX;
            cloneSpriteRenderer.flipY = sourceSpriteRenderer.flipY;
            cloneSpriteRenderer.drawMode = sourceSpriteRenderer.drawMode;
            cloneSpriteRenderer.size = sourceSpriteRenderer.size;
            cloneSpriteRenderer.sortingLayerID = sourceSpriteRenderer.sortingLayerID;
            cloneSpriteRenderer.sortingOrder = sourceSpriteRenderer.sortingOrder;
            previewRenderers.Add(cloneSpriteRenderer);
        }

        if (!loggedSkinnedMeshWarning && source.GetComponent<SkinnedMeshRenderer>() != null)
        {
            loggedSkinnedMeshWarning = true;
            Debug.LogWarning("CharacterInfoPanelController: SkinnedMeshRenderer preview clone is not supported yet. Current preview will skip that renderer.", this);
        }
    }

    private void SyncPreviewPose()
    {
        if (previewCharacterTransform == null || playerTransform == null)
            return;

        previewCharacterTransform.position = previewStagePosition;
        previewCharacterTransform.rotation = Quaternion.Euler(previewModelEuler);
        previewCharacterTransform.localScale = playerTransform.localScale;

        for (int i = 0; i < previewTransformBindings.Count; i++)
        {
            PreviewTransformBinding binding = previewTransformBindings[i];
            if (binding.Source == null || binding.Clone == null)
                continue;

            binding.Clone.localPosition = binding.Source.localPosition;
            binding.Clone.localRotation = binding.Source.localRotation;
            binding.Clone.localScale = binding.Source.localScale;
        }
    }

    private void UpdatePreviewCamera()
    {
        if (previewCamera == null || previewCharacterTransform == null)
            return;

        Vector3 lookTarget = previewCharacterTransform.position + previewLookOffset;
        previewCamera.transform.position = previewCharacterTransform.position + previewCameraOffset;
        previewCamera.transform.LookAt(lookTarget);
    }

    private void SetPreviewVisible(bool visible)
    {
        if (previewCamera != null)
            previewCamera.enabled = false;

        if (characterPreviewImage != null)
            characterPreviewImage.color = visible ? Color.white : new Color(1f, 1f, 1f, 0f);
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

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private Image ResolveSlotIcon(RectTransform slotRoot)
    {
        Image[] images = slotRoot != null ? slotRoot.GetComponentsInChildren<Image>(true) : Array.Empty<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].transform == slotRoot)
                continue;

            if (images[i].name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0)
                return images[i];
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].transform == slotRoot)
                continue;

            string name = images[i].name;
            if (name.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Line", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            return images[i];
        }

        return null;
    }

    private Image ResolveSlotBackground(RectTransform slotRoot)
    {
        Image[] images = slotRoot != null ? slotRoot.GetComponentsInChildren<Image>(true) : Array.Empty<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;

            string name = images[i].name;
            if (name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0)
                return images[i];
        }

        return slotRoot != null ? slotRoot.GetComponent<Image>() : null;
    }

    private TMP_Text ResolveSlotNameText(RectTransform slotRoot)
    {
        TMP_Text[] texts = slotRoot != null ? slotRoot.GetComponentsInChildren<TMP_Text>(true) : Array.Empty<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            if (texts[i].name.IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0)
                return texts[i];
        }

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            if (texts[i].name.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            return texts[i];
        }

        return null;
    }

    private TMP_Text ResolveSlotLevelText(RectTransform slotRoot)
    {
        TMP_Text[] texts = slotRoot != null ? slotRoot.GetComponentsInChildren<TMP_Text>(true) : Array.Empty<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0)
                return texts[i];
        }

        return null;
    }

    private static RectTransform FindRectTransformRecursive(RectTransform root, string targetName)
    {
        return FindTransformRecursive(root, targetName) as RectTransform;
    }

    private static TMP_Text FindTextRecursive(RectTransform root, string targetName)
    {
        Transform transform = FindTransformRecursive(root, targetName);
        return transform != null ? transform.GetComponent<TMP_Text>() : null;
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

    private static Sprite LoadEquipmentIcon(EquipListTable table)
    {
        if (table == null)
            return null;

        string key = string.IsNullOrEmpty(table.iconResource) ? table.equipmentName : table.iconResource;
        return string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>(key);
    }

    private static int GetEquipmentStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
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
                return "물리 방어력";
            case StatType.MAGIC_DEF:
                return "마법 방어력";
            case StatType.CRIT_CHANCE:
                return "치명타 확률";
            case StatType.CRIT_MULT:
                return "치명타 피해";
            case StatType.MOVE_SPEED:
                return "이동 속도";
            case StatType.COOLDOWN_REDUCE:
                return "재사용 대기시간 감소";
            case StatType.GOLD_GAIN:
                return "골드 획득량";
            case StatType.EXP_GAIN:
                return "경험치 획득량";
            case StatType.BOSS_DMG:
                return "보스 피해";
            case StatType.NORMAL_DMG:
                return "일반 몬스터 피해";
            case StatType.DMG_MULT:
                return "최종 피해";
            case StatType.ALL_STAT:
                return "모든 능력치";
            default:
                return statType.ToString();
        }
    }

    private static string FormatStatValue(float value, StatType statType)
    {
        if (StatGroups.MultTypes.Contains(statType))
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}%", value * 100f);

        if (Mathf.Abs(value) >= 1000f)
        {
            BigDouble bigValue = new BigDouble(value);
            return bigValue.ToString("F2");
        }

        if (Mathf.Abs(value - Mathf.Round(value)) <= 0.01f)
            return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static Sprite ResolveStatIcon(StatType statType)
    {
        if (IconManager.StatIconSO == null || IconManager.StatIconSO.StatIconDict == null)
            return null;

        return IconManager.StatIconSO.StatIconDict.TryGetValue(statType, out Sprite icon) ? icon : null;
    }
}
