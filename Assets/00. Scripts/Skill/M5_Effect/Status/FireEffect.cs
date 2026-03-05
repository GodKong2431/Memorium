using UnityEngine;
public class FireEffect : StatusEffectBase
{
    private float defReduction;
    private SkillModule5Table tableData;

    public FireEffect(SkillModule5Table data)
    {
        tableData = data;
        duration = 0.15f;
        tickInterval = data.damage;        
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
#if UNITY_EDITOR
        Debug.Log($"[FireEffect] Applied to {target.transform.name} | Duration: {duration}s | Damage per tick: {damage}");
#endif
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
    }
}
