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
        tickInterval = data.damage;
        damage = 1;
    }
    protected override void OnTick()
    {
        target.TakeDamage(damage);
    }
    public override void OnTargetDeath()
    {
        int count = DetectInRadius(target.transform.position, spreadRadius, layerMask);
        var buffer = GetHitBuffer();

        for (int i = 0; i < count; i++)
        {
            if (buffer[i].TryGetComponent<EffectController>(out var effect)
                && buffer[i].TryGetComponent<EnemyStateMachine>(out var enemy)
                && enemy.IsAlive && enemy != target)
            {
                effect.ApplyStatusEffect(new PoisonEffect(tableData));
            }
        }
    }
}