using System.Collections;
using UnityEngine;
public class WarpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementSubject subject, Vector3 direction, skillModule1 data)
    {
        Vector3 targetPos = subject.Position + (direction * data.M1Scale);

        subject.SetPosition(targetPos);

        if (data.M1Duration > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.M1Duration);
        }
    }
}