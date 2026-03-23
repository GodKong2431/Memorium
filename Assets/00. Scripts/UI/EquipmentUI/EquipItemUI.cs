using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Slider mergeSlider;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private RectTransform levelDisplayRoot;
    [SerializeField] private Image tierPanel;
    [SerializeField] private RectTransform tierRoot;
    [SerializeField] private Image tierStar;
    [SerializeField] private TextMeshProUGUI currentCountText;
    [SerializeField] private TextMeshProUGUI needCountText;
    [SerializeField] private Image[] frames;

    public Button Button => button;
    public Slider MergeSlider => mergeSlider;
    public Image Icon => icon;
    public TextMeshProUGUI LevelText => levelText;
    public RectTransform LevelDisplayRoot => levelDisplayRoot;
    public Image TierPanel => tierPanel;
    public RectTransform TierRoot => tierRoot;
    public Image TierStar => tierStar;
    public TextMeshProUGUI CurrentCountText => currentCountText;
    public TextMeshProUGUI NeedCountText => needCountText;
    public Image[] Frames => frames;

    private bool bindingsValidated;

    private void Awake()
    {
        EnsureBindings();
    }

    public void EnsureBindings()
    {
        if (bindingsValidated)
            return;

        bindingsValidated = true;

        bool hasMissingReference =
            button == null ||
            mergeSlider == null ||
            icon == null ||
            levelText == null ||
            levelDisplayRoot == null ||
            tierPanel == null ||
            tierRoot == null ||
            tierStar == null ||
            currentCountText == null ||
            needCountText == null ||
            frames == null ||
            frames.Length == 0;

        if (hasMissingReference)
            Debug.LogWarning($"[{nameof(EquipItemUI)}] '{name}' prefab bindings are incomplete. Assign references in the prefab.", this);
    }
}

internal static class EquipmentIconResolver
{
    public static Sprite LoadSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string normalizedKey = NormalizeResourcesPath(key);
        if (string.IsNullOrEmpty(normalizedKey))
            return null;

        return Resources.Load<Sprite>(normalizedKey);
    }

    private static string NormalizeResourcesPath(string rawKey)
    {
        string path = rawKey.Trim().Replace('\\', '/');
        const string resourcesToken = "Resources/";

        int resourcesIndex = path.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
            path = path.Substring(resourcesIndex + resourcesToken.Length);

        int extensionIndex = path.LastIndexOf('.', path.Length - 1);
        if (extensionIndex > 0)
            path = path.Substring(0, extensionIndex);

        return path.TrimStart('/');
    }
}
