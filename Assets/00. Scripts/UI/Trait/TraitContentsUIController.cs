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
        // 화면에 생성된 티어 그룹과 그 안의 노드를 함께 보관합니다.
        public int TierNumber;
        public string TierLabel;
        public TraitGroupItemUI GroupUI;
        public readonly List<TraitRuntime> Items = new List<TraitRuntime>();
    }

    private sealed class TraitRuntime
    {
        // 실제 특성 데이터와 생성된 노드 UI를 묶어 둡니다.
        public PlayerTrait Trait;
        public TraitNodeItemUI ItemUI;
    }

    [Header("런타임 참조")]
    [SerializeField] private CharacterStatManager statManager;

    [Header("목록 참조")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TextMeshProUGUI pointText;
    [SerializeField] private GameObject groupPrefab;
    [SerializeField] private GameObject itemPrefab;

    [Header("팝업 루트")]
    [SerializeField] private GameObject popupRootObject;
    [SerializeField] private Button popupCloseButton;

    [Header("팝업 정보")]
    [SerializeField] private Image popupStatIconImage;
    [SerializeField] private TextMeshProUGUI popupStatText;
    [SerializeField] private TextMeshProUGUI popupBeforeStatText;
    [SerializeField] private TextMeshProUGUI popupAfterStatText;

    [Header("팝업 성장 버튼")]
    [SerializeField] private Button popupUpgradeButton;
    [SerializeField] private Image popupUpgradeButtonImage;
    [SerializeField] private TextMeshProUGUI popupRequirePointText;
    [SerializeField] private TextMeshProUGUI popupGrowthText;

    [Header("비주얼 설정")]
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

    private Coroutine bootstrapRoutine;
    private CurrencyInventoryModule currencyModule;
    private bool traitEventsSubscribed;
    private bool popupEventsConfigured;
    private bool isBuilt;
    private bool missingBindingsLogged;
    private bool missingPopupBindingsLogged;
    private int builtTraitCount;
    private PlayerTrait selectedTrait;

    private bool HasPopupBindings =>
        popupRootObject != null &&
        popupCloseButton != null &&
        popupStatText != null &&
        popupBeforeStatText != null &&
        popupAfterStatText != null &&
        popupUpgradeButton != null &&
        popupRequirePointText != null &&
        popupGrowthText != null;

    protected override void Initialize()
    {
        // 인스펙터에서 받은 스탯 아이콘을 빠르게 찾을 수 있게 캐시합니다.
        CacheStatIcons();

        // 팝업 버튼은 인스펙터 참조를 기준으로 한 번만 연결합니다.
        ConfigurePopupEvents();
        HideTraitPopup();
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
        // 데이터 매니저와 인벤토리가 준비될 때까지 대기합니다.
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
        
        if (IconManager.StatIconSO == null || IconManager.StatIconSO.StatIconDict == null)
            return;

        foreach (var icon in IconManager.StatIconSO.StatIconDict)
        {
            iconByStat[icon.Key] = icon.Value;
        }
    }

    private void ConfigurePopupEvents()
    {
        // 팝업 관련 버튼 이벤트는 인스펙터 참조를 기준으로 연결합니다.
        if (popupEventsConfigured)
            return;

        if (popupCloseButton != null)
        {
            popupCloseButton.onClick.RemoveListener(CloseTraitPopup);
            popupCloseButton.onClick.AddListener(CloseTraitPopup);
        }

        if (popupUpgradeButton != null)
        {
            popupUpgradeButton.onClick.RemoveListener(OnPopupUpgradeClicked);
            popupUpgradeButton.onClick.AddListener(OnPopupUpgradeClicked);
        }

        popupEventsConfigured = true;
    }

    private bool TryPrepareRuntime()
    {
        // 런타임에 필요한 씬 바인딩과 매니저 준비 상태를 확인합니다.
        ConfigurePopupEvents();

        if (contentRoot == null || pointText == null || groupPrefab == null || itemPrefab == null)
        {
            if (!missingBindingsLogged)
            {
                Debug.LogWarning("[TraitContentsUIController] TraitContents 바인딩이 비어 있습니다. 인스펙터의 목록 참조를 확인해 주세요.");
                missingBindingsLogged = true;
            }

            return false;
        }

        if (!TryResolveStatManager())
            return false;

        InventoryManager inventoryManager = InventoryManager.Instance;
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

    private bool TryResolveStatManager()
    {
        if (statManager == null)
            statManager = CharacterStatManager.Instance;

        return statManager != null &&
               statManager.TableLoad &&
               statManager.Traits != null;
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
        // 특성 개수가 바뀌지 않았다면 기존 생성 결과를 재사용합니다.
        List<PlayerTrait> orderedTraits = CollectOrderedTraits();
        if (isBuilt && builtTraitCount == orderedTraits.Count)
            return;

        Rebuild(orderedTraits);
    }

    private List<PlayerTrait> CollectOrderedTraits()
    {
        // 딕셔너리 순서에 의존하지 않도록 티어와 ID 기준으로 정렬합니다.
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
        // 현재 테이블 기준으로 그룹과 노드를 모두 다시 생성합니다.
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
        // 이전에 생성한 런타임 UI를 모두 제거합니다.
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
        // 티어 그룹 프리팹을 생성하고 그룹 UI를 묶어 둡니다.
        GameObject groupObject = Instantiate(groupPrefab, contentRoot, false);
        if (groupObject == null)
            return null;

        groupObject.name = string.Format(CultureInfo.InvariantCulture, "(Panel)TraitGroup_Tier_{0:00}", Mathf.Max(1, tierNumber));

        TraitGroupItemUI groupUI = groupObject.GetComponent<TraitGroupItemUI>();
        if (groupUI == null)
        {
            Debug.LogWarning("[TraitContentsUIController] 특성 그룹 프리팹에 TraitGroupItemUI가 필요합니다.");
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
        // 각 특성 노드 프리팹을 생성하고 클릭 시 팝업이 열리도록 연결합니다.
        if (tierRuntime.GroupUI == null || tierRuntime.GroupUI.ButtonRoot == null)
            return null;

        GameObject itemObject = Instantiate(itemPrefab, tierRuntime.GroupUI.ButtonRoot, false);
        if (itemObject == null)
            return null;

        itemObject.name = string.Format(CultureInfo.InvariantCulture, "(Btn)Trait_{0}", trait.ID);

        TraitNodeItemUI itemUI = itemObject.GetComponent<TraitNodeItemUI>();
        if (itemUI == null)
        {
            Debug.LogWarning("[TraitContentsUIController] 특성 노드 프리팹에 TraitNodeItemUI가 필요합니다.");
            Destroy(itemObject);
            return null;
        }

        itemUI.EnsureBindings();

        if (itemUI.StatIconImage != null)
            itemUI.StatIconImage.sprite = ResolveTraitIcon(trait);

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
        // 포인트, 티어 상태, 노드 상태, 팝업 상태를 한 번에 갱신합니다.
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
            pointText.text = points.ToString("F0");
    }

    private void RefreshTierHeader(TierRuntime tierRuntime, bool tierUnlocked, bool tierMastered)
    {
        // 티어 잠금 여부와 마스터 여부에 따라 헤더 상태를 바꿉니다.
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
        // 노드마다 잠금, 만렙, 강화 가능 상태를 색과 텍스트에 반영합니다.
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
            // 노드 선택은 항상 가능해야 하므로 버튼은 막지 않습니다.
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
            itemUI.StatIconImage.sprite = ResolveTraitIcon(trait);

            itemUI.StatIconImage.color = isLocked ? DisabledTextColor : Color.white;
        }
    }

    private void OpenTraitPopup(PlayerTrait trait)
    {
        // 선택한 특성을 팝업에 표시합니다.
        if (trait == null || !TryPrepareRuntime())
            return;

        if (!HasPopupBindings)
        {
            if (!missingPopupBindingsLogged)
            {
                Debug.LogWarning("[TraitContentsUIController] TraitInfo 팝업 바인딩이 비어 있습니다. 인스펙터의 팝업 참조를 확인해 주세요.");
                missingPopupBindingsLogged = true;
            }

            return;
        }

        selectedTrait = trait;
        ShowTraitPopup();
        RefreshTraitPopup(currencyModule.GetAmount(CurrencyType.TraitPoint));
    }

    private void ShowTraitPopup()
    {
        if (popupRootObject == null)
            return;

        popupRootObject.SetActive(true);
        popupRootObject.transform.SetAsLastSibling();
    }

    private void HideTraitPopup()
    {
        if (popupRootObject == null)
            return;

        popupRootObject.SetActive(false);
    }

    private void CloseTraitPopup()
    {
        // 팝업을 닫을 때 선택 상태도 함께 해제합니다.
        selectedTrait = null;
        HideTraitPopup();
    }

    private void RefreshTraitPopup(BigDouble points)
    {
        // 현재 선택된 특성 기준으로 팝업 내용을 다시 그립니다.
        if (!HasPopupBindings)
            return;

        if (selectedTrait == null)
        {
            HideTraitPopup();
            return;
        }

        bool tierUnlocked = IsTierUnlocked(ParseTierNumber(selectedTrait.TraitTier));
        bool isMaxed = selectedTrait.CurrentLevel >= selectedTrait.MaxLevel;
        bool hasEnoughPoints = points >= new BigDouble(selectedTrait.DecreasePoint);
        bool canUpgrade = tierUnlocked && !isMaxed && hasEnoughPoints;

        // 팝업에는 캐릭터 최종 스탯이 아니라, 선택한 특성이 직접 제공하는 수치만 표시한다.
        float beforeStat = selectedTrait.CurrentStat;
        float afterStat = selectedTrait.CurrentStat + (isMaxed ? 0f : selectedTrait.StatUP);

        Sprite icon = ResolveTraitIcon(selectedTrait);
        if (popupStatIconImage != null)
        {
            popupStatIconImage.sprite = icon;
            popupStatIconImage.enabled = icon != null;
        }

        if (popupStatText != null)
            popupStatText.text = BuildPopupTitle(selectedTrait);

        if (popupBeforeStatText != null)
        {
            popupBeforeStatText.text = FormatStatValue(beforeStat, selectedTrait.statType);
            popupBeforeStatText.color = DisabledTextColor;
        }

        if (popupAfterStatText != null)
        {
            popupAfterStatText.text = isMaxed ? "MAX" : FormatStatValue(afterStat, selectedTrait.statType);
            popupAfterStatText.color = isMaxed ? maxedAccent : Color.white;
        }

        if (popupRequirePointText != null)
        {
            popupRequirePointText.text = selectedTrait.DecreasePoint.ToString(CultureInfo.InvariantCulture);
            popupRequirePointText.color = hasEnoughPoints ? Color.white : lockedAccent;
        }

        if (popupGrowthText != null)
        {
            popupGrowthText.text = isMaxed ? "MAX" : FormatSignedStatValue(selectedTrait.StatUP, selectedTrait.statType);
            popupGrowthText.color = isMaxed ? maxedAccent : tierUnlocked ? Color.white : DisabledTextColor;
        }

        if (popupUpgradeButton != null)
            popupUpgradeButton.interactable = canUpgrade;

        if (popupUpgradeButtonImage != null)
            popupUpgradeButtonImage.color = canUpgrade ? Color.white : PopupLockedTint;

        ShowTraitPopup();
    }

    private void OnPopupUpgradeClicked()
    {
        // 팝업의 성장 버튼은 현재 선택된 특성만 강화합니다.
        if (selectedTrait == null)
            return;

        TryUpgradeTrait(selectedTrait);
    }

    private void TryUpgradeTrait(PlayerTrait trait)
    {
        // 해금 조건, 만렙 여부, 포인트를 확인한 뒤 실제 강화 로직을 호출합니다.
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
        // 이전 티어가 모두 마스터된 경우에만 다음 티어를 엽니다.
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
        // 티어 안의 모든 특성이 만렙이어야 마스터로 판정합니다.
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
        // 인스펙터 매핑이 없으면 현재 노드에 붙은 아이콘을 그대로 사용합니다.
        if (trait == null)
            return null;

        if (iconByStat.TryGetValue(trait.statType, out Sprite mappedIcon) && mappedIcon != null)
            return mappedIcon;

        return IconManager.GetStatIcon(trait.statType);
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        // 특성 포인트가 바뀌면 전체 상태를 다시 갱신합니다.
        if (type != CurrencyType.TraitPoint)
            return;

        RefreshAll();
    }

    private void OnTraitUpdated(int traitId, int currentLevel, int maxLevel)
    {
        // 특성 레벨이 바뀌면 리스트와 팝업을 함께 갱신합니다.
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
        // 퍼센트형 스탯은 보기 좋은 형태로 변환합니다.
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
        // 팝업 상단에는 현재 레벨과 누적 특성 수치를 함께 표시합니다.
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
}
