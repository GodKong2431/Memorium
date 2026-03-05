using UnityEngine;

/// <summary>
/// 최소 UI 컨트롤러 공통 생명주기 베이스.
/// </summary>
public abstract class UIControllerBase : MonoBehaviour
{
    private bool isSubscribed;

    // 파생 컨트롤러의 의존성 초기화를 수행한다.
    protected virtual void Awake()
    {
        Initialize();
    }

    // 활성화 시 구독 후 현재 상태를 반영한다.
    protected virtual void OnEnable()
    {
        if (!isSubscribed)
        {
            Subscribe();
            isSubscribed = true;
        }

        RefreshView();
    }

    // 비활성화 시 구독을 해제한다.
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

/// <summary>
/// UIViewBase 기반 컨트롤러를 위한 호환 베이스.
/// </summary>
public abstract class UIControllerBase<TView> : UIControllerBase where TView : UIViewBase
{
    [SerializeField] protected TView view; // 컨트롤러가 제어할 View.

    protected override void Awake()
    {
        if (view == null)
            view = GetComponentInChildren<TView>(true);

        base.Awake();
    }

    protected override void OnEnable()
    {
        if (view == null)
            return;

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (view == null)
            return;

        base.OnDisable();
    }
}
