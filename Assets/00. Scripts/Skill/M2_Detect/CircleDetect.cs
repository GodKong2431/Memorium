using UnityEngine;

public class CircleDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, SkillModule2Table data, ISkillDetectable provider, int targetLayer)
    {
        Collider[] buffer = provider.GetBuffer();

        float cylinderHalfHeight = SkillConstants.DETECT_HEIGHT;

        Vector3 pointBottom = center - (Vector3.up * cylinderHalfHeight);
        Vector3 pointTop = center + (Vector3.up * cylinderHalfHeight);

        return Physics.OverlapCapsuleNonAlloc(pointBottom, pointTop, data.m2S1, buffer, targetLayer);
    }
    public void DrawGizmo(Vector3 center, Vector3 direction, SkillModule2Table data)
    {
        Gizmos.color = Color.red;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(1, 0.01f, 1));

        Gizmos.DrawWireSphere(Vector3.zero, data.m2S1);

        Gizmos.matrix = oldMatrix;

        Gizmos.DrawLine(center, center + Vector3.up * 2f);
    }
}