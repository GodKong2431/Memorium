using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전체 화면 팝업 레이어의 표시 모드를 관리하는 매니저 클래스
/// </summary>
public class GlobalPopupManager : MonoBehaviour
{
    [Header("Core UI")]
    public GameObject popupLayerRoot;
    public Button btnCommonClose;

    [Header("시트 팝업")]
    public GameObject bottomSheetHandleMenu;
    public GameObject bottomSheetContentArea;

    [Header("캐릭 인포 팝업")]
    public GameObject charInfoArea;

    // 현재 팝업이 어떤 모드로 열려있는지 추적
    private bool isBottomSheetMode = false;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 시작 시 팝업 닫기
        ClosePopup();

        // 닫기 버튼 이벤트 연결
        if (btnCommonClose != null)
        {
            btnCommonClose.onClick.RemoveAllListeners();
            btnCommonClose.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 현재 모드에 따라 적절한 종료 처리를 수행
    /// </summary>
    private void OnCloseButtonClicked()
    {
        ClosePopup();
    }

    /// <summary>
    /// 시트 팝업을 엽니다.
    /// 화살표 메뉴를 표시하고, 캐릭터 정보창은 숨김
    /// </summary>
    public void OpenBottomSheetMode()
    {
        isBottomSheetMode = true;

        if (popupLayerRoot != null) popupLayerRoot.SetActive(true);

        // 바텀 시트 관련 UI 활성화
        if (bottomSheetHandleMenu != null) bottomSheetHandleMenu.SetActive(true);
        if (bottomSheetContentArea != null) bottomSheetContentArea.SetActive(true);

        // 캐릭터 정보 UI 비활성화
        if (charInfoArea != null) charInfoArea.SetActive(false);
    }

    /// <summary>
    /// 캐릭 인포 팝업을 엽니다.
    /// 화살표 메뉴를 숨기고, 캐릭터 정보창을 표시
    /// </summary>
    public void OpenCharInfoMode()
    {
        isBottomSheetMode = false;

        if (popupLayerRoot != null) popupLayerRoot.SetActive(true);

        // 바텀 시트 관련 UI 비활성화
        if (bottomSheetHandleMenu != null) bottomSheetHandleMenu.SetActive(false);
        if (bottomSheetContentArea != null) bottomSheetContentArea.SetActive(false);

        // 캐릭터 정보 UI 활성화
        if (charInfoArea != null) charInfoArea.SetActive(true);
    }

    /// <summary>
    /// 팝업 레이어를 닫고 초기화
    /// </summary>
    public void ClosePopup()
    {
        if (popupLayerRoot != null) popupLayerRoot.SetActive(false);
    }
}