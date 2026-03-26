using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GemItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;

    private int gemId;
    private Action<int> onClick;

    public void Bind(int gemId, Sprite icon, int count, Action<int> clickHandler)
    {
        this.gemId = gemId;
        this.onClick = clickHandler;

        if (iconImage != null&&icon!=null)
            iconImage.sprite = icon;

        if (countText != null)
            countText.text = count.ToString();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            UiButtonSoundPlayer.Ensure(button, UiSoundIds.DefaultButton);
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke(gemId);
    }
}
