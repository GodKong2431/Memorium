using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum EquipParticle
{
    UPGRADE = 0,
    MERGE = 1
}

public class EquipParticleManager : Singleton<EquipParticleManager>
{
    public ParticleSystem upgradeEffect;
    public ParticleSystem mergeEffect;
    public ParticleSystem gachaEffect;

    private readonly Queue<ParticleSystem> upgradeParticleQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> mergeParticleQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> gachaParticleQueue = new Queue<ParticleSystem>();
    private Transform poolRoot;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        EnsurePoolRoot();
    }

    public void PlayUpgradeEffect(Transform target)
    {
        if (target == null || !TryGetTemplate(upgradeEffect, nameof(upgradeEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = GetParticle(upgradeParticleQueue, template);
        if (particle == null)
            return;

        particle.transform.SetParent(poolRoot, false);
        ApplyLocalTransform(particle.transform, template.transform);
        particle.transform.position = target.position;
        particle.transform.rotation = template.transform.rotation;

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        StartCoroutine(PlayTwiceAndReturnToQueue(particle, template, upgradeParticleQueue));
    }

    public void PlayMergeEffect(Transform target)
    {
        if (target == null || !TryGetTemplate(mergeEffect, nameof(mergeEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = GetParticle(mergeParticleQueue, template);
        if (particle == null)
            return;

        particle.transform.SetParent(target, false);
        ApplyMergeTransform(particle.transform, template.transform, target);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        StartCoroutine(PlayOnceAndReturnToQueue(particle, template, mergeParticleQueue));
    }

    public void PlayGachaEffect(Transform target)
    {
        if (target == null || !TryGetTemplate(gachaEffect, nameof(gachaEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = GetParticle(gachaParticleQueue, template);
        if (particle == null)
            return;

        particle.transform.SetParent(target, false);
        ApplyLocalTransform(particle.transform, template.transform);

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        StartCoroutine(PlayOnceAndReturnToQueue(particle, template, gachaParticleQueue, false));
    }

    private IEnumerator PlayTwiceAndReturnToQueue(ParticleSystem particle, ParticleSystem template, Queue<ParticleSystem> queue)
    {
        if (particle == null)
            yield break;

        int playCount = 0;
        float duration = particle.main.duration;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        while (playCount < 2)
        {
            if (particle == null)
                yield break;

            particle.Play(true);
            playCount++;
            yield return new WaitForSeconds(duration);

            if (particle == null)
                yield break;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ReturnToPool(particle, template, queue);
    }

    private IEnumerator PlayOnceAndReturnToQueue(ParticleSystem particle, ParticleSystem template, Queue<ParticleSystem> queue, bool checkReturn=true)
    {
        if (particle == null)
            yield break;

        float duration = particle.main.duration;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);

        yield return new WaitForSeconds(duration);

        if(checkReturn)
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

            cached.transform.SetParent(poolRoot, false);
            cached.gameObject.SetActive(true);
            cached.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return cached;
        }

        ParticleSystem created = Instantiate(template, poolRoot);
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
        particle.transform.SetParent(poolRoot, false);
        if (template != null)
            ApplyLocalTransform(particle.transform, template.transform);
        particle.gameObject.SetActive(false);
        queue.Enqueue(particle);
    }

    private bool TryGetTemplate(ParticleSystem template, string fieldName, out ParticleSystem resolved)
    {
        resolved = template;
        if (resolved != null)
            return true;

        Debug.LogWarning($"[EquipParticleManager] {fieldName} is missing.", this);
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

    private static void ApplyMergeTransform(Transform target, Transform template, Transform parent)
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
}
