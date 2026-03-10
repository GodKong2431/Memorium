
using UnityEngine;

public class PixieSpawner : MonoBehaviour
{
    [SerializeField]private PixieFollower pixiePrefab;

    private PixieFollower spawnedPixie;
    private OwnedPixieData fairyData;


    PlayerStateMachine playerStateMachine;
    EffectController effectController;

    private void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();
        effectController = GetComponent<EffectController>();
    }
    public bool IsSpawned => spawnedPixie != null;

    public void Spawn(OwnedPixieData data)
    {
        if (pixiePrefab == null || data == null) return;

        if (spawnedPixie != null)
            Despawn();

        fairyData = data;

        if (playerStateMachine == null || effectController == null) return;

        spawnedPixie = Instantiate(pixiePrefab, transform.position, Quaternion.identity);

        if (spawnedPixie.TryGetComponent<PixieFollower>(out var follower))
            follower.Init(transform, data, effectController);
    }

    public void Despawn()
    {
        if (spawnedPixie != null)
        {
            Destroy(spawnedPixie);
            spawnedPixie = null;
        }
        fairyData = null;
    }

    public OwnedPixieData GetCurrentFairyData() => fairyData;

}