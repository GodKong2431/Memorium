using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class SheetHeaderGestureHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BottomPanelController controller;

    public void Bind(BottomPanelController target)
    {
        controller = target;
        EnsureRaycastTarget();
    }

    private void Awake()
    {
        EnsureRaycastTarget();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        controller?.HandleHeaderPointerDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        controller?.HandleHeaderPointerUp();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        controller?.HandleHeaderBeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        controller?.HandleHeaderDrag(eventData.delta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        controller?.HandleHeaderEndDrag();
    }

    private void EnsureRaycastTarget()
    {
        Graphic graphic = GetComponent<Graphic>();
        if (graphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            return;
        }

        graphic.raycastTarget = true;
    }
}
