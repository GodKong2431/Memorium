using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 보스 스킬 연출용 StateMachineBehaviour. 임베디드 VFX는 보스 루트 아래에서 이름(또는 선택적 계층 경로)으로 찾는다.
/// <see cref="BossEmbeddedVfxUtility.FindUnderBoss"/>, <see cref="EnemyStateMachine"/> 및 선택적 Attack/공격 타입 조건과 연동한다.
/// </summary>
public class BossSkillVfxSMB : StateMachineBehaviour
{
    [Header("대상 VFX (보스 루트 하위 오브젝트 이름)")]
    [Tooltip("이름만 넣으면 보스 루트부터 전체 자식에서 깊이 우선으로 첫 일치를 찾습니다. '부모/자식' 형태면 해당 경로만 탐색합니다.")]
    [FormerlySerializedAs("embeddedVfxPath")]
    [SerializeField] string embeddedVfxObjectName;
    [SerializeField] bool enableOnEnter = true;

    [Header("보스 로직 조건(선택)")]
    [SerializeField] bool requireEnemyAttackState = true;
    [SerializeField] bool filterByAttackType;
    [SerializeField] AttackType requiredAttackType = AttackType.skillAttack2;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!enableOnEnter) return;
        if (!TryResolve(animator, out var root, out var nameOrPath)) return;
        BossEmbeddedVfxUtility.SetActiveUnderBoss(root, nameOrPath, true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 사망·강제 전환 등으로 EnemyStateType이 Attack이 아닐 때도 반드시 끈다.
        // (기존 TryResolve는 requireEnemyAttackState 때문에 Dead 등에서 false → VFX가 켜진 채 풀링됨)
        if (!TryResolveForForcedHide(animator, out var root, out var nameOrPath)) return;
        BossEmbeddedVfxUtility.SetActiveUnderBoss(root, nameOrPath, false);
    }

    /// <summary>현재 공격/보스 스킬 종류와 관계없이 대상 오브젝트만 찾는다. Off 전용.</summary>
    bool TryResolveForForcedHide(Animator animator, out Transform bossRoot, out string nameOrPath)
    {
        bossRoot = null;
        nameOrPath = null;

        var fsm = animator.GetComponent<EnemyStateMachine>();
        if (fsm == null)
            fsm = animator.GetComponentInParent<EnemyStateMachine>();
        if (fsm == null)
            return false;

        bossRoot = fsm.transform;
        nameOrPath = embeddedVfxObjectName;
        return !string.IsNullOrWhiteSpace(nameOrPath);
    }

    bool TryResolve(Animator animator, out Transform bossRoot, out string nameOrPath)
    {
        bossRoot = null;
        nameOrPath = null;

        var fsm = animator.GetComponent<EnemyStateMachine>();
        if (fsm == null)
            fsm = animator.GetComponentInParent<EnemyStateMachine>();
        if (fsm == null)
            return false;

        bossRoot = fsm.transform;

        if (requireEnemyAttackState && fsm.CurrentStateType != EnemyStateType.Attack)
            return false;

        var ctx = fsm.Context;
        if (ctx == null) return false;

        if (filterByAttackType)
        {
            var cur = ctx.BossAttackManager?.CurrentAttack;
            if (cur == null || cur.attackType != requiredAttackType)
                return false;
        }

        nameOrPath = embeddedVfxObjectName;
        return !string.IsNullOrWhiteSpace(nameOrPath);
    }
}
