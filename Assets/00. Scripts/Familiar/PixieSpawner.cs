
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PixieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject pixiePrefab;

    private PixieFollower spawnedPixie;
    private OwnedPixieData fairyData;

    PlayerStateMachine playerStateMachine;
    EffectController effectController;

    public Transform SpawnedPixie => spawnedPixie != null ? spawnedPixie.transform : null;
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
        if (data == null) { Despawn(); return; }
        fairyData = data;
        if (playerStateMachine == null || effectController == null) return;

        if (spawnedPixie != null)
        {
            Destroy(spawnedPixie.gameObject);
            spawnedPixie = null;
        }

        LoadAndSpawn(data);
    }
    private void LoadAndSpawn(OwnedPixieData data)
    {
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(data.pixieId, out var info)
            || string.IsNullOrEmpty(info.prefabPath))
        {
            SpawnPrefab(pixiePrefab, data);
            return;
        }

        Addressables.LoadAssetAsync<GameObject>(info.prefabPath).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                SpawnPrefab(handle.Result, data);
            else
                SpawnPrefab(pixiePrefab, data);
        };
    }

    private void SpawnPrefab(GameObject prefab, OwnedPixieData data)
    {
        if (prefab == null) return;
        
        spawnedPixie = Instantiate(prefab, new Vector3(transform.position.x, 0, transform.position.z), Quaternion.identity)
            .GetComponent<PixieFollower>();
        spawnedPixie.gameObject.SetActive(true);
        spawnedPixie.Init(transform, data, effectController, playerStateMachine._ctx);
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