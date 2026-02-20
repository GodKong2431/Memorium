using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

//벽에 막히는 기능추가
public class JumpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementTarget subject, Vector3 target, SkillModule1Table data)
    {
        subject.SetInvincible(true);


        Vector3 direction = (target - subject.Position);
        direction.y = 0;
        direction = direction.normalized;

        Vector3 startPos = subject.Position;
        Vector3 endPos = startPos + (direction * data.m1Scale);

        float jumpHeight = data.m1Scale * 0.3f;
        if (jumpHeight < 1.0f) jumpHeight = 1.0f;

        float elapsedTime = 0f;
        if (data.m1Duration <= 0)
        {
            subject.SetPosition(endPos);
            subject.SetInvincible(false);
            yield break;
        }

        while (elapsedTime < data.m1Duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / data.m1Duration;

            // 수평 이동 
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

            // 수직 이동
            currentPos.y += 4f * jumpHeight * t * (1f - t);

            subject.SetPosition(currentPos);

            yield return null;
        }

        // 착지
        subject.SetPosition(endPos);
        subject.SetInvincible(false); 
    }
}