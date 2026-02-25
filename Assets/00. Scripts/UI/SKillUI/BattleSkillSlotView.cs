using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleSkillSlotView : MonoBehaviour
{
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text cooldownText;

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
        skillIcon.color = icon != null ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }
    public void SetEmpty()
    {
        UpdateIcon(null);
        UpdateCooldown(0f, 0f);
    }
}