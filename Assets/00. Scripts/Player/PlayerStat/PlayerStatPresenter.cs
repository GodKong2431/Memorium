using UnityEngine;

public class PlayerStatPresenter : MonoBehaviour
{
    [SerializeField] public CharacterStatManager playerStat;
    
    [SerializeField] PlayerStatView view;

    private CharacterBaseStatInfoTable _data;
    public CharacterBaseStatInfoTable Data {  get { return _data; } }

    private void Start()
    {
    }

    private void OnEnable()
    {
        if (view != null)
        {
            view.hpUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.HP));
            view.hpRegenUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(1, PlayerStatType.HP_REGEN));
            view.atkUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(100, PlayerStatType.ATK));
            view.atkSpeedUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.ATK_SPEED));
            view.defUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.PHYS_DEF));
            view.magicDEFUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.MAGIC_DEF));
            view.manaUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.MP));
            view.manaRegenUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.MP_REGEN));
            view.critUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(1, PlayerStatType.CRIT_CHANCE));
            view.critMultUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.CRIT_MULT));
            view.coolDownUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.COOLDOWN_REDUCE));
            view.moveSpeedUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.MOVE_SPEED));
            view.expGainUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.EXP_GAIN));
            view.goldGainUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, PlayerStatType.GOLD_GAIN));

            //playerStat.StatChanged += view.SetStat;
        }
        
    }

    private void OnDisable()
    {
 
        if (view != null)
        {
            //playerStat.StatChanged -= view.SetStat;

        }
    }

    private void OnClickUpgrade(float value, PlayerStatType statType)
    {
        //playerStat.Upgrade(value, statUpgrade);
    }
}
