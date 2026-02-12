using UnityEngine;

public static class EnemyTarget
{
    public static Enemy GetTarget(Vector3 from)
    {
        var enemies = EnemyRegistry.Enemies;
        Enemy target = null;

        float shorDist = float.PositiveInfinity;

        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            float dist = (enemy.transform.position - from).sqrMagnitude;
            if (dist < shorDist)
            {
                shorDist = dist;
                target = enemy;
                target.transform.name = "currentTarget";
            }
        }

        return target;
    }
}
