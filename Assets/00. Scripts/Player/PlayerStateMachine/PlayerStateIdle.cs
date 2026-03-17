using UnityEngine;
using UnityEngine.AI;

[System.Serializable]

public class PlayerStateIdle : IPlayerState
{
    private bool goal = false;

    public PlayerStateType Type => PlayerStateType.Idle;
    public void OnEnter(PlayerStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.isStopped = true;
        SetAnimatorTrigger(ctx, "Idle");
    }

    public void OnExit(PlayerStateContext ctx)
    {
        ctx.Animator.ResetTrigger("Idle");
    }

    public void OnUpdate(PlayerStateContext ctx)
    {
        if(EnemyRegistry.isEnemyExist == true)
        {
            ctx.RequestState(PlayerStateType.Chase);
            return;
        }

        if (ctx.isGoal == false)
        {
            ctx.RequestState(PlayerStateType.Move);
        }
    }
    private static void SetAnimatorTrigger(PlayerStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
        {
            ctx.Animator.SetTrigger(trigger);
        }
    }
}
