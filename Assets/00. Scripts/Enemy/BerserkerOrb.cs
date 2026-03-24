using NUnit.Framework.Constraints;
using System;
using System.Collections;
using UnityEngine;

public class BerserkerOrb : MonoBehaviour
{
    [SerializeField] private int increaseOrb = 0;
    [SerializeField] Transform player;
    [SerializeField] private float currentSpeed = 0;
    [SerializeField] private float acceleration;
    [SerializeField] private float hitDistance;

    public static event Action<int> OnBerserkerOrbEarned;

    public void Init(int increaseCount)
    {
        increaseOrb = increaseCount;
        
        if (player == null)
        {
            player = CharacterStatManager.playerTransform;
        }
        StartCoroutine(StartPlayerTracker());
    }

    void OnEnable()
    {
        
    }
    void OnDisable()
    {

    }

    IEnumerator StartPlayerTracker()
    {
        currentSpeed = 0;

        var particle = GetComponent<PoolableParticle>();

        while (player != null)
        {
            currentSpeed += acceleration * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, player.position, currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, player.position) <= hitDistance)
            {
                if (BerserkerModeController.Instance != null && !BerserkerModeController.Instance.IsActive)
                    OnBerserkerOrbEarned?.Invoke(increaseOrb);
                particle?.StopAndReturnManual();
                yield break;
            }

            yield return null;
        }
        
        particle?.StopAndReturnManual();
    }
}
