using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipReinforceUIController : UIControllerBase
{
    private enum SelectionMode
    {
        EquippedSlot,
        InventoryItem
    }

    [Serializable]
    private struct StatIconEntry
    {
        [SerializeField] public StatType statType;
        [SerializeField] public Sprite icon;
    }

    private readonly struct StatPreviewLine
    {
        public StatPreviewLine(StatType statType, float currentValue, float nextValue)
        {
            StatType = statType;
            CurrentValue = currentValue;
            NextValue = nextValue;
        }

        public StatType StatType { get; }
        public float CurrentValue { get; }
        public float NextValue { get; }
    }

    private const int MaxReinforcementLevel = 100;
    private const float SingleStatRowSpacing = 60f;
    private const float MultiStatRowSpacing = 25f;
    private const float MultiStatRowScale = 0.82f;
    private const float MultiStatRowYOffset = 15f;

    [Header("Data")]
    [SerializeField] private EquipmentHandler handler;

    [Header("Panel")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private Button reinforceButton;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private string maxCostText = "MAX";

    [Header("Preview")]
    [SerializeField] private Button previewButton;
    [SerializeField] private Image previewIcon;
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private RectTransform previewLevelRoot;
    [SerializeField] private TextMeshProUGUI previewLevelText;
    [SerializeField] private Image previewTierPanel;
    [SerializeField] private RectTransform previewTierRoot;
    [SerializeField] private RectTransform previewTierStarTemplate;
    [SerializeField] private Image[] previewFrames;

    [Header("Stat")]
    [SerializeField] private EquipReinforceStatRowUI statRowTemplate;
    [SerializeField] private List<StatIconEntry> statIcons = new List<StatIconEntry>();

    private readonly Dictionary<StatType, Sprite> iconByStat = new Dictionary<StatType, Sprite>();
    private readonly List<Image> previewStars = new List<Image>();
    private readonly List<EquipReinforceStatRowUI> statRows = new List<EquipReinforceStatRowUI>();

    private InventoryManager inventory;
    private SelectionMode selectionMode = SelectionMode.EquippedSlot;
    private EquipmentType selectedType = EquipmentType.Weapon;
    private int selectedItemId;
    private bool isVisible;
    private int ignoreCloseUntilFrame = -1;
    private Vector2 statRowTemplatePosition;
    private Vector3 statRowTemplateScale;

    private void Update()
    {
        if (!isVisible)
            return;

        if (ShouldClosePopup())
            Hide();
    }

    protected override void Initialize()
    {
        CacheStatIcons();
        CacheStatRows();
        SetVisible(false);

        if (previewButton != null)
        {
            previewButton.onClick.RemoveAllListeners();
            previewButton.interactable = false;
        }
    }

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        PlayerEquipment.EquippedItemChanged += HandleEquippedItemChanged;

        if (reinforceButton != null)
            reinforceButton.onClick.AddListener(ClickReinforce);

        BindInventory();
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        PlayerEquipment.EquippedItemChanged -= HandleEquippedItemChanged;

        if (reinforceButton != null)
            reinforceButton.onClick.RemoveListener(ClickReinforce);

        UnbindInventory();
    }

    protected override void RefreshView()
    {
        if (!isVisible)
            return;

        if (!TryResolveSelection(out selectedItemId))
        {
            Hide();
            return;
        }

        if (!TryGetEquipmentContext(selectedItemId, out EquipListTable equipInfo, out EquipmentData equipmentData))
            return;

        Render(equipInfo, equipmentData);
    }

    public void Show(EquipmentType type)
    {
        if (!TryResolveSelectedItem(type, out int itemId))
            return;

        selectionMode = SelectionMode.EquippedSlot;
        selectedType = type;
        selectedItemId = itemId;
        ShowSelectedItem();
    }

    public void Show(int itemId)
    {
        if (itemId == 0)
            return;

        selectionMode = SelectionMode.InventoryItem;
        isVisible = true;
        selectedItemId = itemId;

        if (TryGetEquipmentType(itemId, out EquipmentType type))
            selectedType = type;

        ShowSelectedItem();
    }

    public void Hide()
    {
        isVisible = false;
        selectedItemId = 0;
        selectionMode = SelectionMode.EquippedSlot;
        SetVisible(false);
    }

    private void ClickReinforce()
    {
        if (selectedItemId == 0 || handler == null)
            return;

        handler.ReinforceEquipment(selectedItemId);
    }

    private void HandleEquippedItemChanged(EquipmentType type, int itemId)
    {
        if (!isVisible || selectionMode != SelectionMode.EquippedSlot || type != selectedType)
            return;

        selectedItemId = itemId;
        RefreshView();
    }

    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (!isVisible)
            return;

        RefreshView();
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

    private void CacheStatIcons()
    {
        iconByStat.Clear();

        for (int i = 0; i < statIcons.Count; i++)
            iconByStat[statIcons[i].statType] = statIcons[i].icon;
    }

    private bool TryResolveSelectedItem(EquipmentType type, out int itemId)
    {
        itemId = 0;

        if (handler == null || !handler.TryGetPlayerEquipment(out PlayerEquipment player))
            return false;

        itemId = player.ReturnItemNum(type);
        return itemId != 0;
    }

    private bool TryResolveSelection(out int itemId)
    {
        if (selectionMode == SelectionMode.InventoryItem)
        {
            itemId = selectedItemId;
            return itemId != 0;
        }

        return TryResolveSelectedItem(selectedType, out itemId);
    }

    private bool TryGetEquipmentContext(int itemId, out EquipListTable equipInfo, out EquipmentData equipmentData)
    {
        equipInfo = null;
        equipmentData = default;

        if (itemId == 0)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out equipInfo))
            return false;

        if (!BindInventory())
            return false;

        EquipmentInventoryModule equipmentModule = inventory.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null || !equipmentModule.IsInitialized)
            return false;

        equipmentData = equipmentModule.GetEquipment(itemId);
        return true;
    }

    private void Render(EquipListTable equipInfo, EquipmentData equipmentData)
    {
        int currentLevel = equipmentData.equipmentId == selectedItemId
            ? equipmentData.equipmentReinforcement
            : 0;
        currentLevel = Mathf.Clamp(currentLevel, 0, MaxReinforcementLevel);

        if (previewNameText != null)
            previewNameText.text = equipInfo.equipmentName;

        if (previewIcon != null)
            previewIcon.sprite = LoadIcon(equipInfo);

        if (previewLevelRoot != null)
            previewLevelRoot.gameObject.SetActive(true);

        if (previewLevelText != null)
            previewLevelText.text = $"Lv. {currentLevel}";

        if (previewTierPanel != null)
            previewTierPanel.color = Color.clear;

        int displayTier = Mathf.Max(1, equipInfo.grade);
        int starCount = GetStarCount(displayTier);
        Color tierColor = RarityColor.TierColorByTier(displayTier);
        SyncPreviewStars(starCount, tierColor);
        SetPreviewFrameColor(RarityColor.ItemGradeColor(equipInfo.rarityType));

        RenderStatPreview(equipInfo, currentLevel);
        RenderCost(equipInfo, equipmentData, currentLevel);
    }

    private void RenderStatPreview(EquipListTable equipInfo, int currentLevel)
    {
        List<StatPreviewLine> lines = BuildStatPreviewLines(equipInfo, currentLevel);
        bool isMax = currentLevel >= MaxReinforcementLevel;
        SyncStatRows(lines.Count);

        for (int i = 0; i < statRows.Count; i++)
        {
            bool active = i < lines.Count;
            statRows[i].Root.gameObject.SetActive(active);

            if (!active)
                continue;

            RenderStatRow(statRows[i], lines[i], isMax);
        }
    }

    private void RenderCost(EquipListTable equipInfo, EquipmentData equipmentData, int currentLevel)
    {
        bool isMax = currentLevel >= MaxReinforcementLevel;
        bool isOwned = equipmentData.equipmentId == equipInfo.ID;
        BigDouble cost = isMax ? BigDouble.Zero : CalculateCost(equipInfo, currentLevel);
        bool canReinforce = isOwned && !isMax && CanAfford(cost);

        if (costText != null)
            costText.text = isMax ? maxCostText : cost.ToString();

        if (reinforceButton != null)
            reinforceButton.interactable = canReinforce;
    }

    private List<StatPreviewLine> BuildStatPreviewLines(EquipListTable equipInfo, int currentLevel)
    {
        List<StatPreviewLine> lines = new List<StatPreviewLine>(2);

        AddStatPreviewLine(lines, equipInfo, equipInfo.statType1, currentLevel);
        AddStatPreviewLine(lines, equipInfo, equipInfo.statType2, currentLevel);

        return lines;
    }

    private static void AddStatPreviewLine(List<StatPreviewLine> lines, EquipListTable equipInfo, int statId, int currentLevel)
    {
        if (statId <= 0)
            return;

        if (DataManager.Instance == null || DataManager.Instance.EquipStatsDict == null)
            return;

        if (!DataManager.Instance.EquipStatsDict.TryGetValue(statId, out EquipStatsTable statTable))
            return;

        float currentValue = CalculateTotalStatValue(equipInfo, statTable, currentLevel);
        float nextValue = CalculateTotalStatValue(equipInfo, statTable, Mathf.Min(currentLevel + 1, MaxReinforcementLevel));
        lines.Add(new StatPreviewLine(statTable.statType, currentValue, nextValue));
    }

    private static float CalculateTotalStatValue(EquipListTable equipInfo, EquipStatsTable statTable, int reinforcementLevel)
    {
        if (statTable == null || reinforcementLevel <= 0)
            return 0f;

        float linearValue = statTable.statPerLevel * reinforcementLevel;
        return linearValue + CalculateBonusStatValue(equipInfo, statTable, reinforcementLevel);
    }

    private static float CalculateBonusStatValue(EquipListTable equipInfo, EquipStatsTable statTable, int reinforcementLevel)
    {
        int bonusStepLevel = Mathf.RoundToInt(statTable.bonusStatPerLevel);
        if (bonusStepLevel <= 0 || reinforcementLevel < bonusStepLevel)
            return 0f;

        int bonusStepCount = reinforcementLevel / bonusStepLevel;
        double currentBonus = statTable.baseBonusStat * Math.Pow(statTable.bonusStatPerTier, Mathf.Max(0, equipInfo.equipmentTier - 1));
        float totalBonus = 0f;

        for (int i = 0; i < bonusStepCount; i++)
        {
            totalBonus += (float)currentBonus;
            currentBonus *= statTable.bonusStatPerStep;
        }

        return totalBonus;
    }

    private BigDouble CalculateCost(EquipListTable equipInfo, int currentLevel)
    {
        if (DataManager.Instance == null || DataManager.Instance.EquipStatsDict == null)
            return BigDouble.Zero;

        if (!DataManager.Instance.EquipStatsDict.TryGetValue(equipInfo.statType1, out EquipStatsTable statTable))
            return BigDouble.Zero;

        BigDouble baseCost = statTable.baseCost * Math.Pow(statTable.costPerTier, Math.Max(0, equipInfo.equipmentTier - 1));
        return baseCost + (baseCost * statTable.costPerLevel * currentLevel);
    }

    private bool CanAfford(BigDouble cost)
    {
        if (handler == null || InventoryManager.Instance == null)
            return false;

        handler.SetGoldID();

        if (handler.goldId == 0)
            return false;

        return InventoryManager.Instance.GetItemAmount(handler.goldId) > cost;
    }

    private void SetPreviewFrameColor(Color color)
    {
        if (previewFrames == null)
            return;

        for (int i = 0; i < previewFrames.Length; i++)
        {
            if (previewFrames[i] != null)
                previewFrames[i].color = color;
        }
    }

    private void SyncPreviewStars(int requiredCount, Color color)
    {
        if (previewTierRoot == null || previewTierStarTemplate == null)
            return;

        if (previewStars.Count == 0)
        {
            Image templateImage = previewTierStarTemplate.GetComponent<Image>();
            if (templateImage != null)
                previewStars.Add(templateImage);
        }

        if (previewStars.Count == 0)
            return;

        while (previewStars.Count < requiredCount)
        {
            RectTransform cloneTransform = Instantiate(previewTierStarTemplate, previewTierRoot);
            cloneTransform.name = $"(Img)TierStar_{previewStars.Count + 1}";

            Image cloneImage = cloneTransform.GetComponent<Image>();
            if (cloneImage == null)
                break;

            previewStars.Add(cloneImage);
        }

        for (int i = 0; i < previewStars.Count; i++)
        {
            bool active = i < requiredCount;
            previewStars[i].gameObject.SetActive(active);
            if (active)
                previewStars[i].color = color;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(previewTierRoot);
    }

    private void SetVisible(bool visible)
    {
        if (panelRoot != null && panelRoot.gameObject.activeSelf != visible)
            panelRoot.gameObject.SetActive(visible);
    }

    private void ShowSelectedItem()
    {
        isVisible = true;
        ignoreCloseUntilFrame = Time.frameCount + 1;
        SetVisible(true);
        RefreshView();
    }

    private bool ShouldClosePopup()
    {
        if (Time.frameCount <= ignoreCloseUntilFrame)
            return false;

        if (!TryGetPointerDownPosition(out Vector2 pointerPosition))
            return false;

        return !IsInsidePopup(pointerPosition);
    }

    private bool IsInsidePopup(Vector2 screenPosition)
    {
        if (popupRoot == null)
            return false;

        Canvas parentCanvas = popupRoot.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = parentCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(popupRoot, screenPosition, eventCamera);
    }

    private static bool TryGetPointerDownPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.Input.touchCount > 0)
        {
            Touch touch = UnityEngine.Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            screenPosition = UnityEngine.Input.mousePosition;
            return true;
        }
