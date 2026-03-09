
//부식
using UnityEngine;

public class LacerationEffect : StatusEffectBase
{
    private float defReduction;
    M5FusionTable fusion;

    public LacerationEffect(M5FusionTable fusion,SkillModule5Table skillModule5)
    {

        this.fusion = fusion;
        duration = fusion.duration;
        tickInterval = fusion.tickInterval;
        damage = 1;
        defReduction = fusion.defDown;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        base.OnApply(target, buffApplicable);

        buffApplicable.ApplyBuff(new StatModifier
        {
            id = fusion.ID,
            statType = StatType.PHYS_DEF,
            value = -defReduction,
            duration = this.duration
        });
    }

    protected override void OnTick()
    {
        target.TakeDamage(damage);
    }

    public override void OnExpire()
    {
        base.OnExpire();
    }
}
