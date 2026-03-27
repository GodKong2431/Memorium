using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SafeAreaFitter : MonoBehaviour
{
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
        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
            safeArea = new Rect(0f, 0f, screenWidth, screenHeight);

        return BuildTargetSafeArea(safeArea, screenWidth, screenHeight);
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
