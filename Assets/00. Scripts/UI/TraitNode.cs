using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 특성 트리의 개별 노드를 담당하는 클래스
/// 자신의 레벨, 선행 조건을 관리하며 시각적 상태를 업데이트
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class TraitNode : MonoBehaviour
{
    [Header("UI 연결")]
    public Image iconImage;
    public TextMeshProUGUI levelText;

    [Header("특성 데이터 설정")]
    public string traitName = "AttackPower";
    public int maxLevel = 5;

    public float statGainPerLevel = 50f;

    [Header("선행 조건 노드")]
    public TraitNode[] requiredNodes;

    private Button nodeButton;
    private CanvasGroup canvasGroup;
    private TraitManager manager;

    public int CurrentLevel { get; private set; } = 0;
    public bool IsMaxed => CurrentLevel >= maxLevel;

    private void Awake()
    {
        nodeButton = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// TraitManager에 의해 초기화되며, 클릭 이벤트를 바인딩합니다.
    /// </summary>
    public void Initialize(TraitManager mgr)
    {
        manager = mgr;
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);
        UpdateVisuals();
    }

    /// <summary>
    /// 선행 조건을 모두 만족하여 현재 이 특성을 찍을 수 있는 상태인지 확인
    /// </summary>
    public bool CanUnlock()
    {
        if (requiredNodes == null || requiredNodes.Length == 0) return true;

        foreach (var node in requiredNodes)
        {
            if (!node.IsMaxed) return false;
        }
        return true;
    }

    /// <summary>
    /// 노드의 해금 상태 및 레벨에 따라 UI를 갱신합니다.
    /// </summary>
    public void UpdateVisuals()
    {
        if (levelText != null)
            levelText.text = $"{CurrentLevel}/{maxLevel}";

        // 조건을 만족하면 진하게, 만족하지 않으면 반투명하게 표시
        canvasGroup.alpha = (CurrentLevel > 0 || CanUnlock()) ? 1.0f : 0.4f;
    }

    /// <summary>
    /// 유저가 이 노드를 터치했을 때 매니저를 통해 팝업을 호출
    /// </summary>
    private void OnNodeClicked()
    {
        if (manager != null) manager.OpenNodePopup(this);
    }

    /// <summary>
    /// 특성의 레벨을 1 증가시키고 UI를 갱신
    /// </summary>
    public void LevelUp()
    {
        if (!IsMaxed)
        {
            CurrentLevel++;
            UpdateVisuals();
        }
    }
}