using UnityEngine;

public class SectorDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer)
    {
        float cylinderHalfHeight = 10f;
        Vector3 pointBottom = center - (Vector3.up * cylinderHalfHeight);
        Vector3 pointTop = center + (Vector3.up * cylinderHalfHeight);
        int count = Physics.OverlapCapsuleNonAlloc(pointBottom, pointTop, data.M2S1, buffer, targetLayer);

        int validCount = 0;

        float cosThreshold = Mathf.Cos((data.M2S2 * 0.5f) * Mathf.Deg2Rad);

        Vector3 flatForward = direction;
        flatForward.y = 0;
        if (flatForward == Vector3.zero) flatForward = Vector3.forward; 
        flatForward.Normalize();

        for (int i = 0; i < count; i++)
        {
            Vector3 targetPos = buffer[i].transform.position;
            targetPos.y = center.y;

            Vector3 dirToTarget = targetPos - center;

            if (dirToTarget.sqrMagnitude < 0.0001f) continue;

            dirToTarget.Normalize();

            if (Vector3.Dot(flatForward, dirToTarget) >= cosThreshold)
            {
                buffer[validCount] = buffer[i];
                validCount++;
            }
        }

        return validCount;
    }

    public void DrawGizmo(Vector3 center, Vector3 direction, skillModule2 data)
    {
        Gizmos.color = Color.green;

        float halfAngle = data.M2S2 * 0.5f;
        float radius = data.M2S1;

        Vector3 flatDir = direction;
        flatDir.y = 0;
        if (flatDir == Vector3.zero) flatDir = Vector3.forward;
        flatDir.Normalize();

        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);

        Vector3 leftDir = leftRot * flatDir;
        Vector3 rightDir = rightRot * flatDir;

        Gizmos.DrawLine(center, center + leftDir * radius);
        Gizmos.DrawLine(center, center + rightDir * radius);

        Vector3 prevPos = center + leftDir * radius;
        int segments = 10;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 currentDir = Vector3.Slerp(leftDir, rightDir, t);
            Vector3 currentPos = center + currentDir * radius;

            Gizmos.DrawLine(prevPos, currentPos);
            prevPos = currentPos;
        }
    }
}
