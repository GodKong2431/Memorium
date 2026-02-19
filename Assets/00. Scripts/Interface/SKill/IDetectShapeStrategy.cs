using UnityEngine;

/// <summary>
/// 스킬 검출 형태 전략 인터페이스 (m2)
/// </summary>
public interface IDetectShapeStrategy
{
    /// <summary>
    /// IHitBufferProvider를 통해 전달받은 버퍼에 형태에 맞는 검출 수행
    /// </summary>
    int Detect(Vector3 center, Vector3 direction, SkillModule2Table data, ISkillDetectable provider, int targetLayer);

    void DrawGizmo(Vector3 center, Vector3 direction, SkillModule2Table data);
}