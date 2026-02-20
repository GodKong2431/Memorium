using UnityEngine;
using System.Threading.Tasks;

public class LoadingScene : SceneBase
{
    public override async Task EnterScene()
    {
        Debug.Log("로딩 씬 로직 진입");
        
        await Task.CompletedTask;
    }

    public override async Task ExitScene()
    {
        Debug.Log("로딩 씬 로직 종료");
        
        await Task.CompletedTask;
    }
}
