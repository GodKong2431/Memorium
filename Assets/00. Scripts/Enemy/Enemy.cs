using UnityEngine;

public class Enemy : MonoBehaviour
{
    private void OnEnable()
    {
        EnemyRegistry.Register(this);
    }

    private void OnDisable()
    {
        EnemyRegistry.UnRegister(this);
    }

    private void OnDestroy()
    {
        EnemyRegistry.UnRegister(this);
    }
}
