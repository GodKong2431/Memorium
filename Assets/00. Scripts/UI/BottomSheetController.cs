using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 하단 탭과 바텀 시트/팝업 간의 UI 상태 및 애니메이션을 관리하는 매니저 클래스
/// 하단 탭 전환 시 페이지를 교체하며, 시트와 팝업 간의 부모 변경을 담당
/// </summary>
public class BottomSheetController : MonoBehaviour
{
    [System.Serializable]
    public struct TabPagePair
    {
        [Tooltip("하단 네비게이션 탭 토글")]
        public Toggle tabToggle;
        [Tooltip("활성화될 대상 페이지 오브젝트 (예: Page_Growth)")]
        public GameObject pageObject;
    }

    [Header("Main Navigation")]
    public TabPagePair[] tabPages;
    public ToggleGroup toggleGroup;

    [Header("Bottom Sheet Setup")]
    public RectTransform panelRect;
    public RectTransform skillPanelRect; // 시트 확장 시 동기화되어 움직일 상단 패널
    public float openHeight = 800f;

    [Header("Popup Setup")]
    public GameObject popupLayer;
    public Button btnArrowUp;    // 시트 -> 팝업 확장 버튼
    public Button btnArrowDown;  // 팝업 -> 시트 축소 버튼
    public Button btnPopupClose; // 팝업 완전 종료 버튼

    [Header("Reparenting Targets")]
    [Tooltip("바텀 시트 내 페이지가 위치할 부모 Transform")]
    public Transform sheetContentParent;
    [Tooltip("팝업 확장 시 페이지가 이동할 부모 Transform")]
    public Transform popupContentParent;

    private float targetHeight = 0f;
    private bool isPopupOpen = false;
    private float skillPanelStartY;

    // 현재 활성화된 페이지 추적용
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
        // 초기 상태에서는 모든 페이지 비활성화, 패널 높이 0, 팝업 레이어 비활성화
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 0f);

        if (skillPanelRect != null)
            skillPanelStartY = skillPanelRect.anchoredPosition.y;

        if (popupLayer != null)
            popupLayer.SetActive(false);

        // 모든 페이지 비활성화
        foreach (var pair in tabPages)
        {
            if (pair.pageObject != null)
                pair.pageObject.SetActive(false);
        }
    }

    private void BindEvents()
    {
        // 버튼 이벤트 바인딩
        if (btnArrowUp != null) btnArrowUp.onClick.AddListener(OpenPopup);
        if (btnArrowDown != null) btnArrowDown.onClick.AddListener(ReturnToSheet);
        if (btnPopupClose != null) btnPopupClose.onClick.AddListener(CloseAll);

        // 하단 탭 토글 이벤트 바인딩
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
        // 팝업이 열려있는 경우 항상 최대 높이, 그렇지 않으면 활성화된 탭 여부에 따라 높이 결정
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

        // 스킬 패널 위치 동기화
        if (skillPanelRect != null)
            skillPanelRect.anchoredPosition = new Vector2(skillPanelRect.anchoredPosition.x, skillPanelStartY + currentH);
    }

    /// <summary>
    /// 하단 메인 탭 전환 시 호출되는 콜백
    /// </summary>
    public void OnMainTabChanged(int tabIndex, bool isOn)
    {
        // 토글이 켜질 때만 페이지 전환 처리
        if (isOn)
        {
            isPopupOpen = false;
            if (popupLayer != null) popupLayer.SetActive(false);

            for (int i = 0; i < tabPages.Length; i++)
            {
                if (tabPages[i].pageObject != null)
                {
                    bool isActive = (i == tabIndex);
                    tabPages[i].pageObject.SetActive(isActive);

                    // 활성화된 페이지를 시트 부모 하위로 종속
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
            // 활성화된 토글이 없다면 페이지 비활성화
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
        if (popupLayer != null) popupLayer.SetActive(true);

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
        if (popupLayer != null) popupLayer.SetActive(false);

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
        if (popupLayer != null) popupLayer.SetActive(false);
        if (toggleGroup != null) toggleGroup.SetAllTogglesOff();

        if (currentPage != null && sheetContentParent != null)
        {
            currentPage.transform.SetParent(sheetContentParent, false);
            currentPage = null;
        }
    }
}