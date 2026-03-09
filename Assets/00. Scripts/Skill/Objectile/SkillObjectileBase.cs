using UnityEngine;

public abstract class SkillObjectileBase : MonoBehaviour, ISkillDetectable
{
    protected ISkillHitHandler owner;     
    protected SkillData data;             
    protected LayerMask targetLayer;       

    protected Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];

    public Collider[] GetBuffer() => hitBuffer;

    protected SkillDataContext skillDataContext;
    public SkillDataContext GetSkillDataContext() => skillDataContext;

    protected Vector3 debugLastCastPos;
    protected Vector3 debugLastCastDir;

    public virtual void Initialize(ISkillHitHandler _owner, SkillDataContext _skillDataContext, LayerMask layer)
    {
        owner = _owner;
        skillDataContext = _skillDataContext;
        targetLayer = layer;
        data= skillDataContext.skillData;
        debugLastCastPos =transform.position;

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