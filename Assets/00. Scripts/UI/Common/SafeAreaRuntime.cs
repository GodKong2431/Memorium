using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SafeAreaRuntime
{
    private const string RuntimeObjectName = "SafeAreaRuntime";
    private const string MainCanvasName = "MainUICanvas";
    private const string SafeAreaObjectName = "SafeArea";

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
        MoveDirectChildrenIntoSafeArea(mainCanvasRect, safeAreaRoot);

        SafeAreaFitter safeAreaFitter = safeAreaRoot.GetComponent<SafeAreaFitter>();
        if (safeAreaFitter == null)
            safeAreaFitter = safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();

        safeAreaFitter.ApplyNow();
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
            return existingRectTransform;

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

    private static void MoveDirectChildrenIntoSafeArea(RectTransform mainCanvasRect, RectTransform safeAreaRoot)
    {
        int childCount = mainCanvasRect.childCount;
        if (childCount == 0)
            return;

        List<Transform> childrenToMove = new List<Transform>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            Transform child = mainCanvasRect.GetChild(i);
            if (child == safeAreaRoot)
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
