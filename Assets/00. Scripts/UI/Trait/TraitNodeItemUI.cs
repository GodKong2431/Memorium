using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TraitNodeItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image statIconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private bool bindingsValidated;

    public Button Button => button;
    public Image Background => background;
    public Image FrameImage => frameImage;
    public Image StatIconImage => statIconImage;
    public TextMeshProUGUI AmountText => amountText;

    private void Awake()
    {
        EnsureBindings();
    }

    public void EnsureBindings()
    {
        if (bindingsValidated)
            return;

        bindingsValidated = true;

        if (button == null)
            button = GetComponent<Button>();

        if (background == null)
            background = GetComponent<Image>();

        bool hasMissingReference =
            button == null ||
            background == null ||
            frameImage == null ||
            statIconImage == null ||
            amountText == null;

        if (hasMissingReference)
            Debug.LogWarning($"[{nameof(TraitNodeItemUI)}] '{name}' binding is missing.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bindingsValidated = false;
        EnsureBindings();
    }
#endif
}
