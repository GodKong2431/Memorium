using UnityEngine;
using System.Collections;

public class StageScene : SceneBase
{
    public override IEnumerator EnterScene()
    {
        Debug.Log("�������� �� ���� ����");

        // GameEventManager.OnQuestProgressChanged?.Invoke();

        yield return null;
    }

    public override void ExitScene()
    {
        Debug.Log("�������� �� ���� ����");
    }
}