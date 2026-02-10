using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : MonoBehaviour
{
    public AssetReferenceGameObject characterPrefab;

    void Start()
    {
        Addressables.LoadAssetAsync<GameObject>("MyHero").Completed += OnLoaded;

        characterPrefab.LoadAssetAsync<GameObject>().Completed += OnLoaded;
    }

    private void OnLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Instantiate(handle.Result);
        }
        else
        {
            Debug.LogError("에셋 로드 실패!");
        }
    }
}
