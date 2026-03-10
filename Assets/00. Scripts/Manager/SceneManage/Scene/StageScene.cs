using UnityEngine;
using System.Collections;

public class StageScene : SceneBase
{
    public override IEnumerator EnterScene()
    {


        // GameEventManager.OnQuestProgressChanged?.Invoke();

        yield return null;
    }

    public override void ExitScene()
    {

    }
}