using UnityEngine;

public interface IBaseState<TType,TCtx>
{
    TType Type { get; }

    void OnEnter(TCtx ctx);
    void OnUpdate(TCtx ctx);
    void OnExit(TCtx ctx);
}
