using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoneBonusItemUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI requirementText;
    [SerializeField] private Image[] statIcons = new Image[2];
    [SerializeField] private TextMeshProUGUI valueText;

    public Image BackgroundImage => backgroundImage;
    public TextMeshProUGUI RequirementText => requirementText;
    public Image[] StatIcons => statIcons;
    public TextMeshProUGUI ValueText => valueText;
}
