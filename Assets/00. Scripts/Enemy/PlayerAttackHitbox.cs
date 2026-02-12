using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player 하위 AttackIndicator에 붙어서,
/// 콜라이더(Trigger)로 적과의 충돌을 감지해 피격 판정을 내리는 스크립트.
/// - Enemy_PlayerMove의 공격 버튼 입력 ⇒ AttackIndicator 활성화
/// - 이 스크립트의 OnTriggerEnter에서 EnemyStateMachine.TakeDamage 호출
/// </summary>
public class PlayerAttackHitbox : MonoBehaviour
{
    [Tooltip("이 히트박스를 소유한 플레이어 이동/공격 스크립트")]
    [SerializeField] private Enemy_PlayerMove owner;

    private readonly HashSet<EnemyStateMachine> _hitEnemies = new HashSet<EnemyStateMachine>();

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<Enemy_PlayerMove>();
    }

    private void OnEnable()
    {
        // 공격 한 번 당 중복 피격 방지를 위해 초기화
        _hitEnemies.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;

        // 레이어 필터: 플레이어가 설정한 enemyLayerMask에 속한 것만 인정
        if ((owner.EnemyLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        var enemy = other.GetComponentInParent<EnemyStateMachine>();
        if (enemy == null) return;

        if (_hitEnemies.Contains(enemy)) return; // 한 공격 내에서 중복 타격 방지
        _hitEnemies.Add(enemy);

        enemy.TakeDamage(owner.AttackDamage);
        //Debug.Log($"[PlayerAttackHitbox] {enemy.name} 피격, 데미지: {owner.AttackDamage}");
    }
}

