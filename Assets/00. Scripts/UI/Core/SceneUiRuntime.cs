using UnityEngine;
using UnityEngine.Rendering.Universal;

internal static class SceneUiRuntime
{
    public static void Refresh(Transform sceneUiRoot)
    {
        if (sceneUiRoot == null)
            return;

        NormalizeCanvasScale(sceneUiRoot);

        Camera gameplayCamera = ResolveGameplayCamera(sceneUiRoot);
        Camera uiCamera = ResolveUiCamera(sceneUiRoot);
        Camera canvasCamera = ResolveCanvasCamera(sceneUiRoot, gameplayCamera, uiCamera);
        if (canvasCamera == null)
            return;

        BindCanvases(sceneUiRoot, canvasCamera);
    }

    private static Camera ResolveCanvasCamera(Transform sceneUiRoot, Camera gameplayCamera, Camera uiCamera)
    {
        if (uiCamera != null)
        {
            EnsureUiLayerVisible(sceneUiRoot, uiCamera);
            uiCamera.enabled = true;

            if (TryAttachUiCamera(gameplayCamera, uiCamera))
                return uiCamera;

            if (gameplayCamera == null)
                return uiCamera;
        }

        if (gameplayCamera != null)
        {
            EnsureUiLayerVisible(sceneUiRoot, gameplayCamera);
            return gameplayCamera;
        }

        return uiCamera;
    }

    private static Camera ResolveGameplayCamera(Transform sceneUiRoot)
    {
        QuarterViewCamera quarterViewCamera = Object.FindFirstObjectByType<QuarterViewCamera>();
        if (quarterViewCamera != null)
        {
            Camera quarterCamera = quarterViewCamera.GetComponent<Camera>();
            if (IsUsableCamera(quarterCamera))
                return quarterCamera;
        }

        if (IsUsableCamera(Camera.main) && !IsSceneUiCamera(sceneUiRoot, Camera.main))
            return Camera.main;

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (!IsUsableCamera(camera) || IsSceneUiCamera(sceneUiRoot, camera))
                continue;

            if (camera.targetTexture != null)
                continue;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData == null || cameraData.renderType == CameraRenderType.Base)
                return camera;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (IsUsableCamera(camera) && !IsSceneUiCamera(sceneUiRoot, camera))
                return camera;
        }

        return null;
    }

    private static Camera ResolveUiCamera(Transform sceneUiRoot)
    {
        Camera[] cameras = sceneUiRoot.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (IsUsableCamera(cameras[i]))
                return cameras[i];
        }

        return null;
    }

    private static bool TryAttachUiCamera(Camera gameplayCamera, Camera uiCamera)
    {
        if (gameplayCamera == null || uiCamera == null || gameplayCamera == uiCamera)
            return false;

        UniversalAdditionalCameraData gameplayData = gameplayCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData uiData = uiCamera.GetUniversalAdditionalCameraData();
        if (gameplayData == null || uiData == null)
            return false;

        uiData.renderType = CameraRenderType.Overlay;
        RemoveFromCameraStacks(uiCamera);

        if (!gameplayData.cameraStack.Contains(uiCamera))
            gameplayData.cameraStack.Add(uiCamera);

        return true;
    }

    private static void RemoveFromCameraStacks(Camera uiCamera)
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || camera == uiCamera)
                continue;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData == null || cameraData.renderType != CameraRenderType.Base)
                continue;

            for (int stackIndex = cameraData.cameraStack.Count - 1; stackIndex >= 0; stackIndex--)
            {
                Camera stackedCamera = cameraData.cameraStack[stackIndex];
                if (stackedCamera == null || stackedCamera == uiCamera)
                    cameraData.cameraStack.RemoveAt(stackIndex);
            }
        }
    }

    private static void NormalizeCanvasScale(Transform sceneUiRoot)
    {
        Canvas[] canvases = sceneUiRoot.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            RectTransform rect = canvases[i] != null ? canvases[i].transform as RectTransform : null;
            if (rect != null && rect.localScale == Vector3.zero)
                rect.localScale = Vector3.one;
        }
    }

    private static void BindCanvases(Transform sceneUiRoot, Camera targetCamera)
    {
        Canvas[] canvases = sceneUiRoot.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera)
                continue;

            if (canvas.worldCamera != targetCamera)
                canvas.worldCamera = targetCamera;
        }
    }

    private static void EnsureUiLayerVisible(Transform sceneUiRoot, Camera targetCamera)
    {
        if (targetCamera == null)
            return;

        int uiLayer = sceneUiRoot.gameObject.layer;
        if (uiLayer < 0 || uiLayer > 31)
            return;

        targetCamera.cullingMask |= 1 << uiLayer;
    }

    private static bool IsSceneUiCamera(Transform sceneUiRoot, Camera camera)
    {
        return camera != null &&
               sceneUiRoot != null &&
               camera.transform.IsChildOf(sceneUiRoot);
    }

    private static bool IsUsableCamera(Camera camera)
    {
        return camera != null &&
               camera.enabled &&
               camera.gameObject.activeInHierarchy;
    }
}
