using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Chase 상태: 플레이어 위치로 NavMesh 이동, 추적 애니메이션.
/// (구 EnemyNavChase 추격 로직 병합: destination 갱신 간격 0.25초, 사거리 내 시 Attack 전환.)
/// </summary>
public class EnemyStateChase : IEnemyState
{
    [SerializeField][Tooltip("추적을 위한 목적지 갱신 주기입니다.")]
    private const float DestinationRefreshInterval = 0.25f;

    public EnemyStateType Type => EnemyStateType.Chase;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = false;
            ctx.Agent.updateRotation = false;
        }
        ctx.Instance.ChaseLastDestinationTime = -DestinationRefreshInterval;
        ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Chase);
        // 이동/발소리 효과음 추가 예정 (선택)
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        Transform player = ctx.PlayerTransform;
        if (player == null) return;

        // 해당 객체는 스테이지에 소환된 직후 플레이어와의 거리가 공격 사거리보다 클 동안 플레이어 위치로 이동
        // 플레이어와의 거리가 공격 사거리 이하(dist <= AttackRange)가 되는 시점에 전투 상태로 전환

        float dist = ctx.GetBoundsDistanceToPlayer();
        if (dist <= ctx.AttackRange)
        {
            ctx.RequestState(EnemyStateType.Attack);
            return;
        }

        NavMeshAgent agent = ctx.Agent;
        if (agent != null)
        {
            // 이 부분에 버프/디버프 적용된 이동속도 반영하도록 추가했습니다.
            agent.speed = ctx.MoveSpeed;

            // 블렌드 트리(Locomotion)용 파라미터 갱신: 0=Idle, 1=Run 기준
            // Animator Controller에서 Locomotion Blend Tree를 구성해두면, 몬스터별 클립만 Override해도 자연스럽게 동작함.
            float speed01 = 0f;
            float denom = Mathf.Max(0.01f, ctx.MoveSpeed);
            speed01 = Mathf.Clamp01(agent.velocity.magnitude / denom);
            ctx.SetLocomotion(speed01);

            if (agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
            {
                var st = ctx.Instance;
                if (Time.time - st.ChaseLastDestinationTime >= DestinationRefreshInterval)
                {
                    st.ChaseLastDestinationTime = Time.time;
                    agent.SetDestination(player.position);
                }
            }
        }

        RotateTowardsTarget(ctx.EnemyTransform, player.position, ctx.ChaseTurnSpeed);
    }

    public void OnExit(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.updateRotation = true;
    }

    private static void RotateTowardsTarget(Transform self, Vector3 targetPos, float turnSpeed)
    {
        if (self == null || turnSpeed <= 0f)
            return;

        Vector3 toTarget = targetPos - self.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
        float t = Mathf.Clamp01(turnSpeed * Time.deltaTime);
        self.rotation = Quaternion.Slerp(self.rotation, targetRot, t);
    }
}
