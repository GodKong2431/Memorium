using TMPro;
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI uiTextMP; // FPS 표시 텍스트.

    [Header("Frame Rate Settings")]
    [SerializeField] private bool applyMobileFrameRate = true; // 모바일 환경에서 프레임 제한을 적용할지 여부.
    [SerializeField] private int preferredMobileFrameRate = 60; // 목표 프레임. 디스플레이 주사율에 맞게 보정된다.
    [SerializeField] private bool matchDisplayRefreshDivisor = true; // 디스플레이 주사율의 약수로 보정해 프레임 페이싱을 안정화한다.
    [SerializeField] private bool keepScreenAwake = true; // 디버그 중 화면 꺼짐 방지.

    [Header("Display Settings")]
    [SerializeField] private float updateInterval = 0.5f; // FPS 갱신 주기.
    [SerializeField] private string format = "{0:F1} FPS"; // FPS 표시 포맷.

    private float _accumulatedDeltaTime = 0f;
    private int _frameCount = 0;
    private float _timeLeft;

    // 시작 시 모바일 프레임 설정과 초기 상태를 적용한다.
    private void Start()
    {
        ApplyFrameRateSettings();
        _timeLeft = updateInterval;

        if (uiTextMP == null)
        {
            Debug.LogWarning("FPS Counter: UI Text component is not assigned!");
        }
    }

    // 일정 주기마다 평균 FPS를 계산해 표시한다.
    private void Update()
    {
        _accumulatedDeltaTime += Time.unscaledDeltaTime;
        _frameCount++;
        _timeLeft -= Time.unscaledDeltaTime;

        if (_timeLeft <= 0.0)
        {
            float fps = _frameCount / _accumulatedDeltaTime;
            UpdateDisplay(fps);

            _timeLeft = updateInterval;
            _accumulatedDeltaTime = 0f;
            _frameCount = 0;
        }
    }

    // 계산된 FPS를 텍스트와 색상으로 반영한다.
    private void UpdateDisplay(float fps)
    {
        if (uiTextMP == null) return;

        string text = string.Format(format, fps);

        uiTextMP.text = text;

        Color color = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
        uiTextMP.color = color;
    }

    // 현재 플랫폼과 디스플레이 주사율 기준으로 프레임 설정을 적용한다.
    private void ApplyFrameRateSettings()
    {
        if (!applyMobileFrameRate) return;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = ResolveTargetFrameRate();

        if (keepScreenAwake)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // 목표 프레임과 실제 주사율을 비교해 가장 안정적인 타겟 프레임을 선택한다.
    private int ResolveTargetFrameRate()
    {
        int displayRefreshRate = RefreshRate();

        if (preferredMobileFrameRate <= 0)
            return displayRefreshRate;

        int clampedTarget = Mathf.Min(preferredMobileFrameRate, displayRefreshRate);
        if (!matchDisplayRefreshDivisor)
            return clampedTarget;

        int divisorTarget = DivisorTargetFrameRate(displayRefreshRate, clampedTarget);
        return divisorTarget > 0 ? divisorTarget : clampedTarget;
    }

    // 현재 디바이스의 디스플레이 주사율을 안전하게 가져온다.
    private int RefreshRate()
    {
        float refreshRate = (float)Screen.currentResolution.refreshRateRatio.value;
        if (refreshRate <= 0f)
            return 60;

        return Mathf.Max(30, Mathf.RoundToInt(refreshRate));
    }

    // 주사율을 깔끔하게 나눌 수 있는 가장 높은 프레임 값을 찾는다.
    private int DivisorTargetFrameRate(int refreshRate, int targetLimit)
    {
        for (int candidate = targetLimit; candidate >= 30; candidate--)
        {
            if (refreshRate % candidate == 0)
                return candidate;
        }

        return 0;
    }
}
