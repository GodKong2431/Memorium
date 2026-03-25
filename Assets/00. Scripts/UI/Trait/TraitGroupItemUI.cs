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

        bool hasMissingReference =
            background == null ||
            buttonRoot == null ||
            tierText == null ||
            lineImage == null;

        if (hasMissingReference)
            Debug.LogWarning($"[{nameof(TraitGroupItemUI)}] '{name}' binding is missing.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bindingsValidated = false;
        EnsureBindings();
    }
#endif
}
