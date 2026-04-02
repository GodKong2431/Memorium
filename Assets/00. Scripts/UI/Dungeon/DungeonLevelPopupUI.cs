using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DungeonLevelPopupUI : MonoBehaviour
{
    private const string InsufficientDungeonTicketMessage = "입장권이 부족합니다.";
    private const string DungeonSweepFailedMessage = "던전 소탕에 실패했습니다.";
    private const string DungeonEnterFailedMessage = "던전 입장에 실패했습니다.";

    private readonly List<RewardManager.DungeonRewardEntry> currentRewardEntries = new List<RewardManager.DungeonRewardEntry>();
    private const string DungeonSweepRequiresClearMessage = "\uD55C \uBC88 \uC774\uC0C1 \uD074\uB9AC\uC5B4\uD55C \uB358\uC804\uB9CC \uC18C\uD0D5\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.";

    [Header("UI")]
    [SerializeField] private Image dungeonBackgroundImage;
    [SerializeField] private TextMeshProUGUI dungeonNameText;
    [SerializeField] private TextMeshProUGUI curLevelText;
    [SerializeField] private Button beforeButton;
    [SerializeField] private Button afterButton;
    [SerializeField] private Button sweepButton;
    [SerializeField] private Button enterButton;
    [SerializeField] private ScrollRect rewardScrollRect;
    [SerializeField] private RectTransform rewardContentRoot;
    [SerializeField] private GameObject equipmentRewardItemPrefab;
    [SerializeField] private GameObject maximumUsePanel;
    [SerializeField] private RectTransform popupContentRoot;

    private bool isButtonBound;
    private StageType currentStageType = StageType.None;
    private int currentLevel = 1;
    private int maxUnlockedLevel = 1;
    private int maxLevelCount = 1;
    private int currentRequiredKeyCount = 1;
    private RectTransform popupRect;
    private Image popupBackdropImage;
    private PopupStackService.Handle popupHandle;

    public void Hide()
    {
        EnsureRuntimeReferences();
        currentStageType = StageType.None;
        currentRewardEntries.Clear();
        PopupStackService.Dismiss(ref popupHandle);

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void BindHost(DungeonUIController controller)
    {
        BindButtons();
    }

    public void ReclaimPopupFocus()
    {
        if (!isActiveAndEnabled || currentStageType == StageType.None)
            return;

        PresentPopup();
    }

    private void Awake()
    {
        EnsureRuntimeReferences();
    }

    private void EnsureRuntimeReferences()
    {
        popupRect = transform as RectTransform;
        if (popupBackdropImage == null)
            popupBackdropImage = GetComponent<Image>();

        // Keep the visual dim on this root, but let the shared popup backdrop
        // receive the outside-click close event.
        if (popupBackdropImage != null && popupBackdropImage.raycastTarget)
            popupBackdropImage.raycastTarget = false;
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAmountChanged += OnItemAmountChanged;

        RefreshState();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAmountChanged -= OnItemAmountChanged;

        PopupStackService.Dismiss(ref popupHandle);
    }

    public void Show(
        StageType stageType,
        string dungeonName,
        int requiredKeyCount)
    {
        if (StageManager.Instance != null && StageManager.Instance.IsDungeonInProgress)
        {
            InstanceMessageManager.TryShowDungeonInProgress();
            Hide();
            return;
        }

        BindButtons();
        EnsureRuntimeReferences();

        currentStageType = stageType;
        currentRequiredKeyCount = Mathf.Max(1, requiredKeyCount);
        maxLevelCount = Mathf.Max(1, CheckDungeon.GetMaxLevelCount(stageType));
        maxUnlockedLevel = CheckDungeon.GetMaxUnlockedLevel(stageType);
        currentLevel = CheckDungeon.ClampLevel(stageType, maxUnlockedLevel);

        if (dungeonNameText != null)
            dungeonNameText.text = dungeonName;

        ApplyDungeonPreview(stageType);

        gameObject.SetActive(true);
        PresentPopup();
        RefreshState();
    }

    private void BindButtons()
    {
        if (isButtonBound)
            return;

        beforeButton.onClick.AddListener(OnClickBefore);
        afterButton.onClick.AddListener(OnClickAfter);
        sweepButton.onClick.AddListener(OnClickSweep);
        enterButton.onClick.AddListener(OnClickEnter);

        isButtonBound = true;
    }

    private void RefreshState()
    {
        if (currentStageType == StageType.None)
            return;

        if (curLevelText != null)
            curLevelText.text = currentLevel.ToString();

        RefreshRewards();

        bool canEnterCurrentLevel = CheckDungeon.CanEnter(currentStageType, currentLevel, currentRequiredKeyCount);
        bool canSweepCurrentLevel = CheckDungeon.CanSweep(currentStageType, currentLevel, currentRequiredKeyCount);

        if (beforeButton != null)
            beforeButton.interactable = currentLevel > 1;

        if (afterButton != null)
            afterButton.interactable = currentLevel < maxUnlockedLevel;

        if (sweepButton != null)
            sweepButton.interactable = canSweepCurrentLevel;

        if (enterButton != null)
            enterButton.interactable = canEnterCurrentLevel;
    }

    private void OnClickBefore()
    {
        if (currentLevel <= 1)
            return;

        currentLevel--;
        RefreshState();
    }

    private void OnClickAfter()
    {
        if (currentLevel >= maxUnlockedLevel)
            return;

        currentLevel++;
        RefreshState();
    }

    private void OnClickSweep()
    {
        if (currentStageType == StageType.None)
            return;

        if (StageManager.Instance != null && StageManager.Instance.IsDungeonInProgress)
        {
            InstanceMessageManager.TryShowDungeonInProgress();
            RefreshState();
            return;
        }

        int ticketCost = currentRequiredKeyCount;
        if (!CheckDungeon.HasClearedDungeon(currentStageType, currentLevel))
        {
            RefreshState();
            InstanceMessageManager.TryShow(DungeonSweepRequiresClearMessage);
            return;
        }

        if (!CheckDungeon.CanSweep(currentStageType, currentLevel, ticketCost))
        {
            RefreshState();
            InstanceMessageManager.TryShow(InsufficientDungeonTicketMessage);
            return;
        }

        if (StageManager.Instance == null ||
            !StageManager.Instance.TrySweepDungeon(currentStageType, currentLevel, ticketCost))
        {
            RefreshState();
            InstanceMessageManager.TryShow(DungeonSweepFailedMessage);
            return;
        }

        maxUnlockedLevel = CheckDungeon.GetMaxUnlockedLevel(currentStageType);
        RefreshState();

        if (!DungeonClearPopupController.TryShowAny(currentStageType, currentLevel))
            InstanceMessageManager.TryShow("소탕 완료");
    }

    private void OnClickEnter()
    {
        if (currentStageType == StageType.None)
            return;

        if (StageManager.Instance != null && StageManager.Instance.IsDungeonInProgress)
        {
            InstanceMessageManager.TryShowDungeonInProgress();
            RefreshState();
            return;
        }

        int ticketCost = currentRequiredKeyCount;

        if (IsMaximumUseEnabled())
            Debug.LogWarning("[DungeonLevelPopupUI] Maximum-use entry is not wired yet.");

        if (!CheckDungeon.CanEnter(currentStageType, currentLevel, ticketCost))
        {
            RefreshState();
            InstanceMessageManager.TryShow(InsufficientDungeonTicketMessage);
            return;
        }

        if (!CheckDungeon.TrySpendTicket(currentStageType, currentLevel, ticketCost))
        {
            RefreshState();
            InstanceMessageManager.TryShow(InsufficientDungeonTicketMessage);
            return;
        }

        if (StageManager.Instance == null ||
            !StageManager.Instance.TryEnterDungeon(currentStageType, currentLevel))
        {
            RefreshState();
            InstanceMessageManager.TryShow(DungeonEnterFailedMessage);
        }
    }

    private void OnItemAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemType != ItemType.Key)
            return;

        RefreshState();
    }

    private void ApplyDungeonPreview(StageType stageType)
    {
        if (dungeonBackgroundImage == null)
            return;

        // TODO: Apply per-dungeon preview sprite when the assets are ready.
    }

    private void RefreshRewards()
    {
        currentRewardEntries.Clear();
        RewardManager.Instance?.TryGetDungeonRewardPreview(currentStageType, currentLevel, currentRewardEntries);
        RebuildRewards(currentRewardEntries);
    }

    private void RebuildRewards(IReadOnlyList<RewardManager.DungeonRewardEntry> rewards)
    {
        DungeonContentUI.RebuildRewardItems(
            rewardContentRoot,
            rewards,
            DungeonUIController.LoadRewardIcon,
            true,
            equipmentRewardItemPrefab);
    }

    private bool IsMaximumUseEnabled()
    {
        return maximumUsePanel != null && maximumUsePanel.activeSelf;
    }

    private RectTransform ResolveOverlayRoot()
    {
        EnsureRuntimeReferences();
        Canvas canvas = popupRect != null ? popupRect.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.rootCanvas != null)
            return canvas.rootCanvas.transform as RectTransform;

        return popupRect != null ? popupRect.parent as RectTransform : null;
    }

    private void PresentPopup()
    {
        EnsureRuntimeReferences();
        if (popupRect == null)
            return;

        PopupStackService.Present(ref popupHandle, new PopupStackService.Request
        {
            PopupRoot = popupRect,
            ContentRoot = ResolvePopupHitRoot(),
            OverlayParent = ResolveOverlayRoot(),
            OnRequestClose = Hide,
            CloseOnOutside = true,
            BackdropColor = popupBackdropImage != null ? Color.clear : new Color(0f, 0f, 0f, 0.78431374f),
            ReparentToOverlayParent = true,
            StretchPopupToOverlayParent = true
        });
    }

    private RectTransform ResolvePopupHitRoot()
    {
        if (popupContentRoot != null)
            return popupContentRoot;

        if (dungeonBackgroundImage != null)
        {
            RectTransform backgroundRect = dungeonBackgroundImage.rectTransform;
            RectTransform parentRect = backgroundRect != null ? backgroundRect.parent as RectTransform : null;
            if (parentRect != null && parentRect != popupRect)
                return parentRect;

            return backgroundRect;
        }

        if (rewardScrollRect != null)
        {
            RectTransform scrollRect = rewardScrollRect.transform as RectTransform;
            if (scrollRect != null)
            {
                RectTransform parentRect = scrollRect.parent as RectTransform;
                if (parentRect != null && parentRect != popupRect)
                    return parentRect;

                return scrollRect;
            }
        }

        return popupRect;
    }

}
