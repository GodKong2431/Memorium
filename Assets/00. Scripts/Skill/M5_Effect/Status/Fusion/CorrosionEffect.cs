
using UnityEngine;

// 부식 출혈+독
public class CorrosionEffect : StatusEffectBase
{
    private float burstDamage;
    private SkillModule5Table poisonData;
    private LayerMask layerMask = LayerMask.GetMask("Enemy");//일단 하드코딩, 생성자에서 설정할수있게하면될듯? 플레이어말고 쓸일이 있을지는 모르겠


    public CorrosionEffect(M5FusionTable fusion, SkillModule5Table poisonSource)
    {
        duration = fusion.duration;
        tickInterval = 1f;
        burstDamage = 1;
        poisonData = poisonSource;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        base.OnApply(target, buffApplicable);
        float count = poisonData.duration / poisonData.damage;
        float totalDamage = burstDamage * count;
        target.TakeDamage(totalDamage);

#if UNITY_EDITOR
        Debug.Log($"[부식] Applied to {target.transform.name} | Duration: {duration}s | Damage per tick: {damage}");
        Debug.Log($"부식 데미지 : {totalDamage}");
#endif
    }

    protected override void OnTick() { }

    public override void OnTargetDeath()
    {
        base.OnTargetDeath();
        int count = DetectInRadius(target.transform.position, 3f, layerMask);
        var buffer = GetHitBuffer();

        for (int i = 0; i < count; i++)
        {
            if (buffer[i].TryGetComponent<EffectController>(out var effectController)
                && buffer[i].TryGetComponent<IDamageable>(out var enemy)
                && enemy.IsAlive && enemy != target)
            {
                if (!effectController.HasStatusEffect())
                    effectController.ApplyStatusEffect(new PoisonEffect(poisonData));
            }
        }
    }
}
