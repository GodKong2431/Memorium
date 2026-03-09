
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


#if UNITY_EDITOR
        Debug.Log($"[부식Effect] Applied to {target.transform.name} | Duration: {duration}s | Damage per tick: {damage}");
#endif
    }

    protected override void OnTick()
    {
        target.TakeDamage(damage);

#if UNITY_EDITOR
            Debug.Log($"부식 데미지 : {dmg}");
#endif
    }

    public override void OnExpire()
    {
        base.OnExpire();
    }
}
