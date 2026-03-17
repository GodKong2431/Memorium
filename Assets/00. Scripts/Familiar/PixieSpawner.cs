using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PixieSpawner : MonoBehaviour
{
    private const string LogPrefix = "[PixieSpawner]";

    [SerializeField] private GameObject pixiePrefab;

    private PixieFollower spawnedPixie;
    private OwnedPixieData fairyData;

    PlayerStateMachine playerStateMachine;
    EffectController effectController;

    public Transform SpawnedPixie => spawnedPixie != null ? spawnedPixie.transform : null;
    public bool IsSpawned => spawnedPixie != null && spawnedPixie.gameObject.activeSelf;

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

        if (spawnedPixie != null)
        {
            Destroy(spawnedPixie.gameObject);
            spawnedPixie = null;
        }

        LoadAndSpawn(data);
    }

    private void LoadAndSpawn(OwnedPixieData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"{LogPrefix} LoadAndSpawn called with null OwnedPixieData on '{name}'.");
            return;
        }

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            Debug.LogError($"{LogPrefix} DataManager is null. Falling back to local pixie prefab for pixieId={data.pixieId}.");
            SpawnPrefab(pixiePrefab, data);
            return;
        }

        if (dataManager.FairyInfoDict == null)
        {
            Debug.LogError($"{LogPrefix} FairyInfoDict is null. CSV data may not be loaded or Addressables CSV load failed. Falling back to local pixie prefab for pixieId={data.pixieId}.");
            SpawnPrefab(pixiePrefab, data);
            return;
        }

        if (!dataManager.FairyInfoDict.TryGetValue(data.pixieId, out FairyInfoTable info))
        {
            Debug.LogWarning($"{LogPrefix} FairyInfo lookup failed for pixieId={data.pixieId}. Falling back to local pixie prefab.");
            SpawnPrefab(pixiePrefab, data);
            return;
        }

        if (string.IsNullOrEmpty(info.prefabPath))
        {
            Debug.LogWarning($"{LogPrefix} prefabPath is empty for pixieId={data.pixieId}. Falling back to local pixie prefab.");
            SpawnPrefab(pixiePrefab, data);
            return;
        }

        Addressables.LoadAssetAsync<GameObject>(info.prefabPath).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                SpawnPrefab(handle.Result, data);
            }
            else
            {
                Debug.LogError($"{LogPrefix} Failed to load pixie prefab address '{info.prefabPath}' for pixieId={data.pixieId}. Falling back to local pixie prefab. {handle.OperationException?.Message}");
                SpawnPrefab(pixiePrefab, data);
            }
        };
    }

    private void SpawnPrefab(GameObject prefab, OwnedPixieData data)
    {
        if (prefab == null)
        {
            Debug.LogError($"{LogPrefix} SpawnPrefab received a null prefab for pixieId={data?.pixieId}.");
            return;
        }

        spawnedPixie = Instantiate(prefab, new Vector3(transform.position.x, 0f, transform.position.z), Quaternion.identity)
            .GetComponent<PixieFollower>();

        if (spawnedPixie == null)
        {
            Debug.LogError($"{LogPrefix} Instantiated prefab '{prefab.name}' does not contain PixieFollower. pixieId={data?.pixieId}.");
            return;
        }

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
