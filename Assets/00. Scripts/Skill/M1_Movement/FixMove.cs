using System.Collections;
using UnityEngine;

public class FixMove : ISkillMovementStrategy
{

    public IEnumerator SkillMove(ISkillMovementTarget subject, Vector3 direction, SkillModule1Table data)
    {
        if (data.m2Duration > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m2Duration);
        }
    }
}