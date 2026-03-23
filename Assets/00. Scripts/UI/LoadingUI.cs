using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private RectTransform _visualRoot;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _loadingText;

    public void Show()
    {
        gameObject.SetActive(true);
        SetVisualRootActive(true);
        EnsureVisibleScale();
    }

    public void Hide()
    {
        SetVisualRootActive(false);
        gameObject.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        EnsureVisibleScale();

        if (_progressBar != null)
        {
            _progressBar.minValue = 0f;
            _progressBar.maxValue = 1f;
            _progressBar.value = clampedProgress;
        }

        if (_loadingText != null)
            _loadingText.text = $"{(clampedProgress * 100f):F0}%";
    }

    private void EnsureVisibleScale()
    {
        RectTransform rectTransform = _visualRoot != null ? _visualRoot : transform as RectTransform;
        if (rectTransform != null && rectTransform.localScale == Vector3.zero)
            rectTransform.localScale = Vector3.one;
    }

    private void SetVisualRootActive(bool isActive)
    {
        if (_visualRoot != null)
            _visualRoot.gameObject.SetActive(isActive);
    }
}
