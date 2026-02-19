using UnityEngine;
using UnityEngine.UI;

public class PlayerStatView : MonoBehaviour
{
    [SerializeField] Text hpText;
    [SerializeField] Text hpRegenText;
    [SerializeField] Text atkText;
    [SerializeField] Text atkSpeedText;
    [SerializeField] Text defText;
    [SerializeField] Text magicDEFText;
    [SerializeField] Text manaText;
    [SerializeField] Text manaRegenText;
    [SerializeField] Text critText;
    [SerializeField] Text critMultText;
    [SerializeField] Text coolDownText;
    [SerializeField] Text moveSpeedText;
    [SerializeField] Text expGainText;
    [SerializeField] Text goldGainText;

    public Button hpUpgradeBtn;
    public Button hpRegenUpgradeBtn;
    public Button atkUpgradeBtn;
    public Button atkSpeedUpgradeBtn;
    public Button defUpgradeBtn;
    public Button magicDEFUpgradeBtn;
    public Button manaUpgradeBtn;
    public Button manaRegenUpgradeBtn;
    public Button critUpgradeBtn;
    public Button critMultUpgradeBtn;
    public Button coolDownUpgradeBtn;
    public Button moveSpeedUpgradeBtn;
    public Button expGainUpgradeBtn;
    public Button goldGainUpgradeBtn;

    public void SetStat(StatType statType,float value)
    {
        switch (statType)
        {
            case StatType.HP:
                hpText.text = "체력 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.HPRegen:
                hpRegenText.text = "체력재생 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.ATK:
                atkText.text = "공격력 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.ATKSpeed:
                atkSpeedText.text = "공격속도 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.DEF:
                defText.text = "물리 방어력 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.MagicDEF:
                magicDEFText.text = "마법 방어력 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.MP:
                manaText.text = "마나 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.MPRegen:
                manaRegenText.text = "마나재생 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.CritChance:
                critText.text = "치명타 확률 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.CritMult:
                critMultText.text = "치명타 배율 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.CoolDown:
                coolDownText.text = "쿨다운 감소 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.MoveSpeed:
                moveSpeedText.text = "이동 속도 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.ExpGain:
                expGainText.text = "추가 경험치 획득량 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            case StatType.GoldGain:
                goldGainText.text = "추가 골드 획득량 : [changeValue]".Replace("[changeValue]", value.ToString());
                break;
            default:
                Debug.Log($"{statType.ToString()}에 해당하는 스탯의 텍스트가 없습니다");
                break;
        }
    }
}
