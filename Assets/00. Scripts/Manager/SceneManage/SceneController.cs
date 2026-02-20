using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    [Header("Settings")]
    [SerializeField] private string _loadingSceneName = "LoadingScene";

    private SceneBase _currentSceneController;
    private bool _isLoading;

    public async void LoadScene(SceneType type)
    {
        if (_isLoading) return;
        _isLoading = true;

        string targetSceneName = type.ToString();

        if (_currentSceneController != null)
        {
            await _currentSceneController.ExitScene();
            _currentSceneController = null;
        }

        AsyncOperation loadingSceneOp = SceneManager.LoadSceneAsync(_loadingSceneName);
        while (!loadingSceneOp.isDone) await Task.Yield();

        LoadingUI loadingUI = Object.FindFirstObjectByType<LoadingUI>();

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            await Task.Yield();

            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (progress >= 1f && timer >= 1.0f)
            {
                if (loadingUI != null) loadingUI.SetProgress(1f);

                op.allowSceneActivation = true;
            }
            else
            {
                if (loadingUI != null) loadingUI.SetProgress(progress);
            }
        }

        _currentSceneController = SceneFactory.Create(type);

        if (_currentSceneController != null)
        {
            await _currentSceneController.EnterScene();
        }

        _isLoading = false;
    }

    public void Button()
    {
        SceneController.Instance.LoadScene(SceneType.DungeonScene);
    }
}