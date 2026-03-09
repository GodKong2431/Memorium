using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 버서커 모드 발동 시 플레이어 중심에 전기 VFX 적용, 지속시간 후 자동 해제.
/// </summary>
public class BerserkerModeController : MonoBehaviour
{
    public static event Action OnBerserkerModeStarted;
    public static event Action OnBerserkerModeEnded;
    public static BerserkerModeController Instance { get; private set; }

    private const string VfxResourcePath = "vfx_Electricity_01";

    [Header("설정")]
    [SerializeField] private float durationSeconds = 60f;
    [Tooltip("비워두면 이 컴포넌트의 transform 사용. VFX 위치용 (플레이어 루트 등).")]
    [SerializeField] private Transform vfxParent;

    public bool IsActive { get; private set; }

    private Transform VfxParent => vfxParent != null ? vfxParent : transform;
    private GameObject _berserkerVfx;
    private Coroutine _durationCoroutine;

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

    /// <summary>버서커 모드 발동. 오브 소모는 호출 전에 확인해야 함.</summary>
    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        AddVfx();
        OnBerserkerModeStarted?.Invoke();

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
            if (notifyEvent)
                OnBerserkerModeEnded?.Invoke();
        }
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(durationSeconds);
        RemoveVfx();
        _durationCoroutine = null;
    }
}
