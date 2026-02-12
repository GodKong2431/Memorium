using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 버서커 모드 발동 시 플레이어에 빨간 라이트 적용, 지속시간 후 자동 해제.
/// 현재는 스탯 변경 없음.
/// </summary>
public class BerserkerModeController : MonoBehaviour
{
    public static event Action OnBerserkerModeEnded;
    public static BerserkerModeController Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private float durationSeconds = 60f;
    [SerializeField] private Color lightColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 5f;

    public bool IsActive { get; private set; }

    private Light _berserkerLight;
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
            RemoveLight();
            Instance = null;
        }
    }

    /// <summary>버서커 모드 발동. 오브 소모는 호출 전에 확인해야 함.</summary>
    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        AddLight();

        if (_durationCoroutine != null)
            StopCoroutine(_durationCoroutine);
        _durationCoroutine = StartCoroutine(DurationRoutine());
    }

    private void AddLight()
    {
        if (_berserkerLight != null) return;

        _berserkerLight = gameObject.AddComponent<Light>();
        _berserkerLight.type = LightType.Point;
        _berserkerLight.color = lightColor;
        _berserkerLight.intensity = lightIntensity;
        _berserkerLight.range = lightRange;
    }

    private void RemoveLight()
    {
        if (_berserkerLight != null)
        {
            Destroy(_berserkerLight);
            _berserkerLight = null;
        }
        if (IsActive)
        {
            IsActive = false;
            OnBerserkerModeEnded?.Invoke();
        }
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(durationSeconds);
        RemoveLight();
        _durationCoroutine = null;
    }
}
