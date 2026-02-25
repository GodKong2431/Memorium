using UnityEngine;

public abstract class BaseUI : MonoBehaviour
{
    protected bool isInitialized = false;
    public abstract UIType GetUIType();
    public virtual void Initialize()
    {
        if (isInitialized) return;
        OnInit();
        isInitialized = true;
    }

    protected virtual void OnInit() { }
    public virtual void UpdateUI() { }
}
