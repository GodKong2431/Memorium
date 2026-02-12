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

    // 팩토리 메서드는 기존에 구현하신 것을 사용한다고 가정
    // private SceneFactory _sceneFactory; 

    public async void LoadScene(SceneType type)
    {
        if (_isLoading) return;
        _isLoading = true;

        string targetSceneName = type.ToString();

        // 이전 씬 정리
        if (_currentSceneController != null)
        {
            await _currentSceneController.ExitScene();
            _currentSceneController = null;
        }

        // 2. 로딩 씬으로 먼저 이동 (UI 표시용)
        // 로딩 씬은 가벼우므로 동기 로드 혹은 빠르게 로드
        AsyncOperation loadingSceneOp = SceneManager.LoadSceneAsync(_loadingSceneName);
        while (!loadingSceneOp.isDone) await Task.Yield();

        // 3. 로딩 씬의 UI 컨트롤러 찾기
        LoadingUI loadingUI = Object.FindFirstObjectByType<LoadingUI>();

        // 4. 목표 씬 비동기 로드 시작 (배경에서 로딩)
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false; // 로딩 완료 전까지 자동 전환 방지

        float timer = 0f;

        // 5. 로딩 진행 루프 (Task.Yield로 메인 스레드 양보하며 UI 갱신)
        while (!op.isDone)
        {
            await Task.Yield(); // 1프레임 대기

            timer += Time.deltaTime;

            // Unity의 로딩 진행률은 0.9에서 멈춤
            // 가짜 로딩(timer)과 실제 로딩(op.progress) 중 큰 값을 사용하여 부드럽게 처리
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            // 로딩이 너무 빠르면 눈에 안 보이므로 최소한의 연출 시간 보장 (예: 1초)
            if (progress >= 1f && timer >= 1.0f)
            {
                if (loadingUI != null) loadingUI.SetProgress(1f);

                // 씬 전환 승인
                op.allowSceneActivation = true;
            }
            else
            {
                // UI 갱신
                if (loadingUI != null) loadingUI.SetProgress(progress);
            }
        }

        // 6. 씬 전환 후 새 컨트롤러 생성 및 진입 (Enter)
        // 주의: 씬이 전환된 직후 프레임에 찾거나 생성해야 함
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