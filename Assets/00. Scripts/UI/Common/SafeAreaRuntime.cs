using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class SafeAreaRuntime
{
    private const string RuntimeObjectName = "SafeAreaRuntime";
    private const string MainCanvasName = "MainUICanvas";
    private const string SafeAreaObjectName = "SafeArea";
    private const string UnsafeAreaMaskObjectName = "UnsafeAreaMask";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<SafeAreaRuntimeUpdater>() != null)
            return;

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        runtimeObject.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<SafeAreaRuntimeUpdater>();
    }

    public static void RefreshLoadedCanvases()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
            RefreshCanvas(canvases[i]);
    }

    public static void RefreshWithin(Transform root)
    {
        if (root == null)
            return;

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
            RefreshCanvas(canvases[i]);
    }

    private static void RefreshCanvas(Canvas canvas)
    {
        if (!IsTargetCanvas(canvas))
            return;

        RectTransform mainCanvasRect = canvas.transform as RectTransform;
        if (mainCanvasRect == null)
            return;

        RectTransform safeAreaRoot = EnsureSafeAreaRoot(mainCanvasRect);
        RectTransform unsafeAreaMaskRoot = EnsureUnsafeAreaMaskRoot(mainCanvasRect);
        MoveDirectChildrenIntoSafeArea(mainCanvasRect, safeAreaRoot, unsafeAreaMaskRoot);

        SafeAreaFitter safeAreaFitter = safeAreaRoot.GetComponent<SafeAreaFitter>();
        if (safeAreaFitter == null)
            safeAreaFitter = safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();

        safeAreaFitter.ApplyNow();

        UnsafeAreaMaskFitter unsafeAreaMaskFitter = unsafeAreaMaskRoot.GetComponent<UnsafeAreaMaskFitter>();
        if (unsafeAreaMaskFitter == null)
            unsafeAreaMaskFitter = unsafeAreaMaskRoot.gameObject.AddComponent<UnsafeAreaMaskFitter>();

        unsafeAreaMaskFitter.Bind(safeAreaFitter);
        unsafeAreaMaskFitter.ApplyNow();
    }

    private static bool IsTargetCanvas(Canvas canvas)
    {
        return canvas != null &&
               canvas.isRootCanvas &&
               canvas.renderMode != RenderMode.WorldSpace &&
               canvas.name == MainCanvasName &&
               canvas.gameObject.scene.IsValid();
    }

    private static RectTransform EnsureSafeAreaRoot(RectTransform mainCanvasRect)
    {
        Transform existing = mainCanvasRect.Find(SafeAreaObjectName);
        if (existing is RectTransform existingRectTransform)
        {
            existingRectTransform.SetAsFirstSibling();
            return existingRectTransform;
        }

        GameObject safeAreaObject = new GameObject(SafeAreaObjectName, typeof(RectTransform));
        safeAreaObject.layer = mainCanvasRect.gameObject.layer;

        RectTransform safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
        safeAreaRect.SetParent(mainCanvasRect, false);
        safeAreaRect.anchorMin = Vector2.zero;
        safeAreaRect.anchorMax = Vector2.one;
        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
        safeAreaRect.localScale = Vector3.one;
        safeAreaRect.SetAsFirstSibling();

        return safeAreaRect;
    }

    private static RectTransform EnsureUnsafeAreaMaskRoot(RectTransform mainCanvasRect)
    {
        Transform existing = mainCanvasRect.Find(UnsafeAreaMaskObjectName);
        if (existing is RectTransform existingRectTransform)
        {
            existingRectTransform.SetAsLastSibling();
            return existingRectTransform;
        }

        GameObject unsafeAreaMaskObject = new GameObject(UnsafeAreaMaskObjectName, typeof(RectTransform));
        unsafeAreaMaskObject.layer = mainCanvasRect.gameObject.layer;

        RectTransform unsafeAreaMaskRect = unsafeAreaMaskObject.GetComponent<RectTransform>();
        unsafeAreaMaskRect.SetParent(mainCanvasRect, false);
        unsafeAreaMaskRect.anchorMin = Vector2.zero;
        unsafeAreaMaskRect.anchorMax = Vector2.one;
        unsafeAreaMaskRect.offsetMin = Vector2.zero;
        unsafeAreaMaskRect.offsetMax = Vector2.zero;
        unsafeAreaMaskRect.localScale = Vector3.one;
        unsafeAreaMaskRect.SetAsLastSibling();

        return unsafeAreaMaskRect;
    }

    private static void MoveDirectChildrenIntoSafeArea(RectTransform mainCanvasRect, RectTransform safeAreaRoot, RectTransform unsafeAreaMaskRoot)
    {
        int childCount = mainCanvasRect.childCount;
        if (childCount == 0)
            return;

        List<Transform> childrenToMove = new List<Transform>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            Transform child = mainCanvasRect.GetChild(i);
            if (child == safeAreaRoot || child == unsafeAreaMaskRoot)
                continue;

            childrenToMove.Add(child);
        }

        for (int i = 0; i < childrenToMove.Count; i++)
            childrenToMove[i].SetParent(safeAreaRoot, false);
    }

    private sealed class SafeAreaRuntimeUpdater : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.5f;

        private Vector2Int lastScreenSize;
        private float nextRefreshTime;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RefreshNow();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void LateUpdate()
        {
            Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (currentScreenSize != lastScreenSize)
            {
                RefreshNow();
                return;
            }

            if (Time.unscaledTime >= nextRefreshTime)
                RefreshNow();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshNow();
        }

        private void RefreshNow()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            RefreshLoadedCanvases();
        }
    }
}

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class UnsafeAreaMaskFitter : MonoBehaviour
{
    private const string LeftMaskName = "LeftMask";
    private const string RightMaskName = "RightMask";
    private const string TopMaskName = "TopMask";
    private const string BottomMaskName = "BottomMask";

    [SerializeField] private SafeAreaFitter safeAreaFitter;
    [SerializeField] private Color maskColor = Color.black;
    [SerializeField] private bool blockRaycasts = true;
    [SerializeField] private Image leftMask;
    [SerializeField] private Image rightMask;
    [SerializeField] private Image topMask;
    [SerializeField] private Image bottomMask;

    private RectTransform cachedRectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    public void Bind(SafeAreaFitter sourceFitter)
    {
        safeAreaFitter = sourceFitter;
        EnsureMaskImages();
    }

    public void ApplyNow()
    {
        ApplyMasks(force: true);
    }

    private void Awake()
    {
        EnsureMaskImages();
        ApplyMasks(force: true);
    }

    private void OnEnable()
    {
        EnsureMaskImages();
        ApplyMasks(force: true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureMaskImages();
        ApplyMasks(force: true);
    }
#endif

    private void Update()
    {
        if (Application.isPlaying)
            return;

        ApplyMasks(force: false);
    }

    private void ApplyMasks(bool force)
    {
        if (!TryResolveRectTransform(out RectTransform rootRect))
            return;

        EnsureMaskImages();

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0)
            return;

        Rect safeArea = safeAreaFitter != null
            ? safeAreaFitter.GetAppliedSafeArea()
            : new Rect(0f, 0f, screenWidth, screenHeight);

        if (safeArea.width <= 0f || safeArea.height <= 0f)
            safeArea = new Rect(0f, 0f, screenWidth, screenHeight);

        Vector2Int screenSize = new Vector2Int(screenWidth, screenHeight);
        if (!force && lastSafeArea == safeArea && lastScreenSize == screenSize)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        float xMin = Mathf.Clamp01(safeArea.xMin / screenWidth);
        float xMax = Mathf.Clamp01(safeArea.xMax / screenWidth);
        float yMin = Mathf.Clamp01(safeArea.yMin / screenHeight);
        float yMax = Mathf.Clamp01(safeArea.yMax / screenHeight);

        ApplyMaskStyle(leftMask);
        ApplyMaskStyle(rightMask);
        ApplyMaskStyle(topMask);
        ApplyMaskStyle(bottomMask);

        StretchMask(leftMask.rectTransform, 0f, 0f, xMin, 1f);
        StretchMask(rightMask.rectTransform, xMax, 0f, 1f, 1f);
        StretchMask(topMask.rectTransform, xMin, yMax, xMax, 1f);
        StretchMask(bottomMask.rectTransform, xMin, 0f, xMax, yMin);

        rootRect.SetAsLastSibling();
    }

    private void EnsureMaskImages()
    {
        if (leftMask != null && rightMask != null && topMask != null && bottomMask != null)
            return;

        if (!TryResolveRectTransform(out RectTransform rootRect))
            return;

        if (leftMask == null)
            leftMask = EnsureMaskImage(rootRect, LeftMaskName);

        if (rightMask == null)
            rightMask = EnsureMaskImage(rootRect, RightMaskName);

        if (topMask == null)
            topMask = EnsureMaskImage(rootRect, TopMaskName);

        if (bottomMask == null)
            bottomMask = EnsureMaskImage(rootRect, BottomMaskName);
    }

    private Image EnsureMaskImage(RectTransform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image != null)
            return image;

        GameObject maskObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        maskObject.layer = parent.gameObject.layer;

        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.SetParent(parent, false);
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
        maskRect.localScale = Vector3.one;

        return maskObject.GetComponent<Image>();
    }

    private void ApplyMaskStyle(Image image)
    {
        if (image == null)
            return;

        image.color = maskColor;
        image.raycastTarget = blockRaycasts;
    }

    private static void StretchMask(RectTransform target, float minX, float minY, float maxX, float maxY)
    {
        if (target == null)
            return;

        bool isVisible = maxX > minX && maxY > minY;
        target.gameObject.SetActive(isVisible);
        if (!isVisible)
            return;

        target.anchorMin = new Vector2(minX, minY);
        target.anchorMax = new Vector2(maxX, maxY);
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
        target.anchoredPosition = Vector2.zero;
        target.localScale = Vector3.one;
    }

    private bool TryResolveRectTransform(out RectTransform rectTransform)
    {
        if (cachedRectTransform == null)
            cachedRectTransform = transform as RectTransform;

        rectTransform = cachedRectTransform;
        return rectTransform != null;
    }
}
