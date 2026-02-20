using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI hpRegenText;
    [SerializeField] TextMeshProUGUI atkText;
    [SerializeField] TextMeshProUGUI atkSpeedText;
    [SerializeField] TextMeshProUGUI defText;
    [SerializeField] TextMeshProUGUI magicDEFText;
    [SerializeField] TextMeshProUGUI manaText;
    [SerializeField] TextMeshProUGUI manaRegenText;
    [SerializeField] TextMeshProUGUI critText;
    [SerializeField] TextMeshProUGUI critMultText;
    [SerializeField] TextMeshProUGUI coolDownText;
    [SerializeField] TextMeshProUGUI moveSpeedText;
    [SerializeField] TextMeshProUGUI expGainText;
    [SerializeField] TextMeshProUGUI goldGainText;
    [SerializeField] TextMeshProUGUI bossDamageText;
    [SerializeField] TextMeshProUGUI normalDamageText;
    [SerializeField] TextMeshProUGUI traitText;

    public Button hpUpgradeBtn;
    public Button hpRegenUpgradeBtn;
    public Button atkUpgradeBtn;
    public Button manaUpgradeBtn;
    public Button manaRegenUpgradeBtn;
    public Button critUpgradeBtn;
    public Button critMultUpgradeBtn;
    public Button bossDamageUpgradeBtn;
    public Button traitUpgradeBtn;

    public void SetStat(PlayerStatType statType,float value)
    {
        switch (statType)
        {
            case PlayerStatType.HP:
                hpText.text = $"HP : {value}";
                break;
            case PlayerStatType.HP_REGEN:
                hpRegenText.text = $"HP Regen : {value}";
                break;
            case PlayerStatType.ATK:
                atkText.text = $"ATK : {value}";
                break;
            case PlayerStatType.ATK_SPEED:
                atkSpeedText.text = $"ATK Speed : {value}";
                break;
            case PlayerStatType.PHYS_DEF:
                defText.text = $"Phys DEF : {value}";
                break;
            case PlayerStatType.MAGIC_DEF:
                magicDEFText.text = $"Magic DEF : {value}";
                break;
            case PlayerStatType.MP:
                manaText.text = $"MP : {value}";
                break;
            case PlayerStatType.MP_REGEN:
                manaRegenText.text = $"MP Regen : {value}";
                break;
            case PlayerStatType.CRIT_CHANCE:
                critText.text = $"Crit Chance : {value}";
                break;
            case PlayerStatType.CRIT_MULT:
                critMultText.text = $"Crit Mult : {value}";
                break;
            case PlayerStatType.COOLDOWN_REDUCE:
                coolDownText.text = $"CoolDown : {value}";
                break;
            case PlayerStatType.MOVE_SPEED:
                moveSpeedText.text = $"Move Speed : {value}";
                break;
            case PlayerStatType.EXP_GAIN:
                expGainText.text = $"Exp Gain : {value}";
                break;
            case PlayerStatType.GOLD_GAIN:
                goldGainText.text = $"Gold Gain : {value}";
                break;
            case PlayerStatType.BOSS_DMG:
                bossDamageText.text = $"Boss Dmg : {value}";
                break;
            case PlayerStatType.NORMAL_DMG:
                normalDamageText.text = $"Normal Dmg : {value}";
                break;
            case PlayerStatType.TRIAT:
                traitText.text = $"Triat : {value}";
                break;
            default:
                Debug.Log($"{statType.ToString()}에 해당하는 스탯의 텍스트가 없습니다");
                break;
        }
    }

    public void SetButton()
    {

    }
}
