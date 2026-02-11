using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

/// <summary>
/// 게임 데이터를 총괄하는 매니저
/// Addressables를 통해 CSV를 로드하고, 파싱하여 Dictionary에 저장
/// 로드 완료 후 원본 텍스트 에셋 메모리는 즉시 해제
/// </summary>
public class DataManager : Singleton<DataManager>
{
    #region 테이블 저장용 딕셔너리 모음
    public Dictionary<int, PlayerExpTable> playerLevelDict { get; private set; }


    #endregion


    protected override void Awake()
    {
        StartCoroutine(LoadAllData());
    }



    /// <summary>
    /// 모든 데이터 테이블을 로드하는 코루틴
    /// </summary>
    private IEnumerator LoadAllData()
    {
        Debug.Log("[DataManager] 데이터 로드 시작");

        yield return StartCoroutine(LoadAddressableCSV<int, PlayerExpTable>("PlayerExpTable",
            data => data.playerLevel,
            dict => playerLevelDict = dict));

        // 추가 데이터 테이블도 여기에 같은 패턴으로 작성
        Debug.Log("[DataManager] 모든 데이터 로드 및 메모리 해제 완료");
    }





    // 제네릭 로더 함수 (로드 -> 파싱 -> 저장 -> 해제)
    private IEnumerator LoadAddressableCSV<Key, Data>(string addressKey, Func<Data, Key> keySelector, Action<Dictionary<Key, Data>> onComplete)
        where Data : new()
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(addressKey);

        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            TextAsset textAsset = handle.Result;
            string csvContent = textAsset.text;

            // CSVHelper를 통해 파싱
            List<Data> list = CSVHelper.ParseCSVData<Data>(csvContent);
            Dictionary<Key, Data> dict = new Dictionary<Key, Data>();

            foreach (Data data in list)
            {
                Key key = keySelector(data);

                if (dict.ContainsKey(key))
                {
                    Debug.LogWarning($"[DataManager] 중복 키 발견: {addressKey} / Key: {key}");
                    dict[key] = data;
                }
                else
                {
                    dict.Add(key, data);
                }
            }

            onComplete(dict);
            Debug.Log($"[DataManager] {addressKey} 파싱 완료 ({list.Count}개)");


            // 메모리 최적화용 원본 TextAsset 해제
            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError($"[DataManager] (로드 실패) 어드레서블 키를 확인: {addressKey}");
        }
    }
}