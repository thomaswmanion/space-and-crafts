using SpaceAndCrafts.Player;
using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.Cameras
{
    /// <summary>
    /// Pulls the camera in when obstructed. Keeps platforming readable.
    /// </summary>
    public sealed class CameraCollision : MonoBehaviour
    {
        private const int HitBufferCapacity = 128;
        private const int CameraOccluderLayer = 31;
        private static readonly ProfilerMarker OcclusionFadeMarker = new ProfilerMarker("SpaceAndCrafts.Camera.OcclusionFade");
        private static readonly ProfilerMarker CollisionCastMarker = new ProfilerMarker("SpaceAndCrafts.Camera.CollisionCast");

        [SerializeField] private CameraSettings settings;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraTransform;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferCapacity];
        private readonly Renderer[] _hiddenRenderers = new Renderer[HitBufferCapacity];
        private readonly bool[] _hiddenRendererStates = new bool[HitBufferCapacity];
        private int _hiddenRendererCount;
        private int _peakHitCount;
        private int _bufferSaturationCount;

        public int HiddenOccluderCountForDiagnostics => _hiddenRendererCount;
        public int HitBufferCapacityForDiagnostics => _hitBuffer.Length;
        public int PeakHitCountForDiagnostics => _peakHitCount;
        public int BufferSaturationCountForDiagnostics => _bufferSaturationCount;

        private void Awake()
        {
            // Decorative occluder meshes shape camera framing without blocking Splat.
            var playerLayer = followTarget != null ? followTarget.gameObject.layer : 6;
            Physics.IgnoreLayerCollision(playerLayer, CameraOccluderLayer, true);
        }

        private void OnDisable()
        {
            RestoreCameraOccluders();
        }

        public void UpdateOcclusionFade()
        {
            using var marker = OcclusionFadeMarker.Auto();
            RestoreCameraOccluders();
            if (settings == null || followTarget == null || cameraTransform == null) return;
            var mask = 1 << CameraOccluderLayer;
            for (var verticalSample = 0; verticalSample < 3; verticalSample++)
            {
                var targetHeight = 0.3f + verticalSample * 0.5f;
                for (var horizontalSample = -1; horizontalSample <= 1; horizontalSample++)
                {
                    var target = followTarget.position + Vector3.up * targetHeight
                        + followTarget.right * (horizontalSample * 0.3f);
                    var sight = target - cameraTransform.position;
                    if (sight.sqrMagnitude < 0.001f) continue;
                    var hitCount = Physics.RaycastNonAlloc(cameraTransform.position, sight.normalized,
                        _hitBuffer, sight.magnitude, mask, QueryTriggerInteraction.Ignore);
                    for (var i = 0; i < hitCount && _hiddenRendererCount < _hiddenRenderers.Length; i++)
                    {
                        var collider = _hitBuffer[i].collider;
                        if (collider == null || collider.gameObject.layer != CameraOccluderLayer) continue;
                        var renderer = collider.GetComponent<Renderer>();
                        if (renderer == null) renderer = collider.GetComponentInParent<Renderer>();
                        if (renderer == null || IsAlreadyHidden(renderer)) continue;
                        _hiddenRenderers[_hiddenRendererCount] = renderer;
                        _hiddenRendererStates[_hiddenRendererCount] = renderer.forceRenderingOff;
                        _hiddenRendererCount++;
                        renderer.forceRenderingOff = true;
                    }
                }
            }
        }

        private bool IsAlreadyHidden(Renderer renderer)
        {
            for (var i = 0; i < _hiddenRendererCount; i++)
            {
                if (_hiddenRenderers[i] == renderer) return true;
            }

            return false;
        }

        private void RestoreCameraOccluders()
        {
            for (var i = 0; i < _hiddenRendererCount; i++)
            {
                if (_hiddenRenderers[i] != null)
                {
                    _hiddenRenderers[i].forceRenderingOff = _hiddenRendererStates[i];
                    _hiddenRenderers[i] = null;
                }
            }

            _hiddenRendererCount = 0;
        }

        public float ResolveDistance(float desiredDistance, float horizontalOffset = 0f)
        {
            using var marker = CollisionCastMarker.Auto();
            if (settings == null || followTarget == null || cameraTransform == null)
            {
                return desiredDistance;
            }

            // Cast along the camera's actual orbit arm. Height is in pivot-local space,
            // so rotate it with the orbit rather than applying it as a world-up offset.
            var pivot = cameraTransform.parent != null ? cameraTransform.parent : cameraTransform;
            var origin = pivot.position + pivot.rotation *
                (Vector3.up * settings.Height + Vector3.right * horizontalOffset);
            var direction = -pivot.forward;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = -followTarget.forward;
            }

            var mask = settings.CollisionMask.value == 0 ? ~0 : settings.CollisionMask.value;
            var maxDistance = desiredDistance;
            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                settings.CollisionRadius,
                direction,
                _hitBuffer,
                desiredDistance + settings.CollisionRadius,
                mask,
                QueryTriggerInteraction.Ignore);
            _peakHitCount = Mathf.Max(_peakHitCount, hitCount);
            if (hitCount <= 0)
            {
                return maxDistance;
            }

            // NonAlloc hit order is unspecified. If the buffer saturates, redo this rare
            // dense-collision cast with a complete result set so the nearest obstacle wins.
            if (hitCount == _hitBuffer.Length)
            {
                _bufferSaturationCount++;
                var allHits = Physics.SphereCastAll(
                    origin,
                    settings.CollisionRadius,
                    direction,
                    desiredDistance + settings.CollisionRadius,
                    mask,
                    QueryTriggerInteraction.Ignore);
                return FindNearestValidHit(allHits, allHits.Length, maxDistance,
                    settings.CollisionBuffer, settings.MinimumCollisionDistance);
            }

            return FindNearestValidHit(_hitBuffer, hitCount, maxDistance,
                settings.CollisionBuffer, settings.MinimumCollisionDistance);
        }

        private static float FindNearestValidHit(RaycastHit[] hits, int hitCount, float maxDistance,
            float collisionBuffer, float minimumCollisionDistance)
        {
            var best = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                // Never pull in from Splat's own capsule / CharacterController.
                if (hit.collider.GetComponentInParent<SplatMotor>() != null)
                {
                    continue;
                }

                if (hit.distance < best)
                {
                    best = hit.distance;
                }
            }

            if (best < float.MaxValue)
            {
                maxDistance = Mathf.Max(minimumCollisionDistance, best - collisionBuffer);
            }

            return maxDistance;
        }
    }
}
