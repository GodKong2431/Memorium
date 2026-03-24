using System.Collections;
using UnityEngine;

public class DungeonScene : SceneBase
{
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private Transform sceneUiRoot;

    private bool isSceneReady;

    public override bool IsSceneReady => isSceneReady;

    private void Awake()
    {
        EnsureUi();
    }

    private void OnDestroy()
    {
        SceneUi.ClearRoot(sceneUiRoot);
    }

    public override IEnumerator EnterScene()
    {
        isSceneReady = false;

        yield return new WaitUntil(() => StageManager.Instance != null && StageManager.Instance.DataLoad);
        yield return new WaitUntil(() => MapManager.Instance != null && MapManager.Instance.mapSetting);

        InfinityMap infinityMap = null;
        yield return new WaitUntil(() =>
        {
            if (infinityMap == null)
                infinityMap = Object.FindFirstObjectByType<InfinityMap>();

            return infinityMap != null &&
                   infinityMap.firstMapSetting &&
                   infinityMap.InitialPlacementComplete;
        });

        RefreshUi();
        yield return null;
        yield return new WaitForEndOfFrame();

        isSceneReady = true;
    }

    public override void ExitScene()
    {
    }

    private void EnsureUi()
    {
        if (TryUseSceneUi())
            return;

        if (uiPrefab == null)
        {
            Debug.LogWarning("[DungeonScene] UI prefab is missing.", this);
            return;
        }

        GameObject ui = InstantiateUi();
        if (ui == null)
        {
            Debug.LogError("[DungeonScene] Failed to instantiate UI prefab.", this);
            return;
        }

        ui.name = uiPrefab.name;
        sceneUiRoot = ui.transform;
        SceneUi.SetRoot(sceneUiRoot);
        RefreshUi();
    }

    private GameObject InstantiateUi()
    {
        Object instance = Instantiate((Object)uiPrefab);
        if (instance is GameObject gameObject)
            return gameObject;

        if (instance is Component component)
            return component.gameObject;

        return null;
    }

    private bool TryUseSceneUi()
    {
        if (sceneUiRoot == null || sceneUiRoot.gameObject.scene != gameObject.scene)
            return false;

        if (!sceneUiRoot.gameObject.activeSelf)
            sceneUiRoot.gameObject.SetActive(true);

        SceneUi.SetRoot(sceneUiRoot);
        RefreshUi();
        return true;
    }

    private void RefreshUi()
    {
        SceneUiRuntime.Refresh(sceneUiRoot);
    }
}
