using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

    private readonly StageKeyCatalog stageKeyCatalog = new StageKeyCatalog();
    private readonly List<int> currentRewardItemIds = new List<int>();

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
    private OverlayPopupPanelUI overlayPanel;
    private OverlayPopupPanelUI boundOverlayPanel;
    private int ignoreCloseUntilFrame = -1;

    public void Hide()
    {
        EnsureRuntimeReferences();
        currentStageType = StageType.None;
        SetOverlayVisible(false);
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
        BindOverlayPanel();
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
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        BindOverlayPanel();
        RefreshState();
    }

    private void OnDisable()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
        SetOverlayVisible(false);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || currentStageType == StageType.None)
            return;

        if (Time.frameCount <= ignoreCloseUntilFrame)
            return;

        if (WasOutsidePointerPressedThisFrame())
            Hide();
    }

    public void Show(
        StageType stageType,
        string dungeonName,
        IReadOnlyList<int> rewardItemIds,
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
        maxLevelCount = ResolveMaxLevelCount(stageType);
        maxUnlockedLevel = ResolveMaxUnlockedLevel(stageType, maxLevelCount);
        currentLevel = Mathf.Clamp(maxUnlockedLevel, 1, Mathf.Max(1, maxLevelCount));

        currentRewardItemIds.Clear();
        if (rewardItemIds != null)
            currentRewardItemIds.AddRange(rewardItemIds);

        if (dungeonNameText != null)
            dungeonNameText.text = dungeonName;

        ApplyDungeonPreview(stageType);
        RebuildRewards(currentRewardItemIds);

        PresentAsOverlay();
        gameObject.SetActive(true);
        BindOverlayPanel();
        SetOverlayVisible(true);
        ignoreCloseUntilFrame = Time.frameCount + 1;
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

        bool isLevelUnlocked = CheckDungeon.HasDungeonAccess(currentStageType, currentLevel);
        bool hasEnoughKeys = HasEnoughKeys();

        if (beforeButton != null)
            beforeButton.interactable = currentLevel > 1;

        if (afterButton != null)
            afterButton.interactable = currentLevel < maxUnlockedLevel;

        if (sweepButton != null)
            sweepButton.interactable = isLevelUnlocked;

        if (enterButton != null)
            enterButton.interactable = isLevelUnlocked && hasEnoughKeys;
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

        if (!CheckDungeon.HasDungeonAccess(currentStageType, currentLevel))
        {
            RefreshState();
            return;
        }

        CurrencyInventoryModule currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;

        if (currencyModule == null)
            return;

        int ticketCost = currentRequiredKeyCount;

        if (IsMaximumUseEnabled())
        {
            // TODO: 열쇠 전부 사용 연결
            Debug.LogWarning("[DungeonLevelPopupUI] 열쇠 전부 사용");
        }

        if (!currencyModule.TrySpend(CurrencyType.DungeonTicket, new BigDouble(ticketCost)))
        {
            RefreshState();
            return;
        }

        int dungeonId = ResolveDungeonId(currentStageType, currentLevel);
        if (dungeonId > 0)
            DungeonManager.Instance.currentDungeonID = dungeonId;

        StageManager.Instance.SetStageType(currentStageType, currentLevel);

        Hide();
        SceneController.Instance.LoadScene(SceneType.DungeonScene);
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != CurrencyType.DungeonTicket)
            return;

        RefreshState();
    }

    private void ApplyDungeonPreview(StageType stageType)
    {
        if (dungeonBackgroundImage == null)
            return;

        // TODO: 배경 스프라이트 변경
    }

    private void RebuildRewards(IReadOnlyList<int> rewardItemIds)
    {
        if (rewardContentRoot == null || rewardContentRoot.childCount == 0)
            return;

        Transform template = rewardContentRoot.GetChild(0);
        int rewardCount = rewardItemIds != null ? rewardItemIds.Count : 0;

        for (int i = 0; i < rewardCount; i++)
        {
            Transform rewardItem = i < rewardContentRoot.childCount
                ? rewardContentRoot.GetChild(i)
                : Instantiate(template, rewardContentRoot, false);

            rewardItem.name = $"(Btn)ItemFrame_{rewardItemIds[i]}";
            rewardItem.gameObject.SetActive(true);

            Sprite icon = DungeonUIController.LoadRewardIcon(rewardItemIds[i]);
            ApplyRewardIcon(rewardItem, icon);
        }

        for (int i = rewardCount; i < rewardContentRoot.childCount; i++)
            rewardContentRoot.GetChild(i).gameObject.SetActive(false);
    }

    private static void ApplyRewardIcon(Transform rewardItem, Sprite icon)
    {
        Image[] images = rewardItem.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].transform == rewardItem)
                continue;

            images[i].sprite = icon;
            return;
        }
    }

    private int ResolveMaxLevelCount(StageType stageType)
    {
        if (!stageKeyCatalog.TryGetStageKeys(stageType, out List<int> stageKeys))
            return 1;

        return Mathf.Max(1, stageKeys.Count);
    }

    private static int ResolveMaxUnlockedLevel(StageType stageType, int maxLevel)
    {
        int highestUnlockedLevel = 0;
        for (int level = 1; level <= maxLevel; level++)
        {
            if (!CheckDungeon.HasDungeonAccess(stageType, level))
                break;

            highestUnlockedLevel = level;
        }

        return Mathf.Clamp(highestUnlockedLevel == 0 ? 1 : highestUnlockedLevel, 1, Mathf.Max(1, maxLevel));
    }

    private static int ResolveDungeonId(StageType stageType, int level)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.DungeonReqDict == null)
            return 0;

        List<int> dungeonIds = new List<int>();
        foreach (KeyValuePair<int, DungeonReqTable> pair in DataManager.Instance.DungeonReqDict)
        {
            if (pair.Value.stageType == stageType)
                dungeonIds.Add(pair.Key);
        }

        if (dungeonIds.Count == 0)
            return 0;

        dungeonIds.Sort();
        int index = Mathf.Clamp(level - 1, 0, dungeonIds.Count - 1);
        return dungeonIds[index];
    }

    private bool HasEnoughKeys()
    {
        CurrencyInventoryModule currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;

        if (currencyModule == null)
            return false;

        return currencyModule.GetAmount(CurrencyType.DungeonTicket) >= new BigDouble(currentRequiredKeyCount);
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
        BindOverlayPanel();
        RefreshOverlayOrder();
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

    private void BindOverlayPanel()
    {
        OverlayPopupPanelUI current = EnsureOverlayPanel();
        if (current == boundOverlayPanel)
            return;

        if (boundOverlayPanel != null)
            boundOverlayPanel.OutsideClicked -= HandleOutsideClick;

        boundOverlayPanel = current;

        if (boundOverlayPanel != null)
            boundOverlayPanel.OutsideClicked += HandleOutsideClick;
    }

    private void HandleOutsideClick()
    {
        if (!gameObject.activeInHierarchy || currentStageType == StageType.None)
            return;

        if (Time.frameCount <= ignoreCloseUntilFrame)
            return;

        Hide();
    }

    private bool WasOutsidePointerPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return !IsInsidePopup(Touchscreen.current.primaryTouch.position.ReadValue());

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return !IsInsidePopup(Mouse.current.position.ReadValue());

        return false;
    }

    private bool IsInsidePopup(Vector2 screenPosition)
    {
        RectTransform hitRoot = ResolvePopupHitRoot();
        if (hitRoot == null)
            return false;

        Canvas parentCanvas = hitRoot.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = parentCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(hitRoot, screenPosition, eventCamera);
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

    private void SetOverlayVisible(bool visible)
    {
        OverlayPopupPanelUI currentOverlay = EnsureOverlayPanel();
        if (currentOverlay == null)
            return;

        if (currentOverlay.gameObject.activeSelf != visible)
            currentOverlay.gameObject.SetActive(visible);

        if (visible)
            currentOverlay.SuppressClickForCurrentFrame();
    }

    private void RefreshOverlayOrder()
    {
        if (popupRect == null)
            return;

        OverlayPopupPanelUI currentOverlay = EnsureOverlayPanel();
        if (currentOverlay == null)
            return;

        currentOverlay.BringToFront();
        popupRect.SetAsLastSibling();
    }

    private OverlayPopupPanelUI EnsureOverlayPanel()
    {
        RectTransform overlayRoot = ResolveOverlayRoot();
        RectTransform sheetRoot = ResolvePopupHitRoot();
        if (overlayRoot == null || sheetRoot == null)
            return null;

        if (overlayPanel != null)
        {
            if (overlayPanel.transform.parent != overlayRoot)
                overlayPanel.transform.SetParent(overlayRoot, false);

            overlayPanel.SetSheetRoot(sheetRoot);
            return overlayPanel;
        }

        GameObject overlayObject = new GameObject(
            "DungeonLevelPopupOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(OverlayPopupPanelUI));
        overlayObject.layer = overlayRoot.gameObject.layer;
        overlayObject.transform.SetParent(overlayRoot, false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image image = overlayObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        overlayPanel = overlayObject.GetComponent<OverlayPopupPanelUI>();
        overlayPanel.SetSheetRoot(sheetRoot);
        overlayObject.SetActive(gameObject.activeInHierarchy && currentStageType != StageType.None);

        return overlayPanel;
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
