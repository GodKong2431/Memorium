using TMPro;
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI uiTextMP;

    [Header("Display Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private string format = "{0:F1} FPS";

    private float _accumulatedDeltaTime = 0f;
    private int _frameCount = 0;
    private float _timeLeft;

    private void Start()
    {
        _timeLeft = updateInterval;

        if (uiTextMP == null)
        {
            Debug.LogWarning("FPS Counter: UI Text component is not assigned!");
        }
    }

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

    private void UpdateDisplay(float fps)
    {
        string text = string.Format(format, fps);

        uiTextMP.text = text;

        Color color = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
        uiTextMP.color = color;
    }
}
