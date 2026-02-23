using UnityEngine;

/// <summary>
/// 스킬 공격형 몬스터용 스킬 시전 핸들러.
/// 플레이어를 타겟으로 스킬을 시전하며, SkillCaster와 연동.
/// </summary>
[RequireComponent(typeof(SkillCaster))]
public class EnemySkillHandler : MonoBehaviour, ISkillStatProvider, ISkillTargetProvider
{
    [SerializeField][Tooltip("시전할 스킬 ID (DataManager SkillInfoDict에 등록된 ID)")]
    private int skillId = 4000001;

    private SkillCaster _skillCaster;
    private EnemyStatPresenter _statPresenter;
    private Transform _playerTransform;
    private SkillDataContext _skillDataContext;
    private float _cooldownTimer;

    public bool IsCasting => _skillCaster != null && _skillCaster.IsCasting;
    public bool IsCooldownReady => _cooldownTimer <= 0f;

    private void Awake()
    {
        _skillCaster = GetComponent<SkillCaster>();
        _statPresenter = GetComponent<EnemyStatPresenter>();
    }

    /// <summary>
    /// 플레이어 Transform 설정. EnemyStateMachine에서 주입.
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        _playerTransform = player;
    }

    /// <summary>
    /// 스킬 ID 설정. DataManager 로드 후 호출.
    /// </summary>
    public void Init(int skillIdToUse)
    {
        skillId = skillIdToUse;
        if (DataManager.Instance != null && DataManager.Instance.SkillInfoDict != null)
        {
            _skillDataContext = new SkillDataContext(skillId, -1, -1);
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
    }

    /// <summary>
    /// 스킬 시전 시도. 사거리·쿨다운 체크 후 시전.
    /// </summary>
    /// <returns>시전 성공 여부</returns>
    public bool TryCastSkill()
    {
        if (_skillCaster == null || _skillCaster.IsCasting) return false;
        if (_cooldownTimer > 0f) return false;
        if (_playerTransform == null) return false;

        if (_skillDataContext == null || _skillDataContext.skillData?.skillTable == null)
        {
            if (DataManager.Instance != null)
                _skillDataContext = new SkillDataContext(skillId, -1, -1);
            if (_skillDataContext?.skillData?.skillTable == null)
            {
                Debug.LogWarning($"[EnemySkillHandler] 스킬 ID {skillId} 로드 실패. DataManager 확인.");
                return false;
            }
        }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        float skillRange = _skillDataContext.skillData.skillTable.skillRange;
        if (dist > skillRange) return false;

        _skillCaster.Init(this, this);
        _skillCaster.CastSkill(_skillDataContext, 0f, false);
        _cooldownTimer = _skillDataContext.skillData.skillTable.skillCooldown;
        return true;
    }

    public float GetAttack() => _statPresenter?.Data?.monsterAttackpoint ?? 10f;
    public float GetCriticalChance() => 0f;
    public float GetCriticalMulti() => 1f;

    public Transform GetTarget() => _playerTransform;
}
