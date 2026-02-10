using UnityEngine;

public class SectorDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer)
    {
        int count = Physics.OverlapSphereNonAlloc(center, data.M2S1, buffer, targetLayer);
        int validCount = 0;
        float cosThreshold = Mathf.Cos((data.M2S2 * 0.5f) * Mathf.Deg2Rad);

        for (int i = 0; i < count; i++)
        {
            Vector3 dirToTarget = (buffer[i].transform.position - center).normalized;
            if (Vector3.Dot(direction, dirToTarget) >= cosThreshold)
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

        // Gizmos.DrawWireSphere(center, data.m2S1);

        float halfAngle = data.M2S2 * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);

        Vector3 leftDir = leftRot * direction;
        Vector3 rightDir = rightRot * direction;

        Gizmos.DrawLine(center, center + leftDir * data.M2S1);
        Gizmos.DrawLine(center, center + rightDir * data.M2S1);

        Gizmos.DrawLine(center + leftDir * data.M2S1, center + direction * data.M2S1);
        Gizmos.DrawLine(center + rightDir * data.M2S1, center + direction * data.M2S1);
    }
}