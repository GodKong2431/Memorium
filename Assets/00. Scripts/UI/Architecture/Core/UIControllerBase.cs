using UnityEngine;

public abstract class UIControllerBase<TView> : MonoBehaviour where TView : UIViewBase
{
    [SerializeField] protected TView view; // 컨트롤러가 제어할 View.

    // 기본 참조를 연결하고 초기화 로직을 실행한다.
    protected virtual void Awake()
    {
        if (view == null)
            view = GetComponentInChildren<TView>(true);

        Initialize();
    }

    // 화면이 켜질 때 이벤트를 연결하고 UI를 갱신한다.
    protected virtual void OnEnable()
    {
        if (view == null)
            return;

        Subscribe();
        RefreshView();
    }

    // 화면이 꺼질 때 이벤트 연결을 해제한다.
    protected virtual void OnDisable()
    {
        if (view == null)
            return;

        Unsubscribe();
    }

    // 파생 클래스가 추가 초기화가 필요할 때 사용한다.
    protected virtual void Initialize()
    {
    }

    // 필요한 이벤트를 연결한다.
    protected abstract void Subscribe();

    // 연결한 이벤트를 해제한다.
    protected abstract void Unsubscribe();

    // 현재 상태를 View에 다시 반영한다.
    protected abstract void RefreshView();
}
