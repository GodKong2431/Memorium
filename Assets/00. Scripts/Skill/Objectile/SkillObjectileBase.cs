using UnityEngine;

public abstract class SkillObjectileBase : MonoBehaviour, ISkillDetectable
{
    protected ISkillHitHandler owner;     
    protected SkillData data;             
    protected LayerMask targetLayer;

    protected Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];

    protected Collider[] cachedTargets = new Collider[SkillConstants.HIT_BUFFER_SIZE];

    public Collider[] GetBuffer() => hitBuffer;

    protected SkillDataContext dataContext;
    public SkillDataContext GetSkillDataContext() => dataContext;

    protected Vector3 debugLastCastPos;
    protected Vector3 debugLastCastDir;

    public virtual void Initialize(ISkillHitHandler _owner, SkillDataContext dataContext, LayerMask layer)
    {
        owner = _owner;
        this.dataContext = dataContext;
        targetLayer = layer;
        data = this.dataContext.skillData;
        debugLastCastPos =transform.position;

    }
    public Collider[] GetAddonBuffer()
    {
        return cachedTargets; 
    }
    protected virtual void OnDrawGizmos()
    {
        if (data == null || data.m2Data == null) return;

        var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
        if (m2 != null)
        {
            m2.DrawGizmo(debugLastCastPos, transform.forward, data.m2Data);
        }
    }
}