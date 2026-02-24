using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 퀘스트 진행상태 UI 클래스
/// </summary>
public class QuestUIController : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI textQuestTitle;
    public TextMeshProUGUI textQuestProgress;
    public Image imageProgressBar;
    public Button btnClaimReward;

    public void Start()
    {
        //yield return new WaitUntil(() => DataManager.Instance != null);
        //yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        // 화면 갱신 이벤트 구독
        GameEventManager.OnQuestProgressChanged += UpdateUI;
        GameEventManager.OnQuestCompleted += ShowRewardButton;

        // 보상 받기 버튼 이벤트 연결
        if (btnClaimReward != null)
        {
            btnClaimReward.onClick.RemoveAllListeners();
            btnClaimReward.onClick.AddListener(OnClickClaimReward);
            btnClaimReward.interactable = false;
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        GameEventManager.OnQuestProgressChanged -= UpdateUI;
        GameEventManager.OnQuestCompleted -= ShowRewardButton;
    }

    /// <summary>
    /// UI를 업데이트하는 메서드, 퀘스트 진행상태 변경 시마다 호출
    /// </summary>
    private void UpdateUI()
    {
        var data = QuestManager.Instance.CurrentQuestData;
        int currentCount = QuestManager.Instance.currentProgress;
        Debug.Log($"Updating Quest UI: {data?.questTitle ?? "No Quest"}, Progress: {currentCount}/{data?.reqCount ?? 0}");
        // 모든 퀘스트 완료 시 예외 처리
        if (data == null)
        {
            if (textQuestTitle != null) textQuestTitle.text = "All Clear";
            if (textQuestProgress != null) textQuestProgress.text = "-";
            if (imageProgressBar != null) imageProgressBar.fillAmount = 1f;
            return;
        }

        if (textQuestTitle != null) textQuestTitle.text = data.questTitle;
        if (textQuestProgress != null) textQuestProgress.text = $"{currentCount} / {data.reqCount}";

        if (imageProgressBar != null && data.reqCount > 0)
        {
            imageProgressBar.fillAmount = (float)currentCount / data.reqCount;
        }

        if (btnClaimReward != null)
        {
            btnClaimReward.interactable = currentCount >= data.reqCount;
        }
    }

    /// <summary>
    /// 보상 버튼 활성화
    /// </summary>
    private void ShowRewardButton()
    {
        if (btnClaimReward != null)
        {
            btnClaimReward.interactable = true;
        }
    }

    /// <summary>
    /// 보상받기 클릭시
    /// </summary>
    private void OnClickClaimReward()
    {
        // 매니저에게 퀘스트 완료 처리
        QuestManager.Instance.ClaimReward();

        // 보상 버튼 다시 숨기기
        if (btnClaimReward != null)
        {
            btnClaimReward.interactable = false;
        }
    }
}