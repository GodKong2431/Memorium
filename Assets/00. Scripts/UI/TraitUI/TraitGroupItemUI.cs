using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TraitGroupItemUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private RectTransform buttonRoot;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private Image lineImage;

    private bool bindingsValidated;

    public Image Background => background;
    public RectTransform ButtonRoot => buttonRoot;
    public TextMeshProUGUI TierText => tierText;
    public Image LineImage => lineImage;

    private void Awake()
    {
        EnsureBindings();
    }

    public void EnsureBindings()
    {
        if (bindingsValidated)
            return;

        bindingsValidated = true;

        if (background == null)
            background = GetComponent<Image>();

        if (buttonRoot == null)
        {
            Transform child = transform.Find("(Panel)TraitBtnGroup");
            buttonRoot = child as RectTransform;
        }

        if (tierText == null)
        {
            Transform child = transform.Find("(Text)TraitLevel");
            tierText = child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        if (lineImage == null)
        {
            Transform child = transform.Find("(Img)Line");
            lineImage = child != null ? child.GetComponent<Image>() : null;
        }

        bool hasMissingReference =
            background == null ||
            buttonRoot == null ||
            tierText == null ||
            lineImage == null;

        if (hasMissingReference)
            Debug.LogWarning($"[{nameof(TraitGroupItemUI)}] '{name}' prefab bindings are incomplete. Assign references in the prefab.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bindingsValidated = false;
        EnsureBindings();
    }
#endif
}
