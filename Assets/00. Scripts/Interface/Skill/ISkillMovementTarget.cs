using UnityEngine;

/// <summary>
/// 스킬로 옮겨질 대상이 구현해야하는 인터페이스, m1로 이동될 객체가 구현해야함, 
/// 분신 몬스터 플레이어 누구던 인터페이스를 상속받으면 스킬로 이동할수있도록 인터페이스로 작성하였음
/// </summary>
public interface ISkillMovementTarget
{
    Transform transform { get; }
    Vector3 Position { get; }

    void SetPosition(Vector3 position);

    void SetInvincible(bool active);
    void PlayAnim(string key);
}