using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DataManager : Singleton<DataManager>
{
    #region Data Maps
    // 장비 드랍 테이블
    public Dictionary<int, EquipmentDropTable> EquipmentDropDict;
    public Dictionary<int, ItemDropTable> ItemDropDict;

    // 던전 테이블
    public Dictionary<int, DungeonReqTable> DungeonReqDict;

    // 몬스터 테이블
    public Dictionary<int, BossManageTable> BossManageDict;
    public Dictionary<int, MonsterBasestatTable> MonsterBasestatDict;
    public Dictionary<int, MonsterGroupTable> MonsterGroupDict;
    public Dictionary<int, MonsterGrowthTable> EnemyGrowthDict;

    // 아이템 테이블
    public Dictionary<int, ItemInfoTable> ItemInfoDict;

    // 장비 테이블
    public Dictionary<int, EquipArmorTable> EquipArmorDict;
    public Dictionary<int, EquipBootsTable> EquipBootsDict;
    public Dictionary<int, EquipWeaponTable> EquipWeaponDict;
    public Dictionary<int, EquipGloveTable> EquipGloveDict;
    public Dictionary<int, EquipHelmetTable> EquipHelmetDict;
    public Dictionary<int, EquipListTable> EquipListDict;
    public Dictionary<int, EquipStatsTable> EquipStatsDict;

    // 페어리/트리거 테이블
    public Dictionary<int, FairyStatTable> FairyStatDict;
    public Dictionary<int, FairyEffectTable> FairyEffectDict;
    public Dictionary<int, FairyGradeTable> FairyGradeDict;
    public Dictionary<int, FairyInfoTable> FairyInfoDict;
    public Dictionary<int, TriggerInfoTable> TriggerInfoDict;

    // 스테이지 테이블
    public Dictionary<int, StageManageTable> StageManageDict;

    // 플레이어 테이블
    public Dictionary<int, CharacterBaseStatInfoTable> CharacterBaseStatInfoDict;
    public Dictionary<int, BerserkmodeManageTable> BerserkmodeManageDict;
    public Dictionary<int, CharacterTable> CharacterDict;
    public Dictionary<int, LevelbonusTable> LevelbonusDict;
    public Dictionary<int, PlayerExpTable> PlayerLevelDict;
    public Dictionary<int, StatUpgradeTable> StatUpgradeDict;
    public Dictionary<int, TraitInfoTable> TraitInfoDict;

    // 퀘스트 테이블
    public Dictionary<int, LineQuestTable> LineQuestDict;
    public Dictionary<int, QuestRewardsTable> QuestRewardsDict;

    // 스킬 테이블
    public Dictionary<int, SkillInfoTable> SkillInfoDict;
    public Dictionary<int, SkillUpTable> SkillUpDict;
    public Dictionary<int, SkillModule1Table> SkillModule1Dict;
    public Dictionary<int, SkillModule2Table> SkillModule2Dict;
    public Dictionary<int, SkillModule3Table> SkillModule3Dict;
    public Dictionary<int, SkillModule4Table> SkillModule4Dict;
    public Dictionary<int, SkillModule5Table> SkillModule5Dict;
    public Dictionary<int, M5FusionTable> M5FusionDict;

    // 패시브 테이블
    public Dictionary<int, PassiveGradeTable> PassiveGradeDict;
    public Dictionary<int, PassiveInfoTable> PassiveInfoDict;
    public Dictionary<int, PassiveSetTable> PassiveSetDict;

    // 문자열 테이블
    public Dictionary<int, StringTable> StringDict;

    // 갸차 테이블
    public Dictionary<int, GachaSkillScrollTable> GachaSkillScrollDict;
    public Dictionary<int, GachaEquipGroupTable> GachaEquipGroupDict;
    public Dictionary<int, GachaEquipTable> GachaEquipDict;
    public Dictionary<int, GachaTicketTable> GachaTicketDict;

    // 어빌리티 스톤 테이블
    public Dictionary<int, StoneTable> StoneDict;
    public Dictionary<int, StoneStatProbabilityTable> StoneStatProbabilityDict;
    public Dictionary<int, StoneGradeStatUpTable> StoneGradeStatUpDict;
    public Dictionary<int, StoneTotalUpBonusTable> StoneTotalUpBonusDict;

    // 빙고
    public Dictionary<int, BoardCellTable> BoardCellDict;
    public Dictionary<int, BoardSlotTable> BoardSlotDict;
    public Dictionary<int, BoardSynergyTable> BoardSynergyDict;
    public Dictionary<int, DustTable> DustDict;
    public Dictionary<int, OneuseItemTalble> OneuseItemDict;
    public Dictionary<int, SynergyTable> SynergyDict;

    // 기타
    public Dictionary<int, ConfigTable> ConfigDict;


    #endregion

    // (현재 개수, 총 개수, 현재 처리 중 파일명)
    public event Action<int, int, string> OnProgress;
    public event Action OnComplete;

    // AutoAddressableImporter에서 설정한 라벨명
    private const string LABEL_TO_LOAD = "CSV_Data";

    // 데이터 로드 완료 여부
    public bool DataLoad = false;

    protected override void Awake()
    {
        base.Awake();
        LoadStart();
    }

    public void LoadStart()
    {
        StartCoroutine(LoadByLabel());
    }

    // 라벨 기반 CSV 로드 코루틴
    private IEnumerator LoadByLabel()
    {
        Debug.Log($"[DataManager] 라벨 '{LABEL_TO_LOAD}' 기반 CSV 로드 시작");

        // 라벨에 매핑된 리소스 개수 조회
        var locationHandle = Addressables.LoadResourceLocationsAsync(LABEL_TO_LOAD);
        yield return locationHandle;

        if (locationHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[DataManager] 라벨 '{LABEL_TO_LOAD}' 리소스 위치 조회 실패. Addressables 그룹 설정을 확인하세요.");
            yield break;
        }

        int totalCount = locationHandle.Result.Count;
        int currentCount = 0;

        Debug.Log($"[DataManager] CSV 에셋 발견: {totalCount}개");
        OnProgress?.Invoke(0, totalCount, "로딩 시작");

        // 라벨에 해당하는 TextAsset 일괄 로드
        var loadHandle = Addressables.LoadAssetsAsync<TextAsset>(LABEL_TO_LOAD, null);
        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            IList<TextAsset> assets = loadHandle.Result;
            Debug.Log($"[DataManager] CSV 다운로드 완료. 파싱 시작 (파일 {assets.Count}개)");

            foreach (TextAsset textAsset in assets)
            {
                if (textAsset == null)
                    continue;

                bool isSuccess = ProcessTextAsset(textAsset);
                if (isSuccess)
                {
                    currentCount++;
                    OnProgress?.Invoke(currentCount, totalCount, textAsset.name);
                }
            }

            OnProgress?.Invoke(totalCount, totalCount, "완료");
            yield return new WaitForSeconds(0.5f);

            OnComplete?.Invoke();
            Debug.Log($"[DataManager] 데이터 로드 완료 (성공 {currentCount}/{totalCount})");

            Addressables.Release(loadHandle);
            DataLoad = true;
        }
        else
        {
            Debug.LogError("[DataManager] CSV 에셋 다운로드 실패");
            Addressables.Release(loadHandle);
        }

        Addressables.Release(locationHandle);
    }

    // TextAsset 한 개를 클래스에 매핑하여 주입
    private bool ProcessTextAsset(TextAsset textAsset)
    {
        string className = textAsset.name;

        Type tableType = Assembly.GetExecutingAssembly().GetTypes()
            .FirstOrDefault(t => t.Name == className && t.IsSubclassOf(typeof(TableBase)));

        if (tableType == null)
        {
            Debug.LogWarning($"[DataManager] 매핑할 테이블 클래스를 찾지 못했습니다: {className}.cs (파일명/클래스명 일치 여부 확인)");
            return false;
        }

        MethodInfo method = typeof(DataManager).GetMethod("ParseAndInject", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo genericMethod = method.MakeGenericMethod(tableType);
        string csvContent = DecodeCsvContent(textAsset);

        object result = genericMethod.Invoke(this, new object[] { csvContent, className });
        return (bool)result;
    }

    /// <summary>
    /// CSV는 UTF-8/CP949가 혼용될 수 있어 bytes 기준으로 복원한다.
    /// UTF-8 엄격 디코딩 실패 시 CP949로 재해석한다.
    /// </summary>
    private static string DecodeCsvContent(TextAsset textAsset)
    {
        byte[] bytes = textAsset.bytes;

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(949).GetString(bytes).TrimStart('\uFEFF');
        }
    }

    // CSV 파싱 후 Dictionary<int, T>를 DataManager 멤버로 주입
    private bool ParseAndInject<T>(string csvContent, string keyName) where T : TableBase, new()
    {
        List<T> list = CSVHelper.ParseCSVData<T>(csvContent);
        Dictionary<int, T> dict = new Dictionary<int, T>();

        foreach (T data in list)
        {
            if (!dict.ContainsKey(data.ID))
                dict.Add(data.ID, data);
            else
                Debug.LogWarning($"[DataManager] {keyName} 중복 ID: {data.ID}");
        }

        // 필드/프로퍼티 모두 탐색해서 타입이 맞는 멤버에 주입
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        bool injected = false;
        Type dictType = typeof(Dictionary<int, T>);

        var fields = GetType().GetFields(flags);
        foreach (var field in fields)
        {
            if (field.FieldType != dictType)
                continue;

            field.SetValue(this, dict);
            injected = true;
            Debug.Log($"[DataManager] {keyName} -> Field: {field.Name}");
            break;
        }

        if (!injected)
        {
            var properties = GetType().GetProperties(flags);
            foreach (var prop in properties)
            {
                if (!prop.CanWrite || prop.PropertyType != dictType)
                    continue;

                prop.SetValue(this, dict);
                injected = true;
                Debug.Log($"[DataManager] {keyName} -> Property: {prop.Name}");
                break;
            }
        }

        if (!injected)
        {
            Debug.LogError($"[DataManager] 주입 실패: {keyName} (Dictionary<int, {typeof(T).Name}> 타입 멤버 필요)");
            return false;
        }

        return true;
    }
}
