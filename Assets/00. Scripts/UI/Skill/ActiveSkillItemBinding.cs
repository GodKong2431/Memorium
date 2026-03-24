using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveSkillItemBinding : MonoBehaviour
{
    private const int UpgradeGemSlotCount = 3;

    [Header("Legacy")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private GameObject lockedSharedRoot;
    [SerializeField] private GameObject notEnoughRoot;
    [SerializeField] private GameObject enoughRoot;
    [SerializeField] private GameObject upgradeRoot;
    [SerializeField] private TMP_Text lockedCountLabel;
    [SerializeField] private TMP_Text upgradeCountLabel;
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelLabel;

    [Header("Skill Icon UI")]
    [SerializeField] private Button skillButton;
    [SerializeField] private Image skillIconDisplay;
    [SerializeField] private RectTransform gemPanelRoot;
    [SerializeField] private TMP_Text iconLevelLabel;
    [SerializeField] private Button equipButton;

    [Header("Upgrade Gem UI")]
    [SerializeField] private RectTransform upgradeGemPanelRoot;
    [SerializeField] private RectTransform[] upgradeGemSlotRoots = new RectTransform[UpgradeGemSlotCount];
    [SerializeField] private Image[] upgradeGemImages = new Image[UpgradeGemSlotCount];
    [SerializeField] private GameObject[] upgradeGemLockObjects = new GameObject[UpgradeGemSlotCount];

    [Header("Level Up")]
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TMP_Text levelUpCostText;

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
    public Button SkillButton => skillButton;
    public Image SkillIconDisplay => skillIconDisplay;
    public RectTransform GemPanelRoot => gemPanelRoot;
    public TMP_Text IconLevelLabel => iconLevelLabel;
    public Button EquipButton => equipButton;
    public RectTransform UpgradeGemPanelRoot => upgradeGemPanelRoot;
    public RectTransform[] UpgradeGemSlotRoots => upgradeGemSlotRoots;
    public Image[] UpgradeGemImages => upgradeGemImages;
    public GameObject[] UpgradeGemLockObjects => upgradeGemLockObjects;
    public Button LevelUpButton => levelUpButton;
    public TMP_Text LevelUpCostText => levelUpCostText;

    private void Awake()
    {
        EnsureReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    public void EnsureReferences()
    {
        EnsureUpgradeGemArrays();

        if (upgradeRoot == null)
            return;

        if (upgradeGemPanelRoot == null)
        {
            Transform panelTransform = FindChildRecursive(upgradeRoot.transform, "(Panel)Gem");
            if (panelTransform != null)
                upgradeGemPanelRoot = panelTransform as RectTransform;
        }

        if (upgradeGemPanelRoot == null)
            return;

        for (int i = 0; i < UpgradeGemSlotCount; i++)
        {
            if (upgradeGemSlotRoots[i] == null)
            {
                Transform buttonTransform = FindChildRecursive(upgradeGemPanelRoot, $"(Btn)Gem{i + 1}");
                if (buttonTransform != null)
                    upgradeGemSlotRoots[i] = buttonTransform as RectTransform;
            }

            if (upgradeGemSlotRoots[i] == null)
                continue;

            Transform buttonRoot = upgradeGemSlotRoots[i];

            if (upgradeGemImages[i] == null)
            {
                Transform gemTransform = FindChildRecursive(buttonRoot, "(Img)Gem");
                if (gemTransform != null)
                    upgradeGemImages[i] = gemTransform.GetComponent<Image>();
            }

            if (upgradeGemLockObjects[i] == null)
            {
                Transform lockTransform = FindChildRecursive(buttonRoot, "(Img)LookIcon");
                if (lockTransform != null)
                    upgradeGemLockObjects[i] = lockTransform.gameObject;
            }
        }
    }

    private void EnsureUpgradeGemArrays()
    {
        if (upgradeGemSlotRoots == null || upgradeGemSlotRoots.Length != UpgradeGemSlotCount)
            upgradeGemSlotRoots = ResizeArray(upgradeGemSlotRoots, UpgradeGemSlotCount);

        if (upgradeGemImages == null || upgradeGemImages.Length != UpgradeGemSlotCount)
            upgradeGemImages = ResizeArray(upgradeGemImages, UpgradeGemSlotCount);

        if (upgradeGemLockObjects == null || upgradeGemLockObjects.Length != UpgradeGemSlotCount)
            upgradeGemLockObjects = ResizeArray(upgradeGemLockObjects, UpgradeGemSlotCount);
    }

    private static T[] ResizeArray<T>(T[] source, int length)
    {
        T[] result = new T[length];
        if (source == null)
            return result;

        int copyLength = Mathf.Min(source.Length, result.Length);
        for (int i = 0; i < copyLength; i++)
            result[i] = source[i];

        return result;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
