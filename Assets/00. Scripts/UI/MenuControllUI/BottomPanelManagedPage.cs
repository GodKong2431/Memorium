using UnityEngine;

[DisallowMultipleComponent]
public class BottomPanelManagedPage : MonoBehaviour
{
    [Header("시트 표시")]
    [SerializeField] private string pageTitle;
    [SerializeField] private bool showSubMenu;
    [SerializeField] private RectTransform subMenuRootOverride;
    [SerializeField] private bool useLegacyScrollResize;

    public string PageTitle => pageTitle;
    public bool ShowSubMenu => showSubMenu;
    public RectTransform SubMenuRootOverride => subMenuRootOverride;
    public bool UseLegacyScrollResize => useLegacyScrollResize;
}
