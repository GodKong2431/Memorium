using UnityEngine;

/// <summary>
/// 적의 스탯 데이터를 제공.
/// DataManager에서 로드한 몬스터 ID 기반 데이터만 사용합니다.
/// </summary>
public class EnemyStatPresenter : MonoBehaviour
{
    private bool _subscribedToDataManager;
    [SerializeField] [Tooltip("DataManager에서 스탯을 로드할 몬스터 ID입니다. 0이면 기본값(내장 스탯) 사용.")]
    public int monsterIdFromDataManager;

    [SerializeField] private EnemyStatData _data;
    private int _loadedMonsterId;

    public EnemyStatData Data => _data ??= ResolveData();

    private EnemyStatData ResolveData()
    {
        // DataManager 몬스터 ID 기반으로만 스탯 로드
        if (monsterIdFromDataManager != 0)
        {
            var fromTable = MonsterDataProvider.GetEnemyStatData(monsterIdFromDataManager);
            if (fromTable != null)
            {
                _loadedMonsterId = monsterIdFromDataManager;
                LogStatDebug($"[EnemyStatPresenter] DataManager에서 로드", fromTable);
                return fromTable;
            }

            Debug.LogWarning($"[EnemyStatPresenter] DataManager.MonsterBasestatDict에서 ID {monsterIdFromDataManager}를 찾지 못했습니다. 기본값 스탯을 사용합니다. GameObject={gameObject.name}");
        }

        Debug.LogWarning($"[EnemyStatPresenter] monsterIdFromDataManager가 0이거나 유효한 데이터가 없습니다. 기본값 스탯을 사용합니다. GameObject={gameObject.name}");
        return null;
    }

    private void OnEnable()
    {
        SubscribeToDataManager();
        RefreshFromDataManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromDataManager();
    }

    private void SubscribeToDataManager()
    {
        if (_subscribedToDataManager)
            return;

        if (DataManager.Instance == null)
            return;

        DataManager.Instance.OnComplete += RefreshFromDataManager;
        _subscribedToDataManager = true;
    }

    private void UnsubscribeFromDataManager()
    {
        if (!_subscribedToDataManager)
            return;

        if (DataManager.Instance != null)
            DataManager.Instance.OnComplete -= RefreshFromDataManager;

        _subscribedToDataManager = false;
    }
    public void SetData(EnemyStatData data)
    {
        _data = data;
        _loadedMonsterId = 0;
    }

    /// <summary>
    /// DataManager에서 몬스터 ID로 스탯 로드.
    /// DataManager.OnComplete 이후 호출.
    /// </summary>
    public void SetMonsterId(int monsterId)
    {
        monsterIdFromDataManager = monsterId;
        _data = MonsterDataProvider.GetEnemyStatData(monsterId);
        _loadedMonsterId = monsterId;
    }

    /// <summary>
    /// 보스 몬스터 여부. DataManager 데이터만 사용합니다.
    /// </summary>
    public bool IsBoss => monsterIdFromDataManager != 0 && MonsterDataProvider.IsBoss(monsterIdFromDataManager);

    /// <summary>
    /// 스킬 공격형 몬스터(skillAttackMonster) 여부. DataManager 기준.
    /// </summary>
    public bool IsSkillAttackMonster =>
        monsterIdFromDataManager != 0 && MonsterDataProvider.IsSkillAttackMonster(monsterIdFromDataManager);

    /// <summary>
    /// DataManager 로드 완료 후, monsterIdFromDataManager로 스탯 재로드.
    /// </summary>
    public void RefreshFromDataManager()
    {
        if (monsterIdFromDataManager != 0)
        {
            _data = MonsterDataProvider.GetEnemyStatData(monsterIdFromDataManager);
            _loadedMonsterId = monsterIdFromDataManager;
            if (_data != null)
                LogStatDebug($"[EnemyStatPresenter] RefreshFromDataManager", _data);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogStatDebug(string prefix, EnemyStatData data)
    {
        if (data == null) return;

    }
}