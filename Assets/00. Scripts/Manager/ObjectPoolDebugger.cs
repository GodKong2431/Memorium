using UnityEngine;

/// <summary>
/// 오브젝트 풀 테스트용. 씬에 배치 후 플레이 시 풀 상태를 확인.
/// - Get/Return 시 Debug.Log 출력 (enableLog = true)
/// - Update에서 주기적으로 풀 상태 로그 (logInterval > 0)
/// </summary>
public class ObjectPoolDebugger : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool enableLog = true;
    [SerializeField] private float logInterval = 5f;

    private float _nextLogTime;

    private void OnEnable()
    {
        ObjectPoolManager.DebugLog = enableLog;
    }

    private void OnDisable()
    {
        ObjectPoolManager.DebugLog = false;
    }

    private void Update()
    {
        if (!enableLog || logInterval <= 0) return;
        if (Time.time < _nextLogTime) return;

        _nextLogTime = Time.time + logInterval;
#if UNITY_EDITOR
        ObjectPoolManager.LogPoolStatus();
#endif
    }

    /// <summary>
    /// 인스펙터 버튼 또는 코드에서 호출해 풀 상태를 즉시 로그.
    /// </summary>
    [ContextMenu("풀 상태 로그")]
    public void LogPoolStatusNow()
    {
#if UNITY_EDITOR
        ObjectPoolManager.LogPoolStatus();
#endif
    }
}
