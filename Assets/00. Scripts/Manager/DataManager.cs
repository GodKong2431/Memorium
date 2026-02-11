using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Linq;
using System.Reflection;

public class DataManager : Singleton<DataManager>
{
    #region 데이터 맵
    // 플레이어 데이터
    public Dictionary<int, PlayerExpTable> PlayerLevelDict;

    // 스킬 데이터
    public Dictionary<int, SkillTable> SkillDict;
    public Dictionary<int, SkillModule1Table> SkillModule1Dict;
    public Dictionary<int, SkillModule2Table> SkillModule2Dict;
    public Dictionary<int, SkillModule3Table> SkillModule3Dict;
    public Dictionary<int, SkillModule4Table> SkillModule4Dict;
    public Dictionary<int, SkillModule5Table> SkillModule5Dict;
    public Dictionary<int, M5FusionTable> M5FusionDict;
    #endregion

    // (현재개수, 총개수, 현재작업중인파일)
    public event Action<int, int, string> OnProgress;
    public event Action OnComplete;

    // AutoAddressableImporter에서 설정한 라벨 이름
    private const string LABEL_TO_LOAD = "CSV_Data";

    protected override void Awake()
    {
        base.Awake();
        LoadStart();
    }

    public void LoadStart()
    {
        StartCoroutine(LoadByLabel());
    }

    // 라벨 기반 데이터 로드 코루틴
    private IEnumerator LoadByLabel()
    {
        Debug.Log($"[DataManager] 라벨 '{LABEL_TO_LOAD}' 기반 데이터 로드 시작");

        // 로딩바 표시용 총 개수 구하기
        var locationHandle = Addressables.LoadResourceLocationsAsync(LABEL_TO_LOAD);
        yield return locationHandle;

        if (locationHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[DataManager] 라벨 '{LABEL_TO_LOAD}'을 찾을 수 없습니다. (Addressables Group 설정을 확인해야됨)");
            yield break;
        }

        int totalCount = locationHandle.Result.Count;
        int currentCount = 0;

        Debug.Log($"[DataManager] 로드 대상 발견: {totalCount}개");
        OnProgress?.Invoke(0, totalCount, "로딩 준비 중");

        // 핸들을 통해서 리스트를 통으로 받아옴
        var loadHandle = Addressables.LoadAssetsAsync<TextAsset>(LABEL_TO_LOAD, null);

        // 로드가 될 때까지 대기
        yield return loadHandle;

        // 로드 완료 후 처리
        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            IList<TextAsset> assets = loadHandle.Result;
            Debug.Log($"[DataManager] 파일 다운로드 완료 / 파싱 시작 (파일 개수: {assets.Count})");

            // 리스트 순회하면서 하나씩 처리
            foreach (TextAsset textAsset in assets)
            {
                if (textAsset != null)
                {
                    //파싱 및 주입 시도
                    bool isSuccess = ProcessTextAsset(textAsset);

                    // 진행도 업데이트
                    if (isSuccess)
                    {
                        currentCount++;
                        OnProgress?.Invoke(currentCount, totalCount, textAsset.name);
                    }
                }
            }

            // 최종 완료 처리
            OnProgress?.Invoke(totalCount, totalCount, "완료");

            // 완료 후 대기 시간
            yield return new WaitForSeconds(0.5f); 

            // 완료 이벤트 호출
            OnComplete?.Invoke();
            Debug.Log($"[DataManager] 최종 완료 (성공: {currentCount} / 총: {totalCount})");

            // 로드되어있던 CSV 
            Addressables.Release(loadHandle);
        }
        else
        {
            Debug.LogError("[DataManager] 데이터 다운로드 실패");
            Addressables.Release(loadHandle);
        }

        Addressables.Release(locationHandle);
    }

    // 텍스트 에셋 처리 함수
    private bool ProcessTextAsset(TextAsset textAsset)
    {
        string className = textAsset.name;

        Type tableType = Assembly.GetExecutingAssembly().GetTypes()
            .FirstOrDefault(t => t.Name == className && t.IsSubclassOf(typeof(TableBase)));

        if (tableType == null)
        {
            Debug.LogWarning($"[DataManager] 클래스를 찾을 수 없음: {className}.cs (파일명과 클래스명이 같은지 확인해야됨)");
            return false;
        }

        // 제네릭 메서드 ParseAndInject 동적 호출
        MethodInfo method = typeof(DataManager).GetMethod("ParseAndInject", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo genericMethod = method.MakeGenericMethod(tableType);

        // 호출 결과 반환
        object result = genericMethod.Invoke(this, new object[] { textAsset.text, className });
        return (bool)result;
    }

    // 제네릭 파싱 및 주입 함수 (리플렉션으로 호출)
    private bool ParseAndInject<T>(string csvContent, string keyName) where T : TableBase, new()
    {
        // 파싱
        List<T> list = CSVHelper.ParseCSVData<T>(csvContent);
        Dictionary<int, T> dict = new Dictionary<int, T>();

        foreach (T data in list)
        {
            if (!dict.ContainsKey(data.ID)) dict.Add(data.ID, data);
            else Debug.LogWarning($"[DataManager] {keyName} - ID: {data.ID} 중복됨");
        }

        // 2. 변수 주입
        // 필드와 프로퍼티 모두 검색하도록 설정
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        bool injected = false;
        Type dictType = typeof(Dictionary<int, T>); // 우리가 찾아야 할 타입

        // 필드 검색
        var fields = this.GetType().GetFields(flags);
        foreach (var field in fields)
        {
            if (field.FieldType == dictType)
            {
                field.SetValue(this, dict); // 값 주입
                injected = true;
                Debug.Log($"[DataManager] {keyName} -> Field: {field.Name}");
                break;
            }
        }

        // 프로퍼티 ({ get; set; }) 검색  (필드에서 못 찾았을 경우)
        if (!injected)
        {
            var properties = this.GetType().GetProperties(flags);
            foreach (var prop in properties)
            {
                if (prop.CanWrite && prop.PropertyType == dictType)
                {
                    prop.SetValue(this, dict);
                    injected = true;
                    Debug.Log($"[DataManager] {keyName} -> Property: {prop.Name}");
                    break;
                }
            }
        }

        if (!injected)
        {
            // 주입 실패 로그
            Debug.LogError($"[DataManager] 변수 없음: {keyName} (Dictionary<int, {typeof(T).Name}> 타입의 변수를 선언해야됨)");
            return false;
        }

        return true;
    }
}