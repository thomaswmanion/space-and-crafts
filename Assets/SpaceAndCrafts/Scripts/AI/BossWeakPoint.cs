using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// High perch / crystal the player must Boing into. Raises hit event for the boss.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BossWeakPoint : MonoBehaviour
    {
        [SerializeField] private UnityEvent hit;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private float pulseSpeed = 8f;
        [SerializeField] private float pulseScale = 0.18f;
        [SerializeField] private float launchHintCooldown = 2f;
        [SerializeField, Min(1f)] private float exposedVisualScale = 2f;

        public bool IsExposed { get; private set; }

        private Vector3 _restScale = Vector3.one;
        private SphereCollider _sphereCollider;
        private float _restColliderRadius;
        private float _nextLaunchHintTime;

        private void Awake()
        {
            if (visualRoot != null)
            {
                _restScale = visualRoot.transform.localScale;
            }

            _sphereCollider = GetComponent<SphereCollider>();
            if (_sphereCollider != null)
            {
                _restColliderRadius = _sphereCollider.radius;
            }

            SetExposed(false);
        }

        private void Update()
        {
            if (!IsExposed || visualRoot == null)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            visualRoot.transform.localScale = _restScale * Mathf.Max(1f, exposedVisualScale) * pulse;
        }

        public void SetExposed(bool exposed)
        {
            IsExposed = exposed;
            var sizeScale = exposed ? Mathf.Max(1f, exposedVisualScale) : 1f;
            if (visualRoot != null)
            {
                visualRoot.SetActive(exposed);
                var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
                for (var i = 0; i < renderers.Length; i++)
                {
                    // The Arrival scene keeps the relay renderer disabled while
                    // dormant. Activating its GameObject alone leaves the hit
                    // target invisible during the promised punish window.
                    if (renderers[i] != null)
                        renderers[i].enabled = exposed;
                }

                visualRoot.transform.localScale = _restScale * sizeScale;
                // Let the strike zone grow with the visible crystal during its
                // brief exposure window. The dormant collider stays disabled.
            }

            if (_sphereCollider != null)
            {
                _sphereCollider.radius = _restColliderRadius * sizeScale;
            }

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = exposed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsExposed)
            {
                return;
            }

            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            // A regular jump or fall shouldn't count as the invention exam; require a
            // recent launch specifically issued by the crafted traversal tool.
            if (motor.IsGrounded)
            {
                return;
            }

            if (!motor.WasRecentlyLaunchedByCraftedInvention)
            {
                if (Time.time >= _nextLaunchHintTime)
                {
                    GameplayFeedback.Toast("Rattle's relay needs a Boing-O-Matic launch!", 1.8f);
                    _nextLaunchHintTime = Time.time + Mathf.Max(0.25f, launchHintCooldown);
                }

                return;
            }

            hit?.Invoke();
            SetExposed(false);
        }
    }
}