#endif

        screenPosition = default;
        return false;
    }

    private Sprite GetStatIcon(StatType statType)
    {
        return iconByStat.TryGetValue(statType, out Sprite icon) ? icon : null;
    }

    private static string GetStatDisplayName(StatType statType)
    {
        switch (statType)
        {
            case StatType.HP:
                return "체력";
            case StatType.ATK:
                return "공격력";
            case StatType.ATK_SPEED:
                return "공격속도";
            case StatType.PHYS_DEF:
                return "방어력";
            case StatType.MAGIC_DEF:
                return "마법방어력";
            case StatType.MOVE_SPEED:
                return "이동속도";
            case StatType.COOLDOWN_REDUCE:
                return "쿨타임 감소";
            default:
                return statType.ToString();
        }
    }

    private static string FormatStatValue(StatType statType, float value)
    {
        if (StatGroups.MultTypes.Contains(statType))
            return $"+{value * 100f:0.##}%";

        return $"+{value:0.##}";
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

    private static bool TryGetEquipmentType(int itemId, out EquipmentType type)
    {
        type = EquipmentType.Weapon;

        if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return false;

        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable equipInfo))
            return false;

        type = equipInfo.equipmentType;
        return true;
    }

    private void CacheStatRows()
    {
        statRows.Clear();

        if (statRowTemplate == null || statRowTemplate.Root == null)
            return;

        statRowTemplatePosition = statRowTemplate.Root.anchoredPosition;
        statRowTemplateScale = statRowTemplate.Root.localScale;
        ApplyStatRowLayout(statRowTemplate, 0, 1);
        statRows.Add(statRowTemplate);
    }

    private void SyncStatRows(int requiredCount)
    {
        if (statRowTemplate == null)
            return;

        while (statRows.Count < requiredCount)
        {
            EquipReinforceStatRowUI clone = Instantiate(statRowTemplate, statRowTemplate.Root.parent);
            clone.name = $"(Panel)Stat_{statRows.Count + 1}";
            statRows.Add(clone);
        }

        for (int i = 0; i < statRows.Count; i++)
        {
            ApplyStatRowLayout(statRows[i], i, requiredCount);
            statRows[i].Root.gameObject.SetActive(i < requiredCount);
        }
    }

    private void RenderStatRow(EquipReinforceStatRowUI row, StatPreviewLine line, bool isMax)
    {
        if (row == null)
            return;

        if (row.Icon != null)
        {
            row.Icon.sprite = GetStatIcon(line.StatType);
            row.Icon.enabled = row.Icon.sprite != null;
        }

        if (row.StatNameText != null)
            row.StatNameText.text = GetStatDisplayName(line.StatType);

        if (row.BeforeText != null)
            row.BeforeText.text = FormatStatValue(line.StatType, line.CurrentValue);

        if (row.AfterText != null)
            row.AfterText.text = isMax ? maxCostText : FormatStatValue(line.StatType, line.NextValue);
    }

    private void ApplyStatRowLayout(EquipReinforceStatRowUI row, int index, int requiredCount)
    {
        if (row == null || row.Root == null)
            return;

        bool isMultiRow = requiredCount > 1;
        float spacing = isMultiRow ? MultiStatRowSpacing : SingleStatRowSpacing;
        float yOffset = isMultiRow ? MultiStatRowYOffset : 0f;

        row.Root.localScale = isMultiRow ? statRowTemplateScale * MultiStatRowScale : statRowTemplateScale;
        row.Root.anchoredPosition = statRowTemplatePosition + new Vector2(0f, yOffset - (spacing * index));
    }
}
