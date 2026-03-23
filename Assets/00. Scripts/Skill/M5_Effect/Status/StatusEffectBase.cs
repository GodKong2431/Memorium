using UnityEngine;
using UnityEngine.Rendering;
public interface IBuffApplicable
{
    void ApplyBuff(StatModifier modifier);
}
public abstract class StatusEffectBase
{
    protected IDamageable target;
    protected IBuffApplicable buffApplicable;
    protected float duration;
    protected float elapsedTime;
    protected float tickTimer;
    protected float tickInterval;
    protected float damage;

    private static readonly Collider[] hitBuffer = new Collider[20];

    protected static Collider[] GetHitBuffer() => hitBuffer;
    public bool IsExpired => duration > 0 && elapsedTime >= duration;

    public virtual void OnApply(IDamageable target, IBuffApplicable buffApplicable)
    {
        this.target = target;
        this.buffApplicable = buffApplicable;
        elapsedTime = 0f;
        tickTimer = 0f; 


    }
    public void Tick(float deltaTime)
    {
        elapsedTime += deltaTime;
        tickTimer += deltaTime;

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            OnTick();
        }
    }
    protected abstract void OnTick();
    public virtual void OnExpire() 
    { 
        elapsedTime = 0f; 
    }
    public virtual void OnTargetDeath() 
    { 
        elapsedTime = 0f; 
    }
    protected int DetectInRadius(Vector3 center, float radius, int layerMask)
    {
        float halfHeight = SkillConstants.DETECT_HEIGHT;
        Vector3 bottom = center - Vector3.up * halfHeight;
        Vector3 top = center + Vector3.up * halfHeight;
        return Physics.OverlapCapsuleNonAlloc(bottom, top, radius, hitBuffer, layerMask);
    }
    public void Refresh()
    {
        elapsedTime = 0f;
    }
}