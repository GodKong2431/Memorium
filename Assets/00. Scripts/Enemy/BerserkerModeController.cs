using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 버서커 모드 발동 시 플레이어 중심에 전기 VFX 적용, 지속시간 후 자동 해제.
/// </summary>
public class BerserkerModeController : MonoBehaviour
{
    public static event Action<bool> OnBerserkerModeChanged;
    public static BerserkerModeController Instance { get; private set; }
    
    [SerializeField] public BerserkModeSo _berserkModeSo;
    
    [Header("오브 오브젝트")]
    [SerializeField] private string OrbKey = "Assets/02. Prefabs/Design/BerserkEffect/HCFX_ElementOrb_04.prefab";
    
    [Header("시작 이펙트")]
    [SerializeField] private string StartEffectKey1 = "Assets/02. Prefabs/Design/BerserkEffect/HCFX_Explosion_02_Air.prefab";
    [SerializeField] private string StartEffectKey2 = "Assets/02. Prefabs/Design/BerserkEffect/HCFX_Energy_04.prefab";
    [Header("시전중 이펙트")]
    [SerializeField] private string EffectKet1 = "Assets/02. Prefabs/Design/BerserkEffect/Poison aura.prefab";
    [SerializeField] private string EffectKet2 = "Assets/02. Prefabs/Design/BerserkEffect/Star aura.prefab";
    
    [Header("설정")]
    [SerializeField] private float durationSeconds = 60f;
    [SerializeField] private float startDelay = 0.3f;
    
    [Tooltip("비워두면 이 컴포넌트의 transform 사용. VFX 위치용 (플레이어 루트 등).")]
    [SerializeField] public Transform vfxParent;
    
    BerserkmodeManageTable berserkmodeManageTable;
    
    public bool IsActive { get; private set; }
    public float DurationSeconds => durationSeconds;
    public float RemainingDurationSeconds => IsActive ? Mathf.Max(0f, _endTime - Time.time) : 0f;
    public float RemainingDurationNormalized =>
        IsActive && durationSeconds > 0f
            ? Mathf.Clamp01(RemainingDurationSeconds / durationSeconds)
            : 0f;

    private Transform VfxParent => vfxParent != null ? vfxParent : transform;
    private GameObject _berserkerVfx;
    private Coroutine _durationCoroutine;
    private float _endTime;
    private readonly List<PoolableParticle> gradeEffects = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (_durationCoroutine != null)
                StopCoroutine(_durationCoroutine);
            // 파괴 과정에서는 UI/스탯 갱신 이벤트를 발행하지 않는다.
            RemoveVfx(false);
            Instance = null;
        }
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        
        SOset();
        
        durationSeconds = berserkmodeManageTable.durationTime;
        PlayerBerserkerOrb.Instance.Init(berserkmodeManageTable);
        EnemyKillRewardDispatcher.OnBerserkerOrb += (pos, count) => SpawnOrb(OrbKey, pos, false, count);
        
    }

    private void SOset()
    {
        if (DataManager.Instance == null || DataManager.Instance.BerserkmodeManageDict == null)
            return;

        if (!DataManager.Instance.BerserkmodeManageDict.TryGetValue(1050001, out berserkmodeManageTable) || berserkmodeManageTable == null)
            return;
        var keys = new List<StatType>(_berserkModeSo.BserserkMultStatSo.Keys);
        
        foreach (var key in keys)
        {
            switch (key)
            {
                case StatType.ATK:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseAttackStatMultiplier;
                    break;
                case StatType.CRIT_MULT:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseCriticalStatMultiplier;
                    break;
                case StatType.BOSS_DMG:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseBossDamageStatMultiplier;
                    break;
                case StatType.NORMAL_DMG:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseNormalDamageStatMultiplier;
                    break;
                case StatType.DMG_MULT:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseFinalDamageStatMultiplier;
                    break;
                case StatType.MOVE_SPEED:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseMoveSpeedStatMultiplier;
                    break;
                case StatType.COOLDOWN_REDUCE:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseCooltimeRegenStatMultiplier;
                    break;
            }
        }
    }

    /// <summary>버서커 모드 발동. 오브 소모는 호출 전에 확인해야 함.</summary>
    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        _endTime = Time.time + durationSeconds;
        AddVfx();
        OnBerserkerModeChanged?.Invoke(true);

        if (_durationCoroutine != null)
            StopCoroutine(_durationCoroutine);
        _durationCoroutine = StartCoroutine(DurationRoutine());
    }

    private void AddVfx()
    {
        SpawnEffect(StartEffectKey1, true);
        SpawnEffect(StartEffectKey2, true);
        
        StartCoroutine(DelayVfx());
    }
    
    private void SpawnOrb(string key, Vector3 transform,bool follow, int count)
    {
        
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(OrbKey, follow : follow, autoReturn : false, targetPosition : transform,
        onSpawned : particle =>{if (particle != null && particle.TryGetComponent<BerserkerOrb>(out var orb))
        {
                orb.Init(count);
            }
        }));
    }
    
    private void SpawnEffect(string key, bool follow)
    {
        if (string.IsNullOrEmpty(key)) return;
        
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(key, VfxParent,follow,true));
    }
    
    private void SpawnLoopEffect(string key, bool follow)
    {
        if (string.IsNullOrEmpty(key)) return;
        
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(key, VfxParent,follow,false,onSpawned: OnGradeEffectSpawned));
    }
    
    private IEnumerator DelayVfx()
    {
        yield return new WaitForSeconds(startDelay);
        
        if (!IsActive) yield break;
        
        PlayLoopEffect();
    }
    
    private void PlayLoopEffect()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayCombatLoopSfx(UiSoundIds.BerserkerActiveLoop);

        SpawnLoopEffect(EffectKet1, true);
        SpawnLoopEffect(EffectKet2, true);
    }
    
    public void RebindFollow (Transform transform)
    {
        if (gradeEffects != null)
        {
            foreach(var effect in gradeEffects)
            {
                effect.SetFollow(transform);
            }
        }
    }
    
    private void RemoveVfx(bool notifyEvent = true)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopCombatLoopSfx();

        foreach(var effect in gradeEffects)
        {
            effect.StopAndReturnManual();
        }
        
        gradeEffects.Clear();
        
        if (IsActive)
        {
            IsActive = false;
            _endTime = 0f;
            
            if (notifyEvent)
                OnBerserkerModeChanged?.Invoke(false);
        }
    }
    
    private void OnGradeEffectSpawned(PoolableParticle particle)
    {
        gradeEffects.Add(particle);
    }
    
    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(durationSeconds);
        RemoveVfx();
        _durationCoroutine = null;
    }
}
