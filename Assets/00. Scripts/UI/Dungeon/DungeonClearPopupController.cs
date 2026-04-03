using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DungeonClearPopupController : MonoBehaviour
{
    private const string InsufficientDungeonTicketMessage = "\uC785\uC7A5\uAD8C\uC774 \uBD80\uC871\uD569\uB2C8\uB2E4.";

#pragma warning disable CS0649
    [Serializable]
    private sealed class DungeonClearPanelBinding
    {
        public StageType stageType = StageType.None;
        public RectTransform panelRoot;
        public TMP_Text dungeonNameText;
        public RectTransform rewardContentRoot;
        public Button exitButton;
        public Button nextLevelButton;
        public TMP_Text nextLevelButtonText;

        public bool Matches(StageType targetStageType)
        {
            return stageType == targetStageType && panelRoot != null;
        }
    }
#pragma warning restore CS0649

    [Header("Popup")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text clearTitleText;
    [SerializeField] private string clearTitleLabel = "클리어!";
    [SerializeField] private string failedTitleLabel = "던전 실패";

    [Header("Dungeon Panels")]
    [SerializeField] private DungeonClearPanelBinding[] panelBindings;

    [Header("Entry")]
    [SerializeField] private int requiredKeyCount = 1;
    [SerializeField] private GameObject equipmentRewardItemPrefab;
    [SerializeField] private ActiveSkillUIController skillUi;
    [SerializeField] private string nextLevelLabel = "다음 단계";
    [SerializeField] private string retryLabel = "재도전";

    private DungeonClearPanelBinding activePanelBinding;
    private StageType activeStageType = StageType.None;
    private int activeStageLevel = 1;
    private bool isFailurePopup;
    private bool isButtonBound;
    private PopupStackService.Handle popupHandle;
    private readonly List<RewardManager.DungeonRewardEntry> rewardPreviewBuffer = new List<RewardManager.DungeonRewardEntry>();

    private void Awake()
    {
        EnsureRuntimeReferences();

        GameEventManager.OnDungeonClearPopupRequested += HandlePopupRequested;
        BindButtons();

        HidePopup();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAmountChanged += OnItemAmountChanged;
    }

    private void OnDestroy()
    {
        GameEventManager.OnDungeonClearPopupRequested -= HandlePopupRequested;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAmountChanged -= OnItemAmountChanged;

        PopupStackService.Release(ref popupHandle);
    }

    public void ResetForSceneChange()
    {
        activePanelBinding = null;
        activeStageType = StageType.None;
        activeStageLevel = 1;
        isFailurePopup = false;
        HidePopup();
    }

    public void RefreshView()
    {
        EnsureRuntimeReferences();

        RefreshActivePanel();
        UpdateNextButtonState();
    }

    public bool ShowPopup(StageType stageType, int stageLevel, bool showFailureState = false)
    {
        EnsureRuntimeReferences();
        if (popupRoot == null)
            return false;

        BindButtons();

        activePanelBinding = FindPanelBinding(stageType);
        if (activePanelBinding == null)
            return false;

        activeStageType = stageType;
        activeStageLevel = CheckDungeon.ClampLevel(stageType, Mathf.Max(1, stageLevel));
        isFailurePopup = showFailureState;
        skillUi?.CloseEquip();

        popupRoot.gameObject.SetActive(true);
        ApplyDungeonPanelVisibility(stageType);
        RefreshActivePanel();
        UpdateNextButtonState();
        PresentPopup();

        if (clearTitleText != null)
        {
            clearTitleText.text = isFailurePopup ? failedTitleLabel : clearTitleLabel;
            clearTitleText.gameObject.SetActive(true);
        }

        return true;
    }

    public static bool TryShowAny(StageType stageType, int stageLevel, bool showFailureState = false)
    {
        DungeonClearPopupController[] controllers = UnityEngine.Object.FindObjectsByType<DungeonClearPopupController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            DungeonClearPopupController controller = controllers[i];
            if (controller != null && controller.ShowPopup(stageType, stageLevel, showFailureState))
                return true;
        }

        return false;
    }

    private void HandlePopupRequested(StageType stageType, int stageLevel)
    {
        if (!ShowPopup(stageType, stageLevel))
            StageManager.Instance?.CheckDungeonClear();
    }

    private void HandleExitClicked()
    {
        HidePopup();
        RestoreDungeonLevelPopupFocus();

        if (StageManager.Instance != null && StageManager.Instance.IsDungeonInProgress)
            StageManager.Instance.CheckDungeonClear();
    }

    private void HandleNextLevelClicked(StageType stageType)
    {
        if (activeStageType != stageType)
            activeStageType = stageType;

        StageManager stageManager = StageManager.Instance;
        if (stageManager == null)
            return;

        int targetLevel = ResolveTargetLevel(stageType, activeStageLevel, out _);
        if (!CheckDungeon.CanEnter(stageType, targetLevel, requiredKeyCount))
        {
            UpdateNextButtonState();
            InstanceMessageManager.TryShow(InsufficientDungeonTicketMessage);
            return;
        }

        if (!CheckDungeon.TryGetDungeonReq(stageType, targetLevel, out int dungeonId, out _))
            return;

        if (!CheckDungeon.TrySpendTicket(stageType, targetLevel, requiredKeyCount))
        {
            UpdateNextButtonState();
            InstanceMessageManager.TryShow(InsufficientDungeonTicketMessage);
            return;
        }

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.currentDungeonID = dungeonId;

        if (stageManager.IsDungeonInProgress)
        {
            HidePopup();
            stageManager.ContinueDungeonAfterClear(stageType, targetLevel);
            return;
        }

        if (!stageManager.TryEnterDungeon(stageType, targetLevel))
        {
            UpdateNextButtonState();
            return;
        }

        HidePopup();
    }

    private void HidePopup()
    {
        PopupStackService.Dismiss(ref popupHandle);

        if (popupRoot != null)
            popupRoot.gameObject.SetActive(false);
    }

    private void EnsureRuntimeReferences()
    {
        if (popupRoot == null)
            popupRoot = transform as RectTransform;
    }

    private void PresentPopup()
    {
        EnsureRuntimeReferences();
        if (popupRoot == null)
            return;

        PopupStackService.Present(ref popupHandle, new PopupStackService.Request
        {
            PopupRoot = popupRoot,
            ContentRoot = activePanelBinding != null && activePanelBinding.panelRoot != null
                ? activePanelBinding.panelRoot
                : popupRoot,
            OverlayParent = ResolveOverlayRoot(),
            CloseOnOutside = false,
            BackdropColor = new Color(0f, 0f, 0f, 0.78431374f),
            ReparentToOverlayParent = true,
            StretchPopupToOverlayParent = false
        });
    }

    private RectTransform ResolveOverlayRoot()
    {
        EnsureRuntimeReferences();
        Canvas canvas = popupRoot != null ? popupRoot.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.rootCanvas != null)
            return canvas.rootCanvas.transform as RectTransform;

        return popupRoot != null ? popupRoot.parent as RectTransform : null;
    }

    private void ApplyDungeonPanelVisibility(StageType stageType)
    {
        if (panelBindings == null)
            return;

        for (int i = 0; i < panelBindings.Length; i++)
        {
            DungeonClearPanelBinding binding = panelBindings[i];
            if (binding?.panelRoot == null)
                continue;

            binding.panelRoot.gameObject.SetActive(binding.stageType == stageType);
        }
    }

    private void BindButtons()
    {
        if (isButtonBound || panelBindings == null)
            return;

        for (int i = 0; i < panelBindings.Length; i++)
        {
            DungeonClearPanelBinding binding = panelBindings[i];
            if (binding == null)
                continue;

            if (binding.exitButton != null)
                binding.exitButton.onClick.AddListener(HandleExitClicked);

            if (binding.nextLevelButton != null)
            {
                StageType boundStageType = binding.stageType;
                binding.nextLevelButton.onClick.AddListener(() => HandleNextLevelClicked(boundStageType));
            }
        }

        isButtonBound = true;
    }

    private void UpdateNextButtonState()
    {
        if (activePanelBinding == null)
            return;

        int targetLevel = ResolveTargetLevel(activeStageType, activeStageLevel, out bool isRetry);

        if (activePanelBinding.nextLevelButtonText != null)
            activePanelBinding.nextLevelButtonText.text = isRetry ? retryLabel : nextLevelLabel;

        if (activePanelBinding.nextLevelButton != null)
            activePanelBinding.nextLevelButton.interactable = CheckDungeon.CanEnter(activeStageType, targetLevel, requiredKeyCount);
    }

    private void RefreshActivePanel()
    {
        if (activePanelBinding == null)
            return;

        if (activePanelBinding.dungeonNameText != null)
            activePanelBinding.dungeonNameText.text = $"{DungeonUIController.GetDungeonName(activeStageType)} {activeStageLevel}단계";

        if (activePanelBinding.rewardContentRoot != null && !activePanelBinding.rewardContentRoot.gameObject.activeSelf)
            activePanelBinding.rewardContentRoot.gameObject.SetActive(true);

        rewardPreviewBuffer.Clear();
        if (isFailurePopup)
        {
            DungeonContentUI.RebuildRewardItems(
                activePanelBinding.rewardContentRoot,
                rewardPreviewBuffer,
                DungeonUIController.LoadRewardIcon,
                true,
                equipmentRewardItemPrefab);
            return;
        }

        bool hasActualRewards = false;
        if (RewardManager.Instance != null)
            hasActualRewards = RewardManager.Instance.TryGetLastDungeonClearRewards(activeStageType, activeStageLevel, rewardPreviewBuffer);

        if (!hasActualRewards && RewardManager.Instance != null)
            RewardManager.Instance.TryGetDungeonRewardPreview(activeStageType, activeStageLevel, rewardPreviewBuffer);

        DungeonContentUI.RebuildRewardItems(
            activePanelBinding.rewardContentRoot,
            rewardPreviewBuffer,
            DungeonUIController.LoadRewardIcon,
            true,
            equipmentRewardItemPrefab);
    }

    private DungeonClearPanelBinding FindPanelBinding(StageType stageType)
    {
        if (panelBindings == null)
            return null;

        for (int i = 0; i < panelBindings.Length; i++)
        {
            DungeonClearPanelBinding binding = panelBindings[i];
            if (binding != null && binding.Matches(stageType))
                return binding;
        }

        return null;
    }

    private int ResolveTargetLevel(StageType stageType, int currentLevel, out bool isRetry)
    {
        if (isFailurePopup)
        {
            isRetry = true;
            return Mathf.Max(1, currentLevel);
        }

        int maxLevelCount = ResolveMaxLevelCount(stageType);
        int clampedCurrentLevel = Mathf.Clamp(currentLevel, 1, Mathf.Max(1, maxLevelCount));

        if (clampedCurrentLevel < maxLevelCount)
        {
            isRetry = false;
            return clampedCurrentLevel + 1;
        }

        isRetry = true;
        return clampedCurrentLevel;
    }

    private int ResolveMaxLevelCount(StageType stageType)
    {
        return Mathf.Max(1, CheckDungeon.GetMaxLevelCount(stageType));
    }

    private void OnItemAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemType != ItemType.Key)
            return;

        UpdateNextButtonState();
    }

    private static void RestoreDungeonLevelPopupFocus()
    {
        DungeonLevelPopupUI[] popups = UnityEngine.Object.FindObjectsByType<DungeonLevelPopupUI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < popups.Length; i++)
        {
            DungeonLevelPopupUI popup = popups[i];
            if (popup == null || !popup.isActiveAndEnabled)
                continue;

            popup.ReclaimPopupFocus();
            return;
        }
    }

}
