
using UnityEngine;

public class FireZone : MonoBehaviour
{
    private SkillModule5Table tableData;
    private float zoneDuration;
    private float elapsed;
    private float detectTimer;
    private float detectInterval = 0.1f;
    private float detectRadius;
    private int targetLayer;

    private static readonly Collider[] hitBuffer = new Collider[20];

    public void Init(SkillModule5Table data,float radius,int layerMask)
    {
        tableData = data;
        zoneDuration = data.duration;
        detectRadius = radius;
        targetLayer = layerMask;
        elapsed = 0f;
        detectTimer = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        if (elapsed >= zoneDuration)
        {
            Destroy(gameObject);
            // TODO: 풀에 반납
            return;
        }

        detectTimer += dt;
        if (detectTimer >= detectInterval)
        {
            detectTimer -= detectInterval;
            DetectAndApply();
        }
    }

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
                effectController.ApplyStatusEffect(new FireEffect(tableData));
            }
        }
    }
}