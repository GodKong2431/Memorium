using UnityEngine;

[DisallowMultipleComponent]
public sealed class OptionPopupController : MonoBehaviour
{
    [SerializeField] private OverlayPopupPanelUI popupPanel;
    [SerializeField] private bool closeOnStart = true;

    public static OptionPopupController Current { get; private set; }

    private PopupStackService.Handle popupHandle;

    private void Awake()
    {
        if (closeOnStart)
            SetPopupOpen(false);
    }

    private void OnEnable()
    {
        Current = this;
    }

    private void OnDisable()
    {
        PopupStackService.Dismiss(ref popupHandle);

        if (Current == this)
            Current = null;
    }

    public void HandleOptionToggleChanged(bool isOn)
    {
        if (popupPanel == null)
            return;

        SetPopupOpen(isOn);
    }

    private void ClosePopup()
    {
        SetPopupOpen(false);
    }

    private void SetPopupOpen(bool isOpen)
    {
        if (popupPanel == null)
            return;

        GameObject popupRoot = popupPanel.gameObject;
        RectTransform sheetRoot = popupPanel.SheetRoot;
        GameObject sheetObject = sheetRoot != null ? sheetRoot.gameObject : null;

        if (isOpen)
        {
            if (!popupRoot.activeSelf)
                popupRoot.SetActive(true);

            if (sheetObject != null && !sheetObject.activeSelf)
                sheetObject.SetActive(true);

            PopupStackService.Present(ref popupHandle, new PopupStackService.Request
            {
                PopupRoot = popupRoot.transform as RectTransform,
                ContentRoot = sheetRoot != null ? sheetRoot : popupRoot.transform as RectTransform,
                OverlayParent = popupRoot.transform.parent as RectTransform,
                OnRequestClose = ClosePopup,
                CloseOnOutside = true
            });
        }
        else if (popupRoot.activeSelf)
        {
            PopupStackService.Dismiss(ref popupHandle);
            popupRoot.SetActive(false);
        }

        OptionButtonUI currentButton = OptionButtonUI.Current;
        if (currentButton != null)
            currentButton.SetState(isOpen);
    }
}
