using UnityEngine;
using Unity.Profiling;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Third-person CharacterController motor. Feel-first; abilities deferred.
    /// Phase 2 fills in polish; architecture is intentionally small.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SplatMotor : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("SplatMotor.Update");
        public const float PlatformContactSkinWidth = 0.03f;
        public const int DefaultWalkableGroundMask = 1 << 0;

        [SerializeField] private SplatMotorSettings settings;
        [SerializeField] private SplatInputReader input;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float craftedLaunchProofDuration = 2.5f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpCutAvailable;
        private float _ungroundedLockout;
        private bool _skipGravityThisFrame;
        private float _moveBoostMultiplier = 1f;
        private float _moveBoostUntil;
        private float _floatUntil;
        private bool _groundPounding;
        private float _craftedLaunchProvenUntil;
        private readonly RaycastHit[] _groundSupportHits = new RaycastHit[8];
        private const float MinimumGroundNormalY = 0.65f;

        public bool IsGrounded { get; private set; }
        public int GroundSupportMask => settings != null && settings.GroundMask.value != 0
            ? settings.GroundMask.value
            : DefaultWalkableGroundMask;
        public float LastLandingImpactSpeed { get; private set; }
        public bool IsGroundPounding => _groundPounding;
        public bool WalkableSupportUnderCapsuleForDiagnostics => HasWalkableSupportBelow();
        public int WalkableSupportProbeIndexForDiagnostics { get; private set; } = -1;
        public Vector3 PlanarVelocity => new Vector3(_velocity.x, 0f, _velocity.z);
        public Vector3 LastRequestedMoveForDiagnostics { get; private set; }
        public Vector3 LastAppliedMoveForDiagnostics { get; private set; }
        public float VerticalVelocity => _velocity.y;
        public float EstimatedJumpFlightSeconds => settings != null
            ? 2f * Mathf.Sqrt(2f * Mathf.Max(0f, settings.JumpHeight)
                / Mathf.Max(0.1f, Mathf.Abs(settings.Gravity)))
            : 0.8f;
        public bool WasRecentlyLaunchedByCraftedInvention => Time.time < _craftedLaunchProvenUntil;

        public event System.Action Landed;
        public event System.Action Jumped;

        /// <summary>Zero locomotion state after checkpoint warp so fall momentum cannot carry over.</summary>
        public void HardResetState()
        {
            _velocity = Vector3.zero;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _jumpCutAvailable = false;
            _ungroundedLockout = 0f;
            _skipGravityThisFrame = false;
            _moveBoostMultiplier = 1f;
            _moveBoostUntil = 0f;
            _floatUntil = 0f;
            _groundPounding = false;
            _craftedLaunchProvenUntil = 0f;
        }

        /// <summary>Used by inventions like Boing-O-Matic. Replaces vertical velocity when upward.</summary>
        public void ApplyLaunch(Vector3 worldVelocity)
        {
            // Only ApplyCraftedLaunch may grant invention-hit proof. Generic
            // launches such as boss knockback, rescue lifts, and enemy hits
            // must invalidate any proof left by an earlier spring.
            _craftedLaunchProvenUntil = 0f;
            _velocity.x = worldVelocity.x;
            _velocity.z = worldVelocity.z;
            if (worldVelocity.y > _velocity.y)
            {
                _velocity.y = worldVelocity.y;
            }

            _coyoteTimer = 0f;
            _jumpCutAvailable = false;
            _skipGravityThisFrame = true;
            BeginAirborne();
            if (worldVelocity.y > 0.1f)
            {
                SeparateFromGround(0.06f);
            }

            Jumped?.Invoke();
        }

        /// <summary>Marks a launch from a crafted traversal tool for invention-gated encounters.</summary>
        public void ApplyCraftedLaunch(Vector3 worldVelocity)
        {
            ApplyLaunch(worldVelocity);
            _craftedLaunchProvenUntil = Time.time + Mathf.Max(0.1f, craftedLaunchProofDuration);
        }

        /// <summary>Temporary planar speed multiplier for invention teach pads.</summary>
        public void ApplyTemporaryMoveBoost(float multiplier, float durationSeconds)
        {
            _moveBoostMultiplier = Mathf.Max(1f, multiplier);
            _moveBoostUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
        }

        /// <summary>Brief low-gravity float used by bubble / seed teach beats.</summary>
        public void ApplyBriefFloat(float durationSeconds, float liftSpeed = 3.5f)
        {
            _floatUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
            ApplyLaunch(Vector3.up * liftSpeed);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (GetComponent<PlayerLandingShadow>() == null)
            {
                gameObject.AddComponent<PlayerLandingShadow>();
            }

            // The shared prefab's 8 cm controller skin leaves a visible gap over
            // the shallow traversal-pad colliders. Keep the collision margin small
            // enough for clean ledge contact while preserving Unity's anti-sticking
            // margin for this 48 cm radius capsule.
            if (_controller != null && _controller.skinWidth > PlatformContactSkinWidth)
            {
                _controller.skinWidth = PlatformContactSkinWidth;
            }
            TryBindCamera();
        }

        private void Start()
        {
            TryBindCamera();
        }

        private void TryBindCamera()
        {
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void Update()
        {
            using var markerScope = UpdateMarker.Auto();
            if (settings == null || input == null)
            {
                return;
            }

            if (cameraTransform == null)
            {
                TryBindCamera();
            }

            // Cap dt so hitchy/batchmode frames don't crush jump apex / accel feel.
            var dt = Mathf.Min(Time.deltaTime, 1f / 30f);

            if (_ungroundedLockout > 0f)
            {
                _ungroundedLockout -= dt;
            }

            UpdateGrounding();
            UpdateTimers(dt);
            TryGroundPound();
            ApplyMove(dt);
            TryJump(dt);
            ApplyGravity(dt);
            LastRequestedMoveForDiagnostics = _velocity * dt;
            var beforeMove = transform.position;
            _controller.Move(LastRequestedMoveForDiagnostics);
            LastAppliedMoveForDiagnostics = transform.position - beforeMove;
            // Reconcile contact after the controller's actual displacement so
            // landing/grounded consumers don't remain one frame behind a new
            // support surface. The existing takeoff lockout still guards the
            // immediate post-jump controller contact reported by Unity.
            UpdateGrounding();
        }

        private void UpdateGrounding()
        {
            var wasGrounded = IsGrounded;
            // isGrounded can stay true while the capsule scrapes the side of a
            // narrow platform. Confirm there is a walkable face beneath the
            // capsule center before pinning the run stance or suppressing gravity.
            // The jump lockout still prevents stale floor contact at takeoff.
            var controllerGrounded = _controller.isGrounded && _ungroundedLockout <= 0f
                && HasWalkableSupportBelow();
            IsGrounded = controllerGrounded;

            if (IsGrounded)
            {
                _craftedLaunchProvenUntil = 0f;
                _coyoteTimer = settings.CoyoteTime;
                if (!wasGrounded)
                {
                    // Preserve downward speed before the ground-stick force
                    // replaces it; the landing animation uses the real impact.
                    LastLandingImpactSpeed = Mathf.Max(0f, -_velocity.y);
                }
                if (_velocity.y < 0f)
                {
                    _velocity.y = settings.GroundedStickForce;
                }

                if (!wasGrounded)
                {
                    if (_groundPounding)
                    {
                        // The impact absorbs some speed but keeps the run-up alive for a follow-up.
                        _velocity.x *= settings.GroundPoundLandingCarry;
                        _velocity.z *= settings.GroundPoundLandingCarry;
                    }

                    Landed?.Invoke();
                    _groundPounding = false;
                }
            }
        }

        private bool HasWalkableSupportBelow()
        {
            WalkableSupportProbeIndexForDiagnostics = -1;
            if (_controller == null || !_controller.enabled)
            {
                return false;
            }

            var bounds = _controller.bounds;
            var origin = new Vector3(bounds.center.x,
                bounds.min.y + _controller.skinWidth + 0.01f, bounds.center.z);
            // The controller can report a walkable Below contact on the uphill
            // rim of its capsule while the center support ray misses the same
            // slope by a few millimetres. Keep the check local to the capsule
            // foot plane, but give that ray enough margin to confirm the contact.
            var probeDistance = _controller.skinWidth + 0.075f;
            // A single center ray falsely drops the grounded state when a narrow
            // deck supports the capsule's sole area but not its exact center.
            // Sample a compact cross under the foot plane; IsGrounded still
            // requires CharacterController's Below contact, so a nearby floor
            // cannot ground a capsule that is simply falling beside a ledge.
            var sampleRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.32f;
            // Confirm support only against the authored ground layers. Using
            // DefaultRaycastLayers here let water, enemies, and phase walls
            // satisfy the visual-ground check even though GroundMask is the
            // player's configured contract for walkable surfaces.
            var groundMask = GroundSupportMask;
            for (var sample = 0; sample < 5; sample++)
            {
                var sampleOrigin = origin;
                switch (sample)
                {
                    case 1: sampleOrigin.x += sampleRadius; break;
                    case 2: sampleOrigin.x -= sampleRadius; break;
                    case 3: sampleOrigin.z += sampleRadius; break;
                    case 4: sampleOrigin.z -= sampleRadius; break;
                }

                var hitCount = Physics.RaycastNonAlloc(sampleOrigin, Vector3.down,
                    _groundSupportHits, probeDistance, groundMask,
                    QueryTriggerInteraction.Ignore);
                for (var i = 0; i < hitCount; i++)
                {
                    var hit = _groundSupportHits[i];
                    if (hit.collider == null || hit.normal.y < MinimumGroundNormalY
                        || hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        continue;
                    }

                    WalkableSupportProbeIndexForDiagnostics = sample;
                    return true;
                }
            }

            return false;
        }

        private void TryGroundPound()
        {
            // Stick down + jump while airborne = Odyssey-style ground pound dive.
            if (IsGrounded || _groundPounding || input == null)
            {
                return;
            }

            if (!input.JumpPressedThisFrame || input.Move.y > -0.45f)
            {
                return;
            }

            _groundPounding = true;
            _jumpCutAvailable = false;
            _jumpBufferTimer = 0f;
            _velocity = new Vector3(_velocity.x * settings.GroundPoundPlanarCarry,
                -28f, _velocity.z * settings.GroundPoundPlanarCarry);
            BeginAirborne();
        }

        private void UpdateTimers(float dt)
        {
            if (!IsGrounded)
            {
                _coyoteTimer -= dt;
            }

            if (input.JumpPressedThisFrame)
            {
                _jumpBufferTimer = settings.JumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= dt;
            }
        }

        private void ApplyMove(float dt)
        {
            // The pound commits to its descent, but carries the pre-pound run-up
            // rather than braking horizontal velocity to zero in mid-air.
            if (_groundPounding)
            {
                return;
            }

            var moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 desired;

            if (cameraTransform != null)
            {
                var forward = cameraTransform.forward;
                var right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                desired = (forward * moveInput.y + right * moveInput.x) * settings.MaxSpeed;
            }
            else
            {
                desired = new Vector3(moveInput.x, 0f, moveInput.y) * settings.MaxSpeed;
            }

            if (Time.time < _moveBoostUntil)
            {
                desired *= _moveBoostMultiplier;
            }
            else
            {
                _moveBoostMultiplier = 1f;
            }

            var currentPlanar = PlanarVelocity;
            var accel = desired.sqrMagnitude > currentPlanar.sqrMagnitude
                ? settings.Acceleration
                : settings.Deceleration;

            if (!IsGrounded)
            {
                accel *= settings.AirControlPercent;
            }

            var planar = Vector3.MoveTowards(currentPlanar, desired, accel * dt);
            _velocity.x = planar.x;
            _velocity.z = planar.z;

            if (planar.sqrMagnitude > 0.01f)
            {
                var look = Quaternion.LookRotation(planar.normalized, Vector3.up);
                // Rotate once at the authored turn rate. A second smoothing layer
                // made direction changes lag behind the already-accelerated motor.
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    look,
                    settings.TurnSpeedDegrees * dt);
            }
        }

        private void TryJump(float dt)
        {
            if (_groundPounding)
            {
                return;
            }

            var canJump = _jumpBufferTimer > 0f && _coyoteTimer > 0f;
            if (!canJump)
            {
                if (!input.JumpHeld && _jumpCutAvailable && _velocity.y > 0f)
                {
                    _velocity.y += settings.Gravity * (settings.JumpCutGravityMultiplier - 1f) * dt;
                }

                return;
            }

            var jumpVelocity = Mathf.Sqrt(Mathf.Abs(2f * settings.Gravity * settings.JumpHeight));
            _velocity.y = jumpVelocity;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _jumpCutAvailable = true;
            _skipGravityThisFrame = true;
            BeginAirborne();
            // CharacterController cancels upward Move while still intersecting ground — peel off the floor.
            SeparateFromGround(0.06f);
            Jumped?.Invoke();
        }

        private void ApplyGravity(float dt)
        {
            if (_skipGravityThisFrame)
            {
                _skipGravityThisFrame = false;
                return;
            }

            if (Time.time < _floatUntil && !_groundPounding)
            {
                // Soft hover: cancel fall, keep a gentle lift.
                if (_velocity.y < 1.2f)
                {
                    _velocity.y = 1.2f;
                }

                return;
            }

            if (IsGrounded && _velocity.y <= 0f)
            {
                _jumpCutAvailable = false;
                _groundPounding = false;
                return;
            }

            if (_groundPounding)
            {
                _velocity.y = Mathf.Min(_velocity.y, -28f);
                return;
            }

            var gravity = settings.Gravity;
            _velocity.y += gravity * dt;
            // Soft terminal fall — avoids brick-landing from tall drops.
            const float maxFall = -28f;
            if (_velocity.y < maxFall)
            {
                _velocity.y = maxFall;
            }
        }

        private void BeginAirborne()
        {
            IsGrounded = false;
            _ungroundedLockout = 0.14f;
        }

        private void SeparateFromGround(float distance)
        {
            if (_controller == null || distance <= 0f)
            {
                return;
            }

            var pos = transform.position;
            _controller.enabled = false;
            transform.position = pos + Vector3.up * distance;
            _controller.enabled = true;
        }
    }
}

