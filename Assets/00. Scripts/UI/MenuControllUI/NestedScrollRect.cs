using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NestedScrollRect : ScrollRect
{
    [Header("Parent Scroll")]
    [SerializeField] private ScrollRect parentScrollRect;
    [SerializeField] private bool routeToParentAtTop = true;
    [SerializeField] private bool routeToParentAtBottom = true;
    [SerializeField] private bool routeToParentWhenContentFits = true;
    [SerializeField, Range(0.9f, 1f)] private float topNormalizedThreshold = 0.98f;
    [SerializeField, Range(0f, 0.1f)] private float bottomNormalizedThreshold = 0.02f;

    private bool isRoutingToParent;
    private bool startedLocalDrag;
    private bool startedParentDrag;

    protected override void Awake()
    {
        base.Awake();
        CacheParentScrollRect();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheParentScrollRect();
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        CacheParentScrollRect();

        isRoutingToParent = false;
        startedLocalDrag = false;
        startedParentDrag = false;

        base.OnInitializePotentialDrag(eventData);
        parentScrollRect?.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        CacheParentScrollRect();
        base.OnBeginDrag(eventData);
        startedLocalDrag = true;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        CacheParentScrollRect();

        if (isRoutingToParent)
        {
            parentScrollRect?.OnDrag(eventData);
            return;
        }

        if (ShouldRouteToParent(eventData))
        {
            RouteDragToParent(eventData);
            return;
        }

        base.OnDrag(eventData);
        startedLocalDrag = true;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (isRoutingToParent)
        {
            if (startedParentDrag)
                parentScrollRect?.OnEndDrag(eventData);

            isRoutingToParent = false;
            startedParentDrag = false;
            return;
        }

        if (!startedLocalDrag)
            return;

        base.OnEndDrag(eventData);
        startedLocalDrag = false;
    }

    public override void OnScroll(PointerEventData data)
    {
        CacheParentScrollRect();

        if (ShouldRouteScrollToParent(data))
        {
            parentScrollRect?.OnScroll(data);
            return;
        }

        base.OnScroll(data);
    }

    private void CacheParentScrollRect()
    {
        if (parentScrollRect != null)
            return;

        ScrollRect[] parentScrollRects = GetComponentsInParent<ScrollRect>(true);
        for (int i = 0; i < parentScrollRects.Length; i++)
        {
            ScrollRect candidate = parentScrollRects[i];
            if (candidate == null || candidate == this)
                continue;

            parentScrollRect = candidate;
            return;
        }
    }

    private bool ShouldRouteToParent(PointerEventData eventData)
    {
        if (!CanRouteToParent())
            return false;

        Vector2 delta = eventData.delta;
        if (Mathf.Abs(delta.y) <= Mathf.Abs(delta.x))
            return false;

        if (!HasScrollableVerticalContent())
            return routeToParentWhenContentFits;

        if (routeToParentAtTop && verticalNormalizedPosition >= topNormalizedThreshold && delta.y < 0f)
            return true;

        if (routeToParentAtBottom && verticalNormalizedPosition <= bottomNormalizedThreshold && delta.y > 0f)
            return true;

        return false;
    }

    private bool ShouldRouteScrollToParent(PointerEventData eventData)
    {
        if (!CanRouteToParent())
            return false;

        float scrollY = eventData.scrollDelta.y;
        if (Mathf.Approximately(scrollY, 0f))
            return false;

        if (!HasScrollableVerticalContent())
            return routeToParentWhenContentFits;

        if (routeToParentAtTop && verticalNormalizedPosition >= topNormalizedThreshold && scrollY < 0f)
            return true;

        if (routeToParentAtBottom && verticalNormalizedPosition <= bottomNormalizedThreshold && scrollY > 0f)
            return true;

        return false;
    }

    private bool CanRouteToParent()
    {
        return vertical && parentScrollRect != null && parentScrollRect.vertical;
    }

    private bool HasScrollableVerticalContent()
    {
        if (content == null || viewport == null)
            return true;

        return content.rect.height > viewport.rect.height + 0.5f;
    }

    private void RouteDragToParent(PointerEventData eventData)
    {
        if (startedLocalDrag)
        {
            base.OnEndDrag(eventData);
            startedLocalDrag = false;
        }

        StopMovement();

        if (!startedParentDrag)
        {
            BeginParentDragFromPreviousPointerPosition(eventData);
            startedParentDrag = true;
        }

        isRoutingToParent = true;
        parentScrollRect?.OnDrag(eventData);
    }

    private void BeginParentDragFromPreviousPointerPosition(PointerEventData eventData)
    {
        if (parentScrollRect == null || eventData == null)
            return;

        Vector2 currentPosition = eventData.position;
        eventData.position = currentPosition - eventData.delta;
        parentScrollRect.OnBeginDrag(eventData);
        eventData.position = currentPosition;
    }
}
