using UnityEngine;
using System.Collections;

/// <summary>
/// 스킬  이동 전략 인터페이스
/// </summary>
public interface ISkillMovementStrategy
{
    IEnumerator SkillMove(ISkillMovementSubject subject, Vector3 direction, skillModule1 data);
}