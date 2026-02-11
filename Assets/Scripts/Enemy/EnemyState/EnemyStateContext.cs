using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 상태 머신과 각 상태가 공유하는 컨텍스트.
/// 플레이어/에이전트(적)/스탯/현재 체력/상태 전환 요청 등.
/// </summary>
public class EnemyStateContext
{
    public Transform EnemyTransform { get; set; }
    public Transform PlayerTransform { get; set; }
    public NavMeshAgent Agent { get; set; }
    public EnemyStatPresenter StatPresenter { get; set; }
    public Animator Animator { get; set; }

    /// <summary>
    /// 최초 스폰 위치 (리스폰 시 사용)
    /// </summary>
    public Vector3 SpawnPosition { get; set; }

    /// <summary>
    /// 현재 체력 (런타임). 피격 시 감소, 0 이하면 Dead 전환
    /// </summary>
    public float CurrentHealth { get; private set; }

    public float MaxHealth => StatPresenter?.Data?.monsterHealth ?? 100f;
    public float AttackRange => StatPresenter?.Data?.attackRange ?? 1.5f;
    public bool IsBoss { get; set; }

    /// <summary>
    /// 공격 시 생성할 이펙트 프리팹. EnemyStateMachine에서 설정
    /// </summary>
    public GameObject AttackEffectPrefab { get; set; }

    private Action<EnemyStateType> _requestStateChange;

    public void Initialize(float? startHealth = null)
    {
        CurrentHealth = startHealth ?? MaxHealth;
    }

    public void SetStateChangeCallback(Action<EnemyStateType> callback)
    {
        _requestStateChange = callback;
    }

    public void RequestState(EnemyStateType next)
    {
        _requestStateChange?.Invoke(next);
    }

    /// <summary>
    /// 피격 시 호출. 데미지 적용 후 Onhit 상태로 전환할지 등은 StateMachine에서 처리
    /// </summary>
    public void TakeDamage(float damage)
    {
        //Debug.Log($"[EnemyStateContext] TakeDamage: {damage}");

        CurrentHealth -= damage;
    }

    /// <summary>
    /// 플레이어가 존재하고 살아있는지. (PlayerHealth 등 체력 컴포넌트가 있으면 해당 로직으로 확장 가능)
    /// </summary>
    public bool IsPlayerAlive()
    {
        // 현재는 플레이어 존재 여부만 체크
        if (PlayerTransform == null) return false;
        return true;
    }
}
