using SpaceAndCrafts.Player;
using UnityEngine;

namespace SpaceAndCrafts.Collectibles
{
    public sealed class WaterGunPickup : MonoBehaviour
    {
        private bool collected;

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player"))
            {
                return;
            }

            PlayerCombat combat = other.GetComponent<PlayerCombat>();
            if (combat == null)
            {
                return;
            }

            collected = true;
            combat.GiveWaterGun();
            Destroy(gameObject);
        }
    }
}
