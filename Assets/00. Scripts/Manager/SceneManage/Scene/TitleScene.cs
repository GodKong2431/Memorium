using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : SceneBase
{
    [SerializeField] private SceneType nextScene = SceneType.StageScene;
    [SerializeField, Range(0.5f, 0.95f)] private float dataLoadProgressWeight = 0.9f;
    [SerializeField] private RectTransform loadingCanvasRoot;
    [SerializeField] private RectTransform loadingPanelRoot;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI loadingText;

    private DataManager dataManager;

    public override IEnumerator EnterScene()
    {
        dataManager = DataManager.Instance;
        ShowLoadingUi();
        SetLoadingProgress(0f);

        SubscribeLoadingEvents();

        if (!dataManager.DataLoad)
        {
            dataManager.LoadStart();
            yield return new WaitUntil(() => dataManager.DataLoad);
        }
        else
        {
            ApplyDataLoadProgress(1f);
        }

        UnsubscribeLoadingEvents();
        yield return StartCoroutine(LoadNextSceneAsync(nextScene));
    }

    public override void ExitScene()
    {
        UnsubscribeLoadingEvents();
    }

    private void HandleDataLoadProgress(float normalizedProgress, string currentFileName)
    {
        ApplyDataLoadProgress(normalizedProgress);
    }

    private void ApplyDataLoadProgress(float normalizedProgress)
    {
        SetLoadingProgress(Mathf.Clamp01(normalizedProgress) * dataLoadProgressWeight);
    }

    private IEnumerator LoadNextSceneAsync(SceneType sceneType)
    {
        if (UIRoot.Instance != null)
            UIRoot.Instance.PrepareForSceneTransfer();

        ExitScene();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneType.ToString());
        if (operation == null)
            yield break;

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float sceneProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float blendedProgress = Mathf.Lerp(dataLoadProgressWeight, 1f, sceneProgress);

            SetLoadingProgress(blendedProgress);

            if (sceneProgress >= 1f)
            {
                SetLoadingProgress(1f);

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void SubscribeLoadingEvents()
    {
        if (dataManager == null)
            return;

        dataManager.OnNormalizedProgress -= HandleDataLoadProgress;
        dataManager.OnNormalizedProgress += HandleDataLoadProgress;
    }

    private void UnsubscribeLoadingEvents()
    {
        if (dataManager == null)
            return;

        dataManager.OnNormalizedProgress -= HandleDataLoadProgress;
    }

    private void ShowLoadingUi()
    {
        if (loadingCanvasRoot != null)
        {
            loadingCanvasRoot.gameObject.SetActive(true);

            if (loadingCanvasRoot.localScale == Vector3.zero)
                loadingCanvasRoot.localScale = Vector3.one;
        }

        if (loadingPanelRoot != null)
            loadingPanelRoot.gameObject.SetActive(true);
    }

    private void SetLoadingProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        ShowLoadingUi();

        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = clampedProgress;
        }

        if (loadingText != null)
            loadingText.text = $"{(clampedProgress * 100f):F0}%";
    }
}
