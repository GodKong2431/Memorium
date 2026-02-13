using UnityEngine;

/// <summary>
/// DataManager 로드 완료 시 씬의 모든 EnemyStatPresenter를 실제 데이터로 갱신.
/// </summary>
public class EnemyStatDataManagerLink : MonoBehaviour
{
    private void OnEnable()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.OnComplete += RefreshAll;
        else
            StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.OnComplete -= RefreshAll;
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        while (DataManager.Instance == null)
            yield return null;
        DataManager.Instance.OnComplete += RefreshAll;
    }

    private void RefreshAll()
    {
        foreach (var presenter in Object.FindObjectsByType<EnemyStatPresenter>(FindObjectsSortMode.None))
            presenter.RefreshFromDataManager();
    }
}
