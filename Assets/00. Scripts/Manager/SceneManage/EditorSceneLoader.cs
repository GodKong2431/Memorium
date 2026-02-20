using UnityEngine;

public class EditorSceneLoader : MonoBehaviour
{
    public string sceneName;

    private void Update()
    {
        if(DataManager.Instance.DataLoad)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
