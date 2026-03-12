using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 액티브 스킬 아이템 프리팹의 UI 참조를 묶어두는 바인딩입니다.
/// </summary>
public class ActiveSkillItemBinding : MonoBehaviour
{
    [Header("Legacy")]
    // 카드 본문에 표시되는 대표 아이콘입니다.
    [SerializeField] private Image iconImage;
    // 스킬 이름 텍스트입니다.
    [SerializeField] private TMP_Text nameLabel;
    // 잠금/조각 상태 공통 루트입니다.
    [SerializeField] private GameObject lockedSharedRoot;
    // 조각이 부족할 때 표시할 루트입니다.
    [SerializeField] private GameObject notEnoughRoot;
    // 잠금 해제가 가능할 때 표시할 루트입니다.
    [SerializeField] private GameObject enoughRoot;
    // 장착 가능 이후 승급 상태 루트입니다.
    [SerializeField] private GameObject upgradeRoot;
    // 잠금 상태 조각 수 텍스트입니다.
    [SerializeField] private TMP_Text lockedCountLabel;
    // 승급 상태 조각 수 텍스트입니다.
    [SerializeField] private TMP_Text upgradeCountLabel;
    // 잠금 해제 버튼입니다.
    [SerializeField] private Button unlockButton;
    // 승급 버튼입니다.
    [SerializeField] private Button upgradeButton;
    // 카드 본문 레벨 텍스트입니다.
    [SerializeField] private TMP_Text levelLabel;

    [Header("Skill Icon UI")]
    // 아이콘 영역 클릭 버튼입니다.
    [SerializeField] private Button skillButton;
    // 아이콘 영역에 표시할 스킬 이미지입니다.
    [SerializeField] private Image skillIconDisplay;
    // 젬 슬롯 아이콘들의 부모입니다.
    [SerializeField] private RectTransform gemPanelRoot;
    // 아이콘 영역 전용 레벨 텍스트입니다.
    [SerializeField] private TMP_Text iconLevelLabel;
    // 장착 버튼입니다.
    [SerializeField] private Button equipButton;

    // 카드 본문 아이콘 참조를 노출합니다.
    public Image IconImage => iconImage;
    // 이름 텍스트 참조를 노출합니다.
    public TMP_Text NameLabel => nameLabel;
    // 잠금 공통 루트 참조를 노출합니다.
    public GameObject LockedSharedRoot => lockedSharedRoot;
    // 부족 상태 루트 참조를 노출합니다.
    public GameObject NotEnoughRoot => notEnoughRoot;
    // 해제 가능 상태 루트 참조를 노출합니다.
    public GameObject EnoughRoot => enoughRoot;
    // 승급 상태 루트 참조를 노출합니다.
    public GameObject UpgradeRoot => upgradeRoot;
    // 잠금 상태 카운트 텍스트를 노출합니다.
    public TMP_Text LockedCountLabel => lockedCountLabel;
    // 승급 상태 카운트 텍스트를 노출합니다.
    public TMP_Text UpgradeCountLabel => upgradeCountLabel;
    // 잠금 해제 버튼 참조를 노출합니다.
    public Button UnlockButton => unlockButton;
    // 승급 버튼 참조를 노출합니다.
    public Button UpgradeButton => upgradeButton;
    // 카드 본문 레벨 텍스트를 노출합니다.
    public TMP_Text LevelLabel => levelLabel;
    // 아이콘 버튼 참조를 노출합니다.
    public Button SkillButton => skillButton;
    // 아이콘 이미지 참조를 노출합니다.
    public Image SkillIconDisplay => skillIconDisplay;
    // 젬 패널 루트 참조를 노출합니다.
    public RectTransform GemPanelRoot => gemPanelRoot;
    // 아이콘 영역 레벨 텍스트를 노출합니다.
    public TMP_Text IconLevelLabel => iconLevelLabel;
    // 장착 버튼 참조를 노출합니다.
    public Button EquipButton => equipButton;
}
