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
    }

    private void OnDisable()
    {
    }

    private void OnClickUpgrade(StatUpgrade statUpgrade)
    {
        playerStat.Upgrade(statUpgrade);
    }
    private void OnClickTraitUpgrade(PlayerTrait playerTrait)
    {
        playerStat.TraitUpgrade(playerTrait);
    }
}
