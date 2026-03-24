using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneTransitionOverlay : MonoBehaviour
{
    private const string OverlayObjectName = "SceneTransitionOverlay";
    private const string OverlayShaderResourcePath = "Shaders/SceneTransitionRadialFade";
    private const string OverlayShaderName = "UI/SceneTransitionRadialFade";
    private const float MinSoftness = 0.001f;
    private const float MaxAnimationDelta = 1f / 30f;

    private Canvas overlayCanvas;
    private CanvasScaler overlayCanvasScaler;
    private GraphicRaycaster overlayRaycaster;
    private RawImage overlayImage;
    private Material radialMaterial;
    private Shader radialShader;
    private bool warnedMissingShader;
    private float currentCoverage;

    private Color configuredColor = Color.black;
    private bool configuredUseRadialIris = true;
    private float configuredSoftness = 0.18f;
    private int configuredSortingOrder = short.MaxValue;

    public void Configure(Color color, bool useRadialIris, float softness, int sortingOrder)
    {
        configuredColor = color;
        configuredUseRadialIris = useRadialIris;
        configuredSoftness = Mathf.Max(MinSoftness, softness);
        configuredSortingOrder = sortingOrder;

        EnsureOverlayBuilt();
        ApplyCoverage(currentCoverage);
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return AnimateCoverage(currentCoverage, 1f, duration, keepVisibleAtEnd: true);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return AnimateCoverage(currentCoverage, 0f, duration, keepVisibleAtEnd: false);
    }

    private IEnumerator AnimateCoverage(float from, float to, float duration, bool keepVisibleAtEnd)
    {
        EnsureOverlayBuilt();
        SetOverlayVisible(true);
        SetRaycastBlocking(true);
        currentCoverage = Mathf.Clamp01(from);
        ApplyCoverage(currentCoverage);

        if (duration <= 0f)
        {
            currentCoverage = Mathf.Clamp01(to);
            ApplyCoverage(currentCoverage);
            FinalizeVisibility(keepVisibleAtEnd);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;

            float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            elapsed += Mathf.Min(deltaTime, MaxAnimationDelta);
            float t = Mathf.Clamp01(elapsed / duration);
            currentCoverage = Mathf.Lerp(from, to, t);
            ApplyCoverage(currentCoverage);
        }

        currentCoverage = Mathf.Clamp01(to);
        ApplyCoverage(currentCoverage);
        FinalizeVisibility(keepVisibleAtEnd);
    }

    private void FinalizeVisibility(bool keepVisibleAtEnd)
    {
        bool shouldRemainVisible = keepVisibleAtEnd || currentCoverage > 0f;
        SetRaycastBlocking(shouldRemainVisible);
        SetOverlayVisible(shouldRemainVisible);
    }

    private void ApplyCoverage(float coverage)
    {
        EnsureOverlayBuilt();

        if (configuredUseRadialIris && TryEnsureRadialMaterial())
        {
            overlayImage.texture = Texture2D.whiteTexture;
            overlayImage.color = Color.white;
            overlayImage.material = radialMaterial;

            float softness = Mathf.Max(MinSoftness, configuredSoftness);
            float aspect = GetAspectRatio();
            float maxRadius = Mathf.Sqrt((aspect * aspect) + 1f) + softness + 0.05f;
            float minRadius = -softness;
            float radius = Mathf.Lerp(maxRadius, minRadius, Mathf.Clamp01(coverage));

            radialMaterial.SetColor("_Color", configuredColor);
            radialMaterial.SetFloat("_Radius", radius);
            radialMaterial.SetFloat("_Softness", softness);
            radialMaterial.SetFloat("_Aspect", aspect);
        }
        else
        {
            overlayImage.material = null;
            overlayImage.texture = Texture2D.whiteTexture;

            Color color = configuredColor;
            color.a *= Mathf.Clamp01(coverage);
            overlayImage.color = color;
        }

        if (overlayCanvas != null)
            overlayCanvas.sortingOrder = configuredSortingOrder;
    }

    private bool TryEnsureRadialMaterial()
    {
        if (radialMaterial != null)
            return true;

        radialShader = radialShader != null
            ? radialShader
            : Resources.Load<Shader>(OverlayShaderResourcePath);

        if (radialShader == null)
            radialShader = Shader.Find(OverlayShaderName);

        if (radialShader == null)
        {
            if (!warnedMissingShader)
            {
                warnedMissingShader = true;
                Debug.LogWarning("[SceneTransitionOverlay] Radial fade shader not found. Falling back to alpha fade.");
            }

            return false;
        }

        radialMaterial = new Material(radialShader)
        {
            name = "SceneTransitionRadialFade (Instance)"
        };
        return true;
    }

    private void EnsureOverlayBuilt()
    {
        if (overlayCanvas != null && overlayImage != null)
            return;

        Transform overlayTransform = transform.Find(OverlayObjectName);
        if (overlayTransform == null)
        {
            GameObject overlayObject = new GameObject(
                OverlayObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            overlayTransform = overlayObject.transform;
            overlayTransform.SetParent(transform, false);
        }

        overlayCanvas = overlayTransform.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = configuredSortingOrder;
        overlayCanvas.pixelPerfect = false;

        overlayCanvasScaler = overlayTransform.GetComponent<CanvasScaler>();
        overlayCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        overlayCanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        overlayCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        overlayCanvasScaler.matchWidthOrHeight = 0.5f;

        overlayRaycaster = overlayTransform.GetComponent<GraphicRaycaster>();
        overlayRaycaster.enabled = true;

        RectTransform overlayRect = overlayTransform.GetComponent<RectTransform>();
        if (overlayRect == null)
            overlayRect = overlayTransform.gameObject.AddComponent<RectTransform>();

        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        Transform imageTransform = overlayTransform.Find("Overlay");
        if (imageTransform == null)
        {
            GameObject imageObject = new GameObject("Overlay", typeof(RectTransform), typeof(RawImage));
            imageTransform = imageObject.transform;
            imageTransform.SetParent(overlayTransform, false);
        }

        overlayImage = imageTransform.GetComponent<RawImage>();
        overlayImage.texture = Texture2D.whiteTexture;
        overlayImage.raycastTarget = false;

        RectTransform imageRect = imageTransform as RectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.localScale = Vector3.one;

        SetOverlayVisible(false);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayCanvas != null && overlayCanvas.gameObject.activeSelf != visible)
            overlayCanvas.gameObject.SetActive(visible);
    }

    private void SetRaycastBlocking(bool isBlocking)
    {
        if (overlayImage != null)
            overlayImage.raycastTarget = isBlocking;
    }

    private static float GetAspectRatio()
    {
        return Screen.height > 0
            ? (float)Screen.width / Screen.height
            : 1f;
    }

    private void OnDestroy()
    {
        if (radialMaterial != null)
            Destroy(radialMaterial);
    }
}
