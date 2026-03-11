
using UnityEngine;

public class PixieSpawner : MonoBehaviour
{
    [SerializeField]private PixieFollower pixiePrefab;

    private PixieFollower spawnedPixie;
    private OwnedPixieData fairyData;

    PlayerStateMachine playerStateMachine;
    EffectController effectController;

    public bool IsSpawned =>spawnedPixie != null && spawnedPixie.gameObject.activeSelf;

    private void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();
        effectController = GetComponent<EffectController>();
    }
    public void OnEnable()
    {
        var module = InventoryManager.Instance.GetModule<PixieInventoryModule>();
        if (module == null) return;

        module.OnPixieEquipped += SpawnPixie;
        int equippedId = module.EquippedPixiedID();
        if (equippedId != 0)
        {
            SpawnPixie(equippedId);
        }
    }

    private void OnDisable()
    {
        var module = InventoryManager.Instance?.GetModule<PixieInventoryModule>();
        if (module != null)
        {
            module.OnPixieEquipped -= SpawnPixie;
        }
        Despawn();
    }
    public void SpawnPixie(int pixieID)
    {
        var module = InventoryManager.Instance.GetModule<PixieInventoryModule>();
        var data = module.GetOwnedPixieData(pixieID);
        if (data == null) return; 
        SpawnPixie(data);
    }
    public void SpawnPixie(OwnedPixieData data)
    {
        if (data == null)
        {
            Despawn();
            return;
        }

        fairyData = data;

        if (playerStateMachine == null || effectController == null) return;
        if (spawnedPixie == null)
            spawnedPixie = Instantiate(pixiePrefab, transform.position, Quaternion.identity);
        
        spawnedPixie.gameObject.SetActive(true);
        spawnedPixie.Init(transform, data, effectController,playerStateMachine._ctx);
    }

    public void Despawn()
    {
        if (IsSpawned)
        {
            spawnedPixie.gameObject.SetActive(false);
        }
    }


    public OwnedPixieData GetCurrentFairyData() => fairyData;

}