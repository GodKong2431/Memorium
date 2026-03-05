
//발화
using UnityEngine;

public class IgnitionEffect : StatusEffectBase
{
    private float explosionDamage;
    private float explosionRadius = 3f;
    private LayerMask layerMask = LayerMask.GetMask("Enemy");//일단 하드코딩, 생성자에서 설정할수있게하면될듯? 플레이어말고 쓸일이 있을지는 모르겠

    public IgnitionEffect(M5FusionTable fusion)
    {
        duration = 0f;
        tickInterval = 1f;
        explosionDamage = 1f;
    }

    public override void OnApply(EnemyStateMachine target, IBuffApplicable buffApplicable)
    {
        base.OnApply(target, buffApplicable);

        int count = DetectInRadius(target.transform.position, explosionRadius, layerMask);
        var buffer = GetHitBuffer();

        for (int i = 0; i < count; i++)
        {
            if (buffer[i].TryGetComponent<EnemyStateMachine>(out var enemy) && enemy.IsAlive)
                enemy.TakeDamage(explosionDamage);
        }
    }

    protected override void OnTick() { }
}