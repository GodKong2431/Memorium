using UnityEngine;

public interface IDamageable
{ 
   void TakeDamage(float damage,DamageType damageType = DamageType.Physical);
   Transform transform { get; }
   bool IsAlive { get; }
   bool isMoving { get; }
}

public interface IKnockbackable
{
    void ApplyKnockback(Vector3 direction, float distance, float duration);
}
