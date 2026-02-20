using UnityEngine;

public class SkillProjectile : SkillObjectileBase
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float maxLifeTime = 5f;


    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
            int count = m2.Detect(transform.position, transform.forward, data.m2Data, this, targetLayer);
            owner.HandleSkillHit(count, skillDataContext, hitBuffer);
        }

        Destroy(gameObject);
    }
}