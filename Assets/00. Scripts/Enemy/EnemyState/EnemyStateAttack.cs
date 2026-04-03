using System;
using UnityEngine;

/// <summary>
/// 몬스터 Attack 상태. 보스는 <see cref="BossManageTable"/> castingDelay/castingTime(타이머)로 히트·종료 시점을 맞추고,
/// 애니는 CSV/트리거 기반. 스킬 VFX 스폰은 AnimatorStateEnteredNotifier·<see cref="EnemyStateMachine.Anim_SpawnSkillPrepareEffect"/> 등.
/// </summary>
public class EnemyStateAttack : IEnemyState
{
    public EnemyStateType Type => EnemyStateType.Attack;

    static bool IsBossSkillPattern(AttackType t) =>
        t == AttackType.skillAttack1 || t == AttackType.skillAttack2;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
            ctx.Agent.updateRotation = false;
        }

        var st = ctx.Instance;
        st.IsSkillAttack = ctx.IsSkillAttackType;

        if (!st.IsSkillAttack)
        {
            st.DamageApplied = false;
            float delay = 0.5f;
            st.AttackEndTime = Time.time + delay;
            st.AttackInProgress = true;
            st.SkillPrepareReturnTime = -1f;

            bool skillTypeMonster = ctx.StatPresenter != null && ctx.StatPresenter.IsSkillAttackMonster;
            if (skillTypeMonster)
                ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Skill);
            else
                ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Attack);

            if (!skillTypeMonster && HasValidAttackEffectPrefab(ctx))
                SpawnAttackEffectFromPool(ctx, spawnOnPlayer: true);
            return;
        }

        if (ctx.BossAttackManager == null)
        {
            Debug.LogWarning("[EnemyStateAttack] BossAttackManager 없음 → Chase로 복귀");
            ctx.RequestState(EnemyStateType.Chase);
            return;
        }

        st.CurrentBossAttack = ctx.BossAttackManager.SelectNextAttack();
        if (st.CurrentBossAttack == null)
        {
            Debug.LogWarning("[EnemyStateAttack] Boss 공격 선택 실패 → Chase로 복귀");
            ctx.RequestState(EnemyStateType.Chase);
            return;
        }

        ReturnSkillPrepare(ctx);
        ReturnSkillCastTracked(ctx);

        st.DamageApplied = false;
        st.AttackInProgress = true;
        st.SkillPrepareReturnTime = -1f;

        ctx.ComputeBossManagedCastingDurations(st.CurrentBossAttack, out var castDelay, out var castTime);
        st.AttackEndTime = Time.time + castDelay + castTime;

        var atk = st.CurrentBossAttack;
        if (atk.attackType != AttackType.normalAttack)
            PlayBossAreaPrepareSound(ctx);

        if (!string.IsNullOrEmpty(atk.animation))
            ctx.SetAnimatorTrigger(atk.animation);
        else if (IsBossSkillPattern(atk.attackType))
            ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Skill);
        else
            ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Attack);

        if (IsBossSkillPattern(atk.attackType) && castDelay > 0f)
            st.SkillPrepareReturnTime = Time.time + castDelay;
        else if (!IsBossSkillPattern(atk.attackType))
            TrySpawnBossNormalVisual(ctx, atk);
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (ctx.PlayerTransform == null)
        {
            ctx.RequestState(EnemyStateType.Idle);
            return;
        }
        var playerStateMachine = ctx.PlayerTransform.GetComponent<PlayerStateMachine>();
        if (playerStateMachine == null || playerStateMachine._ctx.CurrentHealth <= 0f)
        {
            ctx.RequestState(EnemyStateType.Idle);
            return;
        }

        if (ctx.CurrentHealth <= 0f)
        {
            ctx.RequestState(EnemyStateType.Dead);
            return;
        }

        var st = ctx.Instance;
        if (st.IsSkillAttack)
        {
            if (ctx.FaceTargetWhileAttacking && ctx.PlayerTransform != null)
                RotateTowardsTarget(ctx.EnemyTransform, ctx.PlayerTransform.position, ctx.AttackTurnSpeed);

            if (st.SkillPrepareReturnTime >= 0f && Time.time >= st.SkillPrepareReturnTime)
            {
                ReturnSkillPrepare(ctx);
                st.SkillPrepareReturnTime = -1f;
            }

            if (st.AttackInProgress && Time.time >= st.AttackEndTime)
            {
                if (!st.DamageApplied && ctx.PlayerTransform != null)
                {
                    float dist = ctx.GetBoundsDistanceToPlayer();
                    if (dist <= ctx.AttackRange)
                    {
                        var damageable = ctx.PlayerTransform.GetComponent<IDamageable>();
                        if (damageable != null && IsTargetWithinAttackAngle(ctx))
                        {
                            float baseDamage = ctx.AttackPoint;
                            float rate = 1f;
                            DamageType damageType = DamageType.Physical;

                            var current = ctx.BossAttackManager?.CurrentAttack;
                            if (current != null)
                            {
                                rate = current.skillDamageRate;
                                damageType = current.atkAttributeType == AtkAttributeType.magicalAttack
                                    ? DamageType.Magic
                                    : DamageType.Physical;
                            }

                            float damage = baseDamage * rate;
                            damageable.TakeDamage(damage, damageType);
                            st.DamageApplied = true;
                            if (st.CurrentBossAttack != null && st.CurrentBossAttack.attackType != AttackType.normalAttack)
                                PlayBossAreaCastSound(ctx);
                            else
                                PlayAttackHitSound(ctx);
                        }
                    }
                }

                st.AttackInProgress = false;
                ClearAttackEffect(ctx);
                ctx.RequestState(EnemyStateType.Chase);
            }
            return;
        }

        if (ctx.FaceTargetWhileAttacking && ctx.PlayerTransform != null)
            RotateTowardsTarget(ctx.EnemyTransform, ctx.PlayerTransform.position, ctx.AttackTurnSpeed);

        if (!st.AttackInProgress || Time.time < st.AttackEndTime)
            return;

        bool skillTypeMonster = ctx.StatPresenter != null && ctx.StatPresenter.IsSkillAttackMonster;
        if (!st.DamageApplied && ctx.PlayerTransform != null)
        {
            float dist = ctx.GetBoundsDistanceToPlayer();
            if (dist <= ctx.AttackRange)
            {
                var damageable = ctx.PlayerTransform.GetComponent<IDamageable>();
                if (damageable != null && IsTargetWithinAttackAngle(ctx))
                {
                    if (skillTypeMonster)
                        ReturnSkillPrepare(ctx);

                    float damage = ctx.AttackPoint;
                    damageable.TakeDamage(damage, DamageType.Physical);
                    st.DamageApplied = true;
                    PlayAttackHitSound(ctx);
                }
            }
        }
        st.AttackInProgress = false;
        ClearAttackEffect(ctx);
        ctx.RequestState(EnemyStateType.Chase);
    }

    public void OnExit(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.updateRotation = true;

        ClearAttackEffect(ctx);
        ctx.Instance.CurrentBossAttack = null;
    }

    private void ClearAttackEffect(EnemyStateContext ctx) => ClearAllAttackPresentationStatic(ctx);

    public static void ClearAllAttackPresentationStatic(EnemyStateContext ctx)
    {
        if (ctx?.Instance == null) return;
        var st = ctx.Instance;
        if (st.CurrentAttackEffect != null)
        {
            EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentAttackEffect);
            st.CurrentAttackEffect = null;
        }
        ReturnSkillPrepareStatic(ctx);
        ReturnSkillCastTrackedStatic(ctx);
        st.SkillPrepareReturnTime = -1f;
    }

    public static void CleanupTrackedSkillParticles(EnemyStateContext ctx)
    {
        if (ctx?.Instance == null) return;
        ReturnSkillPrepareStatic(ctx);
        ReturnSkillCastTrackedStatic(ctx);
        ctx.Instance.SkillPrepareReturnTime = -1f;
    }

    private static void TrySpawnBossNormalVisual(EnemyStateContext ctx, BossManageTable atk)
    {
        if (ctx.EnemyTransform == null || atk == null)
            return;

        if (ctx.BossNormalAttackEffectPrefab != null)
        {
            SpawnAttackEffectFromPool(ctx, spawnOnPlayer: true, ctx.BossNormalAttackEffectPrefab);
            return;
        }
        if (HasValidAttackEffectPrefab(ctx))
            SpawnAttackEffectFromPool(ctx, spawnOnPlayer: true);
    }

    private static void SpawnAttackEffectFromPool(EnemyStateContext ctx, bool spawnOnPlayer, GameObject prefabOverride = null)
    {
        var prefab = prefabOverride != null ? prefabOverride : ctx.AttackEffectPrefab;
        if (prefab == null || ctx.EnemyTransform == null)
            return;

        Transform parent = ctx.EnemyTransform;
        Transform anchor = ctx.EnemyTransform;

        if (spawnOnPlayer)
        {
            if (ctx.PlayerTransform == null) return;
            parent = ctx.PlayerTransform;
            anchor = ctx.PlayerTransform;
        }

        var st = ctx.Instance;
        if (st.CurrentAttackEffect != null)
            EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentAttackEffect);

        Vector3 localPos = parent.InverseTransformPoint(anchor.position + Vector3.up * 1f);
        var go = ObjectPoolManager.Get(prefab, parent.position, parent.rotation, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        ApplyEnemyAttackScale(ctx, go, prefab);
        st.CurrentAttackEffect = go;
    }

    private static void ApplyEnemyAttackScale(EnemyStateContext ctx, GameObject instance, GameObject prefab)
    {
        if (instance == null || prefab == null) return;
        float m = ctx.AttackEffectScaleMultiplier;
        if (m <= 0f) m = 1f;
        instance.transform.localScale = prefab.transform.localScale * m;
    }

    private static void ApplySkillEffectScale(EnemyStateContext ctx, GameObject instance, GameObject prefab, float skillUniformMultiplier)
    {
        if (instance == null || prefab == null) return;
        float sm = skillUniformMultiplier > 0f ? skillUniformMultiplier : 1f;
        float m = ctx.AttackEffectScaleMultiplier > 0f ? ctx.AttackEffectScaleMultiplier : 1f;
        instance.transform.localScale = prefab.transform.localScale * m * sm;
    }

    private static bool HasValidAttackEffectPrefab(EnemyStateContext ctx) =>
        ctx != null && ctx.AttackEffectPrefab != null;

    public static void ExternalSpawnSkillPrepare(EnemyStateContext ctx)
    {
        if (ctx == null || ctx.SkillPrepareEffectPrefab == null) return;
        SpawnSkillPooled(ctx, ctx.SkillPrepareEffectPrefab, ctx.SkillPrepareAttachTo, ctx.SkillPrepareSpawn, trackPrepare: true);
    }

    public static void ExternalSpawnSkillCast(EnemyStateContext ctx)
    {
        if (ctx == null || ctx.SkillCastEffectPrefab == null) return;
        SpawnSkillPooled(ctx, ctx.SkillCastEffectPrefab, ctx.SkillCastAttachTo, ctx.SkillCastSpawn, trackPrepare: false, trackCast: true);
    }

    public static void ExternalReturnSkillPrepare(EnemyStateContext ctx) => ReturnSkillPrepareStatic(ctx);

    public static void ExternalReturnSkillCast(EnemyStateContext ctx) => ReturnSkillCastTrackedStatic(ctx);

    private static void SpawnSkillPooled(
        EnemyStateContext ctx,
        GameObject prefab,
        SkillEffectAttachTarget attach,
        SkillEffectSpawnTransform presentation,
        bool trackPrepare,
        bool trackCast = false)
    {
        if (prefab == null) return;
        if (ctx.EnemyTransform == null) return;

        Transform attachParent = ResolveSkillAttachParent(ctx.EnemyTransform, presentation.attachParentNameOrPath);
        if (attachParent == null)
            attachParent = ctx.EnemyTransform;

        Vector3 localCombined = presentation.localPosition + ctx.SkillAttackEffectOffset;
        var st = ctx.Instance;

        GameObject go;
        if (attach == SkillEffectAttachTarget.Field)
        {
            Vector3 worldPos = attachParent.TransformPoint(localCombined);
            Quaternion worldRot = attachParent.rotation * Quaternion.Euler(presentation.localEulerAngles);
            go = ObjectPoolManager.Get(prefab, worldPos, worldRot, null);
            ApplySkillEffectScale(ctx, go, prefab, presentation.uniformScaleMultiplier);
        }
        else
        {
            go = ObjectPoolManager.Get(prefab, attachParent.position, attachParent.rotation, attachParent);
            go.transform.localPosition = localCombined;
            go.transform.localRotation = Quaternion.Euler(presentation.localEulerAngles);
            ApplySkillEffectScale(ctx, go, prefab, presentation.uniformScaleMultiplier);
        }

        if (trackPrepare)
        {
            if (st.CurrentSkillPrepareEffect != null)
                EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentSkillPrepareEffect);
            st.CurrentSkillPrepareEffect = go;
        }
        if (trackCast)
        {
            if (st.CurrentSkillCastEffect != null)
                EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentSkillCastEffect);
            st.CurrentSkillCastEffect = go;
        }
    }

    private static Transform ResolveSkillAttachParent(Transform enemyRoot, string anchor)
    {
        if (enemyRoot == null)
            return null;
        if (string.IsNullOrWhiteSpace(anchor))
            return enemyRoot;

        string t = anchor.Trim();
        Transform byPath = enemyRoot.Find(t);
        if (byPath != null)
            return byPath;

        Transform deep = FindDeepChildByName(enemyRoot, t);
        if (deep != null)
            return deep;

        Debug.LogWarning($"[EnemyStateAttack] 스킬 이펙트 앵커 '{t}' 없음 ({enemyRoot.name}). 루트 사용.");
        return enemyRoot;
    }

    private static Transform FindDeepChildByName(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name)
                return c;
            Transform nested = FindDeepChildByName(c, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void ReturnSkillPrepare(EnemyStateContext ctx) => ReturnSkillPrepareStatic(ctx);

    private static void ReturnSkillPrepareStatic(EnemyStateContext ctx)
    {
        var st = ctx?.Instance;
        if (st?.CurrentSkillPrepareEffect == null) return;
        EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentSkillPrepareEffect);
        st.CurrentSkillPrepareEffect = null;
    }

    private void ReturnSkillCastTracked(EnemyStateContext ctx) => ReturnSkillCastTrackedStatic(ctx);

    private static void ReturnSkillCastTrackedStatic(EnemyStateContext ctx)
    {
        var st = ctx?.Instance;
        if (st?.CurrentSkillCastEffect == null) return;
        EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentSkillCastEffect);
        st.CurrentSkillCastEffect = null;
    }

    private static void PlayAttackHitSound(EnemyStateContext ctx)
    {
        if (ctx.AttackSoundId <= 0 || SoundManager.Instance == null || ctx.EnemyTransform == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(ctx.AttackSoundId, ctx.EnemyTransform.position);
    }

    private static void PlayBossAreaPrepareSound(EnemyStateContext ctx)
    {
        if (ctx.BossAreaAttackPrepareSoundId <= 0 || SoundManager.Instance == null || ctx.EnemyTransform == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(ctx.BossAreaAttackPrepareSoundId, ctx.EnemyTransform.position);
    }

    private static void PlayBossAreaCastSound(EnemyStateContext ctx)
    {
        if (ctx.BossAreaAttackCastSoundId <= 0 || SoundManager.Instance == null || ctx.EnemyTransform == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(ctx.BossAreaAttackCastSoundId, ctx.EnemyTransform.position);
    }

    private static bool IsTargetWithinAttackAngle(EnemyStateContext ctx)
    {
        if (ctx?.EnemyTransform == null || ctx.PlayerTransform == null)
            return false;

        Vector3 toPlayer = ctx.PlayerTransform.position - ctx.EnemyTransform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            return true;

        float angle = Vector3.Angle(ctx.EnemyTransform.forward, toPlayer.normalized);
        return angle <= Mathf.Clamp(ctx.MaxAttackAngle, 1f, 180f);
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
