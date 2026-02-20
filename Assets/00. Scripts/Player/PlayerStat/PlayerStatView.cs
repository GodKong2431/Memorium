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

    [SerializeField] TextMeshProUGUI hpTraitText;
    [SerializeField] TextMeshProUGUI mpTraitText;
    [SerializeField] TextMeshProUGUI atkTraitText;
    [SerializeField] TextMeshProUGUI atkSpeedTraitText;
    [SerializeField] TextMeshProUGUI critTraitText;
    [SerializeField] TextMeshProUGUI critMultTraitText;
    [SerializeField] TextMeshProUGUI bossDamageTraitText;
    [SerializeField] TextMeshProUGUI coolDownTraitText;
    [SerializeField] TextMeshProUGUI dmgMultTraitText;

    public Button hpUpgradeBtn;
    public Button hpRegenUpgradeBtn;
    public Button atkUpgradeBtn;
    public Button manaUpgradeBtn;
    public Button manaRegenUpgradeBtn;
    public Button critUpgradeBtn;
    public Button critMultUpgradeBtn;
    public Button bossDamageUpgradeBtn;
    public Button traitUpgradeBtn;

    public Button hpTraitBtn;
    public Button mpTraitBtn;
    public Button atkTraitBtn;
    public Button atkSpeedTraitBtn;
    public Button critTraitBtn;
    public Button critMultTraitBtn;
    public Button bossDmgTraitBtn;
    public Button coolDownTraitBtn;
    public Button dmgMultTraitBtn;

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

    public void SetTrait(int id, int currentLevel , int maxLevel)
    {
        switch (id)
        {
            case 1040012:
                hpTraitText.text = $"HPTarit {currentLevel} / {maxLevel}";
                break;
            case 1040011:
                mpTraitText.text = $"MPTarit {currentLevel} / {maxLevel}";
                break;
            case 1040001:
                atkTraitText.text = $"ATKTarit {currentLevel} / {maxLevel}";
                break;
            case 1040013:
                atkSpeedTraitText.text = $"ATKSPEEDTarit {currentLevel} / {maxLevel}";
                break;
            case 1040021:
                critTraitText.text = $"CRITTarit {currentLevel} / {maxLevel}";
                break;
            case 1040032:
                critMultTraitText.text = $"CRITMULTTarit {currentLevel} / {maxLevel}";
                break;
            case 1040033:
                bossDamageTraitText.text = $"BOSSDMGTarit {currentLevel} / {maxLevel}";
                break;
            case 1040034:
                coolDownTraitText.text = $"COOLDOWNTarit {currentLevel} / {maxLevel}";
                break;
            case 1040041:
                dmgMultTraitText.text = $"DMGMULTTarit {currentLevel} / {maxLevel}";
                break;
            default:
                Debug.Log($"에 해당하는 특성의 텍스트가 없습니다");
                break;
        }
    }
}
