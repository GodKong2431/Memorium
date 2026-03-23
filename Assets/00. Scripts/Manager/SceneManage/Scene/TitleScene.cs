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
    [SerializeField] private Color loadingErrorTextColor = new Color(1f, 0.55f, 0.55f, 1f);

    private DataManager dataManager;
    private float currentLoadingProgress;
    private string currentLoadingStatus = string.Empty;
    private bool currentLoadingStatusIsError;
    private Color defaultLoadingTextColor = Color.white;

    public override IEnumerator EnterScene()
    {
        dataManager = DataManager.Instance;

        if (loadingText != null)
            defaultLoadingTextColor = loadingText.color;

        ShowLoadingUi();
        SetLoadingStatus("게임 데이터를 준비하는 중...");
        SetLoadingProgress(0f);

        SubscribeLoadingEvents();

        if (dataManager.DataLoad)
        {
            ApplyDataLoadProgress(1f);
            SetLoadingStatus("게임 데이터를 확인했습니다.");
        }
        else if (!dataManager.IsLoading)
        {
            dataManager.LoadStart();
        }

        while (!dataManager.DataLoad)
        {
            if (dataManager.RequiresUserRetry && WasRetryRequestedThisFrame())
            {
                SetLoadingStatus("다시 시도하는 중...");
                dataManager.RetryLoad();
            }

            yield return null;
        }

        SetLoadingStatus("게임 씬을 준비하는 중...");
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

    private void HandleStatusMessageChanged(string statusMessage, bool isError)
    {
        SetLoadingStatus(statusMessage, isError);
    }

    private void ApplyDataLoadProgress(float normalizedProgress)
    {
        SetLoadingProgress(Mathf.Clamp01(normalizedProgress) * dataLoadProgressWeight);
    }

    private IEnumerator LoadNextSceneAsync(SceneType sceneType)
    {
        if (UIRoot.Instance != null)
            UIRoot.Instance.PrepareForSceneTransfer();

        Scene sourceScene = gameObject.scene;
        string targetSceneName = sceneType.ToString();
        Scene loadedScene = default;
        bool targetSceneLoaded = false;

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, targetSceneName, System.StringComparison.Ordinal))
                return;

            loadedScene = scene;
            targetSceneLoaded = true;
            SceneManager.SetActiveScene(scene);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        SetLoadingStatus("게임 씬을 불러오는 중...");

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone || !targetSceneLoaded)
        {
            float sceneProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float blendedProgress = Mathf.Lerp(dataLoadProgressWeight, 0.98f, sceneProgress);

            SetLoadingProgress(blendedProgress);

            if (sceneProgress >= 1f)
                operation.allowSceneActivation = true;

            yield return null;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        StageScene stageScene = null;
        while (stageScene == null)
        {
            stageScene = FindSceneComponent<StageScene>(loadedScene);
            yield return null;
        }

        SetLoadingProgress(0.99f);
        SetLoadingStatus("스테이지를 준비하는 중...");
        yield return new WaitUntil(() => stageScene.IsSceneReady);
        SetLoadingProgress(1f);
        SetLoadingStatus("준비 완료");

        ExitScene();
        SceneManager.SetActiveScene(loadedScene);

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sourceScene);
        if (unloadOperation == null)
            yield break;

        while (!unloadOperation.isDone)
            yield return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            T component = rootObjects[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private void SubscribeLoadingEvents()
    {
        if (dataManager == null)
            return;

        dataManager.OnNormalizedProgress -= HandleDataLoadProgress;
        dataManager.OnNormalizedProgress += HandleDataLoadProgress;
        dataManager.OnStatusMessageChanged -= HandleStatusMessageChanged;
        dataManager.OnStatusMessageChanged += HandleStatusMessageChanged;

        if (!string.IsNullOrWhiteSpace(dataManager.CurrentStatusMessage))
            HandleStatusMessageChanged(dataManager.CurrentStatusMessage, dataManager.CurrentStatusIsError);
    }

    private void UnsubscribeLoadingEvents()
    {
        if (dataManager == null)
            return;

        dataManager.OnNormalizedProgress -= HandleDataLoadProgress;
        dataManager.OnStatusMessageChanged -= HandleStatusMessageChanged;
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
        currentLoadingProgress = Mathf.Clamp01(progress);
        RefreshLoadingUi();
    }

    private void SetLoadingStatus(string statusMessage, bool isError = false)
    {
        currentLoadingStatus = statusMessage ?? string.Empty;
        currentLoadingStatusIsError = isError;
        RefreshLoadingUi();
    }

    private void RefreshLoadingUi()
    {
        ShowLoadingUi();

        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = currentLoadingProgress;
        }

        if (loadingText == null)
            return;

        loadingText.color = currentLoadingStatusIsError ? loadingErrorTextColor : defaultLoadingTextColor;

        string progressText = $"{(currentLoadingProgress * 100f):F0}%";
        loadingText.text = string.IsNullOrWhiteSpace(currentLoadingStatus)
            ? progressText
            : $"{progressText}\n{currentLoadingStatus}";
    }

    private static bool WasRetryRequestedThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }
}
