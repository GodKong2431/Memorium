using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private const float EdgeThresholdPixels = 1f;

    [SerializeField] private bool applyLeft = true;
    [SerializeField] private bool applyRight = true;
    [SerializeField] private bool applyTop = true;
    [SerializeField] private bool applyBottom = true;
    [SerializeField] private Vector4 extraPadding;

    private RectTransform cachedRectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    public void ApplyNow()
    {
        ApplySafeArea(force: true);
    }

    public Rect GetAppliedSafeArea()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0)
            return Rect.zero;

        return GetAppliedSafeArea(screenWidth, screenHeight);
    }

    private void Awake()
    {
        ApplySafeArea(force: true);
    }

    private void OnEnable()
    {
        ApplySafeArea(force: true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplySafeArea(force: true);
    }
#endif

    private void Update()
    {
        if (Application.isPlaying)
            return;

        ApplySafeArea(force: false);
    }

    private void ApplySafeArea(bool force)
    {
        if (!TryResolveRectTransform(out RectTransform rectTransform))
            return;

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0)
            return;

        Rect safeArea = GetAppliedSafeArea(screenWidth, screenHeight);

        Vector2Int screenSize = new Vector2Int(screenWidth, screenHeight);
        if (!force && lastSafeArea == safeArea && lastScreenSize == screenSize)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenWidth;
        anchorMin.y /= screenHeight;
        anchorMax.x /= screenWidth;
        anchorMax.y /= screenHeight;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private Rect GetAppliedSafeArea(int screenWidth, int screenHeight)
    {
        Rect safeArea = NormalizeSafeArea(Screen.safeArea, screenWidth, screenHeight);

        GetSafeAreaInsets(safeArea, screenWidth, screenHeight, out float leftInset, out float rightInset, out float topInset, out float bottomInset);
        GetVerticalCutoutInsets(screenWidth, screenHeight, out float cutoutTopInset, out float cutoutBottomInset);

        // Some Android notch devices report the portrait top inset as bottom-safe-area space.
        ResolveOppositeEdgeMismatch(ref bottomInset, ref topInset, cutoutBottomInset, cutoutTopInset);

        float resolvedTopInset = Mathf.Max(topInset, cutoutTopInset);
        float resolvedBottomInset = Mathf.Max(bottomInset, cutoutBottomInset);

        float xMin = leftInset;
        float xMax = screenWidth - rightInset;
        float yMin = resolvedBottomInset;
        float yMax = screenHeight - resolvedTopInset;

        if (xMax > xMin && yMax > yMin)
            safeArea = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

        return BuildTargetSafeArea(safeArea, screenWidth, screenHeight);
    }

    private static Rect NormalizeSafeArea(Rect safeArea, int screenWidth, int screenHeight)
    {
        float xMin = Mathf.Clamp(safeArea.xMin, 0f, screenWidth);
        float xMax = Mathf.Clamp(safeArea.xMax, 0f, screenWidth);
        float yMin = Mathf.Clamp(safeArea.yMin, 0f, screenHeight);
        float yMax = Mathf.Clamp(safeArea.yMax, 0f, screenHeight);

        if (xMax <= xMin || yMax <= yMin)
            return new Rect(0f, 0f, screenWidth, screenHeight);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void GetSafeAreaInsets(Rect safeArea, int screenWidth, int screenHeight, out float leftInset, out float rightInset, out float topInset, out float bottomInset)
    {
        leftInset = Mathf.Clamp(safeArea.xMin, 0f, screenWidth);
        rightInset = Mathf.Clamp(screenWidth - safeArea.xMax, 0f, screenWidth);
        topInset = Mathf.Clamp(screenHeight - safeArea.yMax, 0f, screenHeight);
        bottomInset = Mathf.Clamp(safeArea.yMin, 0f, screenHeight);
    }

    private static void GetVerticalCutoutInsets(int screenWidth, int screenHeight, out float topInset, out float bottomInset)
    {
        topInset = 0f;
        bottomInset = 0f;

        Rect[] cutouts = Screen.cutouts;
        if (cutouts == null || cutouts.Length == 0)
            return;

        for (int i = 0; i < cutouts.Length; i++)
        {
            Rect cutout = NormalizeCutout(cutouts[i], screenWidth, screenHeight);
            if (cutout.width <= 0f || cutout.height <= 0f)
                continue;

            if (cutout.yMin <= EdgeThresholdPixels)
                bottomInset = Mathf.Max(bottomInset, cutout.yMax);

            if (screenHeight - cutout.yMax <= EdgeThresholdPixels)
                topInset = Mathf.Max(topInset, screenHeight - cutout.yMin);
        }
    }

    private static Rect NormalizeCutout(Rect cutout, int screenWidth, int screenHeight)
    {
        float xMin = Mathf.Clamp(cutout.xMin, 0f, screenWidth);
        float xMax = Mathf.Clamp(cutout.xMax, 0f, screenWidth);
        float yMin = Mathf.Clamp(cutout.yMin, 0f, screenHeight);
        float yMax = Mathf.Clamp(cutout.yMax, 0f, screenHeight);

        if (xMax <= xMin || yMax <= yMin)
            return Rect.zero;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void ResolveOppositeEdgeMismatch(ref float startInset, ref float endInset, float cutoutStartInset, float cutoutEndInset)
    {
        bool hasCutoutAtStart = cutoutStartInset > EdgeThresholdPixels;
        bool hasCutoutAtEnd = cutoutEndInset > EdgeThresholdPixels;

        if (hasCutoutAtStart && !hasCutoutAtEnd && startInset + EdgeThresholdPixels < cutoutStartInset && endInset > EdgeThresholdPixels)
        {
            endInset = 0f;
            return;
        }

        if (hasCutoutAtEnd && !hasCutoutAtStart && endInset + EdgeThresholdPixels < cutoutEndInset && startInset > EdgeThresholdPixels)
            startInset = 0f;
    }

    private Rect BuildTargetSafeArea(Rect safeArea, int screenWidth, int screenHeight)
    {
        float xMin = applyLeft ? safeArea.xMin : 0f;
        float xMax = applyRight ? safeArea.xMax : screenWidth;
        float yMin = applyBottom ? safeArea.yMin : 0f;
        float yMax = applyTop ? safeArea.yMax : screenHeight;

        xMin += extraPadding.x;
        yMax -= extraPadding.y;
        xMax -= extraPadding.z;
        yMin += extraPadding.w;

        xMin = Mathf.Clamp(xMin, 0f, screenWidth);
        xMax = Mathf.Clamp(xMax, 0f, screenWidth);
        yMin = Mathf.Clamp(yMin, 0f, screenHeight);
        yMax = Mathf.Clamp(yMax, 0f, screenHeight);

        if (xMax < xMin)
            xMax = xMin;

        if (yMax < yMin)
            yMax = yMin;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private bool TryResolveRectTransform(out RectTransform rectTransform)
    {
        if (cachedRectTransform == null)
            cachedRectTransform = transform as RectTransform;

        rectTransform = cachedRectTransform;
        return rectTransform != null;
    }
}
