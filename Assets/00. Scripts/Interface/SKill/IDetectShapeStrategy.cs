using UnityEngine;

/// <summary>
/// 스킬 검출 형태 전략 인터페이스
/// </summary>
public interface IDetectShapeStrategy
{
    /// <summary>
    /// 데이터값 넣어서 형태에 맞는 검출 수행
    /// </summary>
    /// <param name="center">발동 중심점</param>
    /// <param name="direction">시전 방향</param>
    /// <param name="data">데이터 테이블</param>
    /// <param name="buffer">유닛들 콜라이더 저장해둘 배열</param>
    /// <param name="targetLayer"></param>
    /// <returns>검출한 유닛 갯수 반환</returns>
    int Detect(Vector3 center, Vector3 direction, skillModule2 data, Collider[] buffer, int targetLayer);
    public void DrawGizmo(Vector3 center, Vector3 direction, skillModule2 data);
}