using UnityEngine;
using System.Collections;
public interface ISkillMovementStrategy
{
    IEnumerator SkillMove(ISkillMovementSubject subject, Vector3 direction, skillModule1 data);
}