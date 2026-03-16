
using UnityEngine;

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
    private GameObject GetPrefab(int fairyID)
    {
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(fairyID, out var info)
            || string.IsNullOrEmpty(info.prefabPath))
            return pixiePrefab;

        var prefab = Resources.Load<GameObject>(info.prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[PixieSpawner] 로드 실패: {info.prefabPath}, fallback 사용");
            return pixiePrefab;
        }
        return prefab;
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

        var prefab = GetPrefab(data.pixieId);
        if (prefab == null) return;

        spawnedPixie = Instantiate(prefab, transform.position, Quaternion.identity)
            .GetComponent<PixieFollower>();

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