using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 특성 강화 시 나타나는 팝업 UI를 관리하는 클래스
/// 선택된 노드의 정보를 시각적으로 보여주고 강화 가능 여부에 따라 버튼 상태를 제어
/// </summary>
public class TraitPopupController : MonoBehaviour
{
    [Header("노드 디스플레이")]
    [Tooltip("클릭한 노드에서 복사해 올 아이콘 이미지")]
    public Image displayIcon;
    public TextMeshProUGUI displayLevelText;

    [Header("스탯 정보 텍스트")]
    public TextMeshProUGUI textCurrentStat;
    public TextMeshProUGUI textNextStat;

    [Header("버튼")]
    public Button btnUpgrade;
    public Button btnClose;

    private void Start()
    {
        if (btnClose != null) btnClose.onClick.AddListener(ClosePopup);
    }

    /// <summary>
    /// 전달받은 노드 데이터를 기반으로 팝업 UI를 갱신하고 화면에 표시
    /// </summary>
    public void OpenPopup(Sprite nodeIcon, int currentLevel, int maxLevel, string statName, float currentStat, float nextStat, bool canUnlock, bool hasEnoughPoints)
    {
        gameObject.SetActive(true);

        // 노드 외형 복사
        if (displayIcon != null) displayIcon.sprite = nodeIcon;
        if (displayLevelText != null) displayLevelText.text = $"{currentLevel}/{maxLevel}";

        // 현재 스탯 텍스트 갱신
        if (textCurrentStat != null) textCurrentStat.text = $"{statName} +{currentStat}";

        // 다음 스탯 및 버튼 활성화 상태 갱신
        if (currentLevel >= maxLevel)
        {
            if (textNextStat != null) textNextStat.text = "Max";
            if (btnUpgrade != null) btnUpgrade.interactable = false;
        }
        else
        {
            if (textNextStat != null) textNextStat.text = $"{statName} +{nextStat}";

            if (btnUpgrade != null) btnUpgrade.interactable = canUnlock && hasEnoughPoints;
        }
    }

    /// <summary>
    /// 팝업을 닫고 비활성화
    /// </summary>
    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}