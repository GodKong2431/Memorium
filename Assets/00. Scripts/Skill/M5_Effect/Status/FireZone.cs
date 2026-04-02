
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireZone : MonoBehaviour
{
    private SkillModule5Table tableData;
    private float zoneDuration;
    private float elapsed;
    private float detectTimer;
    private float detectInterval = 0.1f;
    private float detectRadius;
    private int targetLayer;

    private static readonly Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];
    private PoolableParticle currentParticle;

    private PlayerStateMachine player;
    private bool isReturned;
    public void Init(SkillModule5Table data,float radius,int layerMask)
    {
        tableData = data;
        zoneDuration = data.duration;
        detectRadius = radius;
        targetLayer = layerMask;
        elapsed = 0f;
        detectTimer = 0f;
        isReturned = false;
        PoolableParticleManager.Instance.SpawnParticle(
            new ParticleSpawnContext(data?.m5VFX2, transform, true, false, onSpawned: (particle) => currentParticle = particle));
    }

    private void OnEnable()
    {
        isReturned = false;

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageClearOrFailed += ReturnToPool;
    }
    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageClearOrFailed -= ReturnToPool;


        if (currentParticle != null)
        {
            currentParticle.StopAndReturnManual();
            currentParticle = null;
        }
    }
    private void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;
        if (elapsed >= zoneDuration)
        {
            ReturnToPool();
            return;
        }

        detectTimer += dt;
        if (detectTimer >= detectInterval)
        {
            detectTimer -= detectInterval;
            DetectAndApply();
        }
    }

    //private bool IsPlayerAlive()=> player != null && player.IsAlive;
    private void DetectAndApply()
    {
        float halfHeight = SkillConstants.DETECT_HEIGHT;
        Vector3 center = transform.position;
        Vector3 bottom = center - Vector3.up * halfHeight;
        Vector3 top = center + Vector3.up * halfHeight;

        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, detectRadius, hitBuffer, targetLayer);

        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].TryGetComponent<EffectController>(out var effectController))
            {
                if (!effectController.HasStatusEffect())
                    effectController.ApplyStatusEffect(StatusEffectFactory.Create(tableData));
            }
        }
    }
    //private void OnSceneUnloaded(Scene scene)
    //{
    //    ReturnToPool();
    //}
    private void ReturnToPool()
    {
        if (isReturned)
            return;

        isReturned = true;
        ObjectPoolManager.Return(gameObject);
    }
}