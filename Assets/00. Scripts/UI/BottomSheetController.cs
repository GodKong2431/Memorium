using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 하단 탭과 바텀 시트/팝업 간의 UI 상태 및 애니메이션을 관리하는 매니저 클래스.
/// 하단 탭 전환 시 페이지를 교체하며, 시트와 팝업 간의 부모 변경을 담당합니다.
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

    [Header("Popup Integration")]
    public GlobalPopupManager popupManager;

    public Button btnArrowUp;    // 시트 -> 팝업 확장 버튼
    public Button btnArrowDown;  // 팝업 -> 시트 축소 버튼

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

        if (popupManager != null)
            popupManager.ClosePopup();

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

        if (popupManager != null && popupManager.btnCommonClose != null)
        {
            popupManager.btnCommonClose.onClick.AddListener(CloseAll);
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
    /// 하단 메인 탭 전환 시 호출되는 콜백
    /// </summary>
    public void OnMainTabChanged(int tabIndex, bool isOn)
    {
        if (isOn)
        {
            isPopupOpen = false;
            if (popupManager != null) popupManager.ClosePopup();

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

        if (popupManager != null)
            popupManager.OpenBottomSheetMode();

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

        if (popupManager != null)
            popupManager.ClosePopup();

        if (currentPage != null && sheetContentParent != null)
        {
            currentPage.transform.SetParent(sheetContentParent, false);
        }
    }

    /// <summary>
    /// 팝업 및 시트를 모두 종료하고 초기 상태로 되돌림
    /// </summary>
    private void CloseAll()
    {
        isPopupOpen = false;

        if (popupManager != null)
            popupManager.ClosePopup();

        if (toggleGroup != null) toggleGroup.SetAllTogglesOff();

        if (currentPage != null && sheetContentParent != null)
        {
            currentPage.transform.SetParent(sheetContentParent, false);
            currentPage = null;
        }
    }
}