using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class PopupStackService
{
    public sealed class Handle
    {
    }

    public sealed class Request
    {
        public RectTransform PopupRoot;
        public RectTransform ContentRoot;
        public RectTransform OverlayParent;
        public Action OnRequestClose;
        public bool CloseOnOutside = true;
        public float OutsideCloseDelay = 0f;
        public Color BackdropColor = new Color(0f, 0f, 0f, 0.78431374f);
        public bool ReparentToOverlayParent = false;
        public bool StretchPopupToOverlayParent = false;
    }

    private struct RectTransformState
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector3 localScale;
        public Quaternion localRotation;

        public static RectTransformState Capture(RectTransform target)
        {
            return new RectTransformState
            {
                anchorMin = target.anchorMin,
                anchorMax = target.anchorMax,
                anchoredPosition = target.anchoredPosition,
                sizeDelta = target.sizeDelta,
                pivot = target.pivot,
                offsetMin = target.offsetMin,
                offsetMax = target.offsetMax,
                localScale = target.localScale,
                localRotation = target.localRotation
            };
        }

        public void Apply(RectTransform target)
        {
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.pivot = pivot;
            target.anchoredPosition = anchoredPosition;
            target.sizeDelta = sizeDelta;
            target.offsetMin = offsetMin;
            target.offsetMax = offsetMax;
            target.localScale = localScale;
            target.localRotation = localRotation;
        }
    }

    private sealed class Entry
    {
        public Handle Handle;
        public RectTransform PopupRoot;
        public RectTransform ContentRoot;
        public RectTransform OverlayParent;
        public Action OnRequestClose;
        public bool CloseOnOutside;
        public int IgnoreOutsideCloseUntilFrame;
        public float IgnoreOutsideCloseUntilTime;
        public Color BackdropColor;
        public bool ReparentToOverlayParent;
        public bool StretchPopupToOverlayParent;
        public RectTransform OriginalParent;
        public int OriginalSiblingIndex;
        public RectTransformState OriginalRectState;
    }

    private const string SharedBackdropName = "GlobalPopupBackdrop";

    private static readonly List<Entry> entries = new List<Entry>();

    private static OverlayPopupPanelUI sharedBackdrop;
    private static Image sharedBackdropImage;
    private static Handle currentBackdropHandle;

    public static bool IsPresent(Handle handle)
    {
        if (CleanupInvalidEntries())
            RefreshBackdrop(false);

        return FindEntry(handle) != null;
    }

    public static void Present(ref Handle handle, Request request)
    {
        CleanupInvalidEntries();

        if (request == null || request.PopupRoot == null)
        {
            Dismiss(ref handle);
            return;
        }

        if (handle == null)
            handle = new Handle();

        RemoveEntry(handle, true);

        RectTransform overlayParent = ResolveOverlayParent(request.PopupRoot, request.OverlayParent);
        RectTransform contentRoot = request.ContentRoot != null
            ? request.ContentRoot
            : request.PopupRoot;

        Entry entry = new Entry
        {
            Handle = handle,
            PopupRoot = request.PopupRoot,
            ContentRoot = contentRoot,
            OverlayParent = overlayParent,
            OnRequestClose = request.OnRequestClose,
            CloseOnOutside = request.CloseOnOutside,
            IgnoreOutsideCloseUntilFrame = Time.frameCount,
            IgnoreOutsideCloseUntilTime = Time.unscaledTime + Mathf.Max(0f, request.OutsideCloseDelay),
            BackdropColor = request.BackdropColor,
            ReparentToOverlayParent = request.ReparentToOverlayParent,
            StretchPopupToOverlayParent = request.StretchPopupToOverlayParent
        };

        ApplyOverlayPresentation(entry);
        entries.Add(entry);

        RefreshBackdrop(true);
    }

    public static void Dismiss(ref Handle handle)
    {
        bool removedInvalidEntries = CleanupInvalidEntries();

        if (handle == null)
        {
            if (removedInvalidEntries)
                RefreshBackdrop(false);

            return;
        }

        RemoveEntry(handle, true);
        handle = null;
        RefreshBackdrop(true);
    }

    public static void Release(ref Handle handle)
    {
        bool removedInvalidEntries = CleanupInvalidEntries();

        if (handle == null)
        {
            if (removedInvalidEntries)
                RefreshBackdrop(false);

            return;
        }

        RemoveEntry(handle, false);
        handle = null;
        RefreshBackdrop(true);
    }

    private static void HandleOutsideClicked()
    {
        if (CleanupInvalidEntries())
            RefreshBackdrop(false);

        Entry entry = GetTopEntry();
        if (entry == null)
            return;

        if (!entry.CloseOnOutside)
            return;

        if (Time.frameCount <= entry.IgnoreOutsideCloseUntilFrame)
            return;

        if (Time.unscaledTime < entry.IgnoreOutsideCloseUntilTime)
            return;

        entry.OnRequestClose?.Invoke();
    }

    private static void RefreshBackdrop(bool suppressClick)
    {
        Entry topEntry = GetTopEntry();
        if (topEntry == null)
        {
            currentBackdropHandle = null;

            if (sharedBackdrop != null && sharedBackdrop.gameObject.activeSelf)
                sharedBackdrop.gameObject.SetActive(false);

            return;
        }

        OverlayPopupPanelUI backdrop = EnsureSharedBackdrop(topEntry.OverlayParent);
        if (backdrop == null)
            return;

        RectTransform backdropRoot = backdrop.transform as RectTransform;
        RectTransform contentRoot = topEntry.ContentRoot != null
            ? topEntry.ContentRoot
            : topEntry.PopupRoot;

        backdrop.SetSheetRoot(contentRoot);

        if (sharedBackdropImage != null)
            sharedBackdropImage.color = topEntry.BackdropColor;

        if (!backdrop.gameObject.activeSelf)
            backdrop.gameObject.SetActive(true);

        if (backdropRoot != null)
            StretchToParent(backdropRoot);

        BringToFront(topEntry, backdropRoot);

        bool topEntryChanged = !ReferenceEquals(currentBackdropHandle, topEntry.Handle);
        if (suppressClick || topEntryChanged)
            backdrop.SuppressClickForCurrentFrame();

        currentBackdropHandle = topEntry.Handle;
    }

    private static OverlayPopupPanelUI EnsureSharedBackdrop(RectTransform parent)
    {
        if (parent == null)
            return null;

        if (sharedBackdrop == null)
        {
            currentBackdropHandle = null;

            GameObject backdropObject = new GameObject(
                SharedBackdropName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(OverlayPopupPanelUI));
            sharedBackdrop = backdropObject.GetComponent<OverlayPopupPanelUI>();
            sharedBackdropImage = backdropObject.GetComponent<Image>();
            sharedBackdrop.OutsideClicked += HandleOutsideClicked;
        }
        else if (sharedBackdropImage == null)
        {
            sharedBackdropImage = sharedBackdrop.GetComponent<Image>();
        }

        if (sharedBackdropImage != null)
            sharedBackdropImage.raycastTarget = true;

        RectTransform backdropRoot = sharedBackdrop.transform as RectTransform;
        if (backdropRoot == null)
            return null;

        if (backdropRoot.parent != parent)
            TrySetParent(backdropRoot, parent);

        sharedBackdrop.gameObject.layer = parent.gameObject.layer;
        StretchToParent(backdropRoot);
        return sharedBackdrop;
    }

    private static void BringToFront(Entry entry, RectTransform backdropRoot)
    {
        if (entry == null || entry.PopupRoot == null)
            return;

        TrySetAsLastSibling(entry.PopupRoot);

        if (backdropRoot == null)
            return;

        if (backdropRoot.parent == entry.PopupRoot.parent)
        {
            int popupSiblingIndex = entry.PopupRoot.GetSiblingIndex();
            TrySetSiblingIndex(backdropRoot, Mathf.Max(0, popupSiblingIndex - 1));
            return;
        }

        TrySetAsLastSibling(backdropRoot);
    }

    private static RectTransform ResolveOverlayParent(RectTransform popupRoot, RectTransform requestedParent)
    {
        if (requestedParent != null)
            return requestedParent;

        return popupRoot != null ? popupRoot.parent as RectTransform : null;
    }

    private static Entry GetTopEntry()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (IsValid(entries[i]))
                return entries[i];
        }

        return null;
    }

    private static Entry FindEntry(Handle handle)
    {
        if (handle == null)
            return null;

        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Handle, handle))
                return entries[i];
        }

        return null;
    }

    private static void RemoveEntry(Handle handle, bool restorePresentation)
    {
        if (handle == null)
            return;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(entries[i].Handle, handle))
                continue;

            if (restorePresentation)
                RestoreOriginalPresentation(entries[i]);

            entries.RemoveAt(i);
            return;
        }
    }

    private static bool CleanupInvalidEntries()
    {
        bool removedAny = false;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (IsValid(entries[i]))
                continue;

            RestoreOriginalPresentation(entries[i]);
            entries.RemoveAt(i);
            removedAny = true;
        }

        return removedAny;
    }

    private static bool IsValid(Entry entry)
    {
        if (entry == null || entry.Handle == null || entry.PopupRoot == null)
            return false;

        return entry.PopupRoot.gameObject.activeInHierarchy;
    }

    private static void ApplyOverlayPresentation(Entry entry)
    {
        if (entry == null || entry.PopupRoot == null || !entry.ReparentToOverlayParent)
            return;

        entry.OriginalParent = entry.PopupRoot.parent as RectTransform;
        entry.OriginalSiblingIndex = entry.PopupRoot.GetSiblingIndex();
        entry.OriginalRectState = RectTransformState.Capture(entry.PopupRoot);

        if (entry.OverlayParent != null && entry.PopupRoot.parent != entry.OverlayParent)
            TrySetParent(entry.PopupRoot, entry.OverlayParent);

        if (entry.StretchPopupToOverlayParent)
            StretchToParent(entry.PopupRoot);
    }

    private static void RestoreOriginalPresentation(Entry entry)
    {
        if (entry == null || entry.PopupRoot == null || !entry.ReparentToOverlayParent)
            return;

        if (!CanRestorePresentation(entry))
            return;

        if (entry.OriginalParent != null && entry.PopupRoot.parent != entry.OriginalParent)
            TrySetParent(entry.PopupRoot, entry.OriginalParent);

        entry.OriginalRectState.Apply(entry.PopupRoot);

        if (entry.OriginalParent == null)
            return;

        int maxSiblingIndex = Mathf.Max(0, entry.OriginalParent.childCount - 1);
        TrySetSiblingIndex(entry.PopupRoot, Mathf.Clamp(entry.OriginalSiblingIndex, 0, maxSiblingIndex));
    }

    private static void StretchToParent(RectTransform target)
    {
        if (target == null)
            return;

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
        target.anchoredPosition = Vector2.zero;
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
    }

    private static bool CanRestorePresentation(Entry entry)
    {
        if (entry == null || entry.PopupRoot == null)
            return false;

        if (!Application.isPlaying)
            return true;

        GameObject popupObject = entry.PopupRoot.gameObject;
        return popupObject != null && popupObject.activeInHierarchy;
    }

    private static bool TrySetParent(RectTransform target, RectTransform parent)
    {
        if (target == null || parent == null)
            return false;

        try
        {
            target.SetParent(parent, false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TrySetSiblingIndex(RectTransform target, int siblingIndex)
    {
        if (target == null)
            return false;

        try
        {
            target.SetSiblingIndex(siblingIndex);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TrySetAsLastSibling(RectTransform target)
    {
        if (target == null)
            return false;

        try
        {
            target.SetAsLastSibling();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
