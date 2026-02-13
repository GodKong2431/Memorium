using System.Threading.Tasks;

public class SceneBase
{
    public virtual async Task EnterScene()
    {
        await Task.CompletedTask;
    }

    public virtual async Task ExitScene()
    {
        await Task.CompletedTask;
    }
}