using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("플레이어의 체력")]
    [SerializeField] private float healthPoint;
    [Tooltip("플레이어의 체력 재생")]
    [SerializeField] private float hpRegeneration;

    [Header("Attack")]
    [Tooltip("플레이어의 공격력")]
    [SerializeField] private float attack;
    [Tooltip("플레이어의 공격속도")]
    [SerializeField] private float attackSpeed;

    [Header("Resistance")]
    [Tooltip("플레이어의 물리저항력")]
    [SerializeField] private float defense;
    [Tooltip("플레이어의 마법저항력")]
    [SerializeField] private float magicResistance;

    [Header("Mana")]
    [Tooltip("플레이어의 마나")]
    [SerializeField] private float mana;
    [Tooltip("플레이어의 마나재생")]
    [SerializeField] private float manaRegeneration;

    [Header("Critical")]
    [Tooltip("플레이어의 치명타 확률")]
    [SerializeField] private float criticalChance;
    [Tooltip("플레이어의 치명타 배율")]
    [SerializeField] private float criticalMultiplier;

    [Header("Skill")]
    [Tooltip("플레이어의 스킬데미지")]
    [SerializeField] private float skillDamage;
    [Tooltip("플레이어의 쿨타임")]
    [SerializeField] private float coolTime;

    [Header("Speed")]
    [Tooltip("플레이어의 이동속도")]
    [SerializeField] private float moveSpeed;

    [Header("Gain")]
    [Tooltip("플레이어의 골드 획득량")]
    [SerializeField] private float goldGain;
    [Tooltip("플레이어의 경험치 획득량")]
    [SerializeField] private float expGain;

    // 다른곳에서 가져가기
    public float getHealth { get { return healthPoint; } }
    public float getHpRegeneration { get { return hpRegeneration; } }
    public float getAttack {  get { return attack; } }
    public float getAttackSpeed { get { return attackSpeed; } }
    public float getDefense { get { return defense; } }
    public float getMagicResistance { get { return magicResistance; } }
    public float getMana { get { return mana; } }
    public float getManaRegeneration { get { return manaRegeneration;} }
    public float getCriticalChance { get { return criticalChance; } }
    public float getCriticalMultiplier { get { return criticalMultiplier; } }
    public float getSkillDamage { get { return skillDamage; } }
    public float getCoolTime { get { return coolTime; } }
    public float getMoveSpeed { get { return moveSpeed; } }
    public float getExpGain { get { return expGain; } }
    public float getGoldGain { get { return goldGain; } }
}
