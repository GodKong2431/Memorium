
using UnityEngine;

public class BleedEffect : StatusEffectBase
{
    private int stackCount = 1;
    private int maxStack;
    private SkillModule5Table tableData;

    public int StackCount => stackCount;

    public BleedEffect(SkillModule5Table data)
    {
        tableData = data;
        duration = data.duration;
        damage = 1;
        tickInterval = data.damage;
        maxStack = data.maxStack;
    }

    public override void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        if (elapsedTime > 0f)
        {
            AddStack();
            return;
        }
        base.OnApply(target, buffApplicable);
#if UNITY_EDITOR
        Debug.Log($"[BleedEffect] Applied to {target.transform.name} | Duration: {duration}s | Damage per tick: {damage} | Current Stack: {stackCount}");
#endif
    }

    public void AddStack()
    {
        stackCount = Mathf.Min(stackCount + 1, maxStack);
        elapsedTime = 0f; 
    }

    protected override void OnTick()
    {
        float dmg = damage * stackCount;
        bool isMoving = target.isMoving;
        if (isMoving) dmg *= 2f;

#if UNITY_EDITOR
        if (isMoving)
            Debug.Log($"출혈 2배 데미지");
#endif
        target.TakeDamage(dmg);
    }
}