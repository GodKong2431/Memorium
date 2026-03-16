using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoneSlotItemUI : MonoBehaviour
{
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image statIconImage;
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI successCountText;
    [SerializeField] private RectTransform progressIconRoot;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    [SerializeField] private TextMeshProUGUI costText;

    private Image[] progressIcons;

    public Button Button => upgradeButton;
    public Image BackgroundImage => backgroundImage;
    public Image StatIconImage => statIconImage;
    public TextMeshProUGUI StatNameText => statNameText;
    public TextMeshProUGUI SuccessCountText => successCountText;
    public TextMeshProUGUI ButtonText => upgradeButtonText;
    public TextMeshProUGUI CostText => costText;
    public Image[] ProgressIcons => GetProgressIcons();

    private Image[] GetProgressIcons()
    {
        if (progressIcons != null)
        {
            return progressIcons;
        }

        if (progressIconRoot == null)
        {
            return progressIcons = System.Array.Empty<Image>();
        }

        progressIcons = new Image[progressIconRoot.childCount];
        for (int i = 0; i < progressIconRoot.childCount; i++)
        {
            progressIcons[i] = progressIconRoot.GetChild(i).GetComponent<Image>();
        }

        return progressIcons;
    }
}
