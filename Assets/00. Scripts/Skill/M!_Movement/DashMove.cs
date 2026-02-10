using UnityEngine;
using System.Collections;


public class DashMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementSubject subject, Vector3 direction, skillModule1 data)
    {

        Vector3 startPos = subject.Position;
        Vector3 endPos = startPos + (direction * data.M1Scale);

        float elapsedTime = 0f;

        if (data.M1Duration <= 0)
        {
            subject.SetPosition(endPos);
            yield break;
        }

        while (elapsedTime < data.M1Duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / data.M1Duration;

            Vector3 nextPos = Vector3.Lerp(startPos, endPos, t);
            subject.SetPosition(nextPos);

            yield return null;
        }

        subject.SetPosition(endPos);
    }
}