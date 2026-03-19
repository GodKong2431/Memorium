using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateDie : IPlayerState
{

    public PlayerStateType Type => PlayerStateType.Die;
    public void OnEnter(PlayerStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            ctx.Agent.isStopped = true;
        SetAnimatorTrigger(ctx, "Dead");
    }

    public void OnExit(PlayerStateContext ctx)
    {
    }

    public void OnUpdate(PlayerStateContext ctx)
    {

    }
    private static void SetAnimatorTrigger(PlayerStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
