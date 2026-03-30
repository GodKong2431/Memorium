using UnityEngine;
public class FireEffect : StatusEffectBase
{
    private float defReduction;
    private SkillModule5Table tableData;

    public FireEffect(SkillModule5Table data)
    {
        tableData = data;
        duration = data.duration;
        tickInterval = data.tickInterval;        
        damage = data.damageValue;
        defReduction = data.defDown;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        if (elapsedTime > 0f)
        {
            Refresh();
            ApplyDefDebuff();
            
            return;
        }
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(tableData.m5VFX, target.transform, true, false, onSpawned: OnParticleSpawned));

        SoundManager.Instance.PlayCombatSfx(tableData.m5SFX);
        base.OnApply(target, buffApplicable);
        ApplyDefDebuff();
       
    }

    private void ApplyDefDebuff()
    {
        buffApplicable.ApplyBuff(new StatModifier
        {
            id = tableData.ID,
            statType = StatType.PHYS_DEF,
            value = -defReduction,
            duration = this.duration
        });
    }

    protected override void OnTick()
    {
        target.TakeDamage(damage,DamageType.FixedPercentageDamage);
    }
    public override void OnExpire()
    {
        base.OnExpire();

    }
}
