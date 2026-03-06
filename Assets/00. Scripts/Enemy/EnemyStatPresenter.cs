using UnityEngine;

/// <summary>
/// 적의 스탯·보상 데이터를 제공.
/// EnemyStatSetting(ScriptableObject) 또는 DataManager 몬스터 ID로 데이터 소스 지정.
/// </summary>
public class EnemyStatPresenter : MonoBehaviour
{
    [SerializeField] private EnemyStatSetting setting;
    [SerializeField] [Tooltip("0이 아니면 DataManager에서 해당 몬스터 ID로 스탯 로드. setting보다 우선.")]
    public int monsterIdFromDataManager;

    private EnemyStatData _data;
    private int _loadedMonsterId;

    public EnemyStatData Data => _data ??= ResolveData();
    public EnemyRewardData RewardData => setting != null ? setting.reward : null;

    private EnemyStatData ResolveData()
    {
        // 1순위: DataManager 몬스터 ID
        if (monsterIdFromDataManager != 0)
        {
            var fromTable = MonsterDataProvider.GetEnemyStatData(monsterIdFromDataManager);
            if (fromTable != null)
            {
                _loadedMonsterId = monsterIdFromDataManager;
                LogStatDebug($"[EnemyStatPresenter] DataManager에서 로드", fromTable);
                return fromTable;
            }
        }
        // 2순위: EnemyStatSetting
        var fromSetting = setting != null ? setting.stat : null;
        if (fromSetting != null)
            LogStatDebug($"[EnemyStatPresenter] EnemyStatSetting에서 로드", fromSetting);
        return fromSetting;
    }

    public void SetData(EnemyStatData data)
    {
        _data = data;
        _loadedMonsterId = 0;
    }

    public void SetSetting(EnemyStatSetting newSetting)
    {
        setting = newSetting;
        _data = null;
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
    /// 스킬 공격형 몬스터용 스킬 ID.
    /// DataManager 기반: MonsterDataProvider.GetSkillId 사용.
    /// EnemyStatSetting 기반: setting.skillId 사용. 0이면 기본값(4000001) 사용.
    /// </summary>
    public int SkillId
    {
        get
        {
            if (monsterIdFromDataManager != 0 && MonsterDataProvider.IsSkillAttackMonster(monsterIdFromDataManager))
                return MonsterDataProvider.GetSkillId(monsterIdFromDataManager);
            return setting?.skillId ?? 0;
        }
    }

    /// <summary>
    /// 보스 몬스터 여부. DataManager 데이터 사용 시 자동 판별.
    /// </summary>
    public bool IsBoss => monsterIdFromDataManager != 0
        ? MonsterDataProvider.IsBoss(monsterIdFromDataManager)
        : (setting?.stat?.monsterType?.ToLowerInvariant().Contains("boss") ?? false);

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
        Debug.Log($"{prefix} | {gameObject.name} | ID:{data.monsterID} {data.monsterName} | HP:{data.monsterHealth} ATK:{data.monsterAttackpoint} SPD:{data.monsterSpeed} RNG:{data.attackRange}");
    }
}
