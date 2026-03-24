using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EquipmentUiRoot : MonoBehaviour
{
    [Header("Current")]
    [SerializeField] private RectTransform currentRoot;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button mergeButton;
    [SerializeField] private RectTransform mergeResultPanelRoot;
    [SerializeField] private TextMeshProUGUI mergeResultTitle;
    [SerializeField] private ScrollRect mergeResultScrollView;
    [SerializeField] private GameObject mergeResultItemPrefab;

    [Header("Reinforce")]
    [SerializeField] private RectTransform reinforcePanelRoot;
    [SerializeField] private RectTransform reinforcePopupRoot;
    [SerializeField] private Button reinforceButton;
    [SerializeField] private TextMeshProUGUI reinforceCostText;
    [SerializeField] private Button previewButton;
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private EquipReinforceStatRowUI statRowTemplate;

    private EquipCurrentUIController currentController;
    private EquipReinforceUIController reinforceController;

    private void Awake()
    {
        EnsureBindings();
    }

    private void OnEnable()
    {
        EnsureBindings();
    }

    public bool TryGetReinforceController(out EquipReinforceUIController controller)
    {
        if (reinforceController == null)
            EnsureBindings();

        controller = reinforceController;
        return controller != null;
    }

    private void EnsureBindings()
    {
        currentController = GetOrAddComponent(currentController);
        reinforceController = GetOrAddComponent(reinforceController);

        ConfigureReinforceController();
        ConfigureCurrentController();
        BindTabs();
    }

    private void ConfigureCurrentController()
    {
        if (currentController == null)
            return;

        GameObject resultItemPrefab = mergeResultItemPrefab != null ? mergeResultItemPrefab : itemPrefab;
        currentController.ApplyBindings(
            EquipmentHandler.Instance,
            reinforceController,
            mergeButton,
            equipButton,
            currentRoot,
            itemPrefab,
            mergeResultPanelRoot,
            mergeResultTitle,
            mergeResultScrollView,
            resultItemPrefab);
    }

    private void ConfigureReinforceController()
    {
        if (reinforceController == null)
            return;

        ResolvePreviewParts(
            out Image previewIcon,
            out RectTransform previewLevelRoot,
            out RectTransform previewTierRoot,
            out RectTransform previewTierStarTemplate,
            out Image[] previewFrames);

        reinforceController.ApplyBindings(
            EquipmentHandler.Instance,
            reinforcePanelRoot,
            reinforcePopupRoot,
            reinforceButton,
            reinforceCostText,
            previewButton,
            previewIcon,
            previewNameText,
            previewLevelRoot,
            previewTierRoot,
            previewTierStarTemplate,
            previewFrames,
            statRowTemplate);
    }

    private void BindTabs()
    {
        EquipTabUIController[] tabs = GetComponentsInChildren<EquipTabUIController>(true);
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null)
                tabs[i].SetReinforceController(reinforceController);
        }
    }

    private void ResolvePreviewParts(
        out Image previewIcon,
        out RectTransform previewLevelRoot,
        out RectTransform previewTierRoot,
        out RectTransform previewTierStarTemplate,
        out Image[] previewFrames)
    {
        previewIcon = null;
        previewLevelRoot = null;
        previewTierRoot = null;
        previewTierStarTemplate = null;

        List<Image> frames = new List<Image>();

        if (previewButton == null)
        {
            previewFrames = frames.ToArray();
            return;
        }

        Transform previewRoot = previewButton.transform;
        for (int i = 0; i < previewRoot.childCount; i++)
        {
            RectTransform child = previewRoot.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            if (child.GetComponent<HorizontalLayoutGroup>() != null)
            {
                previewTierRoot = child;
                if (child.childCount > 0)
                    previewTierStarTemplate = child.GetChild(0) as RectTransform;

                continue;
            }

            if (previewLevelRoot == null && child.GetComponentInChildren<TextMeshProUGUI>(true) != null)
            {
                previewLevelRoot = child;
                continue;
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
                continue;

            if (IsStretchRect(child))
            {
                frames.Add(image);
                continue;
            }

            if (previewIcon == null)
                previewIcon = image;
        }

        previewFrames = frames.ToArray();
    }

    private static bool IsStretchRect(RectTransform rect)
    {
        return Approximately(rect.anchorMin, Vector2.zero) &&
               Approximately(rect.anchorMax, Vector2.one);
    }

    private static bool Approximately(Vector2 lhs, Vector2 rhs)
    {
        return Mathf.Approximately(lhs.x, rhs.x) &&
               Mathf.Approximately(lhs.y, rhs.y);
    }

    private T GetOrAddComponent<T>(T component)
        where T : Component
    {
        if (component != null)
            return component;

        if (!TryGetComponent(out component))
            component = gameObject.AddComponent<T>();

        return component;
    }
}
