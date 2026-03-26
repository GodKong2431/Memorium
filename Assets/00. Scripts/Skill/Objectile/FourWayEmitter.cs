    using UnityEngine;

    public class FourWayEmitter : MonoBehaviour
    {[SerializeField] private GameObject effectPrefab;
    [SerializeField] private float offset = 0.5f;


    private void Start()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3[] directions = { forward, -forward, right, -right };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 spawnPos = transform.position + directions[i] * offset;
            ObjectPoolManager.Get(effectPrefab, spawnPos, Quaternion.LookRotation(directions[i]));
        }
        ObjectPoolManager.Return(effectPrefab);
    }
}