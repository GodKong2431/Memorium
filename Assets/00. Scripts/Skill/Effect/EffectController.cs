using System.Collections.Generic;
using UnityEngine;

public abstract class EffectController : MonoBehaviour, IBuffApplicable
{
    public BuffDebuffHandler BuffDebuff { get; protected set; }
    public StatusEffectHandler StatusEffect { get; protected set; }

    protected virtual void Awake()
    {
        BuffDebuff = new BuffDebuffHandler();
    }

    private void Update()
    {
        BuffDebuff?.Tick(Time.deltaTime);
        StatusEffect?.Tick(Time.deltaTime);
    }

    public void ApplyBuff(StatModifier modifier)
    {
        BuffDebuff.AddOrRefreshModifier(modifier);
    }

    public void ApplyStatusEffect(StatusEffectBase effect)
    {
        StatusEffect.Apply(effect);
    }
    public abstract float GetModifiedStat(StatType type, float baseValue);

    public void ClearAll()
    {
        BuffDebuff?.ClearModifiers();
        StatusEffect?.ClearAll();
    }
}