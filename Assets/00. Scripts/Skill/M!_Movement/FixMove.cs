using System.Collections;
using UnityEngine;

public class FixMove : ISkillMovementStrategy
{

    public IEnumerator SkillMove(ISkillMovementSubject subject, Vector3 direction, skillModule1 data)
    {
        if (data.M1Duration > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.M1Duration);
        }
    }
}