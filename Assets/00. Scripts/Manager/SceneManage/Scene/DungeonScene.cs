using System.Collections;
using UnityEngine;

public class DungeonScene : SceneBase
{
    private bool isSceneReady;

    public override bool IsSceneReady => isSceneReady;

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

        yield return null;
        yield return new WaitForEndOfFrame();

        isSceneReady = true;
    }

    public override void ExitScene()
    {
    }
}
