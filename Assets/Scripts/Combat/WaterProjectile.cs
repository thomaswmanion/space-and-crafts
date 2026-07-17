using UnityEngine;

namespace SpaceAndCrafts.Combat
{
    public sealed class WaterProjectile : MonoBehaviour
    {
        private GameObject owner;

        public void Initialize(GameObject projectileOwner, float lifetime)
        {
            owner = projectileOwner;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(1, transform.position, transform.forward);
            }

            Destroy(gameObject);
        }
    }
}
