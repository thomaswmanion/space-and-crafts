using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// World pickup. Prefer trigger auto-collect for materials; interact for chunky components.
    /// </summary>
    public sealed class CollectiblePickup : Interactable
    {
        [SerializeField] private CollectibleDefinition definition;
        [SerializeField] private bool collectOnTriggerEnter = true;
        [SerializeField] private bool disableAfterCollect = true;
        [SerializeField] [Range(0f, 2f)] private float proximityAssistRadius = 1f;

        private SphereCollider _proximityAssistSensor;
        private readonly Collider[] _assistOverlaps = new Collider[16];
        public int LastAssistOverlapCountForDiagnostics { get; private set; }
        public float AssistRadiusWorldForDiagnostics => _proximityAssistSensor == null
            ? 0f
            : _proximityAssistSensor.radius * Mathf.Max(
                Mathf.Abs(_proximityAssistSensor.transform.lossyScale.x),
                Mathf.Abs(_proximityAssistSensor.transform.lossyScale.y),
                Mathf.Abs(_proximityAssistSensor.transform.lossyScale.z));

        public CollectibleDefinition Definition => definition;
        public SphereCollider ProximityAssistSensor => _proximityAssistSensor;

        private void Awake()
        {
            if (!collectOnTriggerEnter || proximityAssistRadius <= 0f)
            {
                return;
            }

            // A slightly wider hidden trigger eases near-misses without changing the
            // authored item's visible or physical collider. Pickup stars complete the
            // small gap to Splat's visible maker pack.
            _proximityAssistSensor = gameObject.AddComponent<SphereCollider>();
            _proximityAssistSensor.isTrigger = true;
            _proximityAssistSensor.radius = proximityAssistRadius / MaximumAbsoluteScale(_proximityAssistSensor.transform.lossyScale);
        }

        protected override void HandleInteract(GameObject interactor)
        {
            TryCollect(interactor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!collectOnTriggerEnter)
            {
                return;
            }

            if (other.GetComponentInParent<Player.SplatMotor>() == null)
            {
                return;
            }

            TryCollect(other.gameObject);
        }

        private void Update()
        {
            // Keep the near-miss assist reliable for CharacterController players.
            // Some Unity/physics combinations do not deliver trigger-enter when
            // neither participant owns a Rigidbody, so query the same authored
            // sensor volume and resolve the player explicitly as a fallback.
            if (!collectOnTriggerEnter || !CanInteract || _proximityAssistSensor == null)
            {
                return;
            }

            var scale = _proximityAssistSensor.transform.lossyScale;
            var maxScale = MaximumAbsoluteScale(scale);
            // The serialized radius is a world-space assist distance. Compensate
            // for art/prefab scaling so a small mesh does not shrink the pickup aid.
            _proximityAssistSensor.radius = proximityAssistRadius / maxScale;
            var radius = _proximityAssistSensor.radius * maxScale;
            var center = _proximityAssistSensor.transform.TransformPoint(_proximityAssistSensor.center);
            var count = Physics.OverlapSphereNonAlloc(center, radius, _assistOverlaps,
                ~0, QueryTriggerInteraction.Ignore);
            LastAssistOverlapCountForDiagnostics = count;
            for (var i = 0; i < count; i++)
            {
                var hit = _assistOverlaps[i];
                if (hit == null) continue;
                var motor = hit.GetComponentInParent<Player.SplatMotor>();
                if (motor == null) continue;
                TryCollect(motor.gameObject);
                return;
            }
        }

        private static float MaximumAbsoluteScale(Vector3 scale) => Mathf.Max(
            0.0001f, Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        private void TryCollect(GameObject interactor)
        {
            if (!CanInteract || definition == null)
            {
                return;
            }

            TempCraftSparkBurst.Spawn(transform.position, 8, 0.4f);
            var motor = interactor != null
                ? interactor.GetComponentInParent<Player.SplatMotor>()
                : FindAnyObjectByType<Player.SplatMotor>();
            var meshyAnimator = motor != null
                ? Player.SplatMeshyAnimatorDriver.FindForPlayer(motor.transform)
                : null;
            if (meshyAnimator != null)
            {
                if (definition.Category == CollectibleCategory.Component)
                {
                    meshyAnimator.TriggerDiscovery();
                }
                else
                {
                    meshyAnimator.TriggerPickup();
                }
            }

            // Keep the pickup fly-in on the visible character even when the
            // promoted Meshy rig is active and the organic animator is disabled.
            if (meshyAnimator != null)
            {
                TempCraftSparkBurst.SpawnPickupArc(transform.position, meshyAnimator.transform);
            }
            else
            {
                motor?.GetComponent<Player.SplatSkeletonAnimator>()?.PlayPickupFlyIn(transform.position);
            }
            GameEvents.RaiseCollectibleAcquired(definition);

            if (disableAfterCollect)
            {
                SetCanInteract(false);
                gameObject.SetActive(false);
            }
        }
    }
}
