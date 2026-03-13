
using UnityEngine;

public class EffectController : MonoBehaviour, IBuffApplicable
{
    public BuffDebuffHandler BuffDebuff { get; protected set; }
    public StatusEffectHandler StatusEffect { get; protected set; }

    private void Awake()
    {
        BuffDebuff = new BuffDebuffHandler();
        var enemy = GetComponent<EnemyStateMachine>();
        if (enemy != null)
            StatusEffect = new StatusEffectHandler(this, enemy);
    }

    private void Update()
    {
        BuffDebuff?.Tick(Time.deltaTime);
        StatusEffect?.Tick(Time.deltaTime);
    }

    private void OnDisable()
    {
        ClearAll();
    }

    public void ApplyBuff(StatModifier modifier)
    {
        BuffDebuff.AddOrRefreshModifier(modifier);
    }

    public void ApplyStatusEffect(StatusEffectBase effect)
    {
        StatusEffect.Apply(effect);
    }
    public float GetModifiedStat(StatType type, float baseValue)
    {
        float buff = BuffDebuff.GetTotal(type);
        return baseValue + buff;
    }
    public bool HasStatusEffect()
    {
        return StatusEffect != null && StatusEffect.HasActive();
    }
    public void ClearAll()
    {
        BuffDebuff?.ClearModifiers();
        StatusEffect?.ClearAll();
    }
}