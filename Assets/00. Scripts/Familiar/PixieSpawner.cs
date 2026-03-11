
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

    private void Start()
    {
        var module = InventoryManager.Instance.GetModule<PixieInventoryModule>();
        if (module == null) return;

        if (module.EquippedPixiedID() != 0)
        {
            var data = module.GetOwnedPixieData(module.EquippedPixiedID());
            if (data != null)
            {
                Spawn(data); 
            }
        }
        module.OnPixieEquipped += Spawn;

    }
    public bool IsSpawned => spawnedPixie != null;

    public void OnEnable()
    {
        if (fairyData != null)
        {
            Spawn(fairyData); 
        }
    }
    public void Spawn(OwnedPixieData data)
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
        spawnedPixie.Init(transform, data, effectController);
    }

    public void Despawn()
    {
        if (spawnedPixie != null)
        {
            spawnedPixie.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        var module = InventoryManager.Instance?.GetModule<PixieInventoryModule>();
        if (module != null)
        {
            module.OnPixieEquipped -= Spawn;    
        }
        Despawn();
    }

    public OwnedPixieData GetCurrentFairyData() => fairyData;

}