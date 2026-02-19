using UnityEngine;
using System.Collections;


//벽에 막히는 기능 추가
public class DashMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementTarget subject, Vector3 direction, SkillModule1Table data)
    {
        
        Vector3 startPos = subject.Position;
        Vector3 endPos = startPos + (direction * data.m1Scale);

        float elapsedTime = 0f;

        if (data.m1Duration <= 0)
        {
            subject.SetPosition(endPos);
            yield break;
        }

        while (elapsedTime < data.m1Duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / data.m1Duration;

            Vector3 nextPos = Vector3.Lerp(startPos, endPos, t);
            subject.SetPosition(nextPos);

            yield return null;
        }

        subject.SetPosition(endPos);
    }
}