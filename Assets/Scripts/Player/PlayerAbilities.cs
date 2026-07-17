using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.Player
{
    public sealed class PlayerAbilities : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float invisibleDuration = 4f;
        [SerializeField, Min(0f)] private float invisibleCooldown = 10f;
        [SerializeField] private Renderer[] splatRenderers;

        private float invisibleUntil;
        private float nextInvisibleTime;
        private int phaseWallLayer = -1;
        private int playerLayer;

        public bool IsInvisible { get; private set; }
        public float InvisibleTimeRemaining => Mathf.Max(0f, invisibleUntil - Time.time);
        public float InvisibleCooldownRemaining => Mathf.Max(0f, nextInvisibleTime - Time.time);

        public void Configure(Renderer[] renderers)
        {
            splatRenderers = renderers;
        }

        private void Awake()
        {
            playerLayer = gameObject.layer;
            phaseWallLayer = LayerMask.NameToLayer("PhaseWall");
        }

        private void Update()
        {
            if (!IsInvisible && Time.time >= nextInvisibleTime && WasAbilityPressed())
            {
                BeginInvisibility();
            }

            if (IsInvisible && Time.time >= invisibleUntil)
            {
                EndInvisibility();
            }
        }

        private void OnDisable()
        {
            if (IsInvisible)
            {
                EndInvisibility();
            }
        }

        private void BeginInvisibility()
        {
            IsInvisible = true;
            invisibleUntil = Time.time + invisibleDuration;
            nextInvisibleTime = invisibleUntil + invisibleCooldown;
            SetVisible(false);
            if (phaseWallLayer >= 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, phaseWallLayer, true);
            }
        }

        private void EndInvisibility()
        {
            IsInvisible = false;
            SetVisible(true);
            if (phaseWallLayer >= 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, phaseWallLayer, false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (splatRenderers == null || splatRenderers.Length == 0)
            {
                splatRenderers = GetComponentsInChildren<Renderer>();
            }

            foreach (Renderer splatRenderer in splatRenderers)
            {
                splatRenderer.enabled = visible;
            }
        }

        private static bool WasAbilityPressed()
        {
            bool keyboard = Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
            return keyboard || gamepad;
        }
    }
}
