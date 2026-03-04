using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUIController : MonoBehaviour
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
    private bool isBound;
    private bool isOpened;
    private bool isQuestCompleted;
    private Vector2 openedAnchoredPosition;
    private bool hasCachedOpenPosition;
    private Vector2 openedToggleAnchoredPosition;
    private bool hasCachedTogglePosition;
    private Vector3 cachedArrowScale;
    private bool hasCachedArrowScale;

    private void Awake()
    {
        CacheOpenedPosition();
        CacheTogglePosition();
        CacheArrowScale();
    }

    private void OnEnable()
    {
        CacheOpenedPosition();
        CacheTogglePosition();
        CacheArrowScale();

        isOpened = startOpened;
        ApplyFoldState(true);

        btnToggleQuestPanel.onClick.AddListener(TogglePanel);

        if (initializeRoutine == null)
            initializeRoutine = StartCoroutine(InitializeAfterDataLoad());
    }

    private void OnDisable()
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

        btnToggleQuestPanel.onClick.RemoveListener(TogglePanel);

        Unbind();
    }

    private IEnumerator InitializeAfterDataLoad()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        Bind();
        RefreshView();
        initializeRoutine = null;
    }

    private void Bind()
    {
        if (isBound)
            return;

        GameEventManager.OnQuestProgressChanged += RefreshView;
        btnRewardTouch.onClick.AddListener(OnClickRewardTouch);
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound)
            return;

        GameEventManager.OnQuestProgressChanged -= RefreshView;
        btnRewardTouch.onClick.RemoveListener(OnClickRewardTouch);
        isBound = false;
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

    private void RefreshView()
    {
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
            btnRewardTouch.interactable = false;
            ApplyFoldState();
            return;
        }

        int requiredCount = Mathf.Max(1, questData.reqCount);
        int clampedCurrent = Mathf.Clamp(currentProgress, 0, requiredCount);
        float progress01 = (float)clampedCurrent / requiredCount;

        isQuestCompleted = currentProgress >= questData.reqCount;

        SetQuestInfo($"no. {questData.questNum}", questData.questTitle);
        SetProgress(progress01);

        if (isQuestCompleted)
        {
            SetRewardSprite(spriteRewardReady);
            btnRewardTouch.interactable = true;
            textRewardTouch.text = rewardTouchText;
        }
        else
        {
            SetRewardSprite(spriteRewardInProgress);
            btnRewardTouch.interactable = false;
        }

        ApplyFoldState();
    }

    private void CacheOpenedPosition()
    {
        if (hasCachedOpenPosition)
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
        float openSign = GetScaleSign(openedArrowScaleX);
        float currentSign = isOpened ? openSign : GetScaleSign(foldedArrowScaleX);

        // 화살표를 좌우 반전하면 Pivot 기준으로 폭만큼 시각 위치가 밀리므로 오프셋을 보정한다.
        float width = rectToggleArrow.rect.width;
        float pivotOffset = width * ((2f * rectToggleArrow.pivot.x) - 1f);
        float signDelta = (currentSign - openSign) * 0.5f; // same sign: 0, +->-: -1, -->+: +1
        float compensationX = -pivotOffset * signDelta;

        return openedToggleAnchoredPosition + new Vector2(compensationX, 0f);
    }

    private static float GetScaleSign(float value)
    {
        return value < 0f ? -1f : 1f;
    }

    private float GetEffectiveFoldDistanceX()
    {
        RectTransform parentRect = (RectTransform)rectQuestPanel.parent;
        Bounds handleBoundsInPanel = RectTransformUtility.CalculateRelativeRectTransformBounds(rectQuestPanel, rectToggleArrow);

        float requestedFold = Mathf.Max(0f, foldDistanceX);
        float openHandleRightX = openedAnchoredPosition.x + handleBoundsInPanel.max.x;
        float minHandleRightX = parentRect.rect.xMin + minVisibleHandleWidth;
        float maxFoldToKeepHandleVisible = Mathf.Max(0f, openHandleRightX - minHandleRightX);

        return Mathf.Min(requestedFold, maxFoldToKeepHandleVisible);
    }

    private void CacheArrowScale()
    {
        if (hasCachedArrowScale)
            return;

        cachedArrowScale = rectToggleArrow.localScale;
        hasCachedArrowScale = true;
    }

    private void CacheTogglePosition()
    {
        if (hasCachedTogglePosition)
            return;

        openedToggleAnchoredPosition = rectToggleArrow.anchoredPosition;
        hasCachedTogglePosition = true;
    }

    private void MovePanel(Vector2 targetPosition, Vector2 targetTogglePosition, bool instant)
    {
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
        objQuestProgressSliderRoot.SetActive(!showRewardTouch);
        btnRewardTouch.gameObject.SetActive(showRewardTouch);
        if (showRewardTouch)
            textRewardTouch.text = rewardTouchText;

        Vector2 targetPosition = isOpened ? openedAnchoredPosition : GetFoldedPosition();
        Vector2 targetTogglePosition = GetTogglePositionByState();
        MovePanel(targetPosition, targetTogglePosition, instant);
        ApplyArrowDirection();
    }

    private void ApplyArrowDirection()
    {
        float baseAbsX = Mathf.Approximately(cachedArrowScale.x, 0f) ? 1f : Mathf.Abs(cachedArrowScale.x);
        float targetScaleX = baseAbsX * (isOpened ? openedArrowScaleX : foldedArrowScaleX);
        rectToggleArrow.localScale = new Vector3(targetScaleX, cachedArrowScale.y, cachedArrowScale.z);
    }

    private void SetQuestInfo(string number, string name)
    {
        textQuestNumber.text = number;
        textQuestName.text = name;
    }

    private void SetProgress(float value)
    {
        sliderQuestProgress.SetValueWithoutNotify(value);
    }

    private void SetRewardSprite(Sprite sprite)
    {
        imageQuestReward.sprite = sprite;
    }
}
