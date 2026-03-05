
using System.Collections.Generic;

public class StatusEffectHandler
{
    private List<StatusEffectBase> activeEffects = new List<StatusEffectBase>();
    private IBuffApplicable buffTarget;
    private IDamageable owner;

    public StatusEffectHandler(IBuffApplicable target, IDamageable enemyStateMachine)
    {
        this.buffTarget = target;
        owner = enemyStateMachine;
    }
    public void Apply(StatusEffectBase effect)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].GetType() == effect.GetType())
            {
                activeEffects[i].OnApply(owner, buffTarget);
                return;
            }
        }
        effect.OnApply(owner, buffTarget);
        activeEffects.Add(effect);
    }
    public void Tick(float deltaTime)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.Tick(deltaTime);
            if (effect.IsExpired)
            {
                effect.OnExpire();
                activeEffects.RemoveAt(i);
            }
        } 
    }
    public void OnDeath()
    {
        foreach (var effect in activeEffects) effect.OnTargetDeath();
        activeEffects.Clear();
    }

    public void ClearAll()
    {
        foreach (var effect in activeEffects) effect.OnExpire();
        activeEffects.Clear();
    }


}