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

        if (frameImage == null)
        {
            Transform child = transform.Find("(Img)Frame");
            frameImage = child != null ? child.GetComponent<Image>() : null;
        }

        if (statIconImage == null)
        {
            Transform child = transform.Find("(img)StatIcon");
            statIconImage = child != null ? child.GetComponent<Image>() : null;
        }

        if (amountText == null)
        {
            Transform child = transform.Find("(Text)Amount");
            amountText = child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        bool hasMissingReference =
            button == null ||
            background == null ||
            frameImage == null ||
            statIconImage == null ||
            amountText == null;

        if (hasMissingReference)
            Debug.LogWarning($"[{nameof(TraitNodeItemUI)}] '{name}' prefab bindings are incomplete. Assign references in the prefab.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bindingsValidated = false;
        EnsureBindings();
    }
#endif
}
