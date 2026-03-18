using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PixieMenuItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image[] gradeImages;

    public Button Button => button;
    public Image IconImage => iconImage;
    public TMP_Text LabelText => labelText;
    public Image[] GradeImages => gradeImages;
    public bool HasBindings => button != null && iconImage != null && labelText != null;
}
