using UnityEngine;

[System.Serializable]

public class CharacterBaseStat
{
    [Header("Class")]
    [Tooltip("플레이어의 클래스")]
    [SerializeField] private ClassType classType;

    // 기본 스탯
    [Header("Health")]
    [Tooltip("플레이어의 체력")]
    [SerializeField] private float baseHealthPoint;
    [Tooltip("플레이어의 체력 재생")]
    [SerializeField] private float baseHpRegeneration;

    [Header("Attack")]
    [Tooltip("플레이어의 공격력")]
    [SerializeField] private float baseAttack;
    [Tooltip("플레이어의 공격속도")]
    [SerializeField] private float baseAttackSpeed;

    [Header("Resistance")]
    [Tooltip("플레이어의 물리저항력")]
    [SerializeField] private float basePhysicalResist;
    [Tooltip("플레이어의 마법저항력")]
    [SerializeField] private float baseMagicResist;

    [Header("Mana")]
    [Tooltip("플레이어의 마나")]
    [SerializeField] private float baseMana;
    [Tooltip("플레이어의 마나재생")]
    [SerializeField] private float baseManaRegeneration;

    [Header("Critical")]
    [Tooltip("플레이어의 치명타 확률")]
    [SerializeField] private float baseCriticalChance;
    [Tooltip("플레이어의 치명타 배율")]
    [SerializeField] private float baseCriticalMultiplier;

    [Header("Skill")]
    [Tooltip("플레이어의 쿨타임")]
    [SerializeField] private float baseCoolDown;

    [Header("Speed")]
    [Tooltip("플레이어의 이동속도")]
    [SerializeField] private float baseMoveSpeed;

    [Header("Gain")]
    [Tooltip("플레이어의 골드 획득량")]
    [SerializeField] private float baseGoldGain;
    [Tooltip("플레이어의 경험치 획득량")]
    [SerializeField] private float baseExpGain;

    [Header("DamageMult")]
    [Tooltip("추가 보스 데미지")]
    [SerializeField] private float baseBossDamage;
    [Tooltip("추가 기본 데미지")]
    [SerializeField] private float baseNormalDamage;
    [Tooltip("데미지 배율")]
    [SerializeField] private float baseDamageMult;

    // 이벤트
    //public event Action<PlayerStatType,float> StatChanged;

    public ClassType CurrentClass { get { return classType;  } }
    public float HP { get { return baseHealthPoint; } }
    public float HpRegeneration { get { return baseHpRegeneration; } }
    public float Attack {  get { return baseAttack; } }
    public float AttackSpeed { get { return baseAttackSpeed; } }
    public float PhysicsResist { get { return basePhysicalResist; } }
    public float MagicResist { get { return baseMagicResist; } }
    public float Mana { get { return baseMana; } }
    public float ManaRegeneration { get { return baseManaRegeneration;} }
    public float CriticalChance { get { return baseCriticalChance; } }
    public float CriticalMultiplier { get { return baseCriticalMultiplier; } }
    public float CoolDown { get { return baseCoolDown; } }
    public float MoveSpeed { get { return baseMoveSpeed; } }
    public float ExpGain { get { return baseExpGain; } }
    public float GoldGain { get { return baseGoldGain; } }

    public float BossDamage { get { return baseBossDamage; } }
    public float NormalDamage { get { return baseNormalDamage; } }
    public float DamageMult { get { return baseDamageMult; } }

    private CharacterBaseStatInfoTable statTable;

    public CharacterBaseStat(int key)
    {
        if (!DataManager.Instance.CharacterBaseStatInfoDict.TryGetValue(key, out statTable))
        {
            Debug.Log($"[CharacterBaseStat] [{key}] ID 값에 해당하는 데이터가 없습니다 ");
            return;
        }
        SetBaseStat();
    }

    // 스탯 설정
    public void SetBaseStat()
    {
        classType = statTable.classType;
        baseHealthPoint = statTable.baseHP;
        baseHpRegeneration = statTable.baseHPRegen;
        baseAttack = statTable.baseAttack;
        baseAttackSpeed = statTable.baseAttackSpeed;
        basePhysicalResist = statTable.basePhysicalResist;
        baseMagicResist = statTable.baseMagicResist;
        baseMana = statTable.baseMP;
        baseManaRegeneration = statTable.baseMPRegen;
        baseCriticalChance = statTable.baseCritical;
        baseCriticalMultiplier = statTable.baseCriticalMultiPlier;
        baseCoolDown = statTable.baseCooltimeRegen;
        baseMoveSpeed = statTable.baseMoveSpeed;
        baseGoldGain = statTable.baseMoneyGain;
        baseExpGain = statTable.baseExpGain;
        baseBossDamage = statTable.baseBossDamage;
        baseNormalDamage = statTable.baseNormalDamage;
        baseDamageMult = statTable.baseFinalMultiPlier;
    }
}
