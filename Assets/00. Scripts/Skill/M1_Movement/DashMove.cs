using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class DashMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementTarget subject, Vector3 target, SkillModule1Table data)
    {
        Vector3 direction = (target - subject.Position);
        direction.y = 0;
        direction = direction.normalized;

        float speed = data.m1Scale / data.m1Duration;

        if (data.m1Duration <= 0)
        {
            Vector3 endPos = subject.Position + direction * data.m1Scale;
            if (NavMesh.SamplePosition(endPos, out var hit, SkillConstants.NAV_SEARCH_RADIUS, NavMesh.AllAreas))
                endPos = hit.position;
            subject.SetPosition(endPos);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < data.m1Duration)
        {
            elapsedTime += Time.deltaTime;
            float step = speed * Time.deltaTime;
            Vector3 nextPos = subject.Position + direction * step;
            if (NavMesh.SamplePosition(nextPos, out var hit, SkillConstants.NAV_SEARCH_RADIUS, NavMesh.AllAreas))
                nextPos = hit.position;
            subject.SetPosition(nextPos);
            yield return null;
        }
    }
}