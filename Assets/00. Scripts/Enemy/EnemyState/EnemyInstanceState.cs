using UnityEngine;

/// <summary>
/// IEnemyState 구현은 타입당 싱글톤이므로, 몬스터 인스턴스마다 달라야 하는 값은 컨텍스트에 둔다.
/// 오브젝트 풀 Get/Return 시 <see cref="EnemyStateMachine"/>에서 초기화한다.
/// </summary>
public sealed class EnemyInstanceState
{
    // EnemyStateAttack
    public float AttackEndTime;
    public bool AttackInProgress;
    public GameObject CurrentAttackEffect;
    public bool IsSkillAttack;
    public bool DamageApplied;
    public BossManageTable CurrentBossAttack;

    // EnemyStateOnhit
    public float OnhitEndTime;
    public KnockbackInfo OnhitKnockbackInfo;
    public float OnhitKnockbackSpeed;
    public float OnhitKnockbackElapsed;
    public bool OnhitKnockbackActive;

    // EnemyStateSpawn
    public float SpawnEndTime;

    // EnemyStateDead
    public float DeadDestroyTime;
    public bool DeadDestroyScheduled;

    // EnemyStateChase (DestinationRefreshInterval 0.25 와 동일한 초기 스킵 패턴)
    public float ChaseLastDestinationTime = -0.25f;

    public void Reset()
    {
        AttackEndTime = 0f;
        AttackInProgress = false;
        CurrentAttackEffect = null;
        IsSkillAttack = false;
        DamageApplied = false;
        CurrentBossAttack = null;

        OnhitEndTime = 0f;
        OnhitKnockbackInfo = default;
        OnhitKnockbackSpeed = 0f;
        OnhitKnockbackElapsed = 0f;
        OnhitKnockbackActive = false;

        SpawnEndTime = 0f;

        DeadDestroyTime = 0f;
        DeadDestroyScheduled = false;

        ChaseLastDestinationTime = -0.25f;
    }
}
