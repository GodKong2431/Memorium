using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;

/// <summary> NavMesh로 플레이어 추격 + 사거리 안이면 공격. destination 갱신 간격·stoppingDistance·Obstacle Avoidance 최적화 적용. </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStatPresenter))]
public class EnemyNavChase : MonoBehaviour
{
    [SerializeField][Tooltip("목적지를 갱신하는 주기입니다.")]
    private float destinationRefreshInterval = 0.25f;
    
    [Header("Prefab")]
    [SerializeField][Tooltip("공격 시 나타나는 이펙트 프리팹입니다.")] 
    private GameObject attackEffectPrefab;

    private Transform player;
    private NavMeshAgent _agent;
    private EnemyStatPresenter _statPresenter;
    private float _lastDestinationTime;
    private bool _isAttacking;
    private GameObject _currentAttackEffect;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _statPresenter = GetComponent<EnemyStatPresenter>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("[EnemyNavChase] 플레이어 오브젝트를 찾을 수 없습니다. 'Player' 태그가 설정되어 있는지 확인하세요.");
            return;
        }

        EnemyStatData data = _statPresenter.Data;
        if (data != null)
        {
            _agent.speed = data.monsterSpeed;
            _agent.stoppingDistance = data.attackRange;
        }

        _lastDestinationTime = -destinationRefreshInterval;
    }

    private void Update()
    {
        if (player == null) return;
        if (_isAttacking) return;

        float dist = Vector3.Distance(transform.position, player.position);
        EnemyStatData data = _statPresenter.Data;
        float attackRange = data != null ? data.attackRange : 1.5f;

        if (dist <= attackRange)
        {
            _agent.isStopped = true;
            TryAttack();
            return;
        }

        _agent.isStopped = false;
        if (Time.time - _lastDestinationTime >= destinationRefreshInterval)
        {
            _lastDestinationTime = Time.time;
            _agent.SetDestination(player.position);
        }
    }

    private void TryAttack()
    {
         // 공격 애니메이션 재생 및 공격 로직 호출 예정
        Debug.Log($"[TryAttack] {gameObject.name}이 공격 시도");

        // 예시용 이펙트 생성
        if (attackEffectPrefab != null)
        {
            if (_currentAttackEffect != null)
                Destroy(_currentAttackEffect);
            _currentAttackEffect = Instantiate(attackEffectPrefab, transform.position + Vector3.up * 1f, Quaternion.identity, transform);
        }

        _isAttacking = true;
        float attackSpeed = _statPresenter.Data?.monsterAttackspeed ?? 1f;
        float delay = attackSpeed > 0f ? 1f / attackSpeed : 0.5f;
        Invoke(nameof(OnAttackEnd), delay);
    }

    private void OnAttackEnd()
    {
        Debug.Log($"[OnAttackEnd] {gameObject.name}이 공격 종료");
        
        if (_currentAttackEffect != null)
        {
            Destroy(_currentAttackEffect);
            _currentAttackEffect = null;
        }
        _isAttacking = false;
    }
}
