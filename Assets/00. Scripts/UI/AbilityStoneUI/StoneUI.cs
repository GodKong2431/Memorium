using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed partial class StoneUI : UIControllerBase
{
    private const string TextStone = "스톤";
    private const string TextBonusStat = "보너스 스탯";
    private const string TextAccumulatedSuccess = "누적 강화 성공";
    private const string TextSuccessRate = "성공 확률";
    private const string TextNeed = "필요";
    private const string TextTimesNeed = "회 필요";
    private const string TextNeedSetup = "설정 필요";
    private const string TextNotConfigured = "미설정";
    private const string TextLocked = "잠김";
    private const string TextUpgradeComplete = "강화 완료";
    private const string TextNotEnoughGold = "골드 부족";
    private const string TextDataLoading = "데이터 로딩 중";
    private const string TextNoData = "데이터 없음";
    private const string TextFinalGrade = "최종 등급";
    private const string TextOpen = "열기";
    private const string TextUpgrade = "강화";
    private const string TextReconfigure = "재설정";
    private const string TextReset = "강화 초기화";
    private const string TextReconfigureInfo = "현재 옵션을 다시 구성합니다.";
    private const string TextResetInfo = "현재 강화 수치를 모두 초기화합니다.";
    private const string TextCurrent = "현재";

    [Header("Runtime")]
    [SerializeField] private CharacterStatManager stats;
    [SerializeField] private StatIconSO icons;

    [Header("Stone List")]
    [SerializeField] private RectTransform stoneRoot;
    [SerializeField] private List<StoneItemUI> stoneViews = new List<StoneItemUI>();
    [SerializeField] private StoneItemUI stoneItem;

    [Header("Placed Panels")]
    [SerializeField] private StoneInfoPanelUI infoPanel;
    [SerializeField] private List<StoneBonusItemUI> bonusViews = new List<StoneBonusItemUI>();
    [SerializeField] private StoneBonusItemUI bonusItem;
    [SerializeField] private StoneUpgradePanelUI upgradePanel;

    [Header("Shared UI")]
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("List Color")]
    [SerializeField] private Color selectedStoneColor = new Color(1f, 0.92f, 0.68f, 1f);
    [SerializeField] private Color unlockedStoneColor = Color.white;
    [SerializeField] private Color lockedStoneColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color unlockedBonusColor = Color.white;
    [SerializeField] private Color lockedBonusColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color positiveResultColor = new Color(0.36f, 0.88f, 1f, 1f);
    [SerializeField] private Color penaltyResultColor = new Color(1f, 0.46f, 0.46f, 1f);
    [SerializeField] private Color failResultColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color waitingResultColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color disabledTextColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    private readonly List<StoneItemUI> runtimeStoneItems = new List<StoneItemUI>();
    private readonly List<StoneBonusItemUI> runtimeBonusItems = new List<StoneBonusItemUI>();
    private readonly Dictionary<StatType, Sprite> iconByStat = new Dictionary<StatType, Sprite>();

    private CharacterStatManager subscribedStatManager;
    private CurrencyInventoryModule currencyModule;
    private Coroutine bootstrapRoutine;
    private bool built;
    private bool bindingsWarningLogged;
    private bool panelEventsBound;
    private StoneGrade? selectedGrade;

    protected override void Initialize()
    {
        // 스탯 아이콘 캐시는 시작할 때 한 번만 준비한다.
        CacheStatIcons();

        if (HasSceneRefs())
        {
            BuildIfNeeded();
        }

        SetPanelActive(infoPanel != null ? infoPanel.gameObject : null, false);
        SetPanelActive(upgradePanel != null ? upgradePanel.gameObject : null, false);
        upgradePanel?.HidePopups();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        StartBootstrap();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
        StopBootstrap();
        UnsubscribeStatManager();
        UnregisterPanelEvents();
    }

    protected override void RefreshView()
    {
        if (!HasSceneRefs())
        {
            return;
        }

        BuildIfNeeded();
        if (!TryPrepareRuntimeData())
        {
            RefreshLoadingState();
            return;
        }

        RefreshAll();
    }

    private void StartBootstrap()
    {
        if (bootstrapRoutine == null)
        {
            bootstrapRoutine = StartCoroutine(BootstrapRoutine());
        }
    }

    private void StopBootstrap()
    {
        if (bootstrapRoutine == null)
        {
            return;
        }

        StopCoroutine(bootstrapRoutine);
        bootstrapRoutine = null;
    }

    private IEnumerator BootstrapRoutine()
    {
        // 씬 참조와 런타임 데이터가 준비될 때까지 순서대로 기다린다.
        while (!HasSceneRefs())
        {
            yield return null;
        }

        BuildIfNeeded();
        RefreshLoadingState();

        while (!TryPrepareRuntimeData())
        {
            yield return null;
        }

        RefreshAll();
        bootstrapRoutine = null;
    }

    private bool HasSceneRefs()
    {
        // 필수 UI 참조가 모두 연결됐는지 먼저 확인한다.
        bool ready =
            stoneRoot != null &&
            stoneItem != null &&
            infoPanel != null &&
            infoPanel.ContentRoot != null &&
            bonusItem != null &&
            upgradePanel != null &&
            upgradePanel.SlotItems != null &&
            upgradePanel.SlotItems.Length >= 3 &&
            upgradePanel.SlotItems[0] != null &&
            upgradePanel.SlotItems[1] != null &&
            upgradePanel.SlotItems[2] != null;

        if (!ready)
        {
            if (!bindingsWarningLogged)
            {
                Debug.LogWarning("[스톤 UI] StageScene 필수 참조가 비어 있습니다.");
                bindingsWarningLogged = true;
            }

            return false;
        }

        CacheStatIcons();
        return true;
    }

    private bool TryPrepareRuntimeData()
    {
        // 실제 스톤 데이터와 재화 모듈이 준비돼야 화면을 채울 수 있다.
        if (stats == null)
        {
            stats = CharacterStatManager.Instance;
        }

        if (stats == null || !stats.TableLoad)
        {
            return false;
        }

        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || !abilityStoneManager.LoadStone || abilityStoneManager.so == null)
        {
            return false;
        }

        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null || !inventoryManager.DataLoad)
        {
            currencyModule = null;
            return false;
        }

        currencyModule = inventoryManager.GetModule<CurrencyInventoryModule>();
        if (currencyModule == null)
        {
            return false;
        }

        EnsureStatManagerSubscription();
        return true;
    }

    private void CacheStatIcons()
    {
        iconByStat.Clear();

        if (IconManager.StatIconSO == null || IconManager.StatIconSO.StatIconDict == null)
        {
            return;
        }

        foreach (KeyValuePair<StatType, Sprite> pair in IconManager.StatIconSO.StatIconDict)
        {
            if (pair.Value != null)
            {
                iconByStat[pair.Key] = pair.Value;
            }
        }
    }

    private void EnsureStatManagerSubscription()
    {
        if (subscribedStatManager == stats)
        {
            return;
        }

        UnsubscribeStatManager();
        subscribedStatManager = stats;
        subscribedStatManager.StatUpdate += OnStatUpdated;
    }

    private void UnsubscribeStatManager()
    {
        if (subscribedStatManager == null)
        {
            return;
        }

        subscribedStatManager.StatUpdate -= OnStatUpdated;
        subscribedStatManager = null;
    }

    private void BuildIfNeeded()
    {
        // 처음 한 번만 UI 풀을 만들고 이후에는 재사용한다.
        RegisterPanelEvents();

        if (built)
        {
            return;
        }

        EnsureStoneItemPool();
        EnsureBonusItemPool();
        built = true;
    }

    private void RegisterPanelEvents()
    {
        // 패널 바깥 클릭 이벤트는 중복 등록을 막는다.
        if (panelEventsBound)
        {
            return;
        }

        if (infoPanel != null)
        {
            infoPanel.OutsideClicked += CloseBonusInfoPanel;
        }

        if (upgradePanel != null)
        {
            upgradePanel.OutsideClicked += CloseUpgradePanel;
            upgradePanel.PopupOutsideClicked += CloseUpgradePopups;
        }

        panelEventsBound = true;
    }

    private void UnregisterPanelEvents()
    {
        if (!panelEventsBound)
        {
            return;
        }

        if (infoPanel != null)
        {
            infoPanel.OutsideClicked -= CloseBonusInfoPanel;
        }

        if (upgradePanel != null)
        {
            upgradePanel.OutsideClicked -= CloseUpgradePanel;
            upgradePanel.PopupOutsideClicked -= CloseUpgradePopups;
        }

        panelEventsBound = false;
    }

    private void EnsureStoneItemPool()
    {
        // 씬에 배치된 아이템을 우선 쓰고 부족한 수만 복제한다.
        runtimeStoneItems.Clear();
        for (int i = 0; i < stoneViews.Count; i++)
        {
            if (stoneViews[i] != null)
            {
                runtimeStoneItems.Add(stoneViews[i]);
            }
        }

        int serializedItemCount = runtimeStoneItems.Count;
        int targetCount = Enum.GetValues(typeof(StoneGrade)).Length;
        while (runtimeStoneItems.Count < targetCount && stoneItem != null)
        {
            StoneItemUI clone = Instantiate(stoneItem, stoneRoot);
            runtimeStoneItems.Add(clone);
        }

        for (int i = 0; i < runtimeStoneItems.Count; i++)
        {
            StoneItemUI itemUI = runtimeStoneItems[i];
            if (itemUI == null)
            {
                continue;
            }

            bool isVisible = i < targetCount;
            itemUI.gameObject.SetActive(isVisible);
            if (!isVisible || itemUI.Button == null || i < serializedItemCount)
            {
                continue;
            }

            StoneGrade capturedGrade = (StoneGrade)i;
            itemUI.Button.onClick.RemoveAllListeners();
            itemUI.Button.onClick.AddListener(() => OpenUpgradePanel(capturedGrade));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(stoneRoot);
    }

    private void EnsureBonusItemPool()
    {
        // 보너스 아이템도 기존 배치 오브젝트를 기준으로 채운다.
        runtimeBonusItems.Clear();
        for (int i = 0; i < bonusViews.Count; i++)
        {
            if (bonusViews[i] != null)
            {
                runtimeBonusItems.Add(bonusViews[i]);
            }
        }

        int targetCount = GetTargetBonusItemCount();
        while (runtimeBonusItems.Count < targetCount && bonusItem != null)
        {
            StoneBonusItemUI clone = Instantiate(bonusItem, infoPanel.ContentRoot);
            runtimeBonusItems.Add(clone);
        }

        for (int i = 0; i < runtimeBonusItems.Count; i++)
        {
            if (runtimeBonusItems[i] != null)
            {
                runtimeBonusItems[i].gameObject.SetActive(i < targetCount);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(infoPanel.ContentRoot);
    }

    private int GetTargetBonusItemCount()
    {
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager != null && abilityStoneManager.LoadStone && abilityStoneManager.so != null)
        {
            return GetOrderedBonusData().Count;
        }

        return Mathf.Max(runtimeBonusItems.Count, bonusViews.Count);
    }

    // 보너스 버튼에서 바로 호출한다.
    public void OnBonusClick()
    {
        ToggleBonusInfoPanel();
    }

    // 스톤 리스트 버튼에서 바로 호출한다.
    public void OnStoneClick(int gradeIndex)
    {
        if (!Enum.IsDefined(typeof(StoneGrade), gradeIndex))
        {
            return;
        }

        OpenUpgradePanel((StoneGrade)gradeIndex);
    }

    // 강화 슬롯 버튼에서 바로 호출한다.
    public void OnSlotClick(int slotIndex)
    {
        OnClickUpgradeSlot(slotIndex);
    }

    // 다음 등급 버튼에서 바로 호출한다.
    public void OnNextClick()
    {
        OnClickNextGrade();
    }

    // 재설정 버튼에서 바로 호출한다.
    public void OnRerollClick()
    {
        OpenReconfigurePopup();
    }

    // 강화 초기화 버튼에서 바로 호출한다.
    public void OnResetClick()
    {
        OpenResetPopup();
    }

    // 재설정 확인 버튼에서 바로 호출한다.
    public void OnRerollOkClick()
    {
        OnClickRerollSelectedStone();
    }

    // 재설정 취소 버튼에서 바로 호출한다.
    public void OnRerollCancelClick()
    {
        CloseUpgradePopups();
    }

    // 강화 초기화 확인 버튼에서 바로 호출한다.
    public void OnResetOkClick()
    {
        OnClickResetSelectedStone();
    }

    // 강화 초기화 취소 버튼에서 바로 호출한다.
    public void OnResetCancelClick()
    {
        CloseUpgradePopups();
    }

    private void RefreshAll()
    {
        RefreshSharedInfo();
        RefreshStoneList();
        RefreshBonusInfoPanel();
        RefreshUpgradePanel();
    }

    private void RefreshSharedInfo()
    {
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        bool stoneDataReady = abilityStoneManager != null
            && abilityStoneManager.LoadStone
            && abilityStoneManager.so != null;

        InventoryManager inventoryManager = InventoryManager.Instance;
        bool inventoryDataReady = inventoryManager != null && inventoryManager.DataLoad;

        if (inventoryDataReady && currencyModule == null)
        {
            currencyModule = inventoryManager.GetModule<CurrencyInventoryModule>();
        }

        int totalSuccessCount = stoneDataReady ? GetTotalSuccessCount() : 0;

        if (totalText != null)
        {
            totalText.text = stoneDataReady
                ? $"+ {totalSuccessCount}"
                : TextDataLoading;
        }

        if (goldText != null)
        {
            goldText.text = inventoryDataReady && currencyModule != null
                ? currencyModule.GetAmount(CurrencyType.Gold).ToString()
                : TextDataLoading;
        }
    }

    private void RefreshStoneList()
    {
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || abilityStoneManager.so == null)
        {
            return;
        }

        for (int i = 0; i < runtimeStoneItems.Count; i++)
        {
            if (!abilityStoneManager.so.AbilityStoneDict.TryGetValue((StoneGrade)i, out AbilityStone stoneData))
            {
                continue;
            }

            RenderStoneItem(runtimeStoneItems[i], (StoneGrade)i, stoneData);
        }
    }

    private void RefreshBonusInfoPanel()
    {
        if (infoPanel == null)
        {
            return;
        }

        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null)
        {
            return;
        }

        int totalSuccessCount = GetTotalSuccessCount();
        abilityStoneManager.CheckTotalUpCount();

        if (infoPanel.TitleText != null)
        {
            infoPanel.TitleText.text = TextBonusStat;
        }

        if (infoPanel.SummaryText != null)
        {
            infoPanel.SummaryText.text = $"{TextAccumulatedSuccess}으로 얻는 보너스 능력치";
        }

        if (infoPanel.CurrentUpgradeValueText != null)
        {
            infoPanel.CurrentUpgradeValueText.text = $"+{totalSuccessCount}";
        }

        List<StoneTotalUpBonusA> orderedBonusData = GetOrderedBonusData();
        for (int i = 0; i < orderedBonusData.Count && i < runtimeBonusItems.Count; i++)
        {
            RenderBonusItem(runtimeBonusItems[i], orderedBonusData[i], totalSuccessCount);
        }
    }

    private void RefreshUpgradePanel()
    {
        if (upgradePanel == null || selectedGrade == null)
        {
            return;
        }

        AbilityStone stoneData = GetSelectedStone();
        if (stoneData == null)
        {
            return;
        }

        StoneGrade grade = selectedGrade.Value;
        bool unlocked = IsStoneUnlocked(grade);
        bool canAffordUpgrade = CanAfford(stoneData.UpCost);
        int totalAttemptCount = stoneData.GetAttemptCount(0) + stoneData.GetAttemptCount(1) + stoneData.GetAttemptCount(2);

        if (upgradePanel.GradeText != null)
        {
            upgradePanel.GradeText.text = $"{GetGradeName(grade)} {TextStone}";
        }

        string probabilityText = stoneData.IsConfigured
            ? $"{TextSuccessRate} {(stoneData.CurrentProbability * 100f):0.#}%"
            : $"{TextSuccessRate} -";

        TextMeshProUGUI[] probabilityLabels = upgradePanel.ProbabilityTexts;
        for (int i = 0; i < probabilityLabels.Length; i++)
        {
            if (probabilityLabels[i] != null)
            {
                probabilityLabels[i].text = probabilityText;
            }
        }

        if (upgradePanel.RerollButtonText != null)
        {
            upgradePanel.RerollButtonText.text = $"{TextReconfigure}\n{FormatCurrency(stoneData.StatRerollCost)}";
        }

        if (upgradePanel.ResetButtonText != null)
        {
            upgradePanel.ResetButtonText.text = $"{TextReset}\n{FormatCurrency(stoneData.UpResetCostValue)}";
        }

        if (upgradePanel.RerollButton != null)
        {
            upgradePanel.RerollButton.interactable = unlocked;
        }

        if (upgradePanel.ResetButton != null)
        {
            upgradePanel.ResetButton.interactable = unlocked && stoneData.IsConfigured && totalAttemptCount > 0;
        }

        UpdateNextGradeButton(grade, unlocked);

        StoneSlotItemUI[] slotViews = upgradePanel.SlotItems;
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
            {
                RenderUpgradeSlotItem(slotViews[i], stoneData, i, unlocked, canAffordUpgrade);
            }
        }

        RefreshReconfigurePopup(stoneData, unlocked);
        RefreshResetPopup(stoneData, unlocked, totalAttemptCount);
    }
}
