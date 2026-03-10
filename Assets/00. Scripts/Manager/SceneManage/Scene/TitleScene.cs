using UnityEngine;
using System.Collections;

public class TitleScene : SceneBase
{
    public override IEnumerator EnterScene()
    {


        DataManager.Instance.LoadStart();

        yield return new WaitUntil(() => DataManager.Instance.DataLoad);



        SceneController.Instance.LoadScene(SceneType.StageScene);
    }

    public override void ExitScene()
    {

    }
}