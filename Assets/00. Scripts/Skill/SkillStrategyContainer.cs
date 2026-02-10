using System.Collections.Generic;
using UnityEngine;

public class SkillStrategyContainer : MonoBehaviour
{
    private Dictionary<ShapeType, IDetectShapeStrategy> detectShapeStrategies;
    private Dictionary<MoveType, ISkillMovementStrategy> moveStrategies;

    private void Awake()
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

    public IDetectShapeStrategy GetStrategy(ShapeType type)
    {
        if (detectShapeStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return null;
    }
    public ISkillMovementStrategy GetMovement(MoveType type)
    {
        if (moveStrategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return moveStrategies[MoveType.Fix];
    }
}
