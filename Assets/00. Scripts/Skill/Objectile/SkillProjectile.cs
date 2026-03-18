using UnityEngine;

public class SkillProjectile : SkillObjectileBase
{
    [SerializeField] private float speed = 20f;


    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        debugLastCastPos = dataContext.skillData.m3Data.m3Distance * transform.forward;
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(dataContext?.skillData.m3Data.m3VFX, transform, false, true));
            var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
            int count = m2.Detect(transform.position, transform.forward, data.m2Data, this, targetLayer);
            owner.HandleSkillHit(count, dataContext, hitBuffer);
        }

        Destroy(gameObject);
    }
}