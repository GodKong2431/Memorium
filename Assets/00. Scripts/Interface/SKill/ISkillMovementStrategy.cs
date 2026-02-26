using UnityEngine;
using System.Collections;

/// <summary>
/// 스킬  이동 전략 인터페이스(m1)
/// </summary>
public interface ISkillMovementStrategy
{
    IEnumerator SkillMove(ISkillCasterMovement subject, Vector3 target, SkillModule1Table module1data);
}