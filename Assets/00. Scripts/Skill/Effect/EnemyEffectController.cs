
using UnityEngine;

[RequireComponent(typeof(EnemyStateMachine))]
public class EnemyEffectController : EffectController
{
    private EnemyStateMachine stateMachine;


    protected override void Awake()
    {
        base.Awake();
        stateMachine = GetComponent<EnemyStateMachine>();
        StatusEffect = new StatusEffectHandler(this, stateMachine);
    }

    public override float GetModifiedStat(StatType type, float baseValue)
    {
        float buff = BuffDebuff.GetTotal(type);
        return baseValue + buff;
    }
}
