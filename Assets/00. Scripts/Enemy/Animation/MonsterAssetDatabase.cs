using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 ID별 애니/이펙트/사운드 에셋 매핑. 프리팹에는 몬스터 ID만 넣고, 여기서 일괄 설정.
/// Resources에 "MonsterAssetDatabase" 로 두면 프리팹에서 참조 없이 자동 로드됨.
/// </summary>
[CreateAssetMenu(menuName = "Monster/Monster Asset Database", fileName = "MonsterAssetDatabase")]
public class MonsterAssetDatabase : ScriptableObject
{
    private const string ResourcesPath = "MonsterAssetDatabase";

    private static MonsterAssetDatabase _instance;

    /// <summary>
    /// Resources에서 로드. 에디터에서 할당한 인스턴스가 없을 때 사용.
    /// </summary>
    public static MonsterAssetDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<MonsterAssetDatabase>(ResourcesPath);
            return _instance;
        }
    }

    public static void SetInstance(MonsterAssetDatabase db) => _instance = db;

    [Serializable]
    public class Entry
    {
        [Tooltip("MonsterBasestatTable ID와 동일")]
        public int monsterId;
        [Header("Animation")]
        public MonsterAnimationConfig animationConfig;
        [Tooltip("공통 컨트롤러를 유지하면서 몬스터별 클립만 교체할 때 사용 (권장)")]
        public AnimatorOverrideController animatorOverrideController;
        [Header("VFX")]
        public GameObject attackEffectPrefab;
        public GameObject onHitEffectPrefab;
        public GameObject dieEffectPrefab;
        [Header("SFX")]
        public AudioClip attackSfx;
        public AudioClip onHitSfx;
        public AudioClip dieSfx;
        public AudioClip footstepSfx;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<int, Entry> _dict;

    private void BuildDict()
    {
        if (_dict != null) return;
        _dict = new Dictionary<int, Entry>();
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (_dict.ContainsKey(e.monsterId)) continue;
            _dict[e.monsterId] = e;
        }
    }

    public Entry GetEntry(int monsterId)
    {
        BuildDict();
        return _dict != null && _dict.TryGetValue(monsterId, out var e) ? e : null;
    }

    public MonsterAnimationConfig GetAnimationConfig(int monsterId)
    {
        var e = GetEntry(monsterId);
        return e?.animationConfig;
    }

    public GameObject GetAttackEffectPrefab(int monsterId)
    {
        var e = GetEntry(monsterId);
        return e?.attackEffectPrefab;
    }
}
