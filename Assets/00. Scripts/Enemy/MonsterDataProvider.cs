using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DataManager의 몬스터 테이블을 Enemy 시스템에서 사용할 수 있도록 변환·제공.
/// MonsterBasestatTable → EnemyStatData 변환, MonsterStringTable 이름 조회 등.
/// </summary>
public static class MonsterDataProvider
{
    /// <summary>
    /// 몬스터 ID로 MonsterBasestatTable 조회.
    /// DataManager 로드 완료 후 사용.
    /// </summary>
    public static MonsterBasestatTable GetMonsterBaseStat(int monsterId)
    {
        var dm = DataManager.Instance;
        if (dm == null || dm.MonsterBasestatDict == null) return null;
        return dm.MonsterBasestatDict.TryGetValue(monsterId, out var stat) ? stat : null;
    }

    /// <summary>
    /// MonsterBasestatTable → EnemyStatData 변환.
    /// EnemyStatPresenter, EnemyStateMachine 등에서 사용하는 형식으로 변환.
    /// </summary>
    public static EnemyStatData ToEnemyStatData(MonsterBasestatTable table)
    {
        if (table == null) return null;

        return new EnemyStatData
        {
            monsterID = table.ID.ToString(),
            monsterName = GetDisplayName(table.monsterName) ?? table.monsterName,
            monsterType = table.monsterType.ToString(),
            monsterHealth = table.healthPoint,
            monsterAttackpoint = table.attackPoint,
            monsterAttackspeed = table.attackSpeed,
            monsterSpeed = table.speed,
            attackRange = table.attackRange,
            monsterDefense = table.baseDef,
        };
    }

    /// <summary>
    /// 몬스터 ID로 EnemyStatData 생성.
    /// DataManager 로드 완료 후 사용.
    /// </summary>
    public static EnemyStatData GetEnemyStatData(int monsterId)
    {
        var table = GetMonsterBaseStat(monsterId);
        return ToEnemyStatData(table);
    }

    /// <summary>
    /// 몬스터 이름 스트링 키(monsterName_01 등)로 표시명 조회.
    /// CSV/테이블 수정 없이 monsterNameKey 그대로 반환.
    /// </summary>
    public static string GetDisplayName(string monsterNameKey)
    {
        return string.IsNullOrEmpty(monsterNameKey) ? null : monsterNameKey;
    }

    /// <summary>
    /// 보스 몬스터 여부.
    /// </summary>
    public static bool IsBoss(int monsterId)
    {
        var table = GetMonsterBaseStat(monsterId);
        return table != null && table.monsterType == MonsterType.bossMonster;
    }

    /// <summary>
    /// 스킬 공격형 몬스터 여부 (monsterType == skillAttackMonster).
    /// </summary>
    public static bool IsSkillAttackMonster(int monsterId)
    {
        var table = GetMonsterBaseStat(monsterId);
        return table != null && table.monsterType == MonsterType.skillAttackMonster;
    }

    /// <summary>
    /// 스킬 공격형 몬스터의 스킬 ID.
    /// </summary>
    public static int GetSkillId(int monsterId)
    {
        var table = GetMonsterBaseStat(monsterId);
        if (table == null || table.monsterType != MonsterType.skillAttackMonster)
            return 0;
        return 4000002;
    }

    /// <summary>
    /// BossManageTable 조회 (보스 전용 공격/스킬 정보).
    /// </summary>
    public static BossManageTable GetBossManage(int bossId)
    {
        var dm = DataManager.Instance;
        if (dm == null || dm.BossManageDict == null) return null;
        return dm.BossManageDict.TryGetValue(bossId, out var boss) ? boss : null;
    }

    /// <summary>
    /// MonsterGroupTable에서 스폰 그룹 ID에 해당하는 몬스터 ID 목록 조회.
    /// groupId: TableBase.ID (스폰 그룹 ID). 동일 그룹 ID가 여러 행이면 dict 상 하나만 유지됨.
    /// </summary>
    public static List<int> GetMonsterIdsByGroup(int groupId)
    {
        var list = new List<int>();
        var dm = DataManager.Instance;
        if (dm?.MonsterGroupDict == null) return list;

        foreach (var kv in dm.MonsterGroupDict)
        {
            if (kv.Value != null && kv.Value.ID == groupId)
                list.Add(kv.Value.MonsterID);
        }
        return list;
    }
}
