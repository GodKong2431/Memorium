using UnityEngine;
public class FireEffect : StatusEffectBase
{
    private float defReduction;
    private SkillModule5Table tableData;
    private PoolableParticle effect;

    public FireEffect(SkillModule5Table data)
    {
        tableData = data;
        duration = 0.15f;
        tickInterval = data.tickInterval;        
        damage = 1;
        defReduction = data.defDown;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        // 재적용 시 — Refresh + 디버프 갱신만
        if (elapsedTime > 0f)
        {
            Refresh();
            ApplyDefDebuff();
            return;
        }
        // 첫 적용
        base.OnApply(target, buffApplicable);
        ApplyDefDebuff();
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(tableData.m5VFX, target.transform, true, false, onSpawned: OnParticleSpawned));
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
        target.TakeDamage(damage);
    }
    public override void OnExpire()
    {
        base.OnExpire();
        effect?.StopAndReturnManual();

    }
    public void OnParticleSpawned(PoolableParticle particle)
    {
        effect = particle;
    }
}
