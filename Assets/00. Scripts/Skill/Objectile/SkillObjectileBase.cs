using UnityEngine;

public abstract class SkillObjectileBase : MonoBehaviour, ISkillDetectable
{
    protected ISkillHitHandler owner;     
    protected SkillData data;             
    protected LayerMask targetLayer;       

    protected Collider[] hitBuffer = new Collider[20];

    public Collider[] GetBuffer() => hitBuffer;

    protected SkillDataContext skillDataContext;
    public SkillDataContext GetSkillDataContext() => skillDataContext;

    public virtual void Initialize(ISkillHitHandler _owner, SkillDataContext _skillDataContext, LayerMask layer)
    {
        owner = _owner;
        skillDataContext = _skillDataContext;
        targetLayer = layer;
        data= skillDataContext.skillData;

    }

    protected virtual void OnDrawGizmos()
    {
        if (data == null || data.m2Data == null) return;

        var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
        if (m2 != null)
        {
            m2.DrawGizmo(transform.position, transform.forward, data.m2Data);
        }
    }
}