using UnityEngine;

public class SkillProjectile : SkillObjectileBase
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    private float elapsed;

    private void OnEnable()
    {
        elapsed = 0;
    }
    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        if (elapsed > lifetime)
        {
            ObjectPoolManager.Return(gameObject);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        debugLastCastPos = dataContext.skillData.m3Data.m3Distance * transform.forward;
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
            int count = m2.Detect(transform.position, transform.forward, data.m2Data, this, targetLayer);

            if (count > 0 && !IsOwnerDestroyed())
                owner?.HandleSkillHit(count, dataContext, hitBuffer);
        }

        ObjectPoolManager.Return(gameObject);
    }
}