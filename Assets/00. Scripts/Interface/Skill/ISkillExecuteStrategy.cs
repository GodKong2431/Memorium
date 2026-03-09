using UnityEngine;
using System.Collections;

/// <summary>
/// 스킬 실행 방식 인터페이스(m3)
/// </summary>
public interface ISkillExecuteStrategy
{
    /// <summary>
    /// 스킬의 실행
    /// (즉시 타격, 투사체 생성, 장판 설치 등)
    /// </summary>
    /// <param name="owner">데미지 처리할 주체</param>
    /// <param name="bufferProvider">범위 계산을 위한 배열 제공자 owner랑 같은 객체면 좋음</param>
    /// <returns></returns>
    IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer, GameObject prefab);
}