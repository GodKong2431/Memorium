using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 임시 이동 (나중에 플레이어 AI로 대체 예정)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Enemy_PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;    // 이동 방향으로 회전 속도
    [Header("공격 설정")]
    //[SerializeField] private float attackRange = 2f;      // 플레이어 전방 거리 (대략 2)
    //[SerializeField] private float attackRadius = 0.75f;  // 폭 (원형 범위 반지름)
    [SerializeField] private float attackDamage = 10f;    // 한 번 공격 시 줄 데미지
    [SerializeField] private LayerMask enemyLayerMask;    // 적이 속한 레이어만 감지
    [Header("공격 시각 효과")]
    [SerializeField] private GameObject attackIndicator;       // Player 하위에 있는 AttackIndicator 오브젝트
    [SerializeField] private float attackIndicatorDuration = 0.2f; // 표시 유지 시간(초)

    private CharacterController _cc;
    private Coroutine _indicatorCoroutine;

    public float AttackDamage => attackDamage;
    public LayerMask EnemyLayerMask => enemyLayerMask;

    [Header("임시 스킬캐스터")]
    [SerializeField] private SkillCaster skillCaster;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        skillCaster = GetComponent<SkillCaster>();
        // 시작 시에는 꺼 두기
        if (attackIndicator != null)
            attackIndicator.SetActive(false);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        float h = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float v = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            // 이동
            _cc.Move(dir * (moveSpeed * Time.deltaTime));

            // 이동 방향으로 부드럽게 회전
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // 좌클릭 시 근접 공격
        if (mouse.leftButton.wasPressedThisFrame)
        {
            //Debug.Log("[PlayerAttack] 근접 공격!");
            PerformAttack();
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SkillDataContext skillDataContext = new SkillDataContext(4000001);
            skillCaster.CastSkill(skillDataContext);

        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SkillDataContext skillDataContext = new SkillDataContext(4000002);
            skillCaster.CastSkill(skillDataContext);
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            SkillDataContext skillDataContext = new SkillDataContext(4000003);
            skillCaster.CastSkill(skillDataContext);
        }

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            SkillDataContext skillDataContext = new SkillDataContext(4000004);
            skillCaster.CastSkill(skillDataContext);
        }

    }

    /// <summary>
    /// 공격 입력 처리. 실제 피격 판정은 AttackIndicator의 콜라이더에서 처리
    /// </summary>
    private void PerformAttack()
    {
        // 공격 범위 시각 효과 On/Off
        ShowAttackIndicator();
    }

    /// <summary>
    /// Player 하위 AttackIndicator 오브젝트를 잠시 켰다가 끄는 함수
    /// </summary>
    private void ShowAttackIndicator()
    {
        if (attackIndicator == null) return;

        if (_indicatorCoroutine != null)
            StopCoroutine(_indicatorCoroutine);

        _indicatorCoroutine = StartCoroutine(AttackIndicatorRoutine());
    }

    private IEnumerator AttackIndicatorRoutine()
    {
        attackIndicator.SetActive(true);
        yield return new WaitForSeconds(attackIndicatorDuration);
        attackIndicator.SetActive(false);
    }
}
