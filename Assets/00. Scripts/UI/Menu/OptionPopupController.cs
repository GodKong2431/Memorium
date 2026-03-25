using UnityEngine;

[DisallowMultipleComponent]
public sealed class OptionPopupController : MonoBehaviour
{
    [SerializeField] private OptionPopupPanelUI popupPanel;
    [SerializeField] private bool closeOnStart = true;

    public static OptionPopupController Current { get; private set; }

    private void Awake()
    {
        if (closeOnStart)
            SetPopupOpen(false);
    }

    private void OnEnable()
    {
        Current = this;

        if (popupPanel != null)
            popupPanel.OutsideClicked += ClosePopup;
    }

    private void OnDisable()
    {
        if (popupPanel != null)
            popupPanel.OutsideClicked -= ClosePopup;

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

            popupPanel.BringToFront();
            popupPanel.SuppressClickForCurrentFrame();
        }
        else if (popupRoot.activeSelf)
        {
            popupRoot.SetActive(false);
        }

        OptionButtonUI currentButton = OptionButtonUI.Current;
        if (currentButton != null)
            currentButton.SetState(isOpen);
    }
}
