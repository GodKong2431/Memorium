using UnityEngine;

/// <summary>
/// Minimal shared lifecycle for UI controllers.
/// </summary>
public abstract class UIControllerBase : MonoBehaviour
{
    private bool isSubscribed;

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void OnEnable()
    {
        if (!isSubscribed)
        {
            Subscribe();
            isSubscribed = true;
        }

        RefreshView();
    }

    protected virtual void OnDisable()
    {
        if (!isSubscribed)
            return;

        Unsubscribe();
        isSubscribed = false;
    }

    protected virtual void Initialize()
    {
    }

    protected abstract void Subscribe();
    protected abstract void Unsubscribe();
    protected abstract void RefreshView();
}
