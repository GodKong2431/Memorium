using System.Collections;
using UnityEngine;
using UnityEngine.AI;

//벽에 막히는 기능추가
public class JumpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillCasterMovement subject, Vector3 target, SkillModule1Table data)
    {
        subject.SetInvincible(true);


        Vector3 direction = (target - subject.Position);
        direction.y = 0;
        direction = direction.normalized;

        Vector3 startPos = subject.Position;
        Vector3 endPos = startPos + (direction * data.m1Scale); 
        if (NavMesh.SamplePosition(endPos, out var navHit, SkillConstants.NAV_SEARCH_RADIUS, NavMesh.AllAreas))
            endPos = navHit.position;
        endPos.y = subject.Position.y;

        float jumpHeight = data.m1Scale * 0.3f;
        
        if (jumpHeight < SkillConstants.JUMP_MIN_HEIGHT) jumpHeight = SkillConstants.JUMP_MIN_HEIGHT;

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
