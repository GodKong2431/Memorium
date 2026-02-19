using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry
{
    private static readonly List<Enemy> enemies = new();
    public static IReadOnlyList<Enemy> Enemies => enemies;

    public static bool isEnemyExist = false;

    // 적 등록
    public static void Register(Enemy enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
        {
            enemies.Add(enemy);
            CheckEnemy();
        }
    }

    // 적 등록 해제
    public static void UnRegister(Enemy enemy)
    {
        if (enemy != null)
        {
            enemies.Remove(enemy);
            CheckEnemy();
        }
    }

    private static void ClearNull()
    {
        enemies.RemoveAll(enemy => enemy == null);
    }

    private static void CheckEnemy()
    {
     
        ClearNull();
        isEnemyExist = enemies.Count > 0;
    }
}
