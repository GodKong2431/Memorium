using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ���� �ϴ� �ǰ� ���� ��Ʈ/�˾� ���� UI ���� �� �ִϸ��̼��� �����ϴ� �Ŵ��� Ŭ����.
/// �ϴ� �� ��ȯ �� �������� ��ü�ϸ�, ��Ʈ�� �˾� ���� �θ� ������ ����մϴ�.
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

    public Button btnArrowUp;    // ��Ʈ -> �˾� Ȯ�� ��ư
    public Button btnArrowDown;  // �˾� -> ��Ʈ ��� ��ư

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
    /// �ϴ� ���� �� ��ȯ �� ȣ��Ǵ� �ݹ�
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
    /// ��Ʈ ��忡�� �˾� ���� ��ȯ
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
    /// �˾� ��忡�� ��Ʈ ���� ����
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
    /// �˾� �� ��Ʈ�� ��� �����ϰ� �ʱ� ���·� �ǵ���
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