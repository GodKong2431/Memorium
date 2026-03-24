using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUIController : UIControllerBase
{
    [Header("퀘스트 패널")]
    [SerializeField] private RectTransform rectQuestPanel;
    [SerializeField] private Button btnToggleQuestPanel;
    [SerializeField] private RectTransform rectToggleArrow;
    [SerializeField] private float foldDistanceX = 490f;
    [SerializeField] private float panelMoveDuration = 0.15f;
    [SerializeField] private float openedArrowScaleX = 1f;
    [SerializeField] private float foldedArrowScaleX = -1f;
    [SerializeField] private float minVisibleHandleWidth = 50f;

    [Header("공통 퀘스트 정보")]
    [SerializeField] private TextMeshProUGUI textQuestName;
    [SerializeField] private TextMeshProUGUI textQuestNumber;
    [SerializeField] private GameObject objQuestProgressSliderRoot;
    [SerializeField] private Slider sliderQuestProgress;

    [Header("공통 보상 UI")]
    [SerializeField] private Image imageQuestReward;
    [SerializeField] private TextMeshProUGUI textQuestRewardCount;
    [SerializeField] private Sprite spriteRewardInProgress;
    [SerializeField] private Sprite spriteRewardReady;
    [SerializeField] private Button btnRewardTouch;
    [SerializeField] private TextMeshProUGUI textRewardTouch;

    [Header("문구/초기 상태")]
    [SerializeField] private bool startOpened;
    [SerializeField] private string allClearTitle = "All Clear";
    [SerializeField] private string rewardTouchText = "Reward to touch";

    private Coroutine initializeRoutine;
    private Coroutine panelMoveRoutine;
    private bool isOpened;
    private bool isQuestCompleted;
    private Vector2 openedAnchoredPosition;
    private bool hasCachedOpenPosition;
    private Vector2 openedToggleAnchoredPosition;
    private bool hasCachedTogglePosition;
    private Vector3 cachedArrowScale;
    private bool hasCachedArrowScale;

    protected override void Initialize()
    {
        CacheOpenedPosition();
        CacheTogglePosition();
        CacheArrowScale();
    }

    protected override void OnEnable()
    {
        CacheOpenedPosition();
        CacheTogglePosition();
        CacheArrowScale();

        isOpened = startOpened;
        ApplyFoldState(true);

        if (btnToggleQuestPanel != null)
            btnToggleQuestPanel.onClick.AddListener(TogglePanel);

        base.OnEnable();
        StartInitializeRoutine();
    }

    protected override void OnDisable()
    {
        if (initializeRoutine != null)
        {
            StopCoroutine(initializeRoutine);
            initializeRoutine = null;
        }

        if (panelMoveRoutine != null)
        {
            StopCoroutine(panelMoveRoutine);
            panelMoveRoutine = null;
        }

        if (btnToggleQuestPanel != null)
            btnToggleQuestPanel.onClick.RemoveListener(TogglePanel);

        base.OnDisable();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnQuestProgressChanged += RefreshView;
        BindRewardButton();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnQuestProgressChanged -= RefreshView;
        UnbindRewardButton();
    }

    protected override void RefreshView()
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.LineQuestDict == null)
            return;

        if (QuestManager.Instance == null)
            return;

        LineQuestTable questData = QuestManager.Instance.CurrentQuestData;
        int currentProgress = QuestManager.Instance.currentProgress;

        if (questData == null)
        {
            isQuestCompleted = false;
            SetQuestInfo("-", allClearTitle);
            SetProgress(1f);
            SetRewardSprite(spriteRewardInProgress);
            SetRewardCount(string.Empty);
            SetRewardButtonInteractable(false);
            ApplyFoldState();
            return;
        }

        QuestRewardsTable rewardData = GetRewardData(questData);
        int requiredCount = Mathf.Max(1, questData.reqCount);
        int clampedCurrent = Mathf.Clamp(currentProgress, 0, requiredCount);
        float progress01 = (float)clampedCurrent / requiredCount;

        isQuestCompleted = currentProgress >= questData.reqCount;
        SetQuestInfo($"no. {questData.questNum}", questData.questTitle);
        SetProgress(progress01);
        SetRewardSprite(ResolveRewardSprite(rewardData) ?? GetRewardFallbackSprite());

        string rewardCountText = FormatRewardCount(rewardData);
        SetRewardCount(rewardCountText);

        if (isQuestCompleted)
        {
            SetRewardButtonInteractable(true);
            SetRewardTouchText(rewardTouchText);
        }
        else
        {
            SetRewardButtonInteractable(false);
        }

        ApplyFoldState();
    }

    private void StartInitializeRoutine()
    {
        if (initializeRoutine != null)
            StopCoroutine(initializeRoutine);

        initializeRoutine = StartCoroutine(InitializeAfterDataLoad());
    }

    private IEnumerator InitializeAfterDataLoad()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        yield return new WaitUntil(() => QuestManager.Instance != null);

        RefreshView();
        initializeRoutine = null;
    }

    private void TogglePanel()
    {
        isOpened = !isOpened;
        ApplyFoldState();
    }

    private void OnClickRewardTouch()
    {
        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.ClaimReward();
        RefreshView();
    }

    private void CacheOpenedPosition()
    {
        if (hasCachedOpenPosition || rectQuestPanel == null)
            return;

        openedAnchoredPosition = rectQuestPanel.anchoredPosition;
        hasCachedOpenPosition = true;
    }

    private Vector2 GetFoldedPosition()
    {
        float effectiveFoldDistance = GetEffectiveFoldDistanceX();
        return openedAnchoredPosition + (Vector2.left * effectiveFoldDistance);
    }

    private Vector2 GetTogglePositionByState()
    {
        if (rectToggleArrow == null)
            return openedToggleAnchoredPosition;

        float openSign = GetScaleSign(openedArrowScaleX);
        float currentSign = isOpened ? openSign : GetScaleSign(foldedArrowScaleX);
        float width = rectToggleArrow.rect.width;
        float pivotOffset = width * ((2f * rectToggleArrow.pivot.x) - 1f);
        float signDelta = (currentSign - openSign) * 0.5f;
        float compensationX = -pivotOffset * signDelta;

        return openedToggleAnchoredPosition + new Vector2(compensationX, 0f);
    }

    private static float GetScaleSign(float value)
    {
        return value < 0f ? -1f : 1f;
    }

    private float GetEffectiveFoldDistanceX()
    {
        if (rectQuestPanel == null || rectToggleArrow == null)
            return Mathf.Max(0f, foldDistanceX);

        RectTransform parentRect = rectQuestPanel.parent as RectTransform;
        if (parentRect == null)
            return Mathf.Max(0f, foldDistanceX);

        Bounds handleBoundsInPanel = RectTransformUtility.CalculateRelativeRectTransformBounds(rectQuestPanel, rectToggleArrow);
        float requestedFold = Mathf.Max(0f, foldDistanceX);
        float openHandleRightX = openedAnchoredPosition.x + handleBoundsInPanel.max.x;
        float minHandleRightX = parentRect.rect.xMin + minVisibleHandleWidth;
        float maxFoldToKeepHandleVisible = Mathf.Max(0f, openHandleRightX - minHandleRightX);
        return Mathf.Min(requestedFold, maxFoldToKeepHandleVisible);
    }

    private void CacheArrowScale()
    {
        if (hasCachedArrowScale || rectToggleArrow == null)
            return;

        cachedArrowScale = rectToggleArrow.localScale;
        hasCachedArrowScale = true;
    }

    private void CacheTogglePosition()
    {
        if (hasCachedTogglePosition || rectToggleArrow == null)
            return;

        openedToggleAnchoredPosition = rectToggleArrow.anchoredPosition;
        hasCachedTogglePosition = true;
    }

    private void MovePanel(Vector2 targetPosition, Vector2 targetTogglePosition, bool instant)
    {
        if (rectQuestPanel == null || rectToggleArrow == null)
            return;

        if (panelMoveRoutine != null)
        {
            StopCoroutine(panelMoveRoutine);
            panelMoveRoutine = null;
        }

        if (instant || panelMoveDuration <= 0f)
        {
            rectQuestPanel.anchoredPosition = targetPosition;
            rectToggleArrow.anchoredPosition = targetTogglePosition;
            return;
        }

        panelMoveRoutine = StartCoroutine(CoMovePanel(targetPosition, targetTogglePosition));
    }

    private IEnumerator CoMovePanel(Vector2 targetPosition, Vector2 targetTogglePosition)
    {
        if (rectQuestPanel == null || rectToggleArrow == null)
            yield break;

        Vector2 startPosition = rectQuestPanel.anchoredPosition;
        Vector2 startTogglePosition = rectToggleArrow.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < panelMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / panelMoveDuration);
            rectQuestPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            rectToggleArrow.anchoredPosition = Vector2.Lerp(startTogglePosition, targetTogglePosition, t);
            yield return null;
        }

        rectQuestPanel.anchoredPosition = targetPosition;
        rectToggleArrow.anchoredPosition = targetTogglePosition;
        panelMoveRoutine = null;
    }

    private void ApplyFoldState(bool instant = false)
    {
        bool showRewardTouch = isQuestCompleted;
        SetProgressVisible(!showRewardTouch);
        SetRewardButtonVisible(showRewardTouch);
        if (showRewardTouch)
            SetRewardTouchText(rewardTouchText);

        Vector2 targetPosition = isOpened ? openedAnchoredPosition : GetFoldedPosition();
        Vector2 targetTogglePosition = GetTogglePositionByState();
        MovePanel(targetPosition, targetTogglePosition, instant);
        ApplyArrowDirection();
    }

    private void ApplyArrowDirection()
    {
        if (rectToggleArrow == null)
            return;

        float baseAbsX = Mathf.Approximately(cachedArrowScale.x, 0f) ? 1f : Mathf.Abs(cachedArrowScale.x);
        float targetScaleX = baseAbsX * (isOpened ? openedArrowScaleX : foldedArrowScaleX);
        rectToggleArrow.localScale = new Vector3(targetScaleX, cachedArrowScale.y, cachedArrowScale.z);
    }

    private QuestRewardsTable GetRewardData(LineQuestTable questData)
    {
        if (questData == null || DataManager.Instance?.QuestRewardsDict == null)
            return null;

        DataManager.Instance.QuestRewardsDict.TryGetValue(questData.rewardGroupID, out QuestRewardsTable rewardData);
        return rewardData;
    }

    private Sprite ResolveRewardSprite(QuestRewardsTable rewardData)
    {
        if (rewardData == null)
            return null;

        Sprite sprite = IconManager.GetResourceSprite(rewardData.rewardItemIcon);
        if (sprite != null)
            return sprite;

        if (DataManager.Instance?.ItemInfoDict != null &&
            DataManager.Instance.ItemInfoDict.TryGetValue(rewardData.ItemID, out ItemInfoTable itemInfo))
        {
            return IconManager.GetItemIcon(itemInfo);
        }

        return null;
    }

    private Sprite GetRewardFallbackSprite()
    {
        return isQuestCompleted ? spriteRewardReady : spriteRewardInProgress;
    }

    private static string FormatRewardCount(QuestRewardsTable rewardData)
    {
        if (rewardData == null || rewardData.rewardItemCount <= 0)
            return string.Empty;

        return rewardData.rewardItemCount.ToString("N0");
    }

    private void BindRewardButton()
    {
        if (btnRewardTouch == null)
            return;

        btnRewardTouch.onClick.RemoveListener(OnClickRewardTouch);
        btnRewardTouch.onClick.AddListener(OnClickRewardTouch);
    }

    private void UnbindRewardButton()
    {
        if (btnRewardTouch != null)
            btnRewardTouch.onClick.RemoveListener(OnClickRewardTouch);
    }

    private void SetQuestInfo(string numberText, string titleText)
    {
        if (textQuestNumber != null)
            textQuestNumber.text = numberText;

        if (textQuestName != null)
            textQuestName.text = titleText;
    }

    private void SetProgress(float progress01)
    {
        if (sliderQuestProgress != null)
            sliderQuestProgress.SetValueWithoutNotify(progress01);
    }

    private void SetProgressVisible(bool visible)
    {
        if (objQuestProgressSliderRoot != null)
            objQuestProgressSliderRoot.SetActive(visible);
    }

    private void SetRewardSprite(Sprite sprite)
    {
        if (imageQuestReward != null)
            imageQuestReward.sprite = sprite;
    }

    private void SetRewardCount(string text)
    {
        bool visible = !string.IsNullOrEmpty(text);

        if (textQuestRewardCount != null)
        {
            textQuestRewardCount.text = text ?? string.Empty;
            textQuestRewardCount.gameObject.SetActive(visible);
        }
    }

    private void SetRewardButtonVisible(bool visible)
    {
        if (btnRewardTouch != null)
            btnRewardTouch.gameObject.SetActive(visible);
    }

    private void SetRewardButtonInteractable(bool interactable)
    {
        if (btnRewardTouch != null)
            btnRewardTouch.interactable = interactable;
    }

    private void SetRewardTouchText(string text)
    {
        if (textRewardTouch != null)
            textRewardTouch.text = text;
    }
}
