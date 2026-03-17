using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PoolableParticle : MonoBehaviour, IPoolableRespawnable
{
    private ParticleSystem particle;
    private Transform followTarget;
    private bool isFollowing;
    private bool autoReturnToPool;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        var main = particle.main;
        main.stopAction = ParticleSystemStopAction.Callback;
        autoReturnToPool = true;
    }

    private void LateUpdate()
    {
        if (!isFollowing || followTarget == null) return;
        transform.position = followTarget.position;
    }
    public void OnSpawnFromPool()
    {
        followTarget = null;
        isFollowing = false;
        autoReturnToPool = true;

        particle.Clear();
        particle.Play();
    }
    public void SetFollow(Transform target)
    {
        followTarget = target;
        isFollowing = target != null;
    }
    public void SetAutoReturn(bool autoReturn)
    {
        autoReturnToPool = autoReturn;
    }
    private void OnParticleSystemStopped()
    {
        followTarget = null;
        isFollowing = false; 
        if (autoReturnToPool)
        {
            ObjectPoolManager.Return(gameObject);
        }
    }
    public void StopAndReturnManual()
    {
        autoReturnToPool = true;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}