using UnityEngine;
using System.Threading.Tasks;
using System.Collections;

public class DungeonScene : SceneBase
{
    public override IEnumerator EnterScene()
    {
        Debug.Log("던전 씬 로직 진입");

        yield return null;
    }

    public override void ExitScene()
    {
        Debug.Log("던전 씬 로직 종료");
    }
}
