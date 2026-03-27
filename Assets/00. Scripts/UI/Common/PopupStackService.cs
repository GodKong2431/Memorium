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

        RemoveEntry(handle);

        RectTransform overlayParent = ResolveOverlayParent(request.PopupRoot, request.OverlayParent);
        RectTransform contentRoot = request.ContentRoot != null
            ? request.ContentRoot
            : request.PopupRoot;

        entries.Add(new Entry
        {
            Handle = handle,
            PopupRoot = request.PopupRoot,
            ContentRoot = contentRoot,
            OverlayParent = overlayParent,
            OnRequestClose = request.OnRequestClose,
            CloseOnOutside = request.CloseOnOutside,
            IgnoreOutsideCloseUntilFrame = Time.frameCount,
            IgnoreOutsideCloseUntilTime = Time.unscaledTime + Mathf.Max(0f, request.OutsideCloseDelay),
            BackdropColor = request.BackdropColor
        });

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

        RemoveEntry(handle);
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
            backdropRoot.SetParent(parent, false);

        sharedBackdrop.gameObject.layer = parent.gameObject.layer;
        StretchToParent(backdropRoot);
        return sharedBackdrop;
    }

    private static void BringToFront(Entry entry, RectTransform backdropRoot)
    {
        if (entry == null || entry.PopupRoot == null)
            return;

        entry.PopupRoot.SetAsLastSibling();

        if (backdropRoot == null)
            return;

        if (backdropRoot.parent == entry.PopupRoot.parent)
        {
            int popupSiblingIndex = entry.PopupRoot.GetSiblingIndex();
            backdropRoot.SetSiblingIndex(Mathf.Max(0, popupSiblingIndex - 1));
            return;
        }

        backdropRoot.SetAsLastSibling();
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

    private static void RemoveEntry(Handle handle)
    {
        if (handle == null)
            return;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(entries[i].Handle, handle))
                continue;

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
}
