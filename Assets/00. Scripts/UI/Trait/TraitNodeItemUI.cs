using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TraitNodeItemUI : MonoBehaviour
{
    // 특성 버튼의 핵심 UI 참조입니다.
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
        // 이미 바인딩을 끝냈다면 다시 찾지 않습니다.
        if (bindingsValidated)
            return;

        bindingsValidated = true;

        // 루트 버튼과 자식 오브젝트를 이름 기준으로 자동 연결합니다.
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
            Debug.LogWarning($"[{nameof(TraitNodeItemUI)}] '{name}' 프리팹 바인딩이 비어 있습니다. 프리팹 참조를 확인해 주세요.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터 값 변경 시 자동 바인딩 결과를 다시 검사합니다.
        bindingsValidated = false;
        EnsureBindings();
    }
#endif
}
