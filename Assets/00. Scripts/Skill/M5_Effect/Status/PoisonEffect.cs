using UnityEngine;
public class PoisonEffect : StatusEffectBase
{
    private float spreadRadius = 3f;
    private SkillModule5Table tableData;
    private LayerMask layerMask= LayerMask.GetMask("Enemy");//일단 하드코딩, 생성자에서 설정할수있게하면될듯? 플레이어말고 쓸일이 있을지는 모르겠

    public PoisonEffect(SkillModule5Table data)
    {
        tableData = data;
        duration = data.duration;
        tickInterval = data.tickInterval;

        var gemModule = InventoryManager.Instance?.GetModule<GemInventoryModule>();
        gemGrade = gemModule != null ? gemModule.GetHighestGrade(data.ID) : 0;
        damage = data.damageValue + data.plusValue * (int)gemGrade;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        base.OnApply(target, buffApplicable);
        SoundManager.Instance.PlayCombatSfx(tableData.m5SFX);
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(tableData.m5VFX, target.transform, true, false, onSpawned: OnParticleSpawned));
    }

    protected override void OnTick()
    {
        target.TakeDamage(damage, DamageType.FixedPercentageDamage);
    }
    public override void OnTargetDeath()
    {
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(tableData.m5VFX2, target.transform, true, false, onSpawned: OnParticleSpawned));
        int count = DetectInRadius(target.transform.position, spreadRadius, layerMask);
        var buffer = GetHitBuffer();

        for (int i = 0; i < count; i++)
        {
            if (buffer[i].TryGetComponent<EffectController>(out var effect)
                && buffer[i].TryGetComponent<IDamageable>(out var enemy)
                && enemy.IsAlive && enemy != target)
            {
                effect.ApplyStatusEffect(StatusEffectFactory.Create(tableData));
            }
        }
    }
}
