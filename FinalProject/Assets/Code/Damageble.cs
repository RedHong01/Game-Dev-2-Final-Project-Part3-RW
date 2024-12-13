using UnityEngine;

public interface Damageable
{
    void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection);
    void TakeDamage(float damage);
}