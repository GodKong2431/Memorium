using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleSkillSlotView : MonoBehaviour
{
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text cooldownText;

    private Button slotButton;
    public Button SlotButton => slotButton;

    private void Awake()
    {
        slotButton = GetComponent<Button>();
    }
    public void UpdateCooldown(float fillAmount, float remainTime)
    {
        bool onCooldown = fillAmount > 0;
        cooldownFill.gameObject.SetActive(onCooldown);
        cooldownFill.fillAmount = fillAmount;

        if (onCooldown)
            cooldownText.SetText("{0:1}", remainTime);
        else
            cooldownText.SetText("");
    }
    public void UpdateIcon(Sprite icon)
    {
        skillIcon.sprite = icon;
        skillIcon.color = icon != null ? Color.white : Color.gray;
    }
    public void SetEmpty()
    {
        UpdateIcon(null);
        UpdateCooldown(0f, 0f);
    }
}