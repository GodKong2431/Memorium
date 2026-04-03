using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>스킬 VFX 스폰 방식. Enemy=0은 구버전 직렬화와 동일.</summary>
public enum SkillEffectAttachTarget
{
    /// <summary>적 트랜스폼 자식으로 부착. 이동·회전 따름(브레스 등).</summary>
    Enemy = 0,
    /// <summary>스폰 시점 월드 좌표에 고정. 보스 이동과 무관(충격파 등).</summary>
    Field = 1,
}

/// <summary>
/// 스킬 준비/시전 프리팹을 부모(Enemy 또는 Player) 아래에 둘 때의 로컬 트랜스폼. 몬스터 ID별로 DB에서 지정.
/// </summary>
[Serializable]
public struct SkillEffectSpawnTransform
{
    [Tooltip("비우면 Enemy 루트. '자식/손자/…' 경로는 슬래시(Unity Transform.Find). 단일 이름만 넣으면 루트 아래에서 재귀 검색해 첫 일치(예: Head, mixamorig:Head).")]
    public string attachParentNameOrPath;
    [Tooltip("부모 트랜스폼 기준 로컬 위치 (앵커가 머리면 보통 0,0,0으로 두고 미세 조정만)")]
    public Vector3 localPosition;
    [Tooltip("부모 기준 로컬 오일러 각(도)")]
    public Vector3 localEulerAngles;
    [Tooltip("프리팹 lossyScale에 곱함. ≤0이면 1로 처리. EnemyStateMachine의 공격 이펙트 배율도 추가로 곱해짐")]
    public float uniformScaleMultiplier;
}

/// <summary>
/// 몬스터 ID별 애니·오버라이드·전투 VFX/SFX.
/// Resources에 "MonsterAssetDatabase" 로 두면 프리팹에서 참조 없이 자동 로드됨.
/// </summary>
[CreateAssetMenu(menuName = "Monster/Monster Asset Database", fileName = "MonsterAssetDatabase")]
public class MonsterAssetDatabase : ScriptableObject
{
    private const string ResourcesPath = "MonsterAssetDatabase";

    private static MonsterAssetDatabase _instance;

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

    [Header("공통 전투 VFX (프리팹 → ObjectPool)")]
    [Tooltip("공격 타격 시 플레이어 기준으로 스폰할 기본 프리팹")]
    public GameObject sharedAttackEffectPrefab;
    [Tooltip("피격(Onhit) 시 몬스터에 부착할 기본 프리팹")]
    public GameObject sharedOnHitEffectPrefab;

    /// <summary>일반 몬스터(보스 아님)용: 스킬 준비/시전·공격·피격 프리팹.</summary>
    [Serializable]
    public class NormalMonsterCombat
    {
        [Tooltip("적 로컬 기준 추가 오프셋. Enemy 부착 시 localPosition에 합산, Field는 스폰 시점 월드 위치 계산에 사용")]
        public Vector3 skillAttackEffectOffset;

        [Header("스킬 VFX")]
        public GameObject skillPrepareEffectPrefab;
        public SkillEffectAttachTarget skillPrepareAttachTo = SkillEffectAttachTarget.Enemy;
        public SkillEffectSpawnTransform skillPrepareSpawn;
        public GameObject skillCastEffectPrefab;
        public SkillEffectAttachTarget skillCastAttachTo = SkillEffectAttachTarget.Field;
        public SkillEffectSpawnTransform skillCastSpawn;

        [Header("이 페이지 프리팹이 비어 있으면 공용 shared 프리팹 사용")]
        public GameObject attackEffectPrefab;
        public GameObject onHitEffectPrefab;

        [Header("SFX — 스킬 (SoundTable ID)")]
        public int skillPrepareSoundId;
        public int skillCastSoundId;
    }

    /// <summary>보스용: 스킬 VFX·일반 공격 전용 프리팹 등.</summary>
    [Serializable]
    public class BossMonsterCombat
    {
        [Tooltip("적 로컬 기준 추가 오프셋. Enemy 부착 시 localPosition에 합산, Field는 스폰 시점 월드 위치 계산에 사용")]
        public Vector3 skillAttackEffectOffset;

        [Header("스킬 VFX")]
        public GameObject skillPrepareEffectPrefab;
        public SkillEffectAttachTarget skillPrepareAttachTo = SkillEffectAttachTarget.Enemy;
        public SkillEffectSpawnTransform skillPrepareSpawn;
        public GameObject skillCastEffectPrefab;
        public SkillEffectAttachTarget skillCastAttachTo = SkillEffectAttachTarget.Field;
        public SkillEffectSpawnTransform skillCastSpawn;

        [Tooltip("보스 일반 공격 연출(플레이어 부착 등). 비우면 공용 공격 프리팹")]
        public GameObject bossNormalAttackEffectPrefab;

        public GameObject attackEffectPrefab;
        public GameObject onHitEffectPrefab;

        [Header("SFX — 보스")]
        public int bossSpawnSoundId;
        public int bossAreaAttackPrepareSoundId;
        public int bossAreaAttackCastSoundId;
    }

    [Serializable]
    public class Entry
    {
        [Tooltip("MonsterBasestatTable ID와 동일")]
        public int monsterId;

        [Header("Animation")]
        public MonsterAnimationConfig animationConfig;
        [Tooltip("공통 컨트롤러를 유지하면서 몬스터별 클립만 교체할 때 사용")]
        public AnimatorOverrideController animatorOverrideController;

        [Header("전투 프레젠테이션 (일반 / 보스)")]
        public NormalMonsterCombat normalCombat = new NormalMonsterCombat();
        public BossMonsterCombat bossCombat = new BossMonsterCombat();

        public GameObject dieEffectPrefab;

        [Header("SFX (SoundTable ID, SoundManager)")]
        public int attackSoundId;
        public int onHitSoundId;
        public int dieSoundId;
        public int footstepSoundId;
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
}
