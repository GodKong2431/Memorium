using UnityEngine;
using UnityEngine.UI;

public class PopupOpenBtn : MonoBehaviour
{
    [Header("Popup Settings")]
    public PopupMode myPopupMode;

    private Button btnOpen;

    private void Start()
    {
        btnOpen = GetComponent<Button>();
        btnOpen.onClick.RemoveAllListeners();
        btnOpen.onClick.AddListener(OnClickOpenPopup);
    }

    private void OnClickOpenPopup()
    {
        if (GlobalPopupManager.Instance != null)
        {
            GlobalPopupManager.Instance.OpenPopupMode(myPopupMode);
        }
    }
}