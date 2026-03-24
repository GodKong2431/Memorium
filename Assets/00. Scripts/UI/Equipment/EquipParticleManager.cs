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

    private readonly Queue<ParticleSystem> upgradeParticleQueue = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> mergeParticleQueue = new Queue<ParticleSystem>();
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
        particle.transform.position = target.position;

        if (particle.gameObject.layer != 5)
            particle.gameObject.layer = 5;

        StartCoroutine(PlayTwiceAndReturnToQueue(particle, upgradeParticleQueue));
    }

    public void PlayMergeEffect(Transform target)
    {
        if (target == null || !TryGetTemplate(mergeEffect, nameof(mergeEffect), out ParticleSystem template))
            return;

        ParticleSystem particle = GetParticle(mergeParticleQueue, template);
        if (particle == null)
            return;

        particle.transform.SetParent(target, false);
        particle.transform.localPosition = Vector3.zero;

        StartCoroutine(PlayOnceAndReturnToQueue(particle, mergeParticleQueue));
    }

    private IEnumerator PlayTwiceAndReturnToQueue(ParticleSystem particle, Queue<ParticleSystem> queue)
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

        ReturnToPool(particle, queue);
    }

    private IEnumerator PlayOnceAndReturnToQueue(ParticleSystem particle, Queue<ParticleSystem> queue)
    {
        if (particle == null)
            yield break;

        float duration = particle.main.duration;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);

        yield return new WaitForSeconds(duration);

        ReturnToPool(particle, queue);
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

    private void ReturnToPool(ParticleSystem particle, Queue<ParticleSystem> queue)
    {
        if (particle == null)
            return;

        EnsurePoolRoot();
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.transform.SetParent(poolRoot, false);
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
}
