using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class DataManager : Singleton<DataManager>
{
    private static bool addressablesCatalogCacheCleared;

    private const int TotalLoadAttemptCount = 3;
    private const float RetryDelaySeconds = 2f;
    private const float CatalogLookupTimeoutSeconds = 15f;
    private const float AssetDownloadTimeoutSeconds = 30f;
    private const string LabelToLoad = "CSV_Data";
    private const string StatusStarting = "게임 데이터를 준비하는 중...";
    private const string StatusLookupCatalog = "콘텐츠 목록을 확인하는 중...";
    private const string StatusDownloadAssets = "게임 데이터를 다운로드하는 중...";
    private const string StatusProcessingFormat = "게임 데이터를 확인하는 중... ({0}/{1})";
    private const string StatusComplete = "게임 데이터 준비가 완료되었습니다.";
    private const string UserRetryHint = "화면을 터치하면 다시 시도합니다.";

    #region Data Maps
    public SerializedDictionary<int, EquipmentDropTable> EquipmentDropDict;
    public SerializedDictionary<int, ItemDropTable> ItemDropDict;

    public SerializedDictionary<int, DungeonReqTable> DungeonReqDict;
    public SerializedDictionary<int, CelestiAlchemyWorkshopRewardTable> CelestiAlchemyWorkshopRewardDict;
    public SerializedDictionary<int, EidosTreasureVaultRewardTable> EidosTreasureVaultRewardDict;
    public SerializedDictionary<int, GuardianTaxVaultRewardTable> GuardianTaxVaultRewardDict;
    public SerializedDictionary<int, HallOfTrainingRewardTable> HallOfTrainingRewardDict;

    public SerializedDictionary<int, BossManageTable> BossManageDict;
    public SerializedDictionary<int, MonsterBasestatTable> MonsterBasestatDict;
    public SerializedDictionary<int, MonsterGroupTable> MonsterGroupDict;
    public SerializedDictionary<int, MonsterGrowthTable> EnemyGrowthDict;

    public SerializedDictionary<int, ItemInfoTable> ItemInfoDict;

    public SerializedDictionary<int, EquipArmorTable> EquipArmorDict;
    public SerializedDictionary<int, EquipBootsTable> EquipBootsDict;
    public SerializedDictionary<int, EquipWeaponTable> EquipWeaponDict;
    public SerializedDictionary<int, EquipGloveTable> EquipGloveDict;
    public SerializedDictionary<int, EquipHelmetTable> EquipHelmetDict;
    public SerializedDictionary<int, EquipListTable> EquipListDict;
    public SerializedDictionary<int, EquipStatsTable> EquipStatsDict;

    public SerializedDictionary<int, FairyStatTable> FairyStatDict;
    public SerializedDictionary<int, FairyEffectTable> FairyEffectDict;
    public SerializedDictionary<int, FairyGradeTable> FairyGradeDict;
    public SerializedDictionary<int, FairyInfoTable> FairyInfoDict;
    public SerializedDictionary<int, TriggerInfoTable> TriggerInfoDict;

    public SerializedDictionary<int, StageManageTable> StageManageDict;

    public SerializedDictionary<int, CharacterBaseStatInfoTable> CharacterBaseStatInfoDict;
    public SerializedDictionary<int, BerserkmodeManageTable> BerserkmodeManageDict;
    public SerializedDictionary<int, CharacterTable> CharacterDict;
    public SerializedDictionary<int, LevelbonusTable> LevelbonusDict;
    public SerializedDictionary<int, PlayerExpTable> PlayerLevelDict;
    public SerializedDictionary<int, StatUpgradeTable> StatUpgradeDict;
    public SerializedDictionary<int, TraitInfoTable> TraitInfoDict;

    public SerializedDictionary<int, LineQuestTable> LineQuestDict;
    public SerializedDictionary<int, QuestRewardsTable> QuestRewardsDict;

    public SerializedDictionary<int, SkillInfoTable> SkillInfoDict;
    public SerializedDictionary<int, SkillUpTable> SkillUpDict;
    public SerializedDictionary<int, SkillModule1Table> SkillModule1Dict;
    public SerializedDictionary<int, SkillModule2Table> SkillModule2Dict;
    public SerializedDictionary<int, SkillModule3Table> SkillModule3Dict;
    public SerializedDictionary<int, SkillModule4Table> SkillModule4Dict;
    public SerializedDictionary<int, SkillModule5Table> SkillModule5Dict;
    public SerializedDictionary<int, M5FusionTable> M5FusionDict;

    public SerializedDictionary<int, PassiveGradeTable> PassiveGradeDict;
    public SerializedDictionary<int, PassiveInfoTable> PassiveInfoDict;
    public SerializedDictionary<int, PassiveSetTable> PassiveSetDict;

    public SerializedDictionary<int, StringTable> StringDict;

    public SerializedDictionary<int, GachaSkillScrollTable> GachaSkillScrollDict;
    public SerializedDictionary<int, GachaEquipGroupTable> GachaEquipGroupDict;
    public SerializedDictionary<int, GachaEquipTable> GachaEquipDict;
    public SerializedDictionary<int, GachaTicketTable> GachaTicketDict;

    public SerializedDictionary<int, StoneTable> StoneDict;
    public SerializedDictionary<int, StoneStatProbabilityTable> StoneStatProbabilityDict;
    public SerializedDictionary<int, StoneGradeStatUpTable> StoneGradeStatUpDict;
    public SerializedDictionary<int, StoneTotalUpBonusTable> StoneTotalUpBonusDict;

    public SerializedDictionary<int, BoardCellTable> BoardCellDict;
    public SerializedDictionary<int, BoardSlotTable> BoardSlotDict;
    public SerializedDictionary<int, BoardSynergyTable> BoardSynergyDict;
    public SerializedDictionary<int, DustTable> DustDict;
    public SerializedDictionary<int, OneuseItemTalble> OneuseItemDict;
    public SerializedDictionary<int, SynergyTable> SynergyDict;

    public SerializedDictionary<int, ConfigTable> ConfigDict;
    #endregion

    public event Action<int, int, string> OnProgress;
    public event Action<float, string> OnNormalizedProgress;
    public event Action<string, bool> OnStatusMessageChanged;
    public event Action OnComplete;

    public bool DataLoad = false;

    private bool isLoading;
    private bool currentAttemptSucceeded;
    private AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> csvLocationHandle;
    private AsyncOperationHandle<IList<TextAsset>> csvAssetHandle;
    private bool hasCsvLocationHandle;
    private bool hasCsvAssetHandle;

    public string CurrentStatusMessage { get; private set; } = string.Empty;
    public string LastFailureUserMessage { get; private set; } = string.Empty;
    public string LastFailureDeveloperMessage { get; private set; } = string.Empty;
    public bool CurrentStatusIsError { get; private set; }
    public bool RequiresUserRetry { get; private set; }
    public bool IsLoading => isLoading;
    public int CurrentLoadAttempt { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        ClearStaleAddressablesCatalogCache();

        if (ShouldAutoStartLoading())
            LoadStart();
    }

    protected override void OnDestroy()
    {
        ReleaseAttemptHandles();
        base.OnDestroy();
    }

    public void LoadStart()
    {
        BeginLoad(userInitiated: false);
    }

    public void RetryLoad()
    {
        BeginLoad(userInitiated: true);
    }

    private void BeginLoad(bool userInitiated)
    {
        if (DataLoad || isLoading)
            return;

        if (RequiresUserRetry && !userInitiated)
            return;

        ResetLoadSession();
        isLoading = true;
        StartCoroutine(LoadByLabel());
    }

    private static void ClearStaleAddressablesCatalogCache()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return;
#endif
        if (addressablesCatalogCacheCleared)
            return;

        addressablesCatalogCacheCleared = true;

        string cacheDirectory = Path.Combine(Application.persistentDataPath, "com.unity.addressables");
        if (!Directory.Exists(cacheDirectory))
            return;

        try
        {
            Directory.Delete(cacheDirectory, true);
            Debug.Log($"[DataManager] Cleared cached Addressables catalog directory: {cacheDirectory}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DataManager] Failed to clear cached Addressables catalog directory: {cacheDirectory}\n{ex}");
        }
    }

    private static bool ShouldAutoStartLoading()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return !activeScene.IsValid() ||
               !string.Equals(activeScene.name, SceneType.TitleScene.ToString(), StringComparison.Ordinal);
    }

    private IEnumerator LoadByLabel()
    {
        Debug.Log($"[DataManager] Label '{LabelToLoad}' based CSV load started. Max attempts: {TotalLoadAttemptCount}");

        for (int attempt = 1; attempt <= TotalLoadAttemptCount && !DataLoad; attempt++)
        {
            CurrentLoadAttempt = attempt;
            currentAttemptSucceeded = false;
            PrepareForLoadAttempt(attempt);

            yield return LoadByLabelAttempt(attempt);

            if (currentAttemptSucceeded)
                break;

            if (attempt < TotalLoadAttemptCount)
            {
                string retryMessage = string.IsNullOrWhiteSpace(LastFailureUserMessage)
                    ? $"로드에 실패했습니다. 잠시 후 다시 시도합니다. ({attempt + 1}/{TotalLoadAttemptCount})"
                    : $"{LastFailureUserMessage}\n잠시 후 다시 시도합니다. ({attempt + 1}/{TotalLoadAttemptCount})";

                SetStatusMessage(retryMessage, true);
                Debug.LogWarning(
                    $"[DataManager] Load attempt {attempt}/{TotalLoadAttemptCount} failed. " +
                    $"Retrying in {RetryDelaySeconds:F1}s.\n{LastFailureDeveloperMessage}");

                yield return new WaitForSecondsRealtime(RetryDelaySeconds);
            }
        }

        isLoading = false;

        if (DataLoad)
            yield break;

        RequiresUserRetry = true;
        ReleaseAttemptHandles();

        string finalUserMessage = string.IsNullOrWhiteSpace(LastFailureUserMessage)
            ? "게임 데이터를 불러오지 못했습니다."
            : LastFailureUserMessage;

        SetStatusMessage($"{finalUserMessage}\n{UserRetryHint}", true);
        Debug.LogError(
            $"[DataManager] CSV load failed after {TotalLoadAttemptCount} attempts.\n" +
            $"{LastFailureDeveloperMessage}");
    }

    private void PrepareForLoadAttempt(int attempt)
    {
        ReleaseAttemptHandles();
        SetStatusMessage(attempt == 1 ? StatusStarting : $"다시 연결하는 중... ({attempt}/{TotalLoadAttemptCount})");
        ReportNormalizedProgress(0f, StatusStarting);
        Debug.Log($"[DataManager] Starting load attempt {attempt}/{TotalLoadAttemptCount}.");
    }

    private IEnumerator LoadByLabelAttempt(int attempt)
    {
        var locationHandle = Addressables.LoadResourceLocationsAsync(LabelToLoad, typeof(TextAsset));
        csvLocationHandle = locationHandle;
        hasCsvLocationHandle = true;

        bool locationTimedOut = false;
        yield return TrackHandleProgress(
            locationHandle,
            0f,
            0.15f,
            StatusLookupCatalog,
            CatalogLookupTimeoutSeconds,
            () =>
            {
                locationTimedOut = true;
                FailCurrentAttempt(
                    BuildUserFacingFailureMessage("콘텐츠 목록 확인", locationHandle.OperationException, true),
                    BuildHandleFailureDetails("LoadResourceLocationsAsync", attempt, locationHandle, CatalogLookupTimeoutSeconds, true));
            });

        if (locationTimedOut)
            yield break;

        if (locationHandle.Status != AsyncOperationStatus.Succeeded || locationHandle.Result == null)
        {
            FailCurrentAttempt(
                BuildUserFacingFailureMessage("콘텐츠 목록 확인", locationHandle.OperationException, false),
                BuildHandleFailureDetails("LoadResourceLocationsAsync", attempt, locationHandle, CatalogLookupTimeoutSeconds, false));
            yield break;
        }

        int totalCount = locationHandle.Result.Count;
        if (totalCount <= 0)
        {
            FailCurrentAttempt(
                "다운로드할 게임 데이터가 없습니다.",
                $"[DataManager] No TextAsset locations were found for label '{LabelToLoad}' on attempt {attempt}/{TotalLoadAttemptCount}.");
            yield break;
        }

        OnProgress?.Invoke(0, totalCount, "로딩 시작");
        Debug.Log($"[DataManager] CSV asset locations found: {totalCount} items.");

        var loadHandle = Addressables.LoadAssetsAsync<TextAsset>(LabelToLoad, null);
        csvAssetHandle = loadHandle;
        hasCsvAssetHandle = true;

        bool assetTimedOut = false;
        yield return TrackHandleProgress(
            loadHandle,
            0.15f,
            0.75f,
            StatusDownloadAssets,
            AssetDownloadTimeoutSeconds,
            () =>
            {
                assetTimedOut = true;
                FailCurrentAttempt(
                    BuildUserFacingFailureMessage("게임 데이터 다운로드", loadHandle.OperationException, true),
                    BuildHandleFailureDetails("LoadAssetsAsync<TextAsset>", attempt, loadHandle, AssetDownloadTimeoutSeconds, true));
            });

        if (assetTimedOut)
            yield break;

        if (loadHandle.Status != AsyncOperationStatus.Succeeded || loadHandle.Result == null)
        {
            FailCurrentAttempt(
                BuildUserFacingFailureMessage("게임 데이터 다운로드", loadHandle.OperationException, false),
                BuildHandleFailureDetails("LoadAssetsAsync<TextAsset>", attempt, loadHandle, AssetDownloadTimeoutSeconds, false));
            yield break;
        }

        IList<TextAsset> assets = loadHandle.Result;
        int currentCount = 0;
        List<string> failedTables = new List<string>();

        Debug.Log($"[DataManager] CSV download completed. Parsing {assets.Count} files.");

        foreach (TextAsset textAsset in assets)
        {
            if (textAsset == null)
            {
                failedTables.Add("<null>");
                continue;
            }

            bool isSuccess = ProcessTextAsset(textAsset);
            if (!isSuccess)
            {
                failedTables.Add(textAsset.name);
                continue;
            }

            currentCount++;
            OnProgress?.Invoke(currentCount, totalCount, textAsset.name);
            ReportNormalizedProgress(
                CalculateProcessingProgress(currentCount, totalCount),
                string.Format(StatusProcessingFormat, currentCount, totalCount));
            yield return null;
        }

        if (failedTables.Count > 0)
        {
            FailCurrentAttempt(
                "일부 게임 데이터를 확인하지 못했습니다. 다시 시도해 주세요.",
                $"[DataManager] Table parse failed on attempt {attempt}/{TotalLoadAttemptCount}. " +
                $"Failed tables: {string.Join(", ", failedTables)}");
            yield break;
        }

        OnProgress?.Invoke(totalCount, totalCount, "완료");
        ReportNormalizedProgress(1f, "완료");
        SetStatusMessage(StatusComplete);

        yield return new WaitForSeconds(0.2f);

        DataLoad = true;
        currentAttemptSucceeded = true;
        LastFailureUserMessage = string.Empty;
        LastFailureDeveloperMessage = string.Empty;

        OnComplete?.Invoke();
        Debug.Log($"[DataManager] Data load completed successfully on attempt {attempt}/{TotalLoadAttemptCount}.");
    }

    private static float CalculateProcessingProgress(int currentCount, int totalCount)
    {
        if (totalCount <= 0)
            return 1f;

        float processingProgress = Mathf.Clamp01((float)currentCount / totalCount);
        return Mathf.Lerp(0.75f, 1f, processingProgress);
    }

    private IEnumerator TrackHandleProgress<T>(
        AsyncOperationHandle<T> handle,
        float startProgress,
        float endProgress,
        string label,
        float timeoutSeconds,
        Action onTimeout)
    {
        SetStatusMessage(label);
        float elapsed = 0f;

        while (!handle.IsDone)
        {
            float normalizedProgress = Mathf.Lerp(startProgress, endProgress, handle.PercentComplete);
            ReportNormalizedProgress(normalizedProgress, label);

            elapsed += Time.unscaledDeltaTime;
            if (timeoutSeconds > 0f && elapsed >= timeoutSeconds)
            {
                onTimeout?.Invoke();
                yield break;
            }

            yield return null;
        }

        ReportNormalizedProgress(endProgress, label);

        // Addressables completes some internal callbacks in LateUpdate.
        // Waiting one frame here prevents releasing the handle before those callbacks run.
        yield return null;
    }

    private void ReportNormalizedProgress(float progress, string currentFileName)
    {
        OnNormalizedProgress?.Invoke(Mathf.Clamp01(progress), currentFileName);
    }

    private void ResetLoadSession()
    {
        CurrentLoadAttempt = 0;
        RequiresUserRetry = false;
        CurrentStatusMessage = string.Empty;
        CurrentStatusIsError = false;
        LastFailureUserMessage = string.Empty;
        LastFailureDeveloperMessage = string.Empty;
    }

    private void FailCurrentAttempt(string userMessage, string developerMessage)
    {
        LastFailureUserMessage = userMessage;
        LastFailureDeveloperMessage = developerMessage;
        SetStatusMessage(userMessage, true);
        ReleaseAttemptHandles();
    }

    private void ReleaseAttemptHandles()
    {
        ReleaseCachedHandle(ref csvAssetHandle, ref hasCsvAssetHandle);
        ReleaseCachedHandle(ref csvLocationHandle, ref hasCsvLocationHandle);
    }

    private void SetStatusMessage(string message, bool isError = false)
    {
        string safeMessage = message ?? string.Empty;
        if (CurrentStatusMessage == safeMessage && CurrentStatusIsError == isError)
            return;

        CurrentStatusMessage = safeMessage;
        CurrentStatusIsError = isError;
        OnStatusMessageChanged?.Invoke(CurrentStatusMessage, CurrentStatusIsError);
    }

    private static string BuildUserFacingFailureMessage(string operationName, Exception exception, bool timedOut)
    {
        if (timedOut)
            return $"{operationName} 시간이 초과되었습니다. 네트워크 상태를 확인해 주세요.";

        if (Application.internetReachability == NetworkReachability.NotReachable)
            return $"{operationName}에 실패했습니다. 인터넷 연결을 확인해 주세요.";

        string detail = exception?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(detail))
            return $"{operationName}에 실패했습니다. 잠시 후 다시 시도해 주세요.";

        string lowerDetail = detail.ToLowerInvariant();
        if (lowerDetail.Contains("catalog"))
            return "콘텐츠 목록을 불러오지 못했습니다. 네트워크 상태를 확인한 뒤 다시 시도해 주세요.";

        if (lowerDetail.Contains("404"))
            return $"{operationName} 대상을 찾지 못했습니다. 최신 콘텐츠가 배포되었는지 확인해 주세요.";

        if (lowerDetail.Contains("403") || lowerDetail.Contains("401"))
            return $"{operationName} 권한 확인에 실패했습니다. 잠시 후 다시 시도해 주세요.";

        if (lowerDetail.Contains("timeout") || lowerDetail.Contains("timed out"))
            return $"{operationName} 시간이 초과되었습니다. 네트워크 상태를 확인해 주세요.";

        if (lowerDetail.Contains("resolve") || lowerDetail.Contains("host") || lowerDetail.Contains("dns"))
            return $"{operationName} 서버에 연결하지 못했습니다. 네트워크 상태를 확인해 주세요.";

        if (lowerDetail.Contains("connection") || lowerDetail.Contains("remoteproviderexception"))
            return $"{operationName}에 실패했습니다. 네트워크 상태를 확인한 뒤 다시 시도해 주세요.";

        return $"{operationName}에 실패했습니다. 잠시 후 다시 시도해 주세요.";
    }

    private static string BuildHandleFailureDetails<T>(
        string operationName,
        int attempt,
        AsyncOperationHandle<T> handle,
        float timeoutSeconds,
        bool timedOut)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[DataManager] Addressables load failure detail");
        builder.AppendLine($"Attempt: {attempt}/{TotalLoadAttemptCount}");
        builder.AppendLine($"Operation: {operationName}");
        builder.AppendLine($"TimedOut: {timedOut}");
        if (timedOut)
            builder.AppendLine($"TimeoutSeconds: {timeoutSeconds:F1}");

        builder.AppendLine($"Status: {handle.Status}");
        builder.AppendLine($"PercentComplete: {handle.PercentComplete:P0}");
        builder.AppendLine($"NetworkReachability: {Application.internetReachability}");
        builder.AppendLine($"OperationException: {handle.OperationException}");
        return builder.ToString();
    }

    private void ReleaseCachedHandle<T>(ref AsyncOperationHandle<T> handle, ref bool hasHandle)
    {
        if (!hasHandle)
            return;

        if (handle.IsValid())
            DeferredAddressablesRelease.Release(handle);

        hasHandle = false;
    }

    private bool ProcessTextAsset(TextAsset textAsset)
    {
        string className = textAsset.name;

        Type tableType = Assembly.GetExecutingAssembly().GetTypes()
            .FirstOrDefault(t => t.Name == className && t.IsSubclassOf(typeof(TableBase)));

        if (tableType == null)
        {
            Debug.LogWarning($"[DataManager] 留ㅽ븨???뚯씠釉??대옒?ㅻ? 李얠? 紐삵뻽?듬땲?? {className}.cs (?뚯씪紐??대옒?ㅻ챸 ?쇱튂 ?щ? ?뺤씤)");
            return false;
        }

        MethodInfo method = typeof(DataManager).GetMethod("ParseAndInject", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo genericMethod = method.MakeGenericMethod(tableType);
        string csvContent = DecodeCsvContent(textAsset);

        try
        {
            object result = genericMethod.Invoke(this, new object[] { csvContent, className });
            return result is bool boolResult && boolResult;
        }
        catch (TargetInvocationException ex)
        {
            Exception innerException = ex.InnerException ?? ex;
            Debug.LogError($"[DataManager] Failed to parse table '{className}'.\n{innerException}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] Failed to process table '{className}'.\n{ex}");
            return false;
        }
    }

    private static string DecodeCsvContent(TextAsset textAsset)
    {
        byte[] bytes = textAsset.bytes;

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            Debug.LogError($"[?몄퐫??踰붿씤 諛쒓껄] ?꾨꼍??UTF-8???꾨땶 ?뚯씪: {textAsset.name}");
            return Encoding.GetEncoding(949).GetString(bytes).TrimStart('\uFEFF');
        }
    }

    private bool ParseAndInject<T>(string csvContent, string keyName) where T : TableBase, new()
    {
        List<T> list = CSVHelper.ParseCSVData<T>(csvContent) ?? new List<T>();
        SerializedDictionary<int, T> dict = new SerializedDictionary<int, T>();

        if (list.Count == 0)
            Debug.LogWarning($"[DataManager] {keyName} parsed 0 rows.");
        else
            Debug.Log($"[DataManager] {keyName} parsed {list.Count} rows.");

        foreach (T data in list)
        {
            if (!dict.ContainsKey(data.ID))
                dict.Add(data.ID, data);
            else
                Debug.LogWarning($"[DataManager] {keyName} 以묐났 ID: {data.ID}");
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        bool injected = false;
        Type dictType = typeof(SerializedDictionary<int, T>);

        FieldInfo[] fields = GetType().GetFields(flags);
        foreach (FieldInfo field in fields)
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
            PropertyInfo[] properties = GetType().GetProperties(flags);
            foreach (PropertyInfo prop in properties)
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
            Debug.LogError($"[DataManager] 二쇱엯 ?ㅽ뙣: {keyName} (Dictionary<int, {typeof(T).Name}> ???硫ㅻ쾭 ?꾩슂)");
            return false;
        }

        return true;
    }
}
