using UnityEngine;

/// <summary>
/// 스킬로 옮겨질 대상이 구현해야하는 인터페이스
/// </summary>
public interface ISkillMovementSubject
{
    Transform transform { get; }
    Vector3 Position { get; }

    void SetPosition(Vector3 position);

    void SetInvincible(bool active);
    void PlayAnim(string key);
}