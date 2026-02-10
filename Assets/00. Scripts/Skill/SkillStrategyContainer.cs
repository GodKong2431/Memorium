using System.Collections.Generic;
using UnityEngine;

public static class SkillStrategyContainer 
{
    private static readonly Dictionary<ShapeType, IDetectShapeStrategy> detectShapeStrategies;
    private static readonly Dictionary<MoveType, ISkillMovementStrategy> moveStrategies;

    static SkillStrategyContainer()
    {
        detectShapeStrategies = new Dictionary<ShapeType, IDetectShapeStrategy>
        {
            { ShapeType.Cross, new CrossDetect() },
            { ShapeType.Circle, new CircleDetect() },
            { ShapeType.Line, new LineDetect() },
            { ShapeType.Sector, new SectorDetect() },
        };

        moveStrategies = new Dictionary<MoveType, ISkillMovementStrategy>
        {
            { MoveType.Fix,  new FixMove() },
            { MoveType.Dash, new DashMove() },
            { MoveType.Warp, new WarpMove() },
            { MoveType.Jump, new JumpMove() },
        };
    }

    public static IDetectShapeStrategy GetStrategy(ShapeType type)
    {
        if (detectShapeStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return detectShapeStrategies[ShapeType.Circle];
    }
    public static ISkillMovementStrategy GetMovement(MoveType type)
    {
        if (moveStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return moveStrategies[MoveType.Fix];
    }
}
