using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitContentsUIController : UIControllerBase
{
    [Serializable]
    private struct StatIconEntry
    {
        [SerializeField] public StatType statType;
        [SerializeField] public Sprite icon;
    }

    private sealed class TierRuntime
    {
        public int TierNumber;
        public string TierLabel;
        public TraitGroupItemUI GroupUI;
        public readonly List<TraitRuntime> Items = new List<TraitRuntime>();
    }

    private sealed class TraitRuntime
    {
        public PlayerTrait Trait;
        public TraitNodeItemUI ItemUI;
    }

    private sealed class TraitInfoPopupRuntime
    {
        public GameObject RootObject;
        public Image BackgroundImage;
        public Button BackgroundButton;
        public Image CardImage;
        public Image StatIconImage;
        public TextMeshProUGUI StatText;
        public TextMeshProUGUI BeforeStatText;
        public TextMeshProUGUI AfterStatText;
        public Button UpgradeButton;
        public Image UpgradeButtonImage;
        public TextMeshProUGUI RequirePointText;
        public Image GrowthIconImage;
        public TextMeshProUGUI GrowthText;

        public bool HasCoreBindings =>
            RootObject != null &&
            StatText != null &&
            BeforeStatText != null &&
            AfterStatText != null &&
            UpgradeButton != null &&
            RequirePointText != null &&
            GrowthText != null;

        public bool IsVisible => RootObject != null && RootObject.activeSelf;

        public void Show()
        {
            if (RootObject == null)
                return;

            RootObject.SetActive(true);
            RootObject.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (RootObject == null)
                return;

            RootObject.SetActive(false);
        }
    }

    [Header("Scene Binding")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TextMeshProUGUI pointText;
    [SerializeField] private GameObject groupPrefab;
    [SerializeField] private GameObject itemPrefab;

    [Header("Visual")]
    [SerializeField] private List<StatIconEntry> statIcons = new List<StatIconEntry>();
    [SerializeField] private Color availableAccent = new Color(1f, 0.83137256f, 0.22745098f, 1f);
    [SerializeField] private Color lockedAccent = new Color(0.59607846f, 0.627451f, 0.6862745f, 1f);
    [SerializeField] private Color maxedAccent = new Color(0.34117648f, 0.8666667f, 0.58431375f, 1f);

    private static readonly Regex TierNumberRegex = new Regex(@"\d+", RegexOptions.Compiled);
    private static readonly Color DisabledTextColor = new Color(1f, 1f, 1f, 0.65f);
    private static readonly Color LockedPanelTint = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color PopupLockedTint = new Color(0.58431375f, 0.6117647f, 0.6745098f, 1f);

    private readonly Dictionary<StatType, Sprite> iconByStat = new Dictionary<StatType, Sprite>();
    private readonly List<TierRuntime> tierRuntimes = new List<TierRuntime>();
    private readonly TraitInfoPopupRuntime popupRuntime = new TraitInfoPopupRuntime();

    private Coroutine bootstrapRoutine;
    private CharacterStatManager statManager;
    private InventoryManager inventoryManager;
    private CurrencyInventoryModule currencyModule;
    private bool traitEventsSubscribed;
    private bool isBuilt;
    private bool missingBindingsLogged;
    private bool missingPopupBindingsLogged;
    private int builtTraitCount;
    private PlayerTrait selectedTrait;

    protected override void Initialize()
    {
        AutoBindSceneReferences();
        AutoBindPopup();
        CacheStatIcons();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        StartBootstrap();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
        UnsubscribeTraitEvents();
        StopBootstrap();
        CloseTraitPopup();
    }

    protected override void RefreshView()
    {
        if (!TryPrepareRuntime())
            return;

        BuildIfNeeded();
        RefreshAll();
    }

    private void StartBootstrap()
    {
        if (bootstrapRoutine == null)
            bootstrapRoutine = StartCoroutine(BootstrapRoutine());
    }

    private void StopBootstrap()
    {
        if (bootstrapRoutine == null)
            return;

        StopCoroutine(bootstrapRoutine);
        bootstrapRoutine = null;
    }

    private IEnumerator BootstrapRoutine()
    {
        while (!TryPrepareRuntime())
            yield return null;

        BuildIfNeeded();
        RefreshAll();
        bootstrapRoutine = null;
    }

    private void CacheStatIcons()
    {
        iconByStat.Clear();

        for (int i = 0; i < statIcons.Count; i++)
        {
            StatIconEntry entry = statIcons[i];
            if (entry.icon == null)
                continue;

            iconByStat[entry.statType] = entry.icon;
        }
    }

    private void AutoBindSceneReferences()
    {
        if (contentRoot == null)
        {
            ScrollRect scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null)
                contentRoot = scrollRect.content;
        }

        if (pointText == null)
        {
            Transform pointPanel = FindDescendantByName(transform, "(Panel)TraitPoint");
            if (pointPanel != null)
                pointText = pointPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void AutoBindPopup()
    {
        if (popupRuntime.HasCoreBindings)
            return;

        Transform popupRoot = FindTraitPopupRoot(transform.root != null ? transform.root : transform);
        if (popupRoot == null)
            return;

        Transform background = FindDirectChild(popupRoot, "(Panel)Background");
        Transform card = FindPopupCard(popupRoot, background);
        Transform statPanel = FindDirectChild(card, "(Img)StatIcon");
        Transform upgradeButton = FindDirectChild(card, "(Btn)Growth");
        Transform requirePointRoot = FindDescendantByName(upgradeButton, "(Text)RequirePoint");

        popupRuntime.RootObject = popupRoot.gameObject;
        popupRuntime.BackgroundImage = background != null ? background.GetComponent<Image>() : null;
        popupRuntime.CardImage = card != null ? card.GetComponent<Image>() : null;
        popupRuntime.StatIconImage = statPanel != null ? statPanel.GetComponent<Image>() : null;
        popupRuntime.StatText = GetTextComponent(statPanel, "(Text)Stat");
        popupRuntime.BeforeStatText = GetTextComponent(statPanel, "(Text)BeforeStat");
        popupRuntime.AfterStatText = GetTextComponent(statPanel, "(Text)AfterStat");
        popupRuntime.UpgradeButton = upgradeButton != null ? upgradeButton.GetComponent<Button>() : null;
        popupRuntime.UpgradeButtonImage = upgradeButton != null ? upgradeButton.GetComponent<Image>() : null;
        popupRuntime.RequirePointText = requirePointRoot != null ? requirePointRoot.GetComponent<TextMeshProUGUI>() : null;
        popupRuntime.GrowthIconImage = GetImageComponent(requirePointRoot, "(Img)TraitIcon");
        popupRuntime.GrowthText = GetTextComponent(requirePointRoot, "(Text)Growth");

        if (popupRuntime.BackgroundImage != null)
        {
            popupRuntime.BackgroundButton = popupRuntime.BackgroundImage.GetComponent<Button>();
            if (popupRuntime.BackgroundButton == null)
            {
                popupRuntime.BackgroundButton = popupRuntime.BackgroundImage.gameObject.AddComponent<Button>();
                popupRuntime.BackgroundButton.transition = Selectable.Transition.None;
                popupRuntime.BackgroundButton.targetGraphic = popupRuntime.BackgroundImage;
            }

            popupRuntime.BackgroundButton.onClick.RemoveListener(CloseTraitPopup);
            popupRuntime.BackgroundButton.onClick.AddListener(CloseTraitPopup);
        }

        if (popupRuntime.UpgradeButton != null)
        {
            popupRuntime.UpgradeButton.onClick.RemoveListener(OnPopupUpgradeClicked);
            popupRuntime.UpgradeButton.onClick.AddListener(OnPopupUpgradeClicked);
        }

        popupRuntime.Hide();
    }

    private bool TryPrepareRuntime()
    {
        AutoBindSceneReferences();
        AutoBindPopup();

        if (contentRoot == null || pointText == null || groupPrefab == null || itemPrefab == null)
        {
            if (!missingBindingsLogged)
            {
                Debug.LogWarning("[TraitContentsUIController] Missing TraitContents bindings. Assign contentRoot, pointText, groupPrefab and itemPrefab in StageScene.");
                missingBindingsLogged = true;
            }

            return false;
        }

        if (statManager == null)
            statManager = FindFirstObjectByType<CharacterStatManager>();

        if (statManager == null || !statManager.TableLoad || statManager.Traits == null)
            return false;

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance != null ? InventoryManager.Instance : FindFirstObjectByType<InventoryManager>();

        if (inventoryManager == null)
            return false;

        currencyModule = inventoryManager.GetModule<CurrencyInventoryModule>();
        if (currencyModule == null)
            return false;

        if (!traitEventsSubscribed)
        {
            statManager.TraitUpdate += OnTraitUpdated;
            traitEventsSubscribed = true;
        }

        return true;
    }

    private void UnsubscribeTraitEvents()
    {
        if (!traitEventsSubscribed || statManager == null)
            return;

        statManager.TraitUpdate -= OnTraitUpdated;
        traitEventsSubscribed = false;
    }

    private void BuildIfNeeded()
    {
        List<PlayerTrait> orderedTraits = CollectOrderedTraits();
        if (isBuilt && builtTraitCount == orderedTraits.Count)
            return;

        Rebuild(orderedTraits);
    }

    private List<PlayerTrait> CollectOrderedTraits()
    {
        List<PlayerTrait> orderedTraits = new List<PlayerTrait>();

        foreach (KeyValuePair<StatType, PlayerTrait> pair in statManager.Traits)
        {
            PlayerTrait trait = pair.Value;
            if (trait == null)
                continue;

            trait.statType = pair.Key;
            orderedTraits.Add(trait);
        }

        orderedTraits.Sort(CompareTraitOrder);
        return orderedTraits;
    }

    private void Rebuild(List<PlayerTrait> orderedTraits)
    {
        ClearContentRoot();
        tierRuntimes.Clear();

        TierRuntime currentTier = null;
        int currentTierNumber = int.MinValue;

        for (int i = 0; i < orderedTraits.Count; i++)
        {
            PlayerTrait trait = orderedTraits[i];
            int tierNumber = ParseTierNumber(trait.TraitTier);

            if (currentTier == null || currentTierNumber != tierNumber)
            {
                currentTierNumber = tierNumber;
                currentTier = CreateTierRuntime(tierNumber, trait.TraitTier);
                if (currentTier != null)
                    tierRuntimes.Add(currentTier);
            }

            if (currentTier == null)
                continue;

            TraitRuntime itemRuntime = CreateTraitRuntime(currentTier, trait);
            if (itemRuntime != null)
                currentTier.Items.Add(itemRuntime);
        }

        isBuilt = true;
        builtTraitCount = orderedTraits.Count;

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private void ClearContentRoot()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child == null)
                continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private TierRuntime CreateTierRuntime(int tierNumber, string tierLabel)
    {
        GameObject groupObject = Instantiate(groupPrefab, contentRoot, false);
        if (groupObject == null)
            return null;

        groupObject.name = string.Format(CultureInfo.InvariantCulture, "(Panel)TraitGroup_Tier_{0:00}", Mathf.Max(1, tierNumber));

        TraitGroupItemUI groupUI = groupObject.GetComponent<TraitGroupItemUI>();
        if (groupUI == null)
        {
            Debug.LogWarning("[TraitContentsUIController] Trait group prefab requires TraitGroupItemUI.");
            Destroy(groupObject);
            return null;
        }

        groupUI.EnsureBindings();

        return new TierRuntime
        {
            TierNumber = tierNumber,
            TierLabel = BuildTierLabel(tierNumber, tierLabel),
            GroupUI = groupUI
        };
    }

    private TraitRuntime CreateTraitRuntime(TierRuntime tierRuntime, PlayerTrait trait)
    {
        if (tierRuntime.GroupUI == null || tierRuntime.GroupUI.ButtonRoot == null)
            return null;

        GameObject itemObject = Instantiate(itemPrefab, tierRuntime.GroupUI.ButtonRoot, false);
        if (itemObject == null)
            return null;

        itemObject.name = string.Format(CultureInfo.InvariantCulture, "(Btn)Trait_{0}", trait.ID);

        TraitNodeItemUI itemUI = itemObject.GetComponent<TraitNodeItemUI>();
        if (itemUI == null)
        {
            Debug.LogWarning("[TraitContentsUIController] Trait item prefab requires TraitNodeItemUI.");
            Destroy(itemObject);
            return null;
        }

        itemUI.EnsureBindings();

        if (itemUI.StatIconImage != null && iconByStat.TryGetValue(trait.statType, out Sprite icon) && icon != null)
            itemUI.StatIconImage.sprite = icon;

        if (itemUI.Button != null)
        {
            PlayerTrait cachedTrait = trait;
            itemUI.Button.onClick.RemoveAllListeners();
            itemUI.Button.onClick.AddListener(delegate { OpenTraitPopup(cachedTrait); });
        }

        return new TraitRuntime
        {
            Trait = trait,
            ItemUI = itemUI
        };
    }

    private void RefreshAll()
    {
        if (!isBuilt || currencyModule == null)
            return;

        BigDouble points = currencyModule.GetAmount(CurrencyType.TraitPoint);
        RefreshPointText(points);

        bool previousTiersMastered = true;

        for (int i = 0; i < tierRuntimes.Count; i++)
        {
            TierRuntime tierRuntime = tierRuntimes[i];
            bool tierUnlocked = previousTiersMastered;
            bool tierMastered = IsTierMastered(tierRuntime);

            RefreshTierHeader(tierRuntime, tierUnlocked, tierMastered);

            for (int itemIndex = 0; itemIndex < tierRuntime.Items.Count; itemIndex++)
                RefreshTraitItem(tierRuntime.Items[itemIndex], tierUnlocked, points);

            if (!tierMastered)
                previousTiersMastered = false;
        }

        RefreshTraitPopup(points);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private void RefreshPointText(BigDouble points)
    {
        if (pointText != null)
            pointText.text = points.ToString();
    }

    private void RefreshTierHeader(TierRuntime tierRuntime, bool tierUnlocked, bool tierMastered)
    {
        if (tierRuntime == null || tierRuntime.GroupUI == null)
            return;

        TraitGroupItemUI groupUI = tierRuntime.GroupUI;
        groupUI.EnsureBindings();

        Color accent = tierMastered ? maxedAccent : tierUnlocked ? availableAccent : lockedAccent;

        if (groupUI.TierText != null)
        {
            groupUI.TierText.text = BuildTierHeaderText(tierRuntime.TierLabel);
            groupUI.TierText.color = accent;
        }

        if (groupUI.Background != null)
            groupUI.Background.color = tierUnlocked ? Color.white : LockedPanelTint;

        if (groupUI.LineImage != null)
            groupUI.LineImage.color = accent;
    }

    private void RefreshTraitItem(TraitRuntime traitRuntime, bool tierUnlocked, BigDouble points)
    {
        if (traitRuntime == null || traitRuntime.Trait == null || traitRuntime.ItemUI == null)
            return;

        PlayerTrait trait = traitRuntime.Trait;
        TraitNodeItemUI itemUI = traitRuntime.ItemUI;
        itemUI.EnsureBindings();

        bool isMaxed = trait.CurrentLevel >= trait.MaxLevel;
        bool hasEnoughPoints = points >= new BigDouble(trait.DecreasePoint);
        bool isLocked = !tierUnlocked;
        bool canUpgrade = tierUnlocked && !isMaxed && hasEnoughPoints;

        Color accent = isMaxed
            ? maxedAccent
            : canUpgrade
                ? availableAccent
                : lockedAccent;

        if (itemUI.Button != null)
            itemUI.Button.interactable = true;

        if (itemUI.AmountText != null)
        {
            itemUI.AmountText.text = string.Format(CultureInfo.InvariantCulture, "{0} / {1}", trait.CurrentLevel, trait.MaxLevel);
            itemUI.AmountText.color = isLocked ? DisabledTextColor : isMaxed ? maxedAccent : Color.white;
        }

        if (itemUI.Background != null)
            itemUI.Background.color = new Color(accent.r, accent.g, accent.b, isLocked ? 0.03f : isMaxed ? 0.18f : canUpgrade ? 0.08f : 0.05f);

        if (itemUI.FrameImage != null)
            itemUI.FrameImage.color = accent;

        if (itemUI.StatIconImage != null)
        {
            if (iconByStat.TryGetValue(trait.statType, out Sprite icon) && icon != null)
                itemUI.StatIconImage.sprite = icon;

            itemUI.StatIconImage.color = isLocked ? DisabledTextColor : Color.white;
        }
    }

    private void OpenTraitPopup(PlayerTrait trait)
    {
        if (trait == null || !TryPrepareRuntime())
            return;

        AutoBindPopup();
        if (!popupRuntime.HasCoreBindings)
        {
            if (!missingPopupBindingsLogged)
            {
                Debug.LogWarning("[TraitContentsUIController] Missing TraitInfo popup bindings in StageScene.");
                missingPopupBindingsLogged = true;
            }

            return;
        }

        selectedTrait = trait;
        popupRuntime.Show();
        RefreshTraitPopup(currencyModule.GetAmount(CurrencyType.TraitPoint));
    }

    private void CloseTraitPopup()
    {
        selectedTrait = null;
        popupRuntime.Hide();
    }

    private void RefreshTraitPopup(BigDouble points)
    {
        if (!popupRuntime.HasCoreBindings)
            return;

        if (selectedTrait == null)
        {
            popupRuntime.Hide();
            return;
        }

        bool tierUnlocked = IsTierUnlocked(ParseTierNumber(selectedTrait.TraitTier));
        bool isMaxed = selectedTrait.CurrentLevel >= selectedTrait.MaxLevel;
        bool hasEnoughPoints = points >= new BigDouble(selectedTrait.DecreasePoint);
        bool canUpgrade = tierUnlocked && !isMaxed && hasEnoughPoints;

        float beforeStat = statManager != null
            ? statManager.GetPreviewFinalStat(selectedTrait.statType, 0f)
            : 0f;
        float afterStat = statManager != null
            ? statManager.GetPreviewFinalStat(selectedTrait.statType, isMaxed ? 0f : selectedTrait.StatUP)
            : beforeStat;

        Sprite icon = ResolveTraitIcon(selectedTrait);
        if (icon != null)
        {
            if (popupRuntime.StatIconImage != null)
                popupRuntime.StatIconImage.sprite = icon;

            if (popupRuntime.GrowthIconImage != null)
                popupRuntime.GrowthIconImage.sprite = icon;
        }

        if (popupRuntime.StatText != null)
            popupRuntime.StatText.text = BuildPopupTitle(selectedTrait);

        if (popupRuntime.BeforeStatText != null)
        {
            popupRuntime.BeforeStatText.text = FormatStatValue(beforeStat, selectedTrait.statType);
            popupRuntime.BeforeStatText.color = DisabledTextColor;
        }

        if (popupRuntime.AfterStatText != null)
        {
            popupRuntime.AfterStatText.text = isMaxed ? "MAX" : FormatStatValue(afterStat, selectedTrait.statType);
            popupRuntime.AfterStatText.color = isMaxed ? maxedAccent : Color.white;
        }

        if (popupRuntime.RequirePointText != null)
        {
            popupRuntime.RequirePointText.text = selectedTrait.DecreasePoint.ToString(CultureInfo.InvariantCulture);
            popupRuntime.RequirePointText.color = hasEnoughPoints ? Color.white : lockedAccent;
        }

        if (popupRuntime.GrowthText != null)
        {
            popupRuntime.GrowthText.text = isMaxed ? "MAX" : FormatSignedStatValue(selectedTrait.StatUP, selectedTrait.statType);
            popupRuntime.GrowthText.color = isMaxed ? maxedAccent : tierUnlocked ? Color.white : DisabledTextColor;
        }

        if (popupRuntime.UpgradeButton != null)
            popupRuntime.UpgradeButton.interactable = canUpgrade;

        if (popupRuntime.UpgradeButtonImage != null)
            popupRuntime.UpgradeButtonImage.color = canUpgrade ? Color.white : PopupLockedTint;

        popupRuntime.Show();
    }

    private void OnPopupUpgradeClicked()
    {
        if (selectedTrait == null)
            return;

        TryUpgradeTrait(selectedTrait);
    }

    private void TryUpgradeTrait(PlayerTrait trait)
    {
        if (trait == null || !TryPrepareRuntime())
            return;

        int tierNumber = ParseTierNumber(trait.TraitTier);
        if (!IsTierUnlocked(tierNumber))
            return;

        if (trait.CurrentLevel >= trait.MaxLevel)
            return;

        BigDouble points = currencyModule.GetAmount(CurrencyType.TraitPoint);
        if (points < new BigDouble(trait.DecreasePoint))
            return;

        if (statManager.TraitUpgrade(trait))
            RefreshAll();
    }

    private bool IsTierUnlocked(int tierNumber)
    {
        for (int i = 0; i < tierRuntimes.Count; i++)
        {
            TierRuntime tierRuntime = tierRuntimes[i];
            if (tierRuntime.TierNumber >= tierNumber)
                return true;

            if (!IsTierMastered(tierRuntime))
                return false;
        }

        return true;
    }

    private bool IsTierMastered(TierRuntime tierRuntime)
    {
        if (tierRuntime == null || tierRuntime.Items.Count == 0)
            return false;

        for (int i = 0; i < tierRuntime.Items.Count; i++)
        {
            TraitRuntime item = tierRuntime.Items[i];
            if (item == null || item.Trait == null)
                continue;

            if (item.Trait.CurrentLevel < item.Trait.MaxLevel)
                return false;
        }

        return true;
    }

    private Sprite ResolveTraitIcon(PlayerTrait trait)
    {
        if (trait == null)
            return null;

        if (iconByStat.TryGetValue(trait.statType, out Sprite mappedIcon) && mappedIcon != null)
            return mappedIcon;

        TraitRuntime runtime = FindTraitRuntime(trait);
        if (runtime != null && runtime.ItemUI != null && runtime.ItemUI.StatIconImage != null)
            return runtime.ItemUI.StatIconImage.sprite;

        return null;
    }

    private TraitRuntime FindTraitRuntime(PlayerTrait trait)
    {
        if (trait == null)
            return null;

        for (int i = 0; i < tierRuntimes.Count; i++)
        {
            List<TraitRuntime> items = tierRuntimes[i].Items;
            for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                TraitRuntime runtime = items[itemIndex];
                if (runtime != null && ReferenceEquals(runtime.Trait, trait))
                    return runtime;
            }
        }

        return null;
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != CurrencyType.TraitPoint)
            return;

        RefreshAll();
    }

    private void OnTraitUpdated(int traitId, int currentLevel, int maxLevel)
    {
        RefreshAll();
    }

    private static int CompareTraitOrder(PlayerTrait left, PlayerTrait right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;

        int tierCompare = ParseTierNumber(left.TraitTier).CompareTo(ParseTierNumber(right.TraitTier));
        if (tierCompare != 0)
            return tierCompare;

        int idCompare = left.ID.CompareTo(right.ID);
        if (idCompare != 0)
            return idCompare;

        return string.CompareOrdinal(left.TraitName, right.TraitName);
    }

    private static string BuildTierLabel(int tierNumber, string sourceLabel)
    {
        if (!string.IsNullOrWhiteSpace(sourceLabel))
            return sourceLabel;

        return tierNumber > 0
            ? string.Format(CultureInfo.InvariantCulture, "Tier {0}", tierNumber)
            : "Trait Tier";
    }

    private static string BuildTierHeaderText(string tierLabel)
    {
        return string.IsNullOrWhiteSpace(tierLabel) ? "Trait Tier" : tierLabel;
    }

    private static int ParseTierNumber(string tierLabel)
    {
        if (string.IsNullOrWhiteSpace(tierLabel))
            return 0;

        Match match = TierNumberRegex.Match(tierLabel);
        if (!match.Success)
            return 0;

        int tierNumber;
        return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out tierNumber) ? tierNumber : 0;
    }

    private static string FormatStatValue(float value, StatType statType)
    {
        float displayValue = StatGroups.MultTypes.Contains(statType) ? value * 100f : value;
        string number = displayValue.ToString("0.##", CultureInfo.InvariantCulture);
        return StatGroups.MultTypes.Contains(statType) ? number + "%" : number;
    }

    private static string FormatSignedStatValue(float value, StatType statType)
    {
        float displayValue = StatGroups.MultTypes.Contains(statType) ? value * 100f : value;
        string format = displayValue >= 0f ? "+0.##;-0.##" : "0.##";
        string number = displayValue.ToString(format, CultureInfo.InvariantCulture);
        return StatGroups.MultTypes.Contains(statType) ? number + "%" : number;
    }

    private string BuildPopupTitle(PlayerTrait trait)
    {
        string statName = GetPopupStatName(trait);
        string currentValue = FormatSignedStatValue(trait.CurrentStat, trait.statType);
        return string.Format(
            CultureInfo.InvariantCulture,
            "[{0} / {1}] {2} {3}",
            trait.CurrentLevel,
            trait.MaxLevel,
            statName,
            currentValue);
    }

    private static string GetPopupStatName(PlayerTrait trait)
    {
        if (trait == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(trait.TraitUPStatName))
            return trait.TraitUPStatName;

        if (!string.IsNullOrWhiteSpace(trait.TraitName))
            return trait.TraitName;

        return trait.statType.ToString();
    }

    private static Transform FindTraitPopupRoot(Transform searchRoot)
    {
        Transform popupRoot = FindTraitPopupRootIn(searchRoot);
        if (popupRoot != null)
            return popupRoot;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return FindTraitPopupRootIn(sceneTransforms);
    }

    private static Transform FindTraitPopupRootIn(Transform searchRoot)
    {
        if (searchRoot == null)
            return null;

        return FindTraitPopupRootIn(searchRoot.GetComponentsInChildren<Transform>(true));
    }

    private static Transform FindTraitPopupRootIn(Transform[] transforms)
    {
        if (transforms == null)
            return null;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current.name != "(Panel)TraitInfo")
                continue;

            if (FindDirectChild(current, "(Panel)Background") == null)
                continue;

            if (FindPopupCard(current, FindDirectChild(current, "(Panel)Background")) != null)
                return current;
        }

        return null;
    }

    private static Transform FindPopupCard(Transform popupRoot, Transform background)
    {
        if (popupRoot == null)
            return null;

        for (int i = 0; i < popupRoot.childCount; i++)
        {
            Transform child = popupRoot.GetChild(i);
            if (child == null || child == background)
                continue;

            if (child.name == "(Panel)TraitInfo")
                return child;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
            return null;

        Transform[] transforms = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name == childName)
                return current;
        }

        return null;
    }

    private static TextMeshProUGUI GetTextComponent(Transform parent, string childName)
    {
        Transform child = FindDescendantByName(parent, childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Image GetImageComponent(Transform parent, string childName)
    {
        Transform child = FindDescendantByName(parent, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }
}
