using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class OptionPopupPanelUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform sheetRoot;

    public event Action OutsideClicked;

    private int suppressClickFrame = -1;

    public RectTransform SheetRoot => sheetRoot;

    public void BringToFront()
    {
        if (transform is RectTransform rect)
            rect.SetAsLastSibling();
    }

    public void SuppressClickForCurrentFrame()
    {
        suppressClickFrame = Time.frameCount;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || sheetRoot == null)
            return;

        if (Time.frameCount == suppressClickFrame)
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(sheetRoot, eventData.position, eventData.pressEventCamera))
            OutsideClicked?.Invoke();
    }
}
