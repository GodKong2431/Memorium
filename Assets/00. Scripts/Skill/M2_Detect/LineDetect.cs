using UnityEngine;

public class LineDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, SkillModule2Table data, ISkillDetectable provider, int targetLayer)
    {
        Collider[] buffer = provider.GetBuffer();

        Vector3 halfExtents = new Vector3(data.m2S2 * 0.5f, 10f, data.m2S1 * 0.5f);
        Quaternion orientation = Quaternion.LookRotation(direction);
        return Physics.OverlapBoxNonAlloc(center, halfExtents, buffer, orientation, targetLayer);
    }

    public void DrawGizmo(Vector3 center, Vector3 direction, SkillModule2Table data)
    {
        Gizmos.color = Color.yellow;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(direction), Vector3.one);

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(data.m2S2, 4f, data.m2S1));

        Gizmos.matrix = oldMatrix;
    }
}