using System.Collections;
using UnityEngine;

//해당방향으로 이동이 아닌 목표 객체의 위치로 순간이동으로 바꿔야함
public class WarpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementTarget subject, Vector3 direction, SkillModule1Table data)
    {
        Vector3 targetPos = subject.Position + (direction * data.m1Scale);

        subject.SetPosition(targetPos);

        if (data.m1Duration > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m1Duration);
        }
    }
}