using UnityEngine;

public class CircleDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer)
    {
        return Physics.OverlapSphereNonAlloc(center, data.M2S1, buffer, targetLayer);
    }

    public void DrawGizmo(Vector3 center, Vector3 direction, skillModule2 data)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, data.M2S1);
    }
}