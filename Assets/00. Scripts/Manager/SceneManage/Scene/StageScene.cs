using System.Collections;
using UnityEngine;

public class StageScene : SceneBase
{
    public bool IsSceneReady { get; private set; }

    public override IEnumerator EnterScene()
    {
        IsSceneReady = false;

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

        IsSceneReady = true;
    }

    public override void ExitScene()
    {
    }
}
