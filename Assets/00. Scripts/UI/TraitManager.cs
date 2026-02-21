using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 특성 페이지 전체를 관리하는 총괄 매니저 클래스
/// 특성 포인트를 관리하며, 노드와 팝업 사이의 데이터 교환 및 실제 강화 로직을 수행
/// </summary>
public class TraitManager : MonoBehaviour
{
    [Header("시스템 연결")]
    public TraitPopupController popupController;
    public TraitNode[] allNodes;

    [Header("포인트 관리")]
    [Tooltip("현재 보유 중인 특성 포인트")]
    public int StatPoints = 10;
    public TextMeshProUGUI textStatPoints;

    private TraitNode selectedNode;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => CharacterStatManager.Instance != null);
        yield return new WaitUntil(() => CharacterStatManager.Instance.TableLoad);

        InitializeSystem();
    }

    /// <summary>
    /// 팝업 버튼 이벤트 연결 및 모든 노드 초기 세팅
    /// </summary>
    private void InitializeSystem()
    {
        if (popupController != null && popupController.btnUpgrade != null)
        {
            popupController.btnUpgrade.onClick.RemoveAllListeners();
            popupController.btnUpgrade.onClick.AddListener(OnUpgradeButtonClicked);
        }

        // 씬 내의 모든 노드 초기화
        foreach (var node in allNodes)
        {
            if (node != null) node.Initialize(this);
        }

        RefreshPointsUI();
    }

    /// <summary>
    /// 상단 보유 포인트 UI 텍스트를 갱신
    /// </summary>
    private void RefreshPointsUI()
    {
        if (textStatPoints != null)
        {
            textStatPoints.text = $"{StatPoints}";
        }
    }

    /// <summary>
    /// 특정 노드가 클릭되었을 때, 데이터를 계산하여 팝업으로 전달하고 팝업을 염
    /// </summary>
    /// <param name="node">클릭된 특성 노드</param>
    public void OpenNodePopup(TraitNode node)
    {
        selectedNode = node;

        // 스탯 증가량 계산
        float currentStat = node.trait.CurrentLevel * node.trait.StatUP;
        float nextStat = (node.trait.CurrentLevel + 1) * node.trait.StatUP;

        bool hasEnoughPoints = StatPoints >= 1;

        popupController.OpenPopup(
            nodeIcon: node.iconImage.sprite,
            currentLevel: node.trait.CurrentLevel,
            maxLevel: node.trait.MaxLevel,
            statName: node.trait.TraitName,
            currentStat: currentStat,
            nextStat: nextStat,
            canUnlock: node.CanUnlock(),
            hasEnoughPoints: hasEnoughPoints
        );
    }

    /// <summary>
    /// 팝업창에서 강화 버튼 클릭 시 호출되며, 실제 재화 차감 및 레벨업을 수행
    /// </summary>
    private void OnUpgradeButtonClicked()
    {
        // 만렙이 아니며, 조건/비용을 모두 만족할 때만 실행
        if (selectedNode != null && !selectedNode.IsMaxed && selectedNode.CanUnlock() && StatPoints >= 1)
        {

            selectedNode.LevelUp();

            RefreshPointsUI();
            // 전체 노드 상태 갱신
            foreach (var node in allNodes)
            {
                if (node != null) node.UpdateVisuals();
            }

            OpenNodePopup(selectedNode);
        }
    }
}