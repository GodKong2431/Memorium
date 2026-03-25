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

    public void PrepareForRuntime()
    {
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.transition = Selectable.Transition.None;
            button.interactable = true;
        }

        if (iconImage != null)
            iconImage.preserveAspect = true;

        if (countText != null)
            countText.gameObject.SetActive(true);
    }

    public void Bind(Sprite icon, string amountText)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(true);
            countText.text = amountText;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
