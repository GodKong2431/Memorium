using UnityEngine;

public class CircleDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer)
    {
        float cylinderHalfHeight = 10f;

        Vector3 pointBottom = center - (Vector3.up * cylinderHalfHeight);
        Vector3 pointTop = center + (Vector3.up * cylinderHalfHeight);

        return Physics.OverlapCapsuleNonAlloc(pointBottom, pointTop, data.M2S1, buffer, targetLayer);
    }

    public void DrawGizmo(Vector3 center, Vector3 direction, skillModule2 data)
    {
        Gizmos.color = Color.red;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(1, 0.01f, 1));

        Gizmos.DrawWireSphere(Vector3.zero, data.M2S1);

        Gizmos.matrix = oldMatrix;

        Gizmos.DrawLine(center, center + Vector3.up * 2f);
    }
}