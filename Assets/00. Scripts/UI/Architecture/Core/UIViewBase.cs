using UnityEngine;

public abstract class UIViewBase : MonoBehaviour
{
    // 화면을 활성화한다.
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    // 화면을 비활성화한다.
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
