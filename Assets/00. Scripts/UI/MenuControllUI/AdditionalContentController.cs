using UnityEngine;

public class AdditionalContentController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BottomPanelController bottomPanelController;

    private void Awake()
    {
        if (bottomPanelController == null)
            bottomPanelController = GetComponentInParent<BottomPanelController>();
    }

    public void OpenContent(RectTransform content)
    {
        if (content == null || bottomPanelController == null)
            return;

        bottomPanelController.OpenManagedContent(content);
    }
}

