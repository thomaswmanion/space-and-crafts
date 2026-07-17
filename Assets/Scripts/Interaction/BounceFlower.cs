using SpaceAndCrafts.Player;
using UnityEngine;

namespace SpaceAndCrafts.Interaction
{
    public sealed class BounceFlower : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(1f)] private float launchSpeed = 13f;

        public string Prompt => "Bounce!";

        public void Interact()
        {
            PlayerController3D player = FindAnyObjectByType<PlayerController3D>();
            if (player != null)
            {
                player.LaunchUpward(launchSpeed);
            }
        }
    }
}
