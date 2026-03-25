using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 탭의 개별 티어 그룹(아코디언 UI)을 제어하는 클래스입니다.
/// 티어 헤더를 클릭하여 하위 장비 목록(Grid)을 펼치거나 접는 기능을 수행합니다.
/// </summary>
public class EquipTierGroupUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Button headerButton;
    public GameObject itemGrid;
    public Transform arrowIcon;

    [Header("설정")]
    public bool startExpanded = false;
    private bool isExpanded;

    private void Start()
    {
        // 초기 상태 설정
        isExpanded = startExpanded;
        ApplyState();

        if (headerButton != null)
        {
            headerButton.onClick.RemoveAllListeners();
            headerButton.onClick.AddListener(ToggleGroup);
            UiButtonSoundPlayer.Ensure(headerButton, UiSoundIds.DefaultButton);
        }
    }

    /// <summary>
    /// 장비 목록의 접기/펴기 상태를 반전
    /// </summary>
    public void ToggleGroup()
    {
        isExpanded = !isExpanded;
        ApplyState();
    }

    /// <summary>
    /// 현재 isExpanded 상태에 맞춰 그리드 On/Off 및 화살표 회전을 실제 화면에 적용
    /// </summary>
    private void ApplyState()
    {
        if (itemGrid != null)
        {
            itemGrid.SetActive(isExpanded);
        }

        // 화살표 회전
        if (arrowIcon != null)
        {
            float targetAngle = isExpanded ? 180f : 0f;
            arrowIcon.localEulerAngles = new Vector3(0, 0, targetAngle);
        }

        // UI 레이아웃 즉시 강제 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
