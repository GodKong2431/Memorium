using UnityEngine;

// 발화 출혈+화상
public class IgnitionEffect : StatusEffectBase
{
    private float explosionDamage;
    private float explosionRadius = 3f;
    M5FusionTable fusion;
    private LayerMask layerMask = LayerMask.GetMask("Enemy");//일단 하드코딩, 생성자에서 설정할수있게하면될듯? 플레이어말고 쓸일이 있을지는 모르겠

    public IgnitionEffect(M5FusionTable fusion)
    {
        duration = fusion.duration;
        tickInterval = fusion.tickInterval;
        explosionDamage = 1f;
        this.fusion = fusion;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        SoundManager.Instance.PlayCombatSfx(fusion.fusionSound);
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(fusion.fusionVFX, target.transform, true, false, onSpawned: OnParticleSpawned));
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

        effect?.StopAndReturnManual();
    }

    protected override void OnTick() { }
}
