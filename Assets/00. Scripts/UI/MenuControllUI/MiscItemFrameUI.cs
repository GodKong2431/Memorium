using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MiscItemFrameUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    public Button Button => button;
    public Image IconImage => iconImage;
    public TMP_Text CountText => countText;
    public bool HasBindings => button != null && iconImage != null && countText != null;
}
