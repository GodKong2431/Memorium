using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DungeonClearPopupController : MonoBehaviour
{
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
    [SerializeField] private string nextLevelLabel = "다음 단계";
    [SerializeField] private string retryLabel = "재도전";

    private DungeonClearPanelBinding activePanelBinding;
    private StageType activeStageType = StageType.None;
    private int activeStageLevel = 1;
    private bool isFailurePopup;
    private bool isButtonBound;
    private readonly List<RewardManager.DungeonRewardEntry> rewardPreviewBuffer = new List<RewardManager.DungeonRewardEntry>();

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = transform as RectTransform;

        GameEventManager.OnDungeonClearPopupRequested += HandlePopupRequested;
        BindButtons();

        HidePopup();
    }

    private void OnDestroy()
    {
        GameEventManager.OnDungeonClearPopupRequested -= HandlePopupRequested;
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
        if (popupRoot == null)
            popupRoot = transform as RectTransform;

        RefreshActivePanel();
        UpdateNextButtonState();
    }

    public bool ShowPopup(StageType stageType, int stageLevel, bool showFailureState = false)
    {
        if (popupRoot == null)
        {
            popupRoot = transform as RectTransform;
            if (popupRoot == null)
                return false;
        }

        BindButtons();

        activePanelBinding = FindPanelBinding(stageType);
        if (activePanelBinding == null)
            return false;

        activeStageType = stageType;
        activeStageLevel = CheckDungeon.ClampLevel(stageType, Mathf.Max(1, stageLevel));
        isFailurePopup = showFailureState;

        ApplyDungeonPanelVisibility(stageType);
        RefreshActivePanel();
        UpdateNextButtonState();

        if (clearTitleText != null)
        {
            clearTitleText.text = isFailurePopup ? failedTitleLabel : clearTitleLabel;
            clearTitleText.gameObject.SetActive(true);
        }

        popupRoot.gameObject.SetActive(true);
        popupRoot.SetAsLastSibling();
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
        StageManager.Instance?.CheckDungeonClear();
    }

    private void HandleNextLevelClicked(StageType stageType)
    {
        if (activeStageType != stageType)
            activeStageType = stageType;

        int targetLevel = ResolveTargetLevel(stageType, activeStageLevel, out _);
        if (!CheckDungeon.CanEnter(stageType, targetLevel, requiredKeyCount))
            return;

        if (!CheckDungeon.TryGetDungeonReq(stageType, targetLevel, out int dungeonId, out _))
            return;

        if (!CheckDungeon.TrySpendTicket(stageType, targetLevel, requiredKeyCount))
            return;

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.currentDungeonID = dungeonId;

        HidePopup();
        StageManager.Instance?.ContinueDungeonAfterClear(stageType, targetLevel);
    }

    private void HidePopup()
    {
        if (popupRoot != null)
            popupRoot.gameObject.SetActive(false);
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
        bool hasActualRewards = false;
        if (!isFailurePopup && RewardManager.Instance != null)
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

}
