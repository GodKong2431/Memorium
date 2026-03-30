using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BingoEffectManager : Singleton<BingoEffectManager>
{
    public ParticleSystem linkRegisterEffect;
    public ParticleSystem synergyRegisterEffect;
    public ParticleSystem itemEquipEffect;
    public ParticleSystem boardEnterEffect;
    public ParticleSystem pluckItemEffect;
    public ParticleSystem recallItemPrimaryEffect;
    public ParticleSystem recallItemSecondaryEffect;
    public ParticleSystem synergyDismantleEffect;
    public ParticleSystem GachaBingoSlot;
    public ParticleSystem GachaSynergySlot;
    private const bool DisableSynergyDismantleTrails = true;

    private readonly Queue<ParticleSystem> linkRegisterEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> synergyRegisterEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> itemEquipEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> boardEnterEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> pluckItemEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> recallItemPrimaryEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> recallItemSecondaryEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> synergyDismantleEffectQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> gachaBingoSlotQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> gachaSynergySlotQueue = new Queue<ParticleSystem>();
    private Transform poolRoot;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        EnsurePoolRoot();
    }

    public void PlayLinkRegisterEffect(Transform target)
    {
        PlayEffect(target, linkRegisterEffect, linkRegisterEffectQueue, nameof(linkRegisterEffect), true);
    }

    public void PlaySynergyRegisterEffect(Transform target)
    {
        PlayEffect(target, synergyRegisterEffect, synergyRegisterEffectQueue, nameof(synergyRegisterEffect), true);
    }

    public ParticleSystem PlayLinkRegisterEffectManual(Transform target)
    {
        if (target == null || !TryGetTemplate(linkRegisterEffect, nameof(linkRegisterEffect), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(linkRegisterEffectQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public ParticleSystem PlaySynergyRegisterEffectManual(Transform target)
    {
        if (target == null || !TryGetTemplate(synergyRegisterEffect, nameof(synergyRegisterEffect), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(synergyRegisterEffectQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnLinkRegisterEffect(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, linkRegisterEffect, linkRegisterEffectQueue);
    }

    public void ReturnSynergyRegisterEffect(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, synergyRegisterEffect, synergyRegisterEffectQueue);
    }

    public void PlayItemEquipEffect(Transform target)
    {
        PlayEffect(target, itemEquipEffect, itemEquipEffectQueue, nameof(itemEquipEffect), true);
    }

    public ParticleSystem PlayItemEquipEffectManual(Transform target)
    {
        if (target == null || !TryGetTemplate(itemEquipEffect, nameof(itemEquipEffect), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(itemEquipEffectQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnItemEquipEffect(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, itemEquipEffect, itemEquipEffectQueue);
    }

    public void PlayBoardEnterEffect(Transform target)
    {
        if (target == null || !TryGetTemplate(boardEnterEffect, nameof(boardEnterEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = GetParticle(boardEnterEffectQueue, template);
        if (particle == null)
            return;

        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);
        particle.transform.SetAsFirstSibling();

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        StartCoroutine(PlayAndReturnToQueue(particle, template, boardEnterEffectQueue));
    }

    public ParticleSystem PlayBoardEnterEffectManual(Transform target)
    {
        if (target == null || !TryGetTemplate(boardEnterEffect, nameof(boardEnterEffect), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(boardEnterEffectQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);
        particle.transform.SetAsFirstSibling();

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnBoardEnterEffect(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, boardEnterEffect, boardEnterEffectQueue);
    }

    public void PlayPluckItemEffect(Transform target, Direction direction)
    {
        if (target == null || !TryGetTemplate(pluckItemEffect, nameof(pluckItemEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = PlayPluckItemEffectManual(target, direction);
        if (particle == null)
            return;

        StartCoroutine(PlayAndReturnToQueue(particle, template, pluckItemEffectQueue));
    }

    public ParticleSystem PlayPluckItemEffectManual(Transform target, Direction direction)
    {
        if (target == null || !TryGetTemplate(pluckItemEffect, nameof(pluckItemEffect), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(pluckItemEffectQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);
        particle.transform.localRotation = GetPluckDirectionRotation(direction, template.transform);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnPluckItemEffect(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, pluckItemEffect, pluckItemEffectQueue);
    }

    public void PlayRecallItemPrimaryEffect(Transform target)
    {
        PlayEffect(target, recallItemPrimaryEffect, recallItemPrimaryEffectQueue, nameof(recallItemPrimaryEffect), true);
    }

    public void PlayRecallItemSecondaryEffect(Transform target)
    {
        if (target == null || !TryGetTemplate(recallItemSecondaryEffect, nameof(recallItemSecondaryEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = GetParticle(recallItemSecondaryEffectQueue, template);
        if (particle == null)
            return;

        // 2번째 리콜 이펙트는 활성화 시 애니메이션이 다시 시작되도록 보장.
        particle.gameObject.SetActive(false);
        particle.transform.SetParent(target, false);
        ApplyUiTransform(particle.transform, template.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.gameObject.SetActive(true);
        RestartAttachedAnimations(particle);

        StartCoroutine(PlayAndReturnToQueue(particle, template, recallItemSecondaryEffectQueue));
    }

    public void PlaySynergyDismantleEffect(Transform target)
    {
        PlayEffect(target, synergyDismantleEffect, synergyDismantleEffectQueue, nameof(synergyDismantleEffect), true);
    }

    public ParticleSystem PlaySynergyDismantleEffectManual(Transform target)
    {
        if (target == null || !TryGetTemplate(synergyDismantleEffect, nameof(synergyDismantleEffect), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(synergyDismantleEffectQueue, template);
        if (particle == null)
            return null;

        if (DisableSynergyDismantleTrails)
            SetTrailRenderingEnabled(template, false);

        particle.transform.SetParent(target, false);
        ApplyLocalPositionRotation(particle.transform, template.transform);
        particle.transform.localScale = GetHierarchyLocalScale(template.transform);
        SetTrailRenderingEnabled(particle, !DisableSynergyDismantleTrails);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnSynergyDismantleEffect(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPoolWithoutScale(
            particle,
            synergyDismantleEffect != null ? synergyDismantleEffect.transform : null,
            synergyDismantleEffectQueue);
    }

    public ParticleSystem PlayGachaBingoSlotManual(Transform target)
    {
        if (target == null || !TryGetTemplate(GachaBingoSlot, nameof(GachaBingoSlot), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(gachaBingoSlotQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        Transform templateTransform = TryGetTemplateTransform(template);
        if (templateTransform != null)
            ApplyUiTransform(particle.transform, templateTransform, target);
        else
            ApplyDefaultUiTransform(particle.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnGachaBingoSlot(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, null, gachaBingoSlotQueue);
    }

    public ParticleSystem PlayGachaSynergySlotManual(Transform target)
    {
        if (target == null || !TryGetTemplate(GachaSynergySlot, nameof(GachaSynergySlot), out ParticleSystem template))
            return null;

        ParticleSystem particle = GetParticle(gachaSynergySlotQueue, template);
        if (particle == null)
            return null;

        particle.transform.SetParent(target, false);
        Transform templateTransform = TryGetTemplateTransform(template);
        if (templateTransform != null)
            ApplyUiTransform(particle.transform, templateTransform, target);
        else
            ApplyDefaultUiTransform(particle.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
        return particle;
    }

    public void ReturnGachaSynergySlot(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ReturnToPool(particle, null, gachaSynergySlotQueue);
    }

    private void PlayEffect(
        Transform target,
        ParticleSystem template,
        Queue<ParticleSystem> queue,
        string fieldName,
        bool useUiTransform,
        bool checkReturn = true,
        int playCount = 1)
    {
        if (target == null || !TryGetTemplate(template, fieldName, out ParticleSystem resolvedTemplate))
            return;

        ParticleSystem particle = GetParticle(queue, resolvedTemplate);
        if (particle == null)
            return;

        particle.transform.SetParent(target, false);
        if (useUiTransform)
            ApplyUiTransform(particle.transform, resolvedTemplate.transform, target);
        else
            ApplyLocalTransform(particle.transform, resolvedTemplate.transform);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        StartCoroutine(PlayAndReturnToQueue(particle, resolvedTemplate, queue, checkReturn, playCount));
    }

    private IEnumerator PlayAndReturnToQueue(
        ParticleSystem particle,
        ParticleSystem template,
        Queue<ParticleSystem> queue,
        bool checkReturn = true,
        int count = 1)
    {
        if (particle == null)
            yield break;

        int playCount = 0;
        float duration = particle.main.duration;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        while (playCount < count)
        {
            if (particle == null)
                yield break;

            particle.Play(true);
            playCount++;
            yield return new WaitForSeconds(duration);

            if (particle == null)
                yield break;

            if (checkReturn)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (checkReturn)
            ReturnToPool(particle, template, queue);
    }

    private ParticleSystem GetParticle(Queue<ParticleSystem> queue, ParticleSystem template)
    {
        EnsurePoolRoot();

        while (queue.Count > 0)
        {
            ParticleSystem cached = queue.Dequeue();
            if (cached == null)
                continue;

            cached.gameObject.SetActive(true);
            cached.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return cached;
        }

        ParticleSystem created = null;
        try
        {
            created = Instantiate(template, poolRoot);
        }
        catch (InvalidCastException)
        {
            // 타입 변경(예: GameObject -> ParticleSystem) 직후 남은 레퍼런스도 안전하게 처리.
            UnityEngine.Object createdObj = Instantiate((UnityEngine.Object)template, poolRoot);
            created = ExtractParticleSystem(createdObj);

            if (created == null)
            {
                Debug.LogWarning("[BingoEffectManager] Failed to create particle instance from template.", this);

                if (createdObj is Component createdComponent)
                    Destroy(createdComponent.gameObject);
                else if (createdObj is GameObject createdGameObject)
                    Destroy(createdGameObject);

                return null;
            }
        }

        if (created == null)
            return null;

        created.gameObject.SetActive(true);
        created.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return created;
    }

    private void ReturnToPool(ParticleSystem particle, ParticleSystem template, Queue<ParticleSystem> queue)
    {
        if (particle == null)
            return;

        EnsurePoolRoot();
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.gameObject.SetActive(false);
        queue.Enqueue(particle);

        // Parent activate/deactivate 타이밍 충돌을 피하려고 다음 프레임에 안전하게 풀 루트로 이동.
        StartCoroutine(MoveToPoolWhenSafe(particle, template));
    }

    private void ReturnToPoolWithoutScale(ParticleSystem particle, Transform templateTransform, Queue<ParticleSystem> queue)
    {
        if (particle == null)
            return;

        EnsurePoolRoot();
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.gameObject.SetActive(false);
        queue.Enqueue(particle);

        StartCoroutine(MoveToPoolWhenSafeWithoutScale(particle, templateTransform));
    }

    private GameObject PlayGameObjectManual(
        Transform target,
        GameObject template,
        Queue<GameObject> queue,
        string fieldName)
    {
        if (target == null || !TryGetTemplate(template, fieldName, out GameObject resolvedTemplate))
            return null;

        GameObject instance = GetPooledObject(queue, resolvedTemplate);
        if (instance == null)
            return null;

        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(target, false);
        ApplyUiTransform(instanceTransform, resolvedTemplate.transform, target);

        if (instance.layer != 5)
            instance.layer = 5;

        instance.SetActive(true);
        RestartAttachedAnimations(instance);
        return instance;
    }

    private GameObject GetPooledObject(Queue<GameObject> queue, GameObject template)
    {
        EnsurePoolRoot();

        while (queue.Count > 0)
        {
            GameObject cached = queue.Dequeue();
            if (cached == null)
                continue;

            cached.SetActive(true);
            return cached;
        }

        GameObject created = Instantiate(template, poolRoot);
        created.SetActive(true);
        return created;
    }

    private void ReturnGameObjectToPool(GameObject instance, GameObject template, Queue<GameObject> queue)
    {
        if (instance == null)
            return;

        EnsurePoolRoot();
        instance.SetActive(false);
        queue.Enqueue(instance);

        StartCoroutine(MoveGameObjectToPoolWhenSafe(instance, template != null ? template.transform : null));
    }

    private bool TryGetTemplate(ParticleSystem template, string fieldName, out ParticleSystem resolved)
    {
        resolved = template;
        if (resolved != null)
            return true;

        Debug.LogWarning($"[BingoEffectManager] {fieldName} is missing.", this);
        return false;
    }

    private bool TryGetTemplate(GameObject template, string fieldName, out GameObject resolved)
    {
        resolved = template;
        if (resolved != null)
            return true;

        Debug.LogWarning($"[BingoEffectManager] {fieldName} is missing.", this);
        return false;
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null)
            return;

        Transform existing = transform.Find("ParticlePool");
        if (existing != null)
        {
            poolRoot = existing;
            return;
        }

        GameObject root = new GameObject("ParticlePool");
        root.transform.SetParent(transform, false);
        poolRoot = root.transform;
    }

    private static void ApplyLocalTransform(Transform target, Transform template)
    {
        if (target == null || template == null)
            return;

        target.localPosition = template.localPosition;
        target.localRotation = template.localRotation;
        target.localScale = template.localScale;
    }

    private static void ApplyLocalPositionRotation(Transform target, Transform template)
    {
        if (target == null || template == null)
            return;

        target.localPosition = template.localPosition;
        target.localRotation = template.localRotation;
    }

    private static Vector3 GetHierarchyLocalScale(Transform leaf)
    {
        if (leaf == null)
            return Vector3.one;

        Vector3 scale = Vector3.one;
        Transform current = leaf;
        while (current != null)
        {
            scale = Vector3.Scale(scale, current.localScale);
            current = current.parent;
        }

        return scale;
    }

    private static void SetTrailRenderingEnabled(ParticleSystem root, bool enabled)
    {
        if (root == null)
            return;

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem system = particleSystems[i];
            if (system == null)
                continue;

            var trails = system.trails;
            trails.enabled = enabled;
        }

        TrailRenderer[] trailRenderers = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trailRenderers.Length; i++)
        {
            TrailRenderer trailRenderer = trailRenderers[i];
            if (trailRenderer == null)
                continue;

            trailRenderer.enabled = enabled;
        }
    }

    private static void ApplyUiTransform(Transform target, Transform template, Transform parent)
    {
        if (target == null || template == null)
            return;

        RectTransform targetRect = target as RectTransform;
        RectTransform parentRect = parent as RectTransform;

        if (targetRect != null && parentRect != null)
        {
            targetRect.anchorMin = Vector2.zero;
            targetRect.anchorMax = Vector2.one;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
            targetRect.anchoredPosition = Vector2.zero;
            targetRect.localRotation = template.localRotation;
            targetRect.localScale = Vector3.one;
            return;
        }

        ApplyLocalTransform(target, template);
    }

    private static void ApplyDefaultUiTransform(Transform target, Transform parent)
    {
        if (target == null || parent == null)
            return;

        RectTransform targetRect = target as RectTransform;
        RectTransform parentRect = parent as RectTransform;

        if (targetRect != null && parentRect != null)
        {
            targetRect.anchorMin = Vector2.zero;
            targetRect.anchorMax = Vector2.one;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
            targetRect.anchoredPosition = Vector2.zero;
            targetRect.localRotation = Quaternion.identity;
            targetRect.localScale = Vector3.one;
            return;
        }

        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    private static Transform TryGetTemplateTransform(ParticleSystem template)
    {
        if (template == null)
            return null;

        try
        {
            return template.transform;
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }

    private static ParticleSystem ExtractParticleSystem(UnityEngine.Object source)
    {
        if (source == null)
            return null;

        if (source is ParticleSystem particle)
            return particle;

        if (source is GameObject gameObject)
            return gameObject.GetComponent<ParticleSystem>() ?? gameObject.GetComponentInChildren<ParticleSystem>(true);

        if (source is Component component)
            return component.GetComponent<ParticleSystem>() ?? component.GetComponentInChildren<ParticleSystem>(true);

        return null;
    }

    private static Quaternion GetPluckDirectionRotation(Direction direction, Transform template)
    {
        float y = -90f;
        float z = -90f;

        if (template != null)
        {
            y = template.localEulerAngles.y;
            z = template.localEulerAngles.z;
        }

        float x;
        switch (direction)
        {
            case Direction.Up:
                x = 270f;
                break;
            case Direction.Left:
                x = 0f;
                break;
            case Direction.Down:
                x = 90f;
                break;
            case Direction.Right:
            default:
                x = 180f;
                break;
        }

        return Quaternion.Euler(x, y, z);
    }

    private static void RestartAttachedAnimations(ParticleSystem particle)
    {
        if (particle == null)
            return;

        Animator[] animators = particle.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || !animator.isActiveAndEnabled)
                continue;

            animator.Rebind();
            animator.Update(0f);
        }

        Animation[] legacyAnimations = particle.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < legacyAnimations.Length; i++)
        {
            Animation animation = legacyAnimations[i];
            if (animation == null || !animation.isActiveAndEnabled)
                continue;

            animation.Stop();
            animation.Play();
        }
    }

    private static void RestartAttachedAnimations(GameObject instance)
    {
        if (instance == null)
            return;

        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || !animator.isActiveAndEnabled)
                continue;

            animator.Rebind();
            animator.Update(0f);
        }

        Animation[] legacyAnimations = instance.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < legacyAnimations.Length; i++)
        {
            Animation animation = legacyAnimations[i];
            if (animation == null || !animation.isActiveAndEnabled)
                continue;

            animation.Stop();
            animation.Play();
        }
    }

    private IEnumerator MoveToPoolWhenSafe(ParticleSystem particle, ParticleSystem template)
    {
        if (particle == null)
            yield break;

        yield return null;

        const int maxRetries = 3;
        int retries = 0;
        while (retries < maxRetries)
        {
            if (particle == null || particle.gameObject.activeSelf)
                yield break;

            EnsurePoolRoot();
            bool retryNextFrame = false;

            try
            {
                particle.transform.SetParent(poolRoot, false);
                if (template != null)
                    ApplyLocalTransform(particle.transform, template.transform);
                yield break;
            }
            catch (UnityException)
            {
                retries++;
                retryNextFrame = true;
            }

            if (retryNextFrame)
                yield return null;
        }
    }

    private IEnumerator MoveToPoolWhenSafeWithoutScale(ParticleSystem particle, Transform templateTransform)
    {
        if (particle == null)
            yield break;

        yield return null;

        const int maxRetries = 3;
        int retries = 0;
        while (retries < maxRetries)
        {
            if (particle == null || particle.gameObject.activeSelf)
                yield break;

            EnsurePoolRoot();
            bool retryNextFrame = false;

            try
            {
                particle.transform.SetParent(poolRoot, false);
                if (templateTransform != null)
                    ApplyLocalPositionRotation(particle.transform, templateTransform);
                yield break;
            }
            catch (UnityException)
            {
                retries++;
                retryNextFrame = true;
            }

            if (retryNextFrame)
                yield return null;
        }
    }

    private IEnumerator MoveGameObjectToPoolWhenSafe(GameObject instance, Transform templateTransform)
    {
        if (instance == null)
            yield break;

        yield return null;

        const int maxRetries = 3;
        int retries = 0;
        while (retries < maxRetries)
        {
            if (instance == null || instance.activeSelf)
                yield break;

            EnsurePoolRoot();
            bool retryNextFrame = false;

            try
            {
                instance.transform.SetParent(poolRoot, false);
                if (templateTransform != null)
                    ApplyLocalTransform(instance.transform, templateTransform);
                yield break;
            }
            catch (UnityException)
            {
                retries++;
                retryNextFrame = true;
            }

            if (retryNextFrame)
                yield return null;
        }
    }
}
