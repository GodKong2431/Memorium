using UnityEngine;

/// <summary>
/// 스킬 추가 효과 전략 인터페이스
/// </summary>
public interface ISkillAddonStrategy { }

//시전 시 발동하는 애드온 (분신)
public interface ISkillCastAddon : ISkillAddonStrategy
{
    void OnCast(ISkillHitHandler handler,ISkillCasterMovement caster, ISkillStatProvider stat, ISkillTargetProvider target, SkillDataContext dataContext, GameObject prefab =null);
}

//타격 시 발동하는 애드온 (넉백, 스플래시)
public interface ISkillHitAddon : ISkillAddonStrategy
{
    void OnHit(ISkillHitHandler caster, Transform target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null);
}