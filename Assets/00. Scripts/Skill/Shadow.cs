using UnityEngine;

[RequireComponent(typeof(SkillCaster))]
public class Shadow : MonoBehaviour
{
    SkillCaster caster;

    private void Awake()
    {
        caster = GetComponent<SkillCaster>();

    }
    private void OnEnable()
    {
        caster.OnSkillEnd += SelfDestroy;
    }
    private void OnDisable()
    {
        caster.OnSkillEnd -= SelfDestroy;
    }   
    private void SelfDestroy()
    {
        Destroy(gameObject);
    }
}
