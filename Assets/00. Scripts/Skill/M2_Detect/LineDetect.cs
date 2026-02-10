using UnityEngine;

public class LineDetect : IDetectShapeStrategy
{
    public int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer)
    {
        // 중심은 시전자 위치
        Vector3 halfExtents = new Vector3(data.M2S2 * 0.5f, 2f, data.M2S1 * 0.5f);
        Quaternion orientation = Quaternion.LookRotation(direction);
        return Physics.OverlapBoxNonAlloc(center, halfExtents, buffer, orientation, targetLayer);
    }

    public void DrawGizmo(Vector3 center, Vector3 direction, skillModule2 data)
    {
        Gizmos.color = Color.yellow;

        // 회전을 반영하기 위해 매트릭스 설정
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(direction), Vector3.one);

        // 박스 그리기 (DrawWireCube는 전체 크기(Size)를 받음 = Extents * 2)
        // 중심은 이미 TRS로 잡았으므로 Vector3.zero
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(data.M2S2, 4f, data.M2S1));

        // 매트릭스 복구 (필수)
        Gizmos.matrix = oldMatrix;
    }
}