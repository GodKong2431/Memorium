using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PoolableParticle : MonoBehaviour, IPoolableRespawnable
{
    private ParticleSystem particle;
    private Transform followTarget;
    private Vector3 followLocalOffset;
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
        transform.position = followTarget.TransformPoint(followLocalOffset);
    }
    public void OnSpawnFromPool()
    {
        followTarget = null;
        followLocalOffset = Vector3.zero;
        isFollowing = false;
        autoReturnToPool = true;

        particle.Clear();
        particle.Play();
    }
    public void SetFollow(Transform target, Vector3 localOffset = default)
    {
        followTarget = target;
        followLocalOffset = localOffset;
        isFollowing = target != null;
    }
    public void SetAutoReturn(bool autoReturn)
    {
        autoReturnToPool = autoReturn;
    }
    private void OnParticleSystemStopped()
    {
        followTarget = null;
        followLocalOffset = Vector3.zero;
        isFollowing = false; 
        if (autoReturnToPool)
        {
            ObjectPoolManager.Return(gameObject);
        }
    }
    public void StopAndReturnManual()
    {
        autoReturnToPool = true;
        if(particle != null)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}