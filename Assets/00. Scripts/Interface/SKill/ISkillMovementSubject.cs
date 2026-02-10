using UnityEngine;

public interface ISkillMovementSubject
{
    Transform transform { get; }
    Vector3 Position { get; }

    void SetPosition(Vector3 position);

    void SetInvincible(bool active);
    void PlayAnim(string key);
}