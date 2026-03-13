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
    private const string VfxResourcePath = "vfx_Electricity_01";

    [Header("설정")]
    [SerializeField] private float durationSeconds = 60f;
    
    [Tooltip("비워두면 이 컴포넌트의 transform 사용. VFX 위치용 (플레이어 루트 등).")]
    [SerializeField] private Transform vfxParent;
    
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
                case StatType.HP:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseHPStatMultiplier;
                    break;
                case StatType.ATK:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseAttackStatMultiplier;
                    break;
                case StatType.HP_REGEN:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseHPRegenStatMultiplier;
                    break;
                case StatType.MP:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseMPStatMultiplier;
                    break;
                case StatType.MP_REGEN:
                    _berserkModeSo.BserserkMultStatSo[key] = berserkmodeManageTable.baseMPRegenStatMultiplier;
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
        if (_berserkerVfx == null)
        {
            var prefab = Resources.Load<GameObject>(VfxResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[BerserkerModeController] Resources/{VfxResourcePath} 프리팹을 찾을 수 없습니다.");
                return;
            }
            _berserkerVfx = Instantiate(prefab, VfxParent);
            _berserkerVfx.transform.localPosition = Vector3.zero;
            _berserkerVfx.name = "BerserkerVfx";
        }

        _berserkerVfx.SetActive(true);
        foreach (var ps in _berserkerVfx.GetComponentsInChildren<ParticleSystem>())
            ps.Play();
    }

    private void RemoveVfx(bool notifyEvent = true)
    {
        if (_berserkerVfx != null)
        {
            _berserkerVfx.SetActive(false);
        }
        if (IsActive)
        {
            IsActive = false;
            _endTime = 0f;
            if (notifyEvent)
                OnBerserkerModeChanged?.Invoke(false);
        }
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(durationSeconds);
        RemoveVfx();
        _durationCoroutine = null;
    }
}
