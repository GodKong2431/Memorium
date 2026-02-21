using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 페이지 내부의 서브 메뉴 탭과 콘텐츠의 전환을 관리
/// </summary>
public class PageController : MonoBehaviour
{
    [System.Serializable]
    public struct SubMenu
    {
        [Tooltip("서브 메뉴 토글 버튼")]
        public Toggle menuToggle;
        [Tooltip("토글 활성화 시 표시될 콘텐츠 오브젝트")]
        public GameObject contentGO;
    }

    [Header("서브 메뉴")]
    public SubMenu[] subMenus;

    void Start()
    {
        BindEvents();
    }

    private void BindEvents()
    {
        for (int i = 0; i < subMenus.Length; i++)
        {
            int index = i; // 클로저 문제 방지 위해 지역 변수로 인덱스 저장
            if (subMenus[i].menuToggle != null)
            {
                subMenus[i].menuToggle.onValueChanged.AddListener((isOn) => OnSubMenuChanged(index, isOn));
            }
        }
    }

    void OnEnable()
    {
        // 페이지 활성화 시 항상 첫 번째 서브 메뉴를 기본 선택 상태로 초기화
        if (subMenus.Length > 0 && subMenus[0].menuToggle != null)
        {
            subMenus[0].menuToggle.isOn = true;
            OnSubMenuChanged(0, true);
        }
    }

    /// <summary>
    /// 서브 메뉴 토글 전환 시 호출되는 콜백
    /// </summary>
    private void OnSubMenuChanged(int targetIndex, bool isOn)
    {
        if (!isOn) return; // 활성화되는 토글만 처리

        for (int i = 0; i < subMenus.Length; i++)
        {
            if (subMenus[i].contentGO != null)
            {
                subMenus[i].contentGO.SetActive(i == targetIndex);
            }
        }
    }
}