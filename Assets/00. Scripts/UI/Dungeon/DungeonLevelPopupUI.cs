using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DungeonLevelPopupUI : MonoBehaviour
{
    private struct RectTransformState
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector3 localScale;
        public Quaternion localRotation;

        public static RectTransformState Capture(RectTransform target)
        {
            return new RectTransformState
            {
                anchorMin = target.anchorMin,
                anchorMax = target.anchorMax,
                anchoredPosition = target.anchoredPosition,
                sizeDelta = target.sizeDelta,
                pivot = target.pivot,
                offsetMin = target.offsetMin,
                offsetMax = target.offsetMax,
                localScale = target.localScale,
                localRotation = target.localRotation
            };
        }

        public void Apply(RectTransform target)
        {
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.pivot = pivot;
            target.anchoredPosition = anchoredPosition;
            target.sizeDelta = sizeDelta;
            target.offsetMin = offsetMin;
            target.offsetMax = offsetMax;
            target.localScale = localScale;
            target.localRotation = localRotation;
        }
    }

    private readonly List<RewardManager.DungeonRewardEntry> currentRewardEntries = new List<RewardManager.DungeonRewardEntry>();

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
    private bool isPresentedAsOverlay;
    private StageType currentStageType = StageType.None;
    private int currentLevel = 1;
    private int maxUnlockedLevel = 1;
    private int maxLevelCount = 1;
    private int currentRequiredKeyCount = 1;
    private RectTransform popupRect;
    private RectTransform originalParent;
    private RectTransform rootCanvasRect;
    private RectTransformState originalRectState;
    private int originalSiblingIndex;
    private PopupStackService.Handle popupHandle;

    public void Hide()
    {
        EnsureRuntimeReferences();
        currentStageType = StageType.None;
        currentRewardEntries.Clear();
        PopupStackService.Dismiss(ref popupHandle);
        RestoreOriginalPresentation();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void BindHost(DungeonUIController controller)
    {
        BindButtons();
    }

    private void Awake()
    {
        EnsureRuntimeReferences();
    }

    private void EnsureRuntimeReferences()
    {
        popupRect = transform as RectTransform;
        if (popupRect != null && originalParent == null)
        {
            originalParent = popupRect.parent as RectTransform;
            originalSiblingIndex = popupRect.GetSiblingIndex();
            originalRectState = RectTransformState.Capture(popupRect);
        }

        if (rootCanvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
                rootCanvasRect = canvas.rootCanvas.transform as RectTransform;
        }
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

        PresentAsOverlay();
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

        bool isLevelUnlocked = CheckDungeon.HasDungeonAccess(currentStageType, currentLevel);

        if (beforeButton != null)
            beforeButton.interactable = currentLevel > 1;

        if (afterButton != null)
            afterButton.interactable = currentLevel < maxUnlockedLevel;

        if (sweepButton != null)
            sweepButton.interactable = isLevelUnlocked;

        if (enterButton != null)
            enterButton.interactable = CheckDungeon.CanEnter(currentStageType, currentLevel, currentRequiredKeyCount);
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
        // TODO: 스윕버튼 연결
        Debug.LogWarning("[DungeonLevelPopupUI] 스윕 눌렀음");
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
        {
            // TODO: 열쇠 전부 사용 연결
            Debug.LogWarning("[DungeonLevelPopupUI] 열쇠 전부 사용");
        }

        if (!CheckDungeon.CanEnter(currentStageType, currentLevel, ticketCost))
        {
            RefreshState();
            return;
        }

        if (!CheckDungeon.TryGetDungeonReq(currentStageType, currentLevel, out int dungeonId, out _))
        {
            RefreshState();
            return;
        }

        if (!CheckDungeon.TrySpendTicket(currentStageType, currentLevel, ticketCost))
        {
            RefreshState();
            return;
        }

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.currentDungeonID = dungeonId;

        StageManager.Instance.SetStageType(currentStageType, currentLevel);

        Hide();
        SceneController.Instance.LoadScene(SceneType.DungeonScene);
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

        // TODO: 배경 스프라이트 변경
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

    private void PresentAsOverlay()
    {
        EnsureRuntimeReferences();
        if (popupRect == null)
            return;

        if (!isPresentedAsOverlay)
        {
            originalParent = popupRect.parent as RectTransform;
            originalSiblingIndex = popupRect.GetSiblingIndex();
            originalRectState = RectTransformState.Capture(popupRect);
            isPresentedAsOverlay = true;
        }

        RectTransform overlayRoot = ResolveOverlayRoot();
        if (overlayRoot == null)
            return;

        if (popupRect.parent != overlayRoot)
            popupRect.SetParent(overlayRoot, false);

        StretchToParent(popupRect);
        popupRect.SetAsLastSibling();
    }

    private void RestoreOriginalPresentation()
    {
        EnsureRuntimeReferences();
        if (!isPresentedAsOverlay || popupRect == null)
            return;

        if (originalParent != null && popupRect.parent != originalParent)
            popupRect.SetParent(originalParent, false);

        originalRectState.Apply(popupRect);

        if (originalParent != null)
        {
            int maxSiblingIndex = Mathf.Max(0, originalParent.childCount - 1);
            popupRect.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, maxSiblingIndex));
        }

        isPresentedAsOverlay = false;
    }

    private RectTransform ResolveOverlayRoot()
    {
        EnsureRuntimeReferences();
        if (rootCanvasRect != null)
            return rootCanvasRect;

        return rootCanvasRect != null ? rootCanvasRect : originalParent;
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
            OverlayParent = popupRect.parent as RectTransform,
            OnRequestClose = Hide,
            CloseOnOutside = true
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

    private static void StretchToParent(RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
    }
}
