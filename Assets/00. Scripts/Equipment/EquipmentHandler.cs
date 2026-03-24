using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class EquipmentHandler : MonoBehaviour
{
    public static event Action EquipmentUiRefreshRequested;
    private static EquipmentHandler persistentInstance;
    public static EquipmentHandler Instance => persistentInstance;

    [SerializeField] private PlayerEquipment playerEquipment;

    public bool dataLoad;

    public int goldId = 0;
    private Coroutine sceneRefreshRoutine;

    private void Awake()
    {
        if (persistentInstance != null && persistentInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        persistentInstance = this;
        StripLegacyUiControllers();

        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>();

        if (playerEquipment != null)
            playerEquipment.equipmentHandler = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (sceneRefreshRoutine != null)
        {
            StopCoroutine(sceneRefreshRoutine);
            sceneRefreshRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (persistentInstance == this)
            persistentInstance = null;
    }

    private void StripLegacyUiControllers()
    {
        RemoveLegacyUiController(GetComponent<EquipCurrentUIController>());
        RemoveLegacyUiController(GetComponent<EquipReinforceUIController>());
        RemoveLegacyUiController(GetComponent<EquipSlotUIController>());
    }

    private static void RemoveLegacyUiController(MonoBehaviour controller)
    {
        if (controller == null)
            return;

        controller.enabled = false;
        Destroy(controller);
    }

    public void TestEquipmentReinforcement()
    {
        Debug.Log("[EquipmentHandler] 강화 시작");
        ReinforceEquipment(playerEquipment.weapon.ID);
    }

    // 플레이어 장비/인벤토리 데이터를 초기 상태로 세팅한다.
    public void SetMyEquipOnStart(int weaponId, int helmetId, int glovesId, int armorId, int bootsId, Dictionary<int, EquipmentData> equipCountDict)
    {
        if (playerEquipment == null)
        {
            Debug.LogWarning("[EquipmentHandler] PlayerEquipment 참조가 없습니다.");
            return;
        }

        playerEquipment.equipmentHandler = this;
        playerEquipment.OnEqipItem(weaponId);
        playerEquipment.OnEqipItem(helmetId);
        playerEquipment.OnEqipItem(glovesId);
        playerEquipment.OnEqipItem(armorId);
        playerEquipment.OnEqipItem(bootsId);

        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
        {
            Debug.LogWarning("[EquipmentHandler] InventoryManager가 없어 장비 인벤토리를 초기화할 수 없습니다.");
            return;
        }

        if (!equipmentModule.Setup(equipCountDict))
            Debug.LogWarning("[EquipmentHandler] 장비 모듈 초기화에 실패했습니다.");

        dataLoad = true;
        RaiseEquipmentUiRefreshRequested();
    }

    public void RefreshMyEquip()
    {
        RaiseEquipmentUiRefreshRequested();
    }


    // 외부 시스템에서 현재 PlayerEquipment를 안전하게 가져오도록 제공한다.
    public bool TryGetPlayerEquipment(out PlayerEquipment equipment)
    {
        equipment = playerEquipment;
        return equipment != null;
    }

    public bool CanAutoMerge()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;

        // 자동 합성 가능 여부는 모듈 계산을 그대로 사용한다.
        return equipmentModule.CanAutoMerge();
    }

    public bool CanAutoEquip()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;
        if (playerEquipment == null)
            return false;

        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int bestItemId = equipmentModule.GetBestEquipmentId(type);
            int currentItemId = playerEquipment.ReturnItemNum(type);

            if (IsBetterEquipment(bestItemId, currentItemId))
                return true;
        }

        return false;
    }

    // 장비 자동 합성을 수행한다.
    public bool TryAutoMerge()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;
        if (!equipmentModule.RunAutoMerge())
            return false;

        //장비 합성 결과 확인 메서드
        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            //if (equipmentModule.TryReturnMergeResult(type, out int id, out int count))
            //{
            //    Debug.Log($"[EquipmentHandler] 장비 합성 결과 {type} 아이디 : {id} 갯수 : {count}");
            //}
            if (equipmentModule.TryReturnMergeResult(type, out int id))
            {
                Debug.Log($"[EquipmentHandler] 장비 합성 결과 {type} 아이디 : {id}");
            }
        }

        // 합성 후 장비 관련 UI를 한 번에 갱신한다.
        RaiseEquipmentUiRefreshRequested();
        return true;
    }

    // UnityEvent 직렬화 호환용 래퍼.
    public void AutoMerge()
    {
        TryAutoMerge();
    }

    // 타입별 최상위 장비를 자동 장착한다.
    public bool TryAutoEquip()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;
        if (playerEquipment == null)
            return false;

        bool changed = false;

        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int bestItemId = equipmentModule.GetBestEquipmentId(type);
            int currentItemId = playerEquipment.ReturnItemNum(type);

            if (!IsBetterEquipment(bestItemId, currentItemId))
                continue;

            playerEquipment.OnEqipItem(bestItemId);
            changed = true;
        }

        if (changed)
            RaiseEquipmentUiRefreshRequested();

        return changed;
    }

    // UnityEvent 직렬화 호환용 래퍼.
    public void AutoEquip()
    {
        TryAutoEquip();
    }

    private static bool TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule)
    {
        equipmentModule = null;

        if (InventoryManager.Instance == null)
            return false;

        equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        return equipmentModule != null;
    }

    private static bool IsBetterEquipment(int candidateItemId, int currentItemId)
    {
        if (candidateItemId == 0)
            return false;
        if (currentItemId == 0)
            return true;
        if (candidateItemId == currentItemId)
            return false;
        if (!TryGetEquipInfo(candidateItemId, out EquipListTable candidateInfo))
            return false;
        if (!TryGetEquipInfo(currentItemId, out EquipListTable currentInfo))
            return true;
        if (candidateInfo.equipmentType != currentInfo.equipmentType)
            return false;

        int tierCompare = candidateInfo.equipmentTier.CompareTo(currentInfo.equipmentTier);
        if (tierCompare != 0)
            return tierCompare > 0;

        int rarityCompare = candidateInfo.rarityType.CompareTo(currentInfo.rarityType);
        if (rarityCompare != 0)
            return rarityCompare > 0;

        int gradeCompare = candidateInfo.grade.CompareTo(currentInfo.grade);
        if (gradeCompare != 0)
            return gradeCompare > 0;

        return candidateItemId > currentItemId;
    }

    private static bool TryGetEquipInfo(int itemId, out EquipListTable equipInfo)
    {
        equipInfo = null;
        if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return false;

        return DataManager.Instance.EquipListDict.TryGetValue(itemId, out equipInfo);
    }

    private static void RaiseEquipmentUiRefreshRequested()
    {
        EquipmentUiRefreshRequested?.Invoke();
    }

    //아이템 강화 메서드이자, 강화 가능한지 체크하는 메서드
    public bool ReinforceEquipment(int itemId)
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
        {
            Debug.Log("[EquipmentHandler] 강화 물품 가져오기 실패");
            return false;
        }
        EquipmentData equipmentData = equipmentModule.GetEquipment(itemId);
        if (equipmentData.equipmentId == 0)
        {
            Debug.Log($"[EquipmentHandler] 강화 물품 아이디 오류 기존 아이템 아이디 {itemId} 변경 후 아이템 아이디 {equipmentData.equipmentId}");
            return false;
        }
        else if (equipmentData.equipmentReinforcement >= 100)
        {
            Debug.Log("[EquipmentHandler] 강화 물품 강화레벨 최대치 도달");
            return false;  
        }

        //골드 아이디 불러오기 한 번 불러온 후로는 리턴
        SetGoldID();

        //아이템 정보 불러오기
        EquipListTable equipment = DataManager.Instance.EquipListDict[itemId];
        //보너스 스탯을 적용할 타입 불러오기
        int Stat1Id=DataManager.Instance.EquipListDict[itemId].statType1;
        int Stat2Id= DataManager.Instance.EquipListDict[itemId].statType2;
        //아이템 티어 불러오기
        int equipmentTier = equipment.equipmentTier; 
        //아이템 타입 불러오기
        EquipmentType equipmentType = equipment.equipmentType;
        //강화 수치 불러오기
        int curEquipmentReinforcement = equipmentData.equipmentReinforcement;

        
        //기본 강화 비용 <- 테이블에서 불러와라
        BigDouble baseCost = DataManager.Instance.EquipStatsDict[Stat1Id].baseCost;

        //티어별 강화 비용 가중치 <- 테이블에서 불러와라
        double tierWeight = DataManager.Instance.EquipStatsDict[Stat1Id].costPerTier;
        //강화 수치별 강화 비용 가중치 <- 테이블에서 불러와라
        float reinforcementWeight = DataManager.Instance.EquipStatsDict[Stat1Id].costPerLevel;

        //강화 비용 계산
        baseCost = baseCost * Math.Pow(tierWeight, (double)(equipmentTier - 1));
        BigDouble cost = baseCost + (baseCost * reinforcementWeight * curEquipmentReinforcement);

        //강화 비용 부족 시 False 반환
        if (InventoryManager.Instance.GetItemAmount(goldId) <= cost)
        {
            Debug.Log("[EquipmentHandler] 강화비용 부족");
            return false;
        }

        //골드 차감
        InventoryManager.Instance.RemoveItem(goldId, cost);

        //강화 수치 증가
        equipmentData.equipmentReinforcement += 1;
        equipmentModule.SetEquipment(equipmentData);
        
        //스탯 성장치 <- 테이블에서 불러와라
        //강화 수치 반영
        ReinforecementEquipmentStat.SetReinforcement(itemId, equipmentData.equipmentReinforcement);

        //강화 보너스 스탯 반영
        bool statBonusUpdate = ReinforecementEquipmentStat.SetBonusStat(itemId, equipmentData.equipmentReinforcement);


        //착용 중인 아이템일 경우 다시 장착하여 스탯 적용
        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int currentItemId = playerEquipment.ReturnItemNum(type);

            //현재 장착중인 장비를 강화했거나 스탯 보너스가 업데이트 되었을 경우
            if (currentItemId == itemId || statBonusUpdate)
            {
                playerEquipment.OnEqipItem(currentItemId);
                break;
            }
        }

        RaiseEquipmentUiRefreshRequested();
        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (persistentInstance != this)
            return;

        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>();

        if (playerEquipment != null)
            playerEquipment.equipmentHandler = this;

        if (sceneRefreshRoutine != null)
            StopCoroutine(sceneRefreshRoutine);

        sceneRefreshRoutine = StartCoroutine(RefreshAfterSceneLoad());
    }

    private System.Collections.IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;
        RaiseEquipmentUiRefreshRequested();
        sceneRefreshRoutine = null;
    }

    public void SetGoldID()
    {

        if (goldId != 0)
            return;
        else
        {
            foreach (var item in DataManager.Instance.ItemInfoDict)
            {
                if (item.Value.itemType == ItemType.FreeCurrency)
                {
                    goldId = item.Key;
                    break;
                }
            }
        }
    }
}
