using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ActiveSkillItem_Refined 프리팹의 참조를 인스펙터에서 직접 할당한다.
/// </summary>
public class ActiveSkillItemBinding : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform root;
    [SerializeField] private LayoutElement layout;

    [Header("Common")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameLabel;

    [Header("States")]
    [SerializeField] private GameObject lockedSharedRoot;
    [SerializeField] private GameObject notEnoughRoot;
    [SerializeField] private GameObject enoughRoot;
    [SerializeField] private GameObject upgradeRoot;

    [Header("State Widgets")]
    [SerializeField] private TMP_Text lockedCountLabel;
    [SerializeField] private TMP_Text upgradeCountLabel;
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelLabel;

    public RectTransform Root => root;
    public LayoutElement Layout => layout;
    public Image IconImage => iconImage;
    public TMP_Text NameLabel => nameLabel;
    public GameObject LockedSharedRoot => lockedSharedRoot;
    public GameObject NotEnoughRoot => notEnoughRoot;
    public GameObject EnoughRoot => enoughRoot;
    public GameObject UpgradeRoot => upgradeRoot;
    public TMP_Text LockedCountLabel => lockedCountLabel;
    public TMP_Text UpgradeCountLabel => upgradeCountLabel;
    public Button UnlockButton => unlockButton;
    public Button UpgradeButton => upgradeButton;
    public TMP_Text LevelLabel => levelLabel;
}
