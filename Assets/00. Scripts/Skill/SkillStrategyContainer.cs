using System.Collections.Generic;
using UnityEngine;

public static class SkillStrategyContainer 
{
    private static readonly Dictionary<M2Type, IDetectShapeStrategy> detectShapeStrategies;
    private static readonly Dictionary<M1Type, ISkillMovementStrategy> moveStrategies;
    private static readonly Dictionary<M3Type, ISkillExecuteStrategy> executeStrategies;
    private static readonly Dictionary<M4Type, ISkillAddonStrategy> addonStrategies;
    static SkillStrategyContainer()
    {
        detectShapeStrategies = new Dictionary<M2Type, IDetectShapeStrategy>
        {
            { M2Type.cross, new CrossDetect() },
            { M2Type.circle, new CircleDetect() },
            { M2Type.line, new LineDetect() },
            { M2Type.sector, new SectorDetect() },
        };

        moveStrategies = new Dictionary<M1Type, ISkillMovementStrategy>
        {
            { M1Type.Fix,  new FixMove() },
            { M1Type.Dash, new DashMove() },
            { M1Type.Warp, new WarpMove() },
            { M1Type.Jump, new JumpMove() },
        };
        executeStrategies = new Dictionary<M3Type, ISkillExecuteStrategy>
        {
            { M3Type.direct, new ExecuteDirect() },
            { M3Type.dbjectile, new ExecuteProjectile() },
            { M3Type.deploy, new ExecuteDeploy() },
            { M3Type.burst, new ExecuteAura() },
        };
        addonStrategies = new Dictionary<M4Type, ISkillAddonStrategy>
        {
            {M4Type.pull, new AddonPull() },
            {M4Type.push, new AddonPush() },
            {M4Type.shadow, new AddonShadow() },
            {M4Type.impact, new AddonImpact() },
        };

    }

    public static IDetectShapeStrategy GetDetect(M2Type type)
    {
        if (detectShapeStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return detectShapeStrategies[M2Type.circle];
    }
    public static ISkillMovementStrategy GetMovement(M1Type type)
    {
        if (moveStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return moveStrategies[M1Type.Fix];
    }
    public static ISkillExecuteStrategy GetExecute(M3Type type)
    {
        if (executeStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return executeStrategies[M3Type.direct];
    }
    public static ISkillAddonStrategy GetAddon(M4Type type)
    {
        if (addonStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return null;
    }
}
