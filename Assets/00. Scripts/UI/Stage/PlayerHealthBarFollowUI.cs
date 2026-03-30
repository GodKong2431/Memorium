using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHealthBarFollowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera worldCamera;

    [Header("Sheet")]
    [SerializeField] private bool useAsSheetToggle = true;
    [SerializeField] private BottomPanelController bottomPanelController;
    [SerializeField] private RectTransform linkedSheetPage;

    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float followSmoothTime = 0.12f;
    [SerializeField] private float maxFollowSpeed = 2500f;
    [SerializeField] private bool clampToScreen = true;
    [SerializeField] private Vector2 screenPadding = new Vector2(48f, 48f);
    [SerializeField] private bool renderBehindSiblingUi = true;
    [SerializeField] private int backOrder = -30;

    private CanvasGroup canvasGroup;
    private Canvas backCanvas;
    private GraphicRaycaster backRaycaster;
    private Toggle sheetToggle;
    private Image toggleRaycastImage;
    private Transform targetTransform;
    private PlayerStateContext stateContext;
    private CharacterStatManager statManager;
    private Vector3 followVelocity;
    private bool hasFollowPosition;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (healthSlider == null)
            healthSlider = FindSlider("(Slider)Health");

        if (manaSlider == null)
            manaSlider = FindSlider("(Slider)Mana");

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureSheetToggle();
        ApplyRenderOrder();
        SetVisible(false);
        RefreshValues();
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerSpawned += OnPlayerSpawned;
        RegisterSheetToggle();
        ApplyRenderOrder();
        TryBindCurrentPlayer();
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerSpawned -= OnPlayerSpawned;
        UnregisterSheetToggle();
        UnbindPlayer();
    }

    private void LateUpdate()
    {
        UpdateFollowPosition();
    }

    private void OnPlayerSpawned(Transform playerTransform)
    {
        BindPlayer(playerTransform);
    }

    public void ResetForSceneChange()
    {
        UnbindPlayer();
        hasFollowPosition = false;
        followVelocity = Vector3.zero;
        if (sheetToggle != null)
            sheetToggle.SetIsOnWithoutNotify(false);
        SetVisible(false);
    }

    private void TryBindCurrentPlayer()
    {
        if (ScenePlayerLocator.TryGetPlayerTransform(out Transform playerTransform))
            BindPlayer(playerTransform);
    }

    private void BindPlayer(Transform playerTransform)
    {
        UnbindPlayer();

        targetTransform = playerTransform;
        if (targetTransform == null)
        {
            hasFollowPosition = false;
            SetVisible(false);
            return;
        }

        PlayerStateMachine playerStateMachine = targetTransform.GetComponent<PlayerStateMachine>();
        stateContext = playerStateMachine != null ? playerStateMachine._ctx : null;
        if (stateContext == null)
        {
            hasFollowPosition = false;
            SetVisible(false);
            return;
        }

        statManager = CharacterStatManager.Instance;

        stateContext.OnHealthChanged += OnHealthChanged;
        stateContext.OnManaChanged += OnManaChanged;

        if (statManager != null)
            statManager.StatUpdate += OnStatUpdated;

        RefreshValues();
        SnapToFollowTarget();
        SetVisible(true);
    }

    private void UnbindPlayer()
    {
        if (stateContext != null)
        {
            stateContext.OnHealthChanged -= OnHealthChanged;
            stateContext.OnManaChanged -= OnManaChanged;
        }

        if (statManager != null)
            statManager.StatUpdate -= OnStatUpdated;

        targetTransform = null;
        stateContext = null;
        statManager = null;
        followVelocity = Vector3.zero;
        hasFollowPosition = false;
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        UpdateSlider(healthSlider, currentHealth, maxHealth);
        UpdateText(healthText, currentHealth, maxHealth);
    }

    private void OnManaChanged(float currentMana, float maxMana)
    {
        UpdateSlider(manaSlider, currentMana, maxMana);
        UpdateText(manaText, currentMana, maxMana);
    }

    private void OnStatUpdated()
    {
        RefreshValues();
    }

    private void RefreshValues()
    {
        float currentHealth = stateContext != null ? stateContext.CurrentHealth : 0f;
        float maxHealth = stateContext != null ? stateContext.MaxHealth : 1f;
        float currentMana = stateContext != null ? stateContext.CurrentMana : 0f;
        float maxMana = stateContext != null ? stateContext.MaxMana : 1f;

        UpdateSlider(healthSlider, currentHealth, maxHealth);
        UpdateSlider(manaSlider, currentMana, maxMana);
        UpdateText(healthText, currentHealth, maxHealth);
        UpdateText(manaText, currentMana, maxMana);
    }

    private void UpdateFollowPosition()
    {
        if (panelRoot == null || targetTransform == null)
            return;

        Canvas canvas = ResolveCanvas();
        Vector3 worldPosition = targetTransform.position + worldOffset;

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            panelRoot.position = GetSmoothedPosition(worldPosition);
            SetVisible(true);
            return;
        }

        Camera cameraToUse = ResolveWorldCamera();
        if (cameraToUse == null)
            return;

        Vector3 screenPosition = cameraToUse.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        if (clampToScreen)
        {
            screenPosition.x = Mathf.Clamp(screenPosition.x, screenPadding.x, Screen.width - screenPadding.x);
            screenPosition.y = Mathf.Clamp(screenPosition.y, screenPadding.y, Screen.height - screenPadding.y);
        }

        RectTransform parentRect = panelRoot.parent as RectTransform;
        Camera uiCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas != null ? canvas.worldCamera : null;

        if (parentRect != null &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPosition, uiCamera, out Vector3 worldPoint))
        {
            panelRoot.position = GetSmoothedPosition(worldPoint);
        }

        SetVisible(true);
    }

    private void SnapToFollowTarget()
    {
        if (panelRoot == null || targetTransform == null)
            return;

        Canvas canvas = ResolveCanvas();
        Vector3 worldPosition = targetTransform.position + worldOffset;

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            panelRoot.position = worldPosition;
            hasFollowPosition = true;
            followVelocity = Vector3.zero;
            return;
        }

        Camera cameraToUse = ResolveWorldCamera();
        RectTransform parentRect = panelRoot.parent as RectTransform;
        Camera uiCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas != null ? canvas.worldCamera : null;
        if (cameraToUse == null || parentRect == null)
            return;

        Vector3 screenPosition = cameraToUse.WorldToScreenPoint(worldPosition);
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPosition, uiCamera, out Vector3 worldPoint))
            return;

        panelRoot.position = worldPoint;
        hasFollowPosition = true;
        followVelocity = Vector3.zero;
    }

    private Canvas ResolveCanvas()
    {
        if (targetCanvas != null)
            return targetCanvas;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        targetCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
        return targetCanvas;
    }

    private Camera ResolveWorldCamera()
    {
        if (worldCamera != null)
            return worldCamera;

        QuarterViewCamera quarterViewCamera = Object.FindFirstObjectByType<QuarterViewCamera>();
        if (quarterViewCamera != null)
        {
            Camera followCamera = quarterViewCamera.GetComponent<Camera>();
            if (followCamera != null)
                return followCamera;
        }

        if (Camera.main != null)
            return Camera.main;

        Camera sceneCamera = Object.FindFirstObjectByType<Camera>();
        return sceneCamera;
    }

    private Slider FindSlider(string sliderName)
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] != null && sliders[i].name == sliderName)
                return sliders[i];
        }

        return null;
    }

    private Vector3 GetSmoothedPosition(Vector3 targetPosition)
    {
        if (panelRoot == null)
            return targetPosition;

        if (!hasFollowPosition || followSmoothTime <= 0f)
        {
            hasFollowPosition = true;
            followVelocity = Vector3.zero;
            return targetPosition;
        }

        return Vector3.SmoothDamp(
            panelRoot.position,
            targetPosition,
            ref followVelocity,
            Mathf.Max(0.01f, followSmoothTime),
            maxFollowSpeed,
            Time.unscaledDeltaTime);
    }

    private void EnsureSheetToggle()
    {
        sheetToggle = GetComponent<Toggle>();
        if (sheetToggle == null)
            sheetToggle = gameObject.AddComponent<Toggle>();

        toggleRaycastImage = GetComponent<Image>();
        if (toggleRaycastImage == null)
            toggleRaycastImage = gameObject.AddComponent<Image>();

        toggleRaycastImage.color = new Color(1f, 1f, 1f, 0f);
        toggleRaycastImage.raycastTarget = true;

        if (sheetToggle.targetGraphic == null)
            sheetToggle.targetGraphic = toggleRaycastImage;

        if (sheetToggle.graphic == sheetToggle.targetGraphic)
            sheetToggle.graphic = null;

        sheetToggle.group = null;
        sheetToggle.toggleTransition = Toggle.ToggleTransition.None;
        sheetToggle.transition = Selectable.Transition.None;

        ConfigureSheetToggleHitArea();
    }

    private void ConfigureSheetToggleHitArea()
    {
        if (!useAsSheetToggle)
            return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null || graphic == toggleRaycastImage)
                continue;

            graphic.raycastTarget = false;
        }

        if (healthSlider != null)
            healthSlider.interactable = false;

        if (manaSlider != null)
            manaSlider.interactable = false;
    }

    private void ApplyRenderOrder()
    {
        if (!renderBehindSiblingUi || panelRoot == null)
            return;

        Canvas canvas = ResolveCanvas();
        RectTransform canvasRoot = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRoot != null && panelRoot.parent != canvasRoot)
            panelRoot.SetParent(canvasRoot, false);

        panelRoot.SetAsFirstSibling();
        ApplyBackSort(canvas);
    }

    private void RegisterSheetToggle()
    {
        if (!useAsSheetToggle || sheetToggle == null)
            return;

        if (bottomPanelController == null)
            bottomPanelController = Object.FindFirstObjectByType<BottomPanelController>();

        if (bottomPanelController == null)
        {
            Debug.LogWarning("PlayerHealthBarFollowUI: BottomPanelController reference is missing.", this);
            return;
        }

        if (linkedSheetPage == null)
        {
            Debug.LogWarning("PlayerHealthBarFollowUI: Assign a managed page to Linked Sheet Page.", this);
            return;
        }

        if (!bottomPanelController.IsManagedPageRegistered(linkedSheetPage))
        {
            Debug.LogWarning("PlayerHealthBarFollowUI: Linked Sheet Page is not managed by BottomPanelController.", linkedSheetPage);
            return;
        }

        bottomPanelController.RegisterExternalPageToggle(sheetToggle, linkedSheetPage);
    }

    private void UnregisterSheetToggle()
    {
        if (bottomPanelController != null && sheetToggle != null)
            bottomPanelController.UnregisterExternalPageToggle(sheetToggle);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private void ApplyBackSort(Canvas rootCanvas)
    {
        if (panelRoot == null || rootCanvas == null)
            return;

        if (backCanvas == null)
            backCanvas = panelRoot.GetComponent<Canvas>();
        if (backCanvas == null)
            backCanvas = panelRoot.gameObject.AddComponent<Canvas>();

        backCanvas.overrideSorting = true;
        backCanvas.renderMode = rootCanvas.renderMode;
        backCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        backCanvas.sortingOrder = backOrder;
        backCanvas.worldCamera = rootCanvas.worldCamera;
        backCanvas.planeDistance = rootCanvas.planeDistance;

        if (backRaycaster == null)
            backRaycaster = panelRoot.GetComponent<GraphicRaycaster>();
        if (backRaycaster == null)
            backRaycaster = panelRoot.gameObject.AddComponent<GraphicRaycaster>();
    }

    private static void UpdateSlider(Slider slider, float currentValue, float maxValue)
    {
        if (slider == null)
            return;

        float clampedMax = Mathf.Max(1f, maxValue);
        slider.minValue = 0f;
        slider.maxValue = clampedMax;
        slider.SetValueWithoutNotify(Mathf.Clamp(currentValue, 0f, clampedMax));
        slider.interactable = false;
    }

    private static void UpdateText(TMP_Text label, float currentValue, float maxValue)
    {
        if (label == null)
            return;

        label.SetText("{0} / {1}", Mathf.CeilToInt(currentValue), Mathf.CeilToInt(maxValue));
    }
}
