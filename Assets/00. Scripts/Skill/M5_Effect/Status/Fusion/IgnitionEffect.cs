
using System.Threading.Tasks;
using UnityEngine;

// 발화 독+화상
public class IgnitionEffect : StatusEffectBase
{
    private float explosionDamage;
    private float explosionRadius = 3f;
    M5FusionTable fusion;
    private LayerMask layerMask = LayerMask.GetMask("Enemy");//일단 하드코딩, 생성자에서 설정할수있게하면될듯? 플레이어말고 쓸일이 있을지는 모르겠

    public IgnitionEffect(M5FusionTable fusion, SkillModule5Table poisonData, SkillModule5Table fireData)
    {
        duration = poisonData.duration;
        tickInterval = poisonData.tickInterval;
        damage = poisonData.damageValue;

        var gemModule = InventoryManager.Instance?.GetModule<GemInventoryModule>();
        gemGrade = gemModule != null ? gemModule.GetHighestGrade(poisonData.ID) : 0;

        damage = poisonData.damageValue + poisonData.plusValue * (int)gemGrade;
        int totalTicks = Mathf.FloorToInt(duration / tickInterval);
        explosionDamage = totalTicks *damage;
        this.fusion = fusion;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        SoundManager.Instance.PlayCombatSfx(fusion.fusionSound);
        //PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(fusion.fusionVFX, target.transform, true, true, onSpawned: OnParticleSpawned));
        //base.OnApply(target, buffApplicable);

        //int count = DetectInRadius(target.transform.position, explosionRadius, layerMask);
        //var buffer = GetHitBuffer();

        //for (int i = 0; i < count; i++)
        //{
        //    if (buffer[i].TryGetComponent<IDamageable>(out var enemy) && enemy.IsAlive)
        //    {

        //        enemy.TakeDamage(explosionDamage, DamageType.FixedPercentageDamage);
        //    }
        //}

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

        int count = DetectInRadius(target.transform.position, explosionRadius, layerMask);
        var buffer = GetHitBuffer();

        for (int i = 0; i < count; i++)
        {
            if (buffer[i].TryGetComponent<IDamageable>(out var enemy) && enemy.IsAlive)
            {

                enemy.TakeDamage(explosionDamage, DamageType.FixedPercentageDamage);
            }
        }

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
}
