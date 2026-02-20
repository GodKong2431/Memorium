using UnityEngine;
using System.Collections;

public class StageScene : SceneBase
{
    public override IEnumerator EnterScene()
    {
        Debug.Log("스테이지 씬 로직 진입");

        // GameEventManager.OnQuestProgressChanged?.Invoke();

        yield return null;
    }

    public override void ExitScene()
    {
        Debug.Log("스테이지 씬 로직 종료");
    }
}