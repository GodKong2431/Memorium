using UnityEngine;
using System.Collections;

public class TitleScene : SceneBase
{
    public override IEnumerator EnterScene()
    {
        Debug.Log("타이틀 씬 로직 진입: 데이터를 부릅니다.");

        DataManager.Instance.LoadStart();

        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        Debug.Log("데이터 로딩 완료! 스테이지로 넘어갑니다.");

        SceneController.Instance.LoadScene(SceneType.StageScene);
    }

    public override void ExitScene()
    {
        Debug.Log("타이틀 씬 로직 종료");
    }
}