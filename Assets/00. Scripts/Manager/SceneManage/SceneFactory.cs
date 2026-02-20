using UnityEngine;

public static class SceneFactory
{
    // eContentsTypes을 받아서 그에 맞는 SceneBase 자식을 반환
    public static SceneBase Create(SceneType type)
    {
        switch (type)
        {
            case SceneType.TitleScene:
                return new TitleScene(); // SceneBase 상속받은 클래스

            case SceneType.LoadingScene:
                return new LoadingScene(); // SceneBase 상속받은 클래스

            case SceneType.StageScene:
                return new StageScene(); // SceneBase 상속받은 클래스

            case SceneType.DungeonScene:
                return new DungeonScene(); // SceneBase 상속받은 클래스

            default:
                Debug.LogError($"[SceneFactory] 정의되지 않은 씬 타입입니다: {type}");
                return null;
        }
    }
}