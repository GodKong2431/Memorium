using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TraitGroupItemUI : MonoBehaviour
{
    // 그룹 패널의 핵심 UI 참조입니다.
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
        // 이미 바인딩을 끝냈다면 다시 찾지 않습니다.
        if (bindingsValidated)
            return;

        bindingsValidated = true;

        // 루트 이미지와 자식 오브젝트를 이름 기준으로 자동 연결합니다.
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
            Debug.LogWarning($"[{nameof(TraitGroupItemUI)}] '{name}' 프리팹 바인딩이 비어 있습니다. 프리팹 참조를 확인해 주세요.", this);
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
