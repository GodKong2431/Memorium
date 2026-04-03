using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 공격 패턴(일반 + 스킬). 전달된 목록에 스킬 행이 둘(attackType 1·2) 있으면 <b>skill2만</b> 사용한다.
/// BossManageTable 자체는 수정하지 않고, 여기서 후보만 줄인다.
/// </summary>
public class BossAttackManager
{
    private readonly Dictionary<AttackType, BossManageTable> _tableByType;

    private readonly Dictionary<AttackType, int> _currentBias = new();
    private readonly Dictionary<AttackType, float> _cooldownRemain = new();

    private int _consecutiveNormalCount;
    private float _elapsedSinceLastAttack;
    private float _elapsedSinceTwoNormals;
    private int _twoNormalElapsedSeconds;

    public BossManageTable CurrentAttack { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    BossManageTable _cheatSelectSkillOnce;
#endif

    public BossAttackManager(IEnumerable<BossManageTable> tables)
    {
        _tableByType = new Dictionary<AttackType, BossManageTable>();

        foreach (var t in tables)
        {
            if (t == null) continue;
            if (_tableByType.ContainsKey(t.attackType))
                continue;
            _tableByType[t.attackType] = t;
        }

        // 스킬은 1종만: skill2 행이 있으면 skill1 행은 출전하지 않음
        if (_tableByType.ContainsKey(AttackType.skillAttack1) && _tableByType.ContainsKey(AttackType.skillAttack2))
            _tableByType.Remove(AttackType.skillAttack1);

        foreach (var kv in _tableByType)
        {
            _currentBias[kv.Key] = kv.Value.baseAtkCastRate;
            _cooldownRemain[kv.Key] = 0f;
        }
    }

    public void Tick(float deltaTime)
    {
        _elapsedSinceLastAttack += deltaTime;

        var keys = new List<AttackType>(_cooldownRemain.Keys);
        foreach (var k in keys)
        {
            if (_cooldownRemain[k] > 0f)
                _cooldownRemain[k] = Mathf.Max(0f, _cooldownRemain[k] - deltaTime);
        }

        if (_consecutiveNormalCount >= 2)
        {
            _elapsedSinceTwoNormals += deltaTime;
            while (_elapsedSinceTwoNormals >= 1f)
            {
                _elapsedSinceTwoNormals -= 1f;
                _twoNormalElapsedSeconds++;
            }
        }
        else
        {
            _elapsedSinceTwoNormals = 0f;
            _twoNormalElapsedSeconds = 0;
        }

        if (_twoNormalElapsedSeconds > 0)
        {
            foreach (var kv in _tableByType)
            {
                var type = kv.Key;
                var table = kv.Value;

                if (type == AttackType.normalAttack)
                    continue;

                if (_cooldownRemain[type] > 0f)
                    continue;

                int baseBias = table.baseAtkCastRate;
                int incPerSec = table.atkBiasIncreaseRate;
                if (incPerSec <= 0) continue;

                int extra = incPerSec * _twoNormalElapsedSeconds;
                _currentBias[type] = baseBias + extra;
            }
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>디버그: 다음 <see cref="SelectNextAttack"/> 한 번만 스킬 행(skill2 우선, 없으면 skill1)을 쓴다.</summary>
    public bool CheatRequestSkillOnce()
    {
        if (_tableByType.TryGetValue(AttackType.skillAttack2, out var table) && table != null)
        {
            _cheatSelectSkillOnce = table;
            return true;
        }
        if (_tableByType.TryGetValue(AttackType.skillAttack1, out table) && table != null)
        {
            _cheatSelectSkillOnce = table;
            return true;
        }
        return false;
    }
#endif

    public BossManageTable SelectNextAttack()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_cheatSelectSkillOnce != null)
        {
            var forced = _cheatSelectSkillOnce;
            _cheatSelectSkillOnce = null;
            ApplySelection(forced.attackType, forced);
            return forced;
        }
#endif

        var candidates = new List<AttackType>();
        foreach (var kv in _tableByType)
        {
            if (_cooldownRemain.TryGetValue(kv.Key, out var remain) && remain <= 0f)
                candidates.Add(kv.Key);
        }

        if (candidates.Count == 0)
        {
            if (_tableByType.TryGetValue(AttackType.normalAttack, out var normalTable))
            {
                ApplySelection(AttackType.normalAttack, normalTable);
                return normalTable;
            }

            foreach (var kv in _tableByType)
            {
                ApplySelection(kv.Key, kv.Value);
                return kv.Value;
            }
            return null;
        }

        if (_consecutiveNormalCount >= 2 &&
            _tableByType.TryGetValue(AttackType.normalAttack, out var normal))
        {
            int skillBias = 0;
            if (_tableByType.ContainsKey(AttackType.skillAttack2))
                skillBias = _currentBias.TryGetValue(AttackType.skillAttack2, out var v2) ? v2 : 0;
            else if (_tableByType.ContainsKey(AttackType.skillAttack1))
                skillBias = _currentBias.TryGetValue(AttackType.skillAttack1, out var v1) ? v1 : 0;
            _currentBias[AttackType.normalAttack] = Mathf.Max(0, normal.baseAtkCastRate - skillBias);
        }

        int totalWeight = 0;
        foreach (var type in candidates)
        {
            if (!_currentBias.TryGetValue(type, out var w) || w <= 0) continue;
            totalWeight += w;
        }

        if (totalWeight <= 0)
        {
            foreach (var kv in _tableByType)
                _currentBias[kv.Key] = kv.Value.baseAtkCastRate;

            totalWeight = 0;
            foreach (var type in candidates)
                totalWeight += _currentBias[type];
        }

        int rand = UnityEngine.Random.Range(0, totalWeight);
        AttackType selected = candidates[0];

        int acc = 0;
        foreach (var type in candidates)
        {
            int w = _currentBias.TryGetValue(type, out var v) ? v : 0;
            acc += w;
            if (rand < acc)
            {
                selected = type;
                break;
            }
        }

        var table = _tableByType[selected];
        ApplySelection(selected, table);
        return table;
    }

    private void ApplySelection(AttackType type, BossManageTable table)
    {
        CurrentAttack = table;
        _elapsedSinceLastAttack = 0f;

        if (type == AttackType.normalAttack)
            _consecutiveNormalCount++;
        else
            _consecutiveNormalCount = 0;

        if (table != null)
            _cooldownRemain[type] = Mathf.Max(0f, table.atkCoolTime);

        if (table != null && string.Equals(table.resetBiasCheck, true))
        {
            foreach (var kv in _tableByType)
                _currentBias[kv.Key] = kv.Value.baseAtkCastRate;
        }
    }
}
