using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _loadingText;

    public void SetProgress(float progress)
    {
        if (_progressBar != null)
            _progressBar.value = progress;

        if (_loadingText != null)
            _loadingText.text = $"{(progress * 100):F0}%";
    }
}