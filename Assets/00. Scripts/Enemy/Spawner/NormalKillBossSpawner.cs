using UnityEngine;

/// <summary>
/// 일반 몬스터를 N마리 처치하면 보스를 (0, 1, -4) 월드좌표에 소환.
/// 스포너 없이 단순 트리거.
/// </summary>
public class NormalKillBossSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private int killThreshold = 5;
    [SerializeField] private Vector3 bossSpawnPosition = new Vector3(0f, 1f, -4f);

    private int _normalKillCount;

    private void OnEnable()
    {
        EnemyKillRewardDispatcher.OnNormalEnemyKilled += OnNormalKilled;
        EnemyKillRewardDispatcher.OnBossKilled += OnBossKilled;
    }

    private void OnDisable()
    {
        EnemyKillRewardDispatcher.OnNormalEnemyKilled -= OnNormalKilled;
        EnemyKillRewardDispatcher.OnBossKilled -= OnBossKilled;
    }

    private void OnNormalKilled()
    {
        _normalKillCount++;
        if (_normalKillCount >= killThreshold)
        {
            SpawnBoss();
            _normalKillCount = 0;
        }
    }

    private void OnBossKilled()
    {
        _normalKillCount = 0;
    }

    private void SpawnBoss()
    {
        var prefab = bossPrefab != null ? bossPrefab : Resources.Load<GameObject>("BossEnemy");
        if (prefab == null)
        {
            Debug.LogWarning("[NormalKillBossSpawner] 보스 프리팹을 할당하거나 Resources/BossEnemy에 넣어주세요.");
            return;
        }

        Object.Instantiate(prefab, bossSpawnPosition, Quaternion.identity);
        Debug.Log($"[NormalKillBossSpawner] 일반 몬스터 {killThreshold}마리 처치 → 보스 소환 ({bossSpawnPosition})");
    }
}
