using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public sealed class OptionButtonUI : MonoBehaviour
{
    private Toggle cachedToggle;

    public static OptionButtonUI Current { get; private set; }

    public Toggle Toggle => cachedToggle != null ? cachedToggle : cachedToggle = GetComponent<Toggle>();

    private void OnEnable()
    {
        Current = this;
        Toggle.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
        Toggle.onValueChanged.RemoveListener(HandleValueChanged);
    }

    public void SetState(bool isOn)
    {
        Toggle.SetIsOnWithoutNotify(isOn);
    }

    private void HandleValueChanged(bool isOn)
    {
        OptionPopupController currentPopup = OptionPopupController.Current;
        if (currentPopup == null)
            return;

        currentPopup.HandleOptionToggleChanged(isOn);
    }
}
