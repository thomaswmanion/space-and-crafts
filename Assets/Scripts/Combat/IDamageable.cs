using UnityEngine;

namespace SpaceAndCrafts.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection);
    }
}
