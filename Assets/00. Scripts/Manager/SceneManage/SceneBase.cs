using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 씬 매니저가 상속받는 부모 클래스.
/// </summary>
public abstract class SceneBase : MonoBehaviour
{
    protected virtual void Start()
    {
        StartCoroutine(EnterScene());
    }

    public abstract IEnumerator EnterScene();

    public virtual void ExitScene()
    {
    }
}