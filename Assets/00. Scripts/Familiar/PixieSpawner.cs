
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PixieSpawner : MonoBehaviour
{

    private PixieFollower spawnedPixie;
    private OwnedPixieData fairyData;

    PlayerStateMachine playerStateMachine;
    EffectController effectController;

    public Transform SpawnedPixie => spawnedPixie != null ? spawnedPixie.transform : null;
    public bool IsSpawned =>spawnedPixie != null && spawnedPixie.gameObject.activeSelf;

    private PixieInventoryModule subscribedPixieModule;

    private void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();
        effectController = GetComponent<EffectController>();
    }
    public void OnEnable()
    {
        EnsurePixieModuleSubscription();
    }
    private void Update()
    {
        if (subscribedPixieModule == null)
            EnsurePixieModuleSubscription();
    }
    private void EnsurePixieModuleSubscription()
    {
        var module = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<PixieInventoryModule>()
            : null;
        if (module == null || subscribedPixieModule == module) return;

        UnsubscribePixieModule();
        subscribedPixieModule = module;
        subscribedPixieModule.OnPixieEquipped += SpawnPixie;

        int equippedId = subscribedPixieModule.EquippedPixiedID();
        if (equippedId != 0)
        {
            PreloadPixie(equippedId);
            SpawnPixie(equippedId);
        }
    }

    private void UnsubscribePixieModule()
    {
        if (subscribedPixieModule == null) return;
        subscribedPixieModule.OnPixieEquipped -= SpawnPixie;
        subscribedPixieModule = null;
    }

    private void OnDisable()
    {
        UnsubscribePixieModule();
        Despawn();
    }

    private void PreloadPixie(int pixieId)
    {
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(pixieId, out var info)) return;
        if (string.IsNullOrEmpty(info.prefabPath)) return;
        PoolAddressableManager.Instance.Preload(info.prefabPath);
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
            ObjectPoolManager.Return(spawnedPixie.gameObject);
            spawnedPixie = null;
        }
        LoadAndSpawn(data);
    }

    private void LoadAndSpawn(OwnedPixieData data)
    {
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(data.pixieId, out var info)) return;
        if (string.IsNullOrEmpty(info.prefabPath))return;

        var obj = PoolAddressableManager.Instance.GetPooledObject(info.prefabPath,
            new Vector3(transform.position.x, 0, transform.position.z), Quaternion.identity);

        if (obj != null)
        {
            SetupPixie(obj, data);
            return;
        }

        PoolAddressableManager.Instance.GetPooledObject(info.prefabPath,
            new Vector3(transform.position.x, 0, transform.position.z), Quaternion.identity,
            loadedObj => SetupPixie(loadedObj, data));
    }

    private void SetupPixie(GameObject obj, OwnedPixieData data)
    {
        if (obj == null) return;

        StartCoroutine(PlayerPosInitBeforeSetUpPixie(obj, data));
    }

    IEnumerator PlayerPosInitBeforeSetUpPixie(GameObject obj, OwnedPixieData data)
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        yield return new WaitUntil(() => agent.isOnNavMesh);
        spawnedPixie = obj.GetComponent<PixieFollower>();
        spawnedPixie.gameObject.SetActive(true);
        //여기서 플레이어 위치 값이 초기화됐는지 확인해야 하는디
        spawnedPixie.Init(transform, data, effectController, playerStateMachine._ctx);
    }

    public void Despawn()
    {
        if (IsSpawned)
        {
            ObjectPoolManager.Return(spawnedPixie.gameObject);
            spawnedPixie = null;
        }
    }
    public OwnedPixieData GetCurrentFairyData() => fairyData;

}