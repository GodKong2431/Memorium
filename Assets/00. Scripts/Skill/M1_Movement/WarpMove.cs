using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WarpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillCasterMovement subject, Vector3 target, SkillModule1Table data)
    {

        if (data.m1Duration > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m1Duration);
        }


        Vector3 finalTarget = subject.GetTargetPosition();
        if (NavMesh.SamplePosition(finalTarget, out var hit, SkillConstants.NAV_SEARCH_RADIUS, NavMesh.AllAreas))
            finalTarget = hit.position;
        finalTarget.y = subject.Position.y;

        subject.SetPosition(finalTarget);
    }
}