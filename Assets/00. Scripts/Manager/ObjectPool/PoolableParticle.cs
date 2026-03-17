using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PoolableParticle : MonoBehaviour, IPoolableRespawnable
{
    private ParticleSystem particle;
    private Transform followTarget;
    private bool isFollowing;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        var main = particle.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    private void Update()
    {
        if (!isFollowing || followTarget == null) return;
        transform.position = followTarget.position;
    }
    public void OnSpawnFromPool()
    {
        followTarget = null;
        isFollowing = false;
        particle.Clear();
        particle.Play();
    }
    public void SetFollow(Transform target)
    {
        followTarget = target;
        isFollowing = target != null;
    }
    private void OnParticleSystemStopped()
    {
        followTarget = null;
        isFollowing = false;
        ObjectPoolManager.Return(gameObject);
    }
}