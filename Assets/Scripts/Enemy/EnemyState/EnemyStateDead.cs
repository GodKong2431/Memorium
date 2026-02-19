using UnityEngine;

/// <summary>
/// 몬스터 Dead 상태: 사망 애니메이션·이펙트 출력 후 오브젝트 Destroy.
/// ㄴ 오브젝트 풀 이용해서 Destroy 대신 비활성화 처리 등으로 재활용 고려.
/// ㄴ 아이템, 골드 등 드랍 시스템도 호출.
/// </summary>
public class EnemyStateDead : IEnemyState
{
    private const float DestroyDelay = 1.5f;
    private float _destroyTime;
    private bool _destroyScheduled;

    public EnemyStateType Type => EnemyStateType.Dead;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            ctx.Agent.isStopped = true;

        SetAnimatorTrigger(ctx, "Dead");
        _destroyTime = Time.time + DestroyDelay;
        _destroyScheduled = false;
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (_destroyScheduled) return;
        if (Time.time >= _destroyTime)
        {
            _destroyScheduled = true;
            
            // 현재 적 정보를 복제해서 리스폰
            Transform enemyTransform = ctx.EnemyTransform;
            GameObject enemyObject = enemyTransform.gameObject;

            // 최초 스폰 위치로 리스폰
            Vector3 spawnPos = ctx.SpawnPosition;
            Quaternion spawnRot = enemyTransform.rotation;

            // 새 적 생성 (Start에서 Chase 상태로 진입)
            Object.Instantiate(enemyObject, spawnPos, spawnRot);

            // 기존 적 제거
            Object.Destroy(enemyObject);
        }
    }

    public void OnExit(EnemyStateContext ctx)
    {
        // 아이템 드랍 등 후처리 로직 추가
        Debug.Log("[EnemyStateDead] 몬스터 사망 처리 완료. 아이템 드랍!");
    }

    private static void SetAnimatorTrigger(EnemyStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
