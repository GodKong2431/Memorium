using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateMachine<TCtx, TState, TType>
    where TCtx : BaseStateContext
    where TState : class,IBaseState<TType, TCtx>
    where TType : Enum
{
    private TCtx _ctx;
    private Dictionary<TType, TState> _states;
    private TState _current;
    private TType _currentType;

    public TCtx Context => _ctx;
    public TType CurrentStateType => _currentType;
    public TState Current => _current;

    public StateMachine(TCtx ctx, Dictionary<TType, TState> states)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _states = states ?? throw new ArgumentNullException(nameof(states));
    }

    public void ChangeState(TType next)
    {
        if (_current != null && _currentType.Equals(next))
        {
            return;
        }

        if (_current != null)
            _current.OnExit(_ctx);

        _currentType = next;
        _current = _states.TryGetValue(next, out var state) ? state : null;

        if (_current != null)
            _current.OnEnter(_ctx);
    }
}
