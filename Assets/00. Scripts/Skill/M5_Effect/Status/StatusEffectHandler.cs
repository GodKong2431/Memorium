
using System.Collections.Generic;

public class StatusEffectHandler
{
    private StatusEffectBase activeEffect;
    //다른 종류의 상태이상이 들어올수있다는 기준으로짰는데, 하나만 적용되는거라 주석처리 함
    //나중에 여러개 적용되는걸로 바꿀수도있으니 일단 남겨둠
    //private List<StatusEffectBase> activeEffects = new List<StatusEffectBase>();
    private IBuffApplicable buffTarget;
    private IDamageable owner;

    public StatusEffectHandler(IBuffApplicable target, IDamageable enemyStateMachine)
    {
        this.buffTarget = target;
        owner = enemyStateMachine;
    }
    public void Apply(StatusEffectBase effect)
    {
        //for (int i = 0; i < activeEffects.Count; i++)
        //{
        //    if (activeEffects[i].GetType() == effect.GetType())
        //    {
        //        activeEffects[i].OnApply(owner, buffTarget);
        //        return;
        //    }
        //}
        //effect.OnApply(owner, buffTarget);
        //activeEffects.Add(effect);
        if (effect == null) return;
        if (activeEffect != null)
        {
            if (activeEffect.GetType() == effect.GetType())
                activeEffect.OnApply(owner, buffTarget);
            return;
        }
        effect.OnApply(owner, buffTarget);
        activeEffect = effect;

    }
    public void Tick(float deltaTime)
    {

        if (activeEffect == null) return;
        activeEffect.Tick(deltaTime);

        if (activeEffect.IsExpired)
        {
            activeEffect.OnExpire();
            activeEffect = null;
        }


        //for (int i = activeEffects.Count - 1; i >= 0; i--)
        //{
        //    var effect = activeEffects[i];
        //    effect.Tick(deltaTime);
        //    if (effect.IsExpired)
        //    {
        //        effect.OnExpire();
        //        activeEffects[i] = activeEffects[activeEffects.Count - 1]; 
        //        activeEffects.RemoveAt(activeEffects.Count - 1);
        //    }
        //} 
    }
    public void OnDeath()
    {
        if (activeEffect == null) return;
        activeEffect.OnTargetDeath();
        activeEffect = null;
        //foreach (var effect in activeEffects) effect.OnTargetDeath();
        //activeEffects.Clear();
    }
    public bool HasActive()
    {
        return activeEffect != null;
        //상태이상 하나만 적용된다는 기준으로 짰는데, 여러개 적용되는걸로 바꿀수도있으니 일단 남겨둠
        //for (int i = 0; i < activeEffects.Count; i++)
        //{
        //    if (activeEffects[i].GetType() == effect.GetType())
        //    {
        //        activeEffects[i].OnApply(owner, buffTarget);
        //        return true;
        //    }
        //}
        //return false;
    }
    public void ClearAll()
    {
        if (activeEffect == null) return;
        activeEffect.OnExpire();
        activeEffect = null;
        //    foreach (var effect in activeEffects) effect.OnExpire();
        //    activeEffects.Clear();
    }


}