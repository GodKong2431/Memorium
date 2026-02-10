using UnityEngine;

public class Presenter : MonoBehaviour
{
    [SerializeField] PlayerStat playerStat;
    [SerializeField] View view;

    private void Start()
    {
        
    }

    private void OnEnable()
    {
        view.hpUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.HP));
        view.hpRegenUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(1, StatType.HPRegen));
        view.atkUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(100, StatType.ATK));
        view.atkSpeedUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.ATKSpeed));
        view.defUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.DEF));
        view.magicDEFUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.MagicDEF));
        view.manaUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.MP));
        view.manaRegenUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.MPRegen));
        view.critUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(1, StatType.CritChance));
        view.critMultUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.CritMult));
        view.coolDownUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.CoolDown));
        view.moveSpeedUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.MoveSpeed));
        view.expGainUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.ExpGain));
        view.goldGainUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(10, StatType.GoldGain));

        playerStat.StatChanged += view.SetStat;
    }

    private void OnDisable()
    {
 
        playerStat.StatChanged -= view.SetStat;
    }

    private void OnClickUpgrade(float value, StatType statType)
    {
        playerStat.AddStat(value, statType);
    }
}
