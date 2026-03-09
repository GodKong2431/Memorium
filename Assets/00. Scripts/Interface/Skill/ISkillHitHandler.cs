using UnityEngine;

/// <summary>
/// 스킬 히트 처리 인터페이스 
/// </summary>
public interface ISkillHitHandler
{
    Transform transform { get; }
    void HandleSkillHit(int hitCount, SkillDataContext dataContext, Collider[] hitBuffer = null);
    void HandleAddonHit(int hitCount, SkillDataContext dataContext, Collider[] hitBuffer = null);
    public void SetChanneling(bool active);
}