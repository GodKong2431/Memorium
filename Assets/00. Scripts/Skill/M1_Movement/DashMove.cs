using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class DashMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillCasterMovement subject, Vector3 target, SkillModule1Table data)
    {
        Vector3 direction = (target - subject.Position);
        direction.y = 0;
        direction = direction.normalized;

        float speed = data.m1Scale / data.m1Duration;

        if (data.m1Duration <= 0)
        {
            Vector3 endPos = subject.Position + direction * data.m1Scale;
            endPos.y = subject.Position.y;
            subject.SetPosition(endPos);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < data.m1Duration)
        {
            elapsedTime += Time.deltaTime;
            float step = speed * Time.deltaTime;
            Vector3 nextPos = subject.Position + direction * step;
            nextPos.y = subject.Position.y;
            subject.SetPosition(nextPos);
            yield return null;
        }
    }
}