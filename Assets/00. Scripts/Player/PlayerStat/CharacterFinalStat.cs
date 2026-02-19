using UnityEngine;

public class CharacterFinalStat : MonoBehaviour
{
    [SerializeField] CharacterBaseStat baseStat;

    public float FinalHP { get; private set; }
    public float FinalHPRegen { get; private set; }
    public float FinalMP { get; private set; }
    public float FinalMPRegen { get; private set; }
    public float FinalATK { get; private set; }
    public float FinalATKSpeed { get; private set; }
    public float FinalPhysDEF { get; private set; }
    public float FinalMagicDEF { get; private set; }
    public float FinalCritChance {  get; private set; }
    public float FinalCritMult { get; private set; }
    public float FinalMoveSpeed { get; private set; }
    public float FinalCoolDownReduce { get; private set; }
    public float FinalGoldGain { get; private set; }
    public float FinalExpGain { get; private set; }

    public void SetStat()
    {
        FinalHP = baseStat.HP;
        FinalHPRegen = baseStat.HpRegeneration;
        FinalMP = baseStat.Mana;
        FinalMPRegen = baseStat.ManaRegeneration;
        FinalATK = baseStat.Attack;
        FinalATKSpeed = baseStat.AttackSpeed;
        FinalPhysDEF = baseStat.PhysicsResist;
        FinalMagicDEF = baseStat.MagicResist;
        FinalCritChance = baseStat.CriticalChance;
        FinalCritMult = baseStat.CriticalMultiplier;
        FinalMoveSpeed = baseStat.MoveSpeed;
        FinalCoolDownReduce = baseStat.CoolDown;
        FinalGoldGain = baseStat.GoldGain;
        FinalExpGain = baseStat.ExpGain;
    }
}
