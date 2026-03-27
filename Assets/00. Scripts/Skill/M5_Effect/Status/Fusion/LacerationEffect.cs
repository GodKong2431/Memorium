
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
        SoundManager.Instance.PlayCombatSfx(fusion.fusionSound);

        //비동기 방식 진행을 위해 메서드 분리
        SpawnAndSetEffect(target);

        base.OnApply(target, buffApplicable);

        buffApplicable.ApplyBuff(new StatModifier
        {
            id = fusion.ID,
            statType = StatType.PHYS_DEF,
            value = -defReduction,
            duration = this.duration
        });
    }

    //생성된 파티클을 불러오고 이를 effect에 배치
    private async void SpawnAndSetEffect(IDamageable target)
    {
        GameObject particle = await PoolableParticleManager.Instance.SpawnParticleAsync(new ParticleSpawnContext(fusion.fusionVFX, target.transform, true, false, onSpawned: OnParticleSpawned));
        if (particle == null)
        {
            Debug.Log("[LacerationEffect] 파티클 가져오기 실패");
            return;
        }

        if (!particle.TryGetComponent<PoolableParticle>(out var poolable))
        {
            poolable = particle.AddComponent<PoolableParticle>();
        }

        effect = particle.GetComponent<PoolableParticle>();
    }

    protected override void OnTick()
    {
        target.TakeDamage(damage, DamageType.FixedPercentageDamage);
    }

    public override void OnExpire()
    {
        base.OnExpire();
        effect?.StopAndReturnManual(); //<- 이거 이펙트 설정 안해서 발동 안함
    }
}
