using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 시트와 팝업 모드를 전환하며 탭/페이지 UI를 관리하는 컨트롤러.
/// 패널 높이와 페이지 부모를 전환해 시트 모드와 팝업 모드를 연결한다.
/// </summary>
public class BottomSheetController : MonoBehaviour
{
    [System.Serializable]
    public struct TabPagePair
    {
        public Toggle tabToggle;
        public GameObject pageObject;
    }

    [Header("Main Navigation")]
    public TabPagePair[] tabPages;
    public ToggleGroup toggleGroup;

    [Header("Bottom Sheet Setup")]
    public RectTransform panelRect;
    public RectTransform skillPanelRect;
    public float openHeight = 800f;

    public Button btnArrowUp;    // 시트 -> 팝업 확장 버튼
    public Button btnArrowDown;  // 팝업 -> 시트 복귀 버튼

    [Header("Reparenting Targets")]
    public Transform sheetContentParent;
    public Transform popupContentParent;

    private float targetHeight = 0f;
    private bool isPopupOpen = false;
    private float skillPanelStartY;

    private GameObject currentPage;

    void Start()
    {
        InitializeUI();
        BindEvents();
    }

    void Update()
    {
        UpdateTargetHeight();
        AnimatePanel();
    }

    private void InitializeUI()
    {
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 0f);

        if (skillPanelRect != null)
            skillPanelStartY = skillPanelRect.anchoredPosition.y;

        if (GlobalPopupManager.Instance != null)
            GlobalPopupManager.Instance.ClosePopup();

        foreach (var pair in tabPages)
        {
            if (pair.pageObject != null)
                pair.pageObject.SetActive(false);
        }
    }

    private void BindEvents()
    {
        if (btnArrowUp != null) btnArrowUp.onClick.AddListener(OpenPopup);
        if (btnArrowDown != null) btnArrowDown.onClick.AddListener(ReturnToSheet);

        if (GlobalPopupManager.Instance != null && GlobalPopupManager.Instance.btnCommonClose != null)
        {
            GlobalPopupManager.Instance.btnCommonClose.onClick.AddListener(CloseAll);
        }

        for (int i = 0; i < tabPages.Length; i++)
        {
            int index = i;
            if (tabPages[i].tabToggle != null)
            {
                tabPages[i].tabToggle.onValueChanged.AddListener((isOn) => OnMainTabChanged(index, isOn));
            }
        }
    }

    private void UpdateTargetHeight()
    {
        if (isPopupOpen)
            targetHeight = 0f;
        else
            targetHeight = (toggleGroup != null && toggleGroup.AnyTogglesOn()) ? openHeight : 0f;
    }

    private void AnimatePanel()
    {
        if (panelRect == null) return;

        float currentH = panelRect.sizeDelta.y;
        if (Mathf.Abs(currentH - targetHeight) > 0.1f)
        {
            currentH = Mathf.Lerp(currentH, targetHeight, Time.deltaTime * 15f);
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, currentH);
        }

        if (skillPanelRect != null)
            skillPanelRect.anchoredPosition = new Vector2(skillPanelRect.anchoredPosition.x, skillPanelStartY + currentH);
    }

    /// <summary>
    /// 하단 탭 선택/해제 시 호출되는 콜백
    /// </summary>
    public void OnMainTabChanged(int tabIndex, bool isOn)
    {
        if (isOn)
        {
            isPopupOpen = false;
            if (GlobalPopupManager.Instance != null) GlobalPopupManager.Instance.ClosePopup();

            for (int i = 0; i < tabPages.Length; i++)
            {
                if (tabPages[i].pageObject != null)
                {
                    bool isActive = (i == tabIndex);
                    tabPages[i].pageObject.SetActive(isActive);

                    if (isActive)
                    {
                        currentPage = tabPages[i].pageObject;
                        if (sheetContentParent != null)
                            currentPage.transform.SetParent(sheetContentParent, false);
                    }
                }
            }
        }
        else
        {
            if (toggleGroup != null && !toggleGroup.AnyTogglesOn() && tabPages[tabIndex].pageObject != null)
            {
                tabPages[tabIndex].pageObject.SetActive(false);
                if (currentPage == tabPages[tabIndex].pageObject)
                    currentPage = null;
            }
        }
    }

    /// <summary>
    /// 시트 모드에서 팝업 모드로 전환
    /// </summary>
    private void OpenPopup()
    {
        isPopupOpen = true;

        if (GlobalPopupManager.Instance != null)
            GlobalPopupManager.Instance.OpenPopupMode(PopupMode.BottomSheet);

        if (currentPage != null && popupContentParent != null)
        {
            currentPage.transform.SetParent(popupContentParent, false);
        }
    }

    /// <summary>
    /// 팝업 모드에서 시트 모드로 복귀
    /// </summary>
    private void ReturnToSheet()
    {
        isPopupOpen = false;

        if (GlobalPopupManager.Instance != null)
            GlobalPopupManager.Instance.ClosePopup();

        if (currentPage != null && sheetContentParent != null)
        {
            currentPage.transform.SetParent(sheetContentParent, false);
        }
    }

    /// <summary>
    /// 팝업과 시트를 모두 닫고 초기 상태로 되돌림
    /// </summary>
    private void CloseAll()
    {
        isPopupOpen = false;

        if (GlobalPopupManager.Instance != null)
            GlobalPopupManager.Instance.ClosePopup();

        if (toggleGroup != null) toggleGroup.SetAllTogglesOff();

        if (currentPage != null && sheetContentParent != null)
        {
            currentPage.transform.SetParent(sheetContentParent, false);
            currentPage = null;
        }
    }
}
