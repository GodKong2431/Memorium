using UnityEngine;
using System.Collections;

public class JumpMove : ISkillMovementStrategy
{
    public IEnumerator SkillMove(ISkillMovementSubject subject, Vector3 direction, skillModule1 data)
    {
        //subject.PlayAnim("Jump");
        subject.SetInvincible(true); 

        Vector3 startPos = subject.Position;
        Vector3 endPos = startPos + (direction * data.M1Scale);

        float jumpHeight = data.M1Scale * 0.3f;
        if (jumpHeight < 1.0f) jumpHeight = 1.0f;

        float elapsedTime = 0f;
        if (data.M1Duration <= 0)
        {
            subject.SetPosition(endPos);
            subject.SetInvincible(false);
            yield break;
        }

        while (elapsedTime < data.M1Duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / data.M1Duration;

            // 1. 수평 이동 (Lerp)
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

            // 2. 수직 이동 (포물선 공식: 4 * h * t * (1-t))
            // t가 0~1로 갈 때, y는 0 -> 1(최고점) -> 0이 됨
            currentPos.y += 4f * jumpHeight * t * (1f - t);

            subject.SetPosition(currentPos);

            yield return null;
        }

        // 착지
        subject.SetPosition(endPos);
        subject.SetInvincible(false); // 무적 해제
    }
}