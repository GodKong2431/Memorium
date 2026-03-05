using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 스탯 업그레이드 리스트를 제어한다.
/// 아이템은 1회 생성하고, 이후에는 렌더 값만 갱신한다.
/// </summary>
public class PlayerStatUpgradeUIController : UIControllerBase
{
    [Serializable]
    private struct StatIconEntry
    {
        // 스탯 행별 아이콘 매핑(선택).
        [SerializeField] public StatType statType;
        [SerializeField] public Sprite icon;
    }

    [Header("List Binding")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject statItemPrefab;
    [SerializeField] private bool clearExistingChildrenOnBuild = true;

    [Header("Display Option")]
    [SerializeField] private List<StatType> displayOrder = new List<StatType>();
    [SerializeField] private List<StatIconEntry> statIcons = new List<StatIconEntry>();

    [Header("Color")]
    [SerializeField] private Color consumeCurrencyCanAffordColor = Color.white;
    [SerializeField] private Color consumeCurrencyLackColor = Color.red;

    private readonly Dictionary<StatType, PlayerStatUpgradeItemView> itemViewByStat = new Dictionary<StatType, PlayerStatUpgradeItemView>();
    private readonly Dictionary<StatType, Sprite> iconByStat = new Dictionary<StatType, Sprite>();

    private PlayerStatUpgradeListView listView;
    private CharacterStatManager subscribedStatManager;
    private Coroutine waitReadyRoutine;
    private bool built;

    protected override void Initialize()
    {
        // 초기화 시 고정 설정 데이터(아이콘 매핑)를 캐시한다.
        CacheStatIcons();
        listView = new PlayerStatUpgradeListView(contentRoot, statItemPrefab, clearExistingChildrenOnBuild);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartWaitReadyRoutine();
    }

    protected override void OnDisable()
    {
        if (waitReadyRoutine != null)
        {
            StopCoroutine(waitReadyRoutine);
            waitReadyRoutine = null;
        }

        base.OnDisable();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        EnsureStatManagerSubscription();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
        UnsubscribeStatManager();
    }

    protected override void RefreshView()
    {
        EnsureStatManagerSubscription();

        // UI 활성화 이후 데이터가 늦게 로드될 수 있으므로 준비 전에는 스킵한다.
        if (!IsDataReady())
            return;

        // 하이브리드 플로우: 1회 생성 후 캐시된 행만 갱신한다.
        EnsureBuilt();
        UpdateAllItems();
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != CurrencyType.Gold)
            return;

        UpdateAllItems();
    }

    private void OnClickUpgrade(StatType statType)
    {
        if (CharacterStatManager.Instance == null)
            return;

        StatUpgrade statUpgrade = CharacterStatManager.Instance.GetUpgradeTable(statType);
        if (statUpgrade == null)
            return;

        statUpgrade.Upgrade();
        UpdateAllItems();
    }

    private void EnsureBuilt()
    {
        if (built)
            return;

        List<StatType> statTypes = ResolveDisplayStatTypes();
        if (statTypes.Count == 0)
            return;

        itemViewByStat.Clear();

        Dictionary<StatType, PlayerStatUpgradeItemView> builtViews = listView.Build(statTypes, OnClickUpgrade);
        if (builtViews.Count == 0)
            return;

        foreach (KeyValuePair<StatType, PlayerStatUpgradeItemView> pair in builtViews)
            itemViewByStat[pair.Key] = pair.Value;

        // 빌드 완료 상태로 전환해 이후에는 생성 없이 값만 갱신한다.
        built = true;
    }

    private void UpdateAllItems()
    {
        if (!built || CharacterStatManager.Instance == null)
            return;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;

        BigDouble currentGold = currencyModule != null
            ? currencyModule.GetAmount(CurrencyType.Gold)
            : BigDouble.Zero;

        foreach (KeyValuePair<StatType, PlayerStatUpgradeItemView> pair in itemViewByStat)
        {
            if (!CharacterStatManager.Instance.Upgrades.TryGetValue(pair.Key, out StatUpgrade statUpgrade))
                continue;

            bool canAfford = currentGold >= statUpgrade.CurrentCost;
            Color costColor = canAfford ? consumeCurrencyCanAffordColor : consumeCurrencyLackColor;

            pair.Value.Render(
                GetIcon(pair.Key),
                $"Lv {statUpgrade.UpgradeCount}",
                statUpgrade.StatName,
                FormatCurrentStatText(pair.Key, statUpgrade.Stat),
                statUpgrade.CurrentCost.ToString(),
                canAfford,
                costColor);
        }
    }

    private List<StatType> ResolveDisplayStatTypes()
    {
        List<StatType> statTypes = new List<StatType>();

        if (CharacterStatManager.Instance == null || CharacterStatManager.Instance.Upgrades == null)
            return statTypes;

        if (displayOrder.Count > 0)
        {
            for (int i = 0; i < displayOrder.Count; i++)
            {
                StatType statType = displayOrder[i];
                if (statType == StatType.None)
                    continue;
                if (!CharacterStatManager.Instance.Upgrades.ContainsKey(statType))
                    continue;
                if (statTypes.Contains(statType))
                    continue;

                statTypes.Add(statType);
            }

            return statTypes;
        }

        foreach (StatType statType in CharacterStatManager.Instance.Upgrades.Keys)
        {
            if (statType == StatType.None)
                continue;

            statTypes.Add(statType);
        }

        statTypes.Sort((a, b) => ((int)a).CompareTo((int)b));
        return statTypes;
    }

    private void CacheStatIcons()
    {
        iconByStat.Clear();

        for (int i = 0; i < statIcons.Count; i++)
        {
            StatIconEntry entry = statIcons[i];
            iconByStat[entry.statType] = entry.icon;
        }
    }

    private Sprite GetIcon(StatType statType)
    {
        return iconByStat.TryGetValue(statType, out Sprite icon) ? icon : null;
    }

    private static string FormatCurrentStatText(StatType statType, float statValue)
    {
        if (StatGroups.MultTypes.Contains(statType))
            return $"+{(statValue * 100f):0.##}%";

        return $"+{statValue:0.##}";
    }

    private void StartWaitReadyRoutine()
    {
        if (waitReadyRoutine != null)
            StopCoroutine(waitReadyRoutine);

        // 스탯 데이터가 준비되면 뷰 초기화를 다시 시도한다.
        waitReadyRoutine = StartCoroutine(CoWaitReady());
    }

    private IEnumerator CoWaitReady()
    {
        yield return new WaitUntil(IsDataReady);
        RefreshView();
        waitReadyRoutine = null;
    }

    private bool IsDataReady()
    {
        return CharacterStatManager.Instance != null
            && CharacterStatManager.Instance.TableLoad
            && CharacterStatManager.Instance.Upgrades != null
            && CharacterStatManager.Instance.Upgrades.Count > 0;
    }

    private void EnsureStatManagerSubscription()
    {
        CharacterStatManager manager = CharacterStatManager.Instance;
        if (manager == null || !manager.TableLoad)
            return;

        if (subscribedStatManager == manager)
            return;

        // StatUpdate 이벤트는 항상 하나의 구독만 유지한다.
        UnsubscribeStatManager();
        subscribedStatManager = manager;
        subscribedStatManager.StatUpdate += RefreshView;
    }

    private void UnsubscribeStatManager()
    {
        if (subscribedStatManager == null)
            return;

        subscribedStatManager.StatUpdate -= RefreshView;
        subscribedStatManager = null;
    }
}
