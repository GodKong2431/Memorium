using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageUIController : MonoBehaviour
{
    [Header("스테이지 텍스트")]
    public TextMeshProUGUI textStageName;

    [Header("진행도 UI")]
    public TextMeshProUGUI textProgress; 
    public Image imageStageProgressBar;

    [Header("보스 소환 버튼")]
    public Button btnSummonBoss;

    private void Start()
    {
        GameEventManager.OnStageChanged += UpdateStageName;
        GameEventManager.OnStageProgressChanged += UpdateStageProgress;

        if (btnSummonBoss != null)
        {
            btnSummonBoss.onClick.RemoveAllListeners();
            btnSummonBoss.onClick.AddListener(OnClickSummonBoss);
            btnSummonBoss.interactable = false;
        }
    }

    private void OnDestroy()
    {
        GameEventManager.OnStageChanged -= UpdateStageName;
        GameEventManager.OnStageProgressChanged -= UpdateStageProgress;
    }

    private void UpdateStageName(int chapter, int stage)
    {
        if (textStageName != null)
            textStageName.text = $"Stage {chapter}-{stage}";
    }

    private void UpdateStageProgress(int currentKill, int maxKill)
    {
        if(currentKill >= maxKill)
            currentKill = maxKill;

        if (textProgress != null)
            textProgress.text = $"{currentKill} / {maxKill}";

        if (imageStageProgressBar != null && maxKill > 0)
            imageStageProgressBar.fillAmount = (float)currentKill / maxKill;

        if (btnSummonBoss != null)
        {
            btnSummonBoss.interactable = (currentKill >= maxKill);
        }
    }

    private void OnClickSummonBoss()
    {
        if (btnSummonBoss != null)
        {
            btnSummonBoss.interactable = false;
        }
        GameEventManager.OnSummonBossClicked?.Invoke();
    }
}