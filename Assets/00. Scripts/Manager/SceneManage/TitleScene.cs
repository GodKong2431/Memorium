using UnityEngine;
using System.Threading.Tasks;

public class TitleScene : SceneBase
{
    public override async Task EnterScene()
    {
        Debug.Log("타이틀 씬 로직 진입");
        // 예: UI 매니저 초기화, 서버 연결 확인 등
        await Task.CompletedTask;
    }

    public override async Task ExitScene()
    {
        Debug.Log("타이틀 씬 로직 종료");
        // 예: 팝업 닫기, 데이터 저장 등
        await Task.CompletedTask;
    }
}
