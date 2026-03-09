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
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.isStopped = true;
        ctx.SetAnimatorTrigger("Dead");
        // 사망 이펙트 추가 예정
        // 사망 효과음 추가 예정
        _destroyTime = Time.time + DestroyDelay;
        _destroyScheduled = false;

        // 보상: 골드·경험치·아이템 (수식은 EnemyRewardData / EnemyRewardCalculator에서 관리)
        // EnemyStatSetting 대신 StageManager에 설정된 보상 데이터를 사용
        EnemyRewardData rewardData = null;
        if (StageManager.Instance != null)
        {
            rewardData = ctx.IsBoss
                ? StageManager.Instance.bossEnemyReward
                : StageManager.Instance.normalEnemyReward;
        }
        if (rewardData != null)
        {
            Vector3 pos = ctx.EnemyTransform != null ? ctx.EnemyTransform.position : Vector3.zero;
            EnemyKillRewardDispatcher.GrantRewards(rewardData, isBoss: ctx.IsBoss, stageLevel: EnemyKillRewardDispatcher.CurrentStageLevel, worldPosition: pos);
        }
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (_destroyScheduled) return;
        if (Time.time < _destroyTime) return;

        _destroyScheduled = true;
        GameObject enemyObject = ctx.EnemyTransform.gameObject;

        if (ObjectPoolManager.IsPooled(enemyObject))
            ObjectPoolManager.Return(enemyObject);
        else
            Object.Destroy(enemyObject);
    }

    public void OnExit(EnemyStateContext ctx)
    {
        // 보상은 OnEnter에서 이미 지급됨
    }
}
