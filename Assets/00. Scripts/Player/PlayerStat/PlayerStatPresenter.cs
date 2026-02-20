using UnityEngine;

public class PlayerStatPresenter : MonoBehaviour
{
    [SerializeField] private CharacterStatManager playerStat;
    
    [SerializeField] PlayerStatView view;

    public CharacterStatManager PlayerStat {  get { return playerStat; } }

    private void Start()
    {

    }

    private void OnEnable()
    {
        if (view != null)
        {
            view.hpUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.HpStatUpgrade));
            view.hpRegenUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.HpRegenStatUpgrade));
            view.atkUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.AttackStatUpgrade));
            view.manaUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.MpStatUpgrade));
            view.manaRegenUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.MpRegenStatUpgrade));
            view.critUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.CritStatUpgrade));
            view.critMultUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.CritMultStatUpgrade));
            view.bossDamageUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.BossDamageStatUpgrade));
            view.traitUpgradeBtn.onClick.AddListener(() => OnClickUpgrade(playerStat.TraitStatUpgrade));

            playerStat.StatUpdate += view.SetStat;
        }
        
    }

    private void OnDisable()
    {
 
        if (view != null)
        {
            playerStat.StatUpdate -= view.SetStat;
        }
    }

    private void OnClickUpgrade(StatUpgrade statUpgrade)
    {
        playerStat.Upgrade(ref statUpgrade);
    }
}
