using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WarpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementTarget subject, Vector3 target, SkillModule1Table data)
    {
        Debug.Log("À¯´Ö »ç¶óÁü");
        if (data.m1Duration > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m1Duration);
        }

        Debug.Log("À¯´Ö ³ªÅ¸³²");
        Vector3 finalTarget = subject.GetTargetPosition();
        if (NavMesh.SamplePosition(finalTarget, out var hit, SkillConstants.NAV_SEARCH_RADIUS, NavMesh.AllAreas))
            finalTarget = hit.position;

        subject.SetPosition(finalTarget);
    }
}