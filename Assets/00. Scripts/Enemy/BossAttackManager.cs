using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 몬스터의 공격 패턴(일반/스킬1/스킬2)을 관리하는 헬퍼.
/// BossManageTable CSV에 정의된 각 공격 유형의 가중치/쿨타임/배율을 사용해 다음 공격을 선택.
/// </summary>
public class BossAttackManager
{
    private readonly Dictionary<AttackType, BossManageTable> _tableByType;

    private readonly Dictionary<AttackType, int> _currentBias = new();
    private readonly Dictionary<AttackType, float> _cooldownRemain = new();

    private int _consecutiveNormalCount;
    private float _elapsedSinceLastAttack;
    // 일반 공격 2회 이상 연속 시전 이후 경과 시간 누적 (소수 포함)
    private float _elapsedSinceTwoNormals;
    // 일반 공격 2회 이상 연속 시전 이후 "정수 초" 카운트 (기획서의 n초)
    private int _twoNormalElapsedSeconds;

    /// <summary>현재 선택된 공격 정보.</summary>
    public BossManageTable CurrentAttack { get; private set; }

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

        foreach (var kv in _tableByType)
        {
            _currentBias[kv.Key] = kv.Value.baseAtkCastRate;
            _cooldownRemain[kv.Key] = 0f;
        }
    }

    /// <summary>
    /// 매 프레임 호출해 쿨타임 및 가중치 누적 시간을 갱신.
    /// </summary>
    public void Tick(float deltaTime)
    {
        _elapsedSinceLastAttack += deltaTime;

        // 쿨타임 감소
        var keys = new List<AttackType>(_cooldownRemain.Keys);
        foreach (var k in keys)
        {
            if (_cooldownRemain[k] > 0f)
                _cooldownRemain[k] = Mathf.Max(0f, _cooldownRemain[k] - deltaTime);
        }

        // 일반 공격 2회 이상 연속 시점부터 경과 시간(n초)을 카운트
        if (_consecutiveNormalCount >= 2)
        {
            _elapsedSinceTwoNormals += deltaTime;

            // 리얼타임 1초 단위로만 n 증가
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

        // 스킬 가중치 = 최초값 + (증가량 × n초)  (n은 정수 초)
        if (_twoNormalElapsedSeconds > 0)
        {
            foreach (var kv in _tableByType)
            {
                var type = kv.Key;
                var table = kv.Value;

                if (type == AttackType.normalAttack)
                    continue;

                // 스킬 시전 후 쿨타임 동안 가중치 증가 없음
                if (_cooldownRemain[type] > 0f)
                    continue;

                int baseBias = table.baseAtkCastRate;        // ex) 130, 20
                int incPerSec = table.atkBiasIncreaseRate;   // ex) 50, 20
                if (incPerSec <= 0) continue;

                int extra = incPerSec * _twoNormalElapsedSeconds; // 50*n, 20*n
                _currentBias[type] = baseBias + extra;
            }
        }
    }

    /// <summary>
    /// 다음 공격을 선택. 기획서의 가중치/연속 일반공격 보정/쿨타임을 간략히 구현.
    /// </summary>
    public BossManageTable SelectNextAttack()
    {
        // Debug.Log($"[BossAttackManager] SelectNextAttack 호출 - elapsed={_elapsedSinceLastAttack:F2}");
        // 사용 가능한 타입만 고려(쿨타임이 남지 않은 것)
        var candidates = new List<AttackType>();
        foreach (var kv in _tableByType)
        {
            if (_cooldownRemain.TryGetValue(kv.Key, out var remain) && remain <= 0f)
                candidates.Add(kv.Key);
        }

        if (candidates.Count == 0)
        {
            // 모두 쿨이라면 강제로 일반 공격 사용
            if (_tableByType.TryGetValue(AttackType.normalAttack, out var normalTable))
            {
                // Debug.Log("[BossAttackManager] 모든 공격 쿨타임중 → 강제 일반 공격 선택");
                ApplySelection(AttackType.normalAttack, normalTable);
                return normalTable;
            }

            // 데이터가 잘못된 경우 첫 번째 항목 반환
            foreach (var kv in _tableByType)
            {
                ApplySelection(kv.Key, kv.Value);
                // Debug.Log($"[BossAttackManager] 후보 없음 → 임의 공격 사용 id={kv.Value.ID}, type={kv.Key}");
                return kv.Value;
            }
            return null;
        }

        // 연속 일반 공격 2회 이상이면 일반 공격 가중치 재계산
        if (_consecutiveNormalCount >= 2 &&
            _tableByType.TryGetValue(AttackType.normalAttack, out var normal))
        {
            int s1 = _currentBias.TryGetValue(AttackType.skillAttack1, out var v1) ? v1 : 0;
            int s2 = _currentBias.TryGetValue(AttackType.skillAttack2, out var v2) ? v2 : 0;
            _currentBias[AttackType.normalAttack] = Mathf.Max(0, normal.baseAtkCastRate - (s1 + s2));
            // Debug.Log($"[BossAttackManager] 연속 일반공격 {_consecutiveNormalCount}회 → normalBias 재계산: { _currentBias[AttackType.normalAttack]} (s1={s1}, s2={s2})");
        }

        int totalWeight = 0;
        foreach (var type in candidates)
        {
            if (!_currentBias.TryGetValue(type, out var w) || w <= 0) continue;
            totalWeight += w;
        }

        if (totalWeight <= 0)
        {
            // 가중치가 모두 0이면 기본값으로 리셋
            foreach (var kv in _tableByType)
                _currentBias[kv.Key] = kv.Value.baseAtkCastRate;

            totalWeight = 0;
            foreach (var type in candidates)
                totalWeight += _currentBias[type];
            // Debug.Log($"[BossAttackManager] totalWeight=0 → 전체 가중치 리셋 후 totalWeight={totalWeight}");
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
        // Debug.Log($"[BossAttackManager] 공격 선택 완료 -> type={selected}, id={table.ID}, damageRate={table.skillDamageRate}, cool={table.atkCoolTime}");
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

        // 선택된 공격 유형은 쿨타임 설정
        if (table != null)
        {
            _cooldownRemain[type] = Mathf.Max(0f, table.atkCoolTime);
        }

        // 스킬 사용 후 가중치 초기화 옵션
        if (table != null && string.Equals(table.resetBiasCheck, "TRUE", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var kv in _tableByType)
                _currentBias[kv.Key] = kv.Value.baseAtkCastRate;
        }
    }
}

