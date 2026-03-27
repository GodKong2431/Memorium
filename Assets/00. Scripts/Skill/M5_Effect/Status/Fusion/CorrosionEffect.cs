
using System.Threading.Tasks;
using UnityEngine;

// 부식 출혈+독
public class CorrosionEffect : StatusEffectBase
{
    private float burstDamage;
    private SkillModule5Table poisonData;
    private LayerMask layerMask = LayerMask.GetMask("Enemy");//일단 하드코딩, 생성자에서 설정할수있게하면될듯? 플레이어말고 쓸일이 있을지는 모르겠
    M5FusionTable fusion;


    public CorrosionEffect(M5FusionTable fusion, SkillModule5Table poisonSource)
    {
        this.fusion = fusion;
        duration = fusion.duration;
        tickInterval = fusion.tickInterval;
        burstDamage = 1;
        poisonData = poisonSource;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        SoundManager.Instance.PlayCombatSfx(fusion.fusionSound);
        //PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(fusion.fusionVFX, target.transform, true, true, onSpawned: OnParticleSpawned));
        //base.OnApply(target, buffApplicable);
        //float count = poisonData.duration / poisonData.tickInterval;
        //float totalDamage = burstDamage * count;
        //target.TakeDamage(totalDamage, DamageType.FixedPercentageDamage);
        //effect?.StopAndReturnManual();

        //비동기 방식 진행을 위해 메서드 분리
        SpawnAndSetEffect(target, buffApplicable);
    }


    //생성된 파티클을 불러오고 이를 effect에 배치
    private async void SpawnAndSetEffect(IDamageable target, IBuffApplicable buffApplicable)
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

        base.OnApply(target, buffApplicable);
        float count = poisonData.duration / poisonData.tickInterval;
        float totalDamage = burstDamage * count;
        target.TakeDamage(totalDamage, DamageType.FixedPercentageDamage);

        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        while (ps != null && ps.IsAlive(true))
        {
            await Task.Yield();
        }
        effect?.StopAndReturnManual();

        if (particle != null && particle.activeSelf)
        {
            ObjectPoolManager.Return(particle);
        }
    }

    protected override void OnTick() { }

    public override void OnTargetDeath()
    {
        base.OnTargetDeath();

        int count = DetectInRadius(target.transform.position, 3f, layerMask);
        var buffer = GetHitBuffer();
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(poisonData.m5VFX2, target.transform, true, false, onSpawned: OnParticleSpawned));
        for (int i = 0; i < count; i++)
        {
            if (buffer[i].TryGetComponent<EffectController>(out var ec)
                && buffer[i].TryGetComponent<IDamageable>(out var enemy)
                && enemy.IsAlive && enemy != target)
            {
                ec.ApplyStatusEffect(new PoisonEffect(poisonData));
            }
        }
    }
}
