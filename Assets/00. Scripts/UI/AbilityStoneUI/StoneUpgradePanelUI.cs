using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoneUpgradePanelUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI[] probabilityTexts = new TextMeshProUGUI[2];
    [SerializeField] private StoneSlotItemUI[] slotItems = new StoneSlotItemUI[3];
    [SerializeField] private Button nextGradeButton;
    [SerializeField] private TextMeshProUGUI nextGradeButtonText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI rerollButtonText;
    [SerializeField] private TextMeshProUGUI resetButtonText;
    [SerializeField] private GameObject reconfigurePopupRoot;
    [SerializeField] private TextMeshProUGUI reconfigureInfoText;
    [SerializeField] private TextMeshProUGUI[] reconfigureSlotTexts = new TextMeshProUGUI[3];
    [SerializeField] private Image[] reconfigureSlotImages = new Image[3];
    [SerializeField] private Button reconfigureConfirmButton;
    [SerializeField] private TextMeshProUGUI reconfigureConfirmCostText;
    [SerializeField] private Button reconfigureCancelButton;
    [SerializeField] private RectTransform reconfigurePopupContentRoot;
    [SerializeField] private GameObject resetPopupRoot;
    [SerializeField] private TextMeshProUGUI resetInfoText;
    [SerializeField] private TextMeshProUGUI[] resetSlotTexts = new TextMeshProUGUI[3];
    [SerializeField] private Image[] resetSlotImages = new Image[3];
    [SerializeField] private Button resetConfirmButton;
    [SerializeField] private TextMeshProUGUI resetConfirmCostText;
    [SerializeField] private Button resetCancelButton;
    [SerializeField] private RectTransform resetPopupContentRoot;

    public event Action OutsideClicked;
    public event Action PopupOutsideClicked;

    public RectTransform PanelRoot => panelRoot;
    public TextMeshProUGUI GradeText => gradeText;
    public TextMeshProUGUI[] ProbabilityTexts => probabilityTexts;
    public StoneSlotItemUI[] SlotItems => slotItems;
    public Button NextGradeButton => nextGradeButton;
    public TextMeshProUGUI NextGradeButtonText => nextGradeButtonText;
    public Button RerollButton => rerollButton;
    public Button ResetButton => resetButton;
    public TextMeshProUGUI RerollButtonText => rerollButtonText;
    public TextMeshProUGUI ResetButtonText => resetButtonText;
    public GameObject ReconfigurePopupRoot => reconfigurePopupRoot;
    public TextMeshProUGUI ReconfigureInfoText => reconfigureInfoText;
    public TextMeshProUGUI[] ReconfigureSlotTexts => reconfigureSlotTexts;
    public Image[] ReconfigureSlotImages => reconfigureSlotImages;
    public Button ReconfigureConfirmButton => reconfigureConfirmButton;
    public TextMeshProUGUI ReconfigureConfirmCostText => reconfigureConfirmCostText;
    public Button ReconfigureCancelButton => reconfigureCancelButton;
    public RectTransform ReconfigurePopupContentRoot => reconfigurePopupContentRoot;
    public GameObject ResetPopupRoot => resetPopupRoot;
    public TextMeshProUGUI ResetInfoText => resetInfoText;
    public TextMeshProUGUI[] ResetSlotTexts => resetSlotTexts;
    public Image[] ResetSlotImages => resetSlotImages;
    public Button ResetConfirmButton => resetConfirmButton;
    public TextMeshProUGUI ResetConfirmCostText => resetConfirmCostText;
    public Button ResetCancelButton => resetCancelButton;
    public RectTransform ResetPopupContentRoot => resetPopupContentRoot;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 확인 팝업이 열려 있으면 그 팝업부터 먼저 닫는다.
        if (eventData == null)
        {
            return;
        }

        if (IsOutsideActivePopup(reconfigurePopupRoot, reconfigurePopupContentRoot, eventData)
            || IsOutsideActivePopup(resetPopupRoot, resetPopupContentRoot, eventData))
        {
            PopupOutsideClicked?.Invoke();
            return;
        }

        if (panelRoot != null
            && !RectTransformUtility.RectangleContainsScreenPoint(panelRoot, eventData.position, eventData.pressEventCamera))
        {
            OutsideClicked?.Invoke();
        }
    }

    public void HidePopups()
    {
        if (reconfigurePopupRoot != null)
        {
            reconfigurePopupRoot.SetActive(false);
        }

        if (resetPopupRoot != null)
        {
            resetPopupRoot.SetActive(false);
        }
    }

    private static bool IsOutsideActivePopup(GameObject popupRoot, RectTransform popupContentRoot, PointerEventData eventData)
    {
        return popupRoot != null
            && popupRoot.activeInHierarchy
            && popupContentRoot != null
            && !RectTransformUtility.RectangleContainsScreenPoint(popupContentRoot, eventData.position, eventData.pressEventCamera);
    }
}
