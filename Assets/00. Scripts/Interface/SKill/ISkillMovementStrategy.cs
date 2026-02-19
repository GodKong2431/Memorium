using UnityEngine;
using System.Collections;

/// <summary>
/// 스킬  이동 전략 인터페이스(m1)
/// </summary>
public interface ISkillMovementStrategy
{
    IEnumerator SkillMove(ISkillMovementTarget Target, Vector3 direction, SkillModule1Table data);
}