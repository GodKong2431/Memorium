using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum EquipParticle
{
    UPGRADE=0, MERGE=1
}
public class EquipParticleManager : Singleton<EquipParticleManager>
{
    public ParticleSystem upgradeEffect;
    public ParticleSystem mergeEffect;

    Queue<ParticleSystem> upgradeParticleQueue=new Queue<ParticleSystem> ();
    Queue<ParticleSystem> mergeParticleQueue=new Queue<ParticleSystem> ();

    public void PlayUpgradeEffect(Transform transform)
    {
        if (upgradeEffect.gameObject.layer != 5)
        {
            upgradeEffect.gameObject.layer = 5;
        }

        ParticleSystem particle;
        if (upgradeParticleQueue.Count > 0)
        {
            particle = upgradeParticleQueue.Dequeue();
            particle.gameObject.SetActive(true);
        }
        else
        {
            particle = Instantiate(upgradeEffect);
        }
        particle.transform.position=transform.position;
        StartCoroutine(PlayTwiceAndReturnToQueue(particle, true));
    }
    public void PlayMergeEffect(Transform transform)
    {
        ParticleSystem particle;
        if (mergeParticleQueue.Count > 0)
        {
            particle = mergeParticleQueue.Dequeue();
            particle.gameObject.SetActive(true);
        }
        else
        {
            particle = Instantiate(mergeEffect);
        }
        particle.transform.SetParent(transform);
        particle.transform.localPosition = Vector3.zero;
        //particle.transform.position=transform.position;
        StartCoroutine(PlayOnceAndReturnToQueue(particle, false));
    }

    IEnumerator PlayTwiceAndReturnToQueue(ParticleSystem ps, bool upgrade)
    {
        int playCount = 0;
        float duration = ps.main.duration;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        while (playCount < 2)
        {
            
            ps.Play(true);
            playCount++;

            // 파티클 한 주기(duration)만큼 대기
            yield return new WaitForSeconds(duration);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // 4. 재생이 끝나면 비활성화하고 다시 큐에 삽입 (재사용 준비)
        ps.gameObject.SetActive(false);
        if (upgrade)
        {
            upgradeParticleQueue.Enqueue(ps);
        }
        else
        {
            mergeParticleQueue.Enqueue(ps);
        }
    }
    IEnumerator PlayOnceAndReturnToQueue(ParticleSystem ps, bool upgrade)
    {
        int playCount = 0;
        float duration = ps.main.duration;

        while (playCount < 1)
        {
            ps.Play();
            playCount++;

            // 파티클 한 주기(duration)만큼 대기
            yield return new WaitForSeconds(duration);
        }

        // 4. 재생이 끝나면 비활성화하고 다시 큐에 삽입 (재사용 준비)
        ps.gameObject.SetActive(false);
        if (upgrade)
        {
            upgradeParticleQueue.Enqueue(ps);
        }
        else
        {
            mergeParticleQueue.Enqueue(ps);
        }

        ps.transform.SetParent(null);
    }
}
