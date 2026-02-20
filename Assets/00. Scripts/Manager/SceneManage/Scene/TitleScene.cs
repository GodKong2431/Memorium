using UnityEngine;
using System.Threading.Tasks;

public class TitleScene : SceneBase
{
    public override async Task EnterScene()
    {
        Debug.Log("타이틀 씬 로직 진입");
        DataManager.Instance.LoadStart();
        while (!DataManager.Instance.DataLoad)
        {
            await Task.Yield();
        }
        SceneController.Instance.LoadScene(SceneType.StageScene);
    }

    public override async Task ExitScene()
    {
        Debug.Log("타이틀 씬 로직 종료");
        await Task.CompletedTask;
    }
}
