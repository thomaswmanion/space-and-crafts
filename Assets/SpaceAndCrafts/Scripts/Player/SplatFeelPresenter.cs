using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using UnityEngine;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Presentation-only squash/stretch and landing feedback hooks.
    /// Keeps feel juice out of the motor.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SplatFeelPresenter : MonoBehaviour
    {
        [SerializeField] private SplatMotor motor;
        [SerializeField] private SplatSkeletonAnimator skeletonAnimator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 landSquash = new Vector3(1.03f, 0.95f, 1.03f);
        [SerializeField] private Vector3 jumpStretch = Vector3.one;
        [SerializeField] private Vector3 skinnedLandSquash = new Vector3(1.02f, 0.97f, 1.02f);
        [SerializeField] private Vector3 skinnedJumpStretch = Vector3.one;
        [SerializeField] private Vector3 skinnedGroundPoundSquash = new Vector3(1.05f, 0.92f, 1.05f);
        [SerializeField] private float recoverSpeed = 14f;

        private static readonly Vector3 GroundPoundSquash = new(1.12f, 0.88f, 1.12f);
        private const float MinimumJumpUniformScale = 0.98f;
        private const float MaximumJumpUniformScale = 1.02f;

        private Vector3 _defaultScale;
        private Vector3 _targetScale;
        private Transform _cachedVisualRoot;
        private Transform _skinnedSquashBone;
        private Vector3 _skinnedSquashBoneRestScale = Vector3.one;
        private Vector3 _skinnedSquashScale = Vector3.one;
        private Vector3 _skinnedSquashTarget = Vector3.one;
        private float _footTimer;
        private float _skinnedSquashRecoverAt;
        private SplatSkeletonAnimator _boundAnimator;
        private SplatMotor _boundMotor;

        public Vector3 SquashBoneLocalScaleForDiagnostics =>
            _skinnedSquashBone != null ? _skinnedSquashBone.localScale : Vector3.one;
        public int LandedEventCountForDiagnostics { get; private set; }
        public GameObject LastLandingDustForDiagnostics { get; private set; }

        private void Awake()
        {
            ResolveVisualRoot();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                {
                    return all[i];
                }
            }

            return null;
        }

        private void OnEnable()
        {
            RefreshEventBindings();
        }

        private void OnDisable()
        {
            if (_boundAnimator != null)
            {
                _boundAnimator.FootPlanted -= HandleFootPlanted;
                _boundAnimator = null;
            }

            if (_boundMotor != null)
            {
                _boundMotor.Landed -= HandleLanded;
                _boundMotor.Jumped -= HandleJumped;
                _boundMotor = null;
            }
        }

        private void Update()
        {
            // Editor bootstraps and runtime character assembly can add this
            // presenter before the rig or motor. Reconcile subscriptions after
            // those components appear so footsteps stay on actual stance events.
            RefreshEventBindings();
            var blend = 1f - Mathf.Exp(-Mathf.Max(0f, recoverSpeed) * Time.deltaTime);
            ResolveVisualRoot();
            if (visualRoot == null)
            {
                return;
            }

            if (Time.time >= _skinnedSquashRecoverAt)
            {
                _skinnedSquashTarget = Vector3.one;
            }

            _targetScale = Vector3.Lerp(_targetScale, _defaultScale, blend);
            var shape = Vector3.one;
            if (motor != null && motor.IsGrounded)
            {
                var speed = motor.PlanarVelocity.magnitude;
                if (speed >= 1.4f)
                {
                    var runPulse = Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(8f, 14f, Mathf.InverseLerp(1.5f, 7.5f, speed)))) * 0.0045f;
                    shape = new Vector3(1f - runPulse * 0.35f, 1f + runPulse, 1f - runPulse * 0.35f);
                }
                else
                {
                    var idlePulse = Mathf.Sin(Time.time * 2.2f) * 0.0025f;
                    shape = new Vector3(1f - idlePulse * 0.35f, 1f + idlePulse, 1f - idlePulse * 0.35f);
                }
            }

            var shapedTarget = Vector3.Scale(_targetScale, shape);
            if (_skinnedSquashBone != null)
            {
                // Keep the model root and feet stable. Apply only the discrete impact squash
                // to this rig; an additional idle/run scale pulse made the body feel gelatinous.
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, _defaultScale, blend);
                _skinnedSquashScale = Vector3.Lerp(_skinnedSquashScale, _skinnedSquashTarget, blend);
                _skinnedSquashBone.localScale = Vector3.Scale(
                    _skinnedSquashBoneRestScale,
                    _skinnedSquashScale);
            }
            else
            {
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, shapedTarget, blend);
            }
            TickFootsteps();
        }

        private void LateUpdate()
        {
            // Meshy animation restores every imported bind scale in its own
            // LateUpdate to prevent animated scale channels stretching Splat.
            // Reapply only the small, authored landing impact after that safety
            // pass; otherwise the Animator erases the squash before rendering.
            // The physics root and jump scale remain untouched.
            if (!SplatMeshyVisualMode.IsEnabledOn(transform) || _skinnedSquashBone == null)
                return;

            _skinnedSquashBone.localScale = Vector3.Scale(
                _skinnedSquashBoneRestScale, _skinnedSquashScale);
        }

        private void RefreshEventBindings()
        {
            if (motor == null) motor = GetComponent<SplatMotor>();
            skeletonAnimator = SplatMeshyVisualMode.IsEnabledOn(transform)
                ? null
                : (skeletonAnimator != null ? skeletonAnimator : GetComponent<SplatSkeletonAnimator>());

            if (_boundMotor != motor)
            {
                if (_boundMotor != null)
                {
                    _boundMotor.Landed -= HandleLanded;
                    _boundMotor.Jumped -= HandleJumped;
                }

                _boundMotor = motor;
                if (_boundMotor != null)
                {
                    _boundMotor.Landed += HandleLanded;
                    _boundMotor.Jumped += HandleJumped;
                }
            }

            if (_boundAnimator != skeletonAnimator)
            {
                if (_boundAnimator != null)
                {
                    _boundAnimator.FootPlanted -= HandleFootPlanted;
                }

                _boundAnimator = skeletonAnimator;
                if (_boundAnimator != null)
                {
                    _boundAnimator.FootPlanted += HandleFootPlanted;
                }
            }
        }

        private void ResolveVisualRoot()
        {
            if (SplatMeshyVisualMode.IsEnabledOn(transform))
            {
                var meshy = transform.Find(SplatMeshyVisualMode.MeshyVisualRootName);
                if (meshy != null && visualRoot != meshy)
                {
                    visualRoot = meshy;
                }
            }

            if (visualRoot == null || visualRoot == transform)
            {
                var organic = transform.Find(OrganicSplatBuilder.VisualRootName);
                var meshy = SplatMeshyVisualMode.IsEnabledOn(transform)
                    ? transform.Find(SplatMeshyVisualMode.MeshyVisualRootName)
                    : null;
                visualRoot = meshy != null ? meshy : organic != null ? organic : transform;
            }

            if (visualRoot == null || visualRoot == _cachedVisualRoot)
            {
                return;
            }

            // SplatVisualRepair can replace the serialized visual after Awake.
            // Adopt the new root so squash feedback reaches the current visual.
            _cachedVisualRoot = visualRoot;
            _defaultScale = visualRoot.localScale;
            _targetScale = _defaultScale;
            _skinnedSquashBone = FindDeep(visualRoot, OrganicSplatBuilder.SpineName);
            if (_skinnedSquashBone == null && SplatMeshyVisualMode.IsEnabledOn(transform))
            {
                // Meshy humanoid exports use standard bone names, unlike the
                // ART_Organic rig. Put the landing impulse on the torso joint
                // instead of scaling the whole visual and moving its feet.
                _skinnedSquashBone = FindDeep(visualRoot, "Spine")
                    ?? FindDeep(visualRoot, "Spine01")
                    ?? FindDeep(visualRoot, "UpperChest");
            }
            _skinnedSquashBoneRestScale = _skinnedSquashBone != null ? _skinnedSquashBone.localScale : Vector3.one;
            _skinnedSquashScale = Vector3.one;
            _skinnedSquashTarget = Vector3.one;
            // SplatSkeletonAnimator is the sole writer for antenna-bone rotation.
            // Two Update loops were overwriting each other's pose based on script order.
        }

        private void TickFootsteps()
        {
            // When the procedural rig is present, sound follows its actual
            // stance transition. Keep the timer only for older visuals without it.
            if (skeletonAnimator != null && skeletonAnimator.enabled)
            {
                _footTimer = 0f;
                return;
            }

            if (motor == null || !motor.IsGrounded)
            {
                _footTimer = 0f;
                return;
            }

            var speed = motor.PlanarVelocity.magnitude;
            if (speed < 1.5f)
            {
                _footTimer = 0f;
                return;
            }

            _footTimer -= Time.deltaTime;
            if (_footTimer > 0f)
            {
                return;
            }

            TempSfxPlayer.PlayFootstep();
            _footTimer = Mathf.Lerp(0.38f, 0.22f, Mathf.InverseLerp(1.5f, 7.5f, speed));
        }

        private void HandleFootPlanted()
        {
            if (motor == null || !motor.IsGrounded || motor.PlanarVelocity.magnitude < 1.5f)
            {
                return;
            }

            TempSfxPlayer.PlayFootstep();
        }

        private void HandleLanded()
        {
            LandedEventCountForDiagnostics++;
            var pounding = motor != null && motor.IsGroundPounding;
            if (_skinnedSquashBone != null)
            {
                _skinnedSquashTarget = pounding ? skinnedGroundPoundSquash : skinnedLandSquash;
                _skinnedSquashRecoverAt = Time.time + (pounding ? 0.28f : 0.2f);
            }
            else
            {
                _targetScale = Vector3.Scale(_defaultScale, pounding ? GroundPoundSquash : landSquash);
            }

            TempSfxPlayer.PlayLandImpact(motor != null ? motor.LastLandingImpactSpeed : 0f, pounding);
            var controller = motor != null ? motor.GetComponent<CharacterController>() : null;
            var contactY = controller != null
                ? controller.bounds.min.y - controller.skinWidth
                : transform.position.y;
            var impactWeight = motor != null ? Mathf.InverseLerp(2f, 14f, motor.LastLandingImpactSpeed) : 0f;
            LastLandingDustForDiagnostics = TempLandDustBurst.Spawn(
                transform.position, contactY, impactWeight, pounding);
            var cam = FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(pounding ? 0.14f : 0.035f);
            }
        }

        private void HandleJumped()
        {
            // Scene overrides can outlive a tuning change. Keep jump feedback
            // deliberately subtle so no stale value can elongate the full hero.
            if (_skinnedSquashBone != null)
            {
                _skinnedSquashTarget = ClampJumpShape(skinnedJumpStretch);
                _skinnedSquashRecoverAt = Time.time;
            }
            else
            {
                _targetScale = Vector3.Scale(_defaultScale, ClampJumpShape(jumpStretch));
            }

            TempSfxPlayer.PlayJump();
        }

        private static Vector3 ClampJumpShape(Vector3 shape)
        {
            // Old scene overrides can contain extreme, axis-specific jump values.
            // Average them into a tiny uniform pulse so stale settings can never
            // make the visible character elongate vertically during a jump.
            var uniformScale = Mathf.Clamp((shape.x + shape.y + shape.z) / 3f,
                MinimumJumpUniformScale, MaximumJumpUniformScale);
            return Vector3.one * uniformScale;
        }
    }
}
