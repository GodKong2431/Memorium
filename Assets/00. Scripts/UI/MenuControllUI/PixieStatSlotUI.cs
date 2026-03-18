using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PixieStatSlotUI : MonoBehaviour
{
    private const float IconSizeRatio = 0.42f;
    private const float IconSizeMin = 28f;
    private const float IconSizeMax = 42f;
    private const float IconLeftInset = 18f;
    private const float TextGap = 12f;
    private const float TextRightPadding = 14f;
    private const float TextMinSize = 12f;
    private const float TextMaxSize = 24f;

    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    public Button Button => button;
    public Image IconImage => iconImage;
    public TMP_Text LabelText => labelText;
    public bool HasBindings => button != null && iconImage != null && labelText != null;

    public void ApplyResponsiveLayout()
    {
        if (!HasBindings)
            return;

        RectTransform slotRect = transform as RectTransform;
        RectTransform iconRect = iconImage.rectTransform;
        RectTransform labelRect = labelText.rectTransform;
        if (slotRect == null || iconRect == null || labelRect == null)
            return;

        float slotHeight = slotRect.rect.height;
        if (slotHeight <= 0f)
            slotHeight = slotRect.sizeDelta.y > 0f ? slotRect.sizeDelta.y : 100f;

        float iconSize = Mathf.Clamp(slotHeight * IconSizeRatio, IconSizeMin, IconSizeMax);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(IconLeftInset, 0f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconImage.preserveAspect = true;

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.offsetMin = new Vector2(IconLeftInset + iconSize + TextGap, 0f);
        labelRect.offsetMax = new Vector2(-TextRightPadding, 0f);

        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = TextMinSize;
        labelText.fontSizeMax = TextMaxSize;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
    }
}
