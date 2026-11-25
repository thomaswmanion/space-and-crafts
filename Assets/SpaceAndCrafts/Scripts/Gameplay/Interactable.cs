using UnityEngine;
using UnityEngine.Events;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Generic activate surface used by pickups, build sites, and triggers.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private bool canInteract = true;
        [SerializeField] private UnityEvent<GameObject> onInteracted;

        public virtual bool CanInteract => canInteract && isActiveAndEnabled;

        public virtual void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            onInteracted?.Invoke(interactor);
            HandleInteract(interactor);
        }

        protected virtual void HandleInteract(GameObject interactor)
        {
        }

        public void SetCanInteract(bool value) => canInteract = value;
    }
}
