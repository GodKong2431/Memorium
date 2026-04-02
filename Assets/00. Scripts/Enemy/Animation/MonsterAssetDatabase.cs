using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>보스 스킬 DB 이펙트(skillAttackEffectPrefab)의 부모 Transform</summary>
public enum BossSkillEffectAttachParent
{
    Player = 0,
    Enemy = 1,
}

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
        [Tooltip("근접/일반 공격 이펙트. 보스는 BossManageTable 이펙트 문자열보다 이 할당이 우선")]
        public GameObject attackEffectPrefab;
        [Tooltip("스킬 공격 이펙트(일반 스킬몹 + 보스 스킬1/2). 보스는 CSV effect(Addressable)보다 이 할당이 우선")]
        public GameObject skillAttackEffectPrefab;
        [Tooltip("스킬 이펙트 로컬 오프셋. 일반 스킬몹: 플레이어 기준. 보스 skillAttackEffectPrefab은 Player 부착일 때만 적용(Enemy 부착 시 오프셋 미사용)")]
        public Vector3 skillAttackEffectOffset;
        [Tooltip("보스 스킬 이펙트 부모. skillAttackEffectPrefab(직접 프리팹)과 BossManageTable.effect(Addressable) 스킬 연출에 공통 적용. EnemyStateMachine.assetDatabaseOverride와 동일 에셋의 항목이어야 함")]
        public BossSkillEffectAttachParent bossSkillEffectAttachParent = BossSkillEffectAttachParent.Player;
        public GameObject onHitEffectPrefab;
        public GameObject dieEffectPrefab;
        [Header("SFX (SoundTable ID, SoundManager)")]
        [Tooltip("공격 타격 시(히트 판정 시). 0이면 재생 안 함")]
        public int attackSoundId;
        [Tooltip("피격(Onhit) 진입 시. 0이면 재생 안 함")]
        public int onHitSoundId;
        [Tooltip("사망(Dead) 진입 시. 0이면 재생 안 함")]
        public int dieSoundId;
        [Tooltip("애니 이벤트 Anim_PlayFootstep 연결용. 0이면 재생 안 함")]
        public int footstepSoundId;

        [Header("SFX — 스킬 (일반몹 SkillCaster 등)")]
        [Tooltip("스킬 시전 준비. SoundTable 예: 9100041")]
        public int skillPrepareSoundId;
        [Tooltip("스킬 발동(M3 실행 직전). 몬스터별 SoundTable 예: 9100042~9100045")]
        public int skillCastSoundId;

        [Header("SFX — 보스")]
        [Tooltip("보스 스폰 연출 진입 시. SoundTable 예: 9100046~9100050")]
        public int bossSpawnSoundId;
        [Tooltip("보스 범위/스킬 공격 시전 준비. SoundTable 예: 9100053(데몬), 9100054(기타)")]
        public int bossAreaAttackPrepareSoundId;
        [Tooltip("보스 범위/스킬 공격 발동(히트 타이밍). SoundTable 예: 9100055(드래곤), 9100056, 9100057")]
        public int bossAreaAttackCastSoundId;
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
