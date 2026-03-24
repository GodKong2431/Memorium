using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : SceneBase
{
    private const string LoadingBackgroundName = "BackGround";
    private const string LoadingBackgroundImageName = "(Img)BackGround";

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
    private bool suppressLoadingUi;
    private Image loadingBackgroundImage;
    private Color loadingBackgroundBaseColor = Color.black;

    public override IEnumerator EnterScene()
    {
        dataManager = DataManager.Instance;
        suppressLoadingUi = false;
        CacheLoadingBackground();

        if (loadingText != null)
            defaultLoadingTextColor = loadingText.color;

        ShowLoadingUi();
        SetLoadingStatus("Preparing game data...");
        SetLoadingProgress(0f);

        SubscribeLoadingEvents();

        if (dataManager.DataLoad)
        {
            ApplyDataLoadProgress(1f);
            SetLoadingStatus("Game data ready.");
        }
        else if (!dataManager.IsLoading)
        {
            dataManager.LoadStart();
        }

        while (!dataManager.DataLoad)
        {
            if (dataManager.RequiresUserRetry && WasRetryRequestedThisFrame())
            {
                SetLoadingStatus("Retrying...");
                dataManager.RetryLoad();
            }

            yield return null;
        }

        SetLoadingStatus("Opening stage...");
        UnsubscribeLoadingEvents();
        yield return StartCoroutine(LoadNextSceneAsync(nextScene));
    }

    public override void ExitScene()
    {
        suppressLoadingUi = true;
        HideLoadingUi();
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

        SetLoadingProgress(1f);
        SetLoadingStatus("Opening stage...");

        SceneController sceneController = SceneController.Instance;
        if (sceneController != null)
        {
            sceneController.LoadScene(sceneType);
            yield return null;

            while (sceneController != null && sceneController.IsLoading)
                yield return null;

            yield break;
        }

        ExitScene();
        SceneManager.LoadScene(sceneType.ToString());
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
        if (suppressLoadingUi)
            return;

        if (loadingCanvasRoot != null)
        {
            loadingCanvasRoot.gameObject.SetActive(true);

            if (loadingCanvasRoot.localScale == Vector3.zero)
                loadingCanvasRoot.localScale = Vector3.one;
        }

        if (loadingPanelRoot != null)
            loadingPanelRoot.gameObject.SetActive(true);

        SetLoadingBackgroundVisible(false);
    }

    private void HideLoadingUi()
    {
        if (loadingPanelRoot != null)
            loadingPanelRoot.gameObject.SetActive(false);

        if (loadingCanvasRoot != null)
            loadingCanvasRoot.gameObject.SetActive(false);
    }

    private void CacheLoadingBackground()
    {
        if (loadingCanvasRoot == null)
            return;

        Transform backgroundTransform = FindChildRecursive(loadingCanvasRoot, LoadingBackgroundName);
        if (backgroundTransform == null)
            backgroundTransform = FindChildRecursive(loadingCanvasRoot, LoadingBackgroundImageName);

        if (backgroundTransform == null)
            return;

        loadingBackgroundImage = backgroundTransform.GetComponent<Image>();
        if (loadingBackgroundImage != null)
            loadingBackgroundBaseColor = loadingBackgroundImage.color;
    }

    private void SetLoadingBackgroundVisible(bool visible)
    {
        if (loadingBackgroundImage == null)
            CacheLoadingBackground();

        if (loadingBackgroundImage == null)
            return;

        Color backgroundColor = loadingBackgroundBaseColor;
        backgroundColor.a = visible ? loadingBackgroundBaseColor.a : 0f;
        loadingBackgroundImage.color = backgroundColor;
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
        if (!suppressLoadingUi)
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

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform foundChild = FindChildRecursive(root.GetChild(i), targetName);
            if (foundChild != null)
                return foundChild;
        }

        return null;
    }
}
