using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float radius = 2.4f;

        private IInteractable nearby;

        private void Update()
        {
            nearby = FindNearest();
            if (nearby != null && WasInteractPressed())
            {
                nearby.Interact();
            }
        }

        private IInteractable FindNearest()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (Collider hit in hits)
            {
                IInteractable candidate = hit.GetComponentInParent<IInteractable>();
                if (candidate == null)
                {
                    continue;
                }

                float distance = (hit.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool WasInteractPressed()
        {
            bool keyboard = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
            return keyboard || gamepad;
        }

        private void OnGUI()
        {
            if (nearby == null)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height - 70f, 360f, 44f), $"E / X: {nearby.Prompt}", style);
        }
    }
}
