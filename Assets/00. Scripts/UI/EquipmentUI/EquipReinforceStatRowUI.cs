using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipReinforceStatRowUI : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI beforeText;
    [SerializeField] private TextMeshProUGUI afterText;

    public RectTransform Root => root != null ? root : (RectTransform)transform;
    public Image Icon => icon;
    public TextMeshProUGUI StatNameText => statNameText;
    public TextMeshProUGUI BeforeText => beforeText;
    public TextMeshProUGUI AfterText => afterText;
}
