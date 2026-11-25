using SpaceAndCrafts.Art;
using SpaceAndCrafts.Gameplay;
using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Small bridge for a Meshy Generic-rig Splat candidate. It drives motor-based
    /// locomotion and jump/land triggers without scaling the body or physics root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SplatMeshyAnimatorDriver : MonoBehaviour
    {
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int LandId = Animator.StringToHash("Land");
        private static readonly int VictoryId = Animator.StringToHash("Victory");
        private static readonly int PersonalityTriggerId = Animator.StringToHash("Personality");
        private static readonly int PersonalityIdleId = Animator.StringToHash("PersonalityIdle");
        private static readonly int PickupId = Animator.StringToHash("Pickup");
        private static readonly int DodgeId = Animator.StringToHash("Dodge");
        private static readonly int AirAttackId = Animator.StringToHash("AirAttack");
        private static readonly int RunJumpRollId = Animator.StringToHash("RunJumpRoll");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int UseInventionId = Animator.StringToHash("UseInvention");
        private static readonly int DiscoveryId = Animator.StringToHash("Discover");
        private static readonly int ListenId = Animator.StringToHash("Listen");
        private static readonly int OopsId = Animator.StringToHash("Oops");

        [SerializeField] private Animator animator;
        [SerializeField] private SplatMotor motor;
        [Tooltip("Motor speed at which the authored Walk clip reaches full weight in the Idle/Walk/Run blend tree.")]
        [SerializeField, Min(0.01f)] private float fullWalkSpeed = 1.2f;
        [SerializeField, Min(0.01f)] private float fullRunSpeed = 7.75f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.12f;
        [SerializeField, Min(0f)] private float groundedSoleInset = 0f;
        [SerializeField, Min(0.01f)] private float maxGroundCorrection = 0.12f;

        private bool _hasGroundedState;
        private bool _wasGrounded;
        private bool _preferInventionActionOnNextLaunch;
        private Transform[] _animatedTransforms = System.Array.Empty<Transform>();
        private Vector3[] _restScales = System.Array.Empty<Vector3>();
        private SkinnedMeshRenderer[] _skinnedRenderers = System.Array.Empty<SkinnedMeshRenderer>();
        private Transform _idleHead;
        private Transform _idleLeftArm;
        private Transform _idleRightArm;
        private float _idleLifeWeight;
        private string _reactionPoseKind = string.Empty;
        private float _reactionPoseStartedAt;
        private RuntimeAnimatorController _parameterController;
        private AnimatorControllerParameter[] _cachedParameters = System.Array.Empty<AnimatorControllerParameter>();

        public float IdleLifeWeightForDiagnostics => _idleLifeWeight;
        public string ReactionPoseKindForDiagnostics => _reactionPoseKind;
        public string RuntimeControllerNameForDiagnostics => animator != null
            ? animator.runtimeAnimatorController?.name ?? "missing"
            : "missing Animator";
        public string RuntimeClipNameForDiagnostics
        {
            get
            {
                if (animator == null) return "missing Animator";
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                return clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : "no current clip";
            }
        }
        public bool HasTriggerForDiagnostics(string name) =>
            HasParameter(Animator.StringToHash(name), AnimatorControllerParameterType.Trigger);
        public bool IsPlayingActionForDiagnostics(string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
                return false;
            var current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.IsName(stateName)) return true;
            return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(stateName);
        }

        public static SplatMeshyAnimatorDriver FindForPlayer(Transform playerRoot)
        {
            if (playerRoot == null) return null;
            var selectedVisual = SplatMeshyVisualMode.ResolveVisual(playerRoot);
            if (selectedVisual == null) return null;
            var candidates = selectedVisual.GetComponentsInChildren<SplatMeshyAnimatorDriver>(true);
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate != null && candidate.isActiveAndEnabled
                    && (candidate.transform == selectedVisual || candidate.transform.IsChildOf(selectedVisual)))
                    return candidate;
            }
            return null;
        }
        public bool IdleHeadBoundForDiagnostics => _idleHead != null;
        public bool IdleLeftArmBoundForDiagnostics => _idleLeftArm != null;
        public bool IdleRightArmBoundForDiagnostics => _idleRightArm != null;
        public Vector3 IdleHeadEulerForDiagnostics => _idleHead != null ? _idleHead.localRotation.eulerAngles : Vector3.zero;
        public Vector3 IdleLeftArmEulerForDiagnostics => _idleLeftArm != null ? _idleLeftArm.localRotation.eulerAngles : Vector3.zero;
        public Vector3 IdleRightArmEulerForDiagnostics => _idleRightArm != null ? _idleRightArm.localRotation.eulerAngles : Vector3.zero;

        private void Awake()
        {
            CacheAnimator();
            CacheRestScales();
            BindMotor();
        }

        private void OnEnable()
        {
            CacheAnimator();
            CacheRestScales();
            BindMotor();
            GameEvents.LevelCompleted += HandleLevelCompleted;
            GameEvents.GatewaySignalReceived += HandleGatewaySignalReceived;
            GameEvents.SecretWonderClaimed += HandleSecretWonderClaimed;
            GameEvents.BuildRefused += HandleBuildRefused;
        }

        private void LateUpdate()
        {
            // Meshy clips can contain animated scale channels. On a single-shell
            // round character, a stray parent scale key can stretch the whole
            // silhouette dramatically. Preserve this candidate's authored bind
            // scales after Animator evaluation; pose and translation channels
            // still play normally.
            for (var i = 0; i < _animatedTransforms.Length; i++)
            {
                var target = _animatedTransforms[i];
                if (target != null)
                {
                    target.localScale = _restScales[i];
                }
            }

            ApplyGroundedIdleLife();
            ApplyReactionPose();

            if (motor != null && motor.IsGrounded)
            {
                var controller = motor.GetComponent<CharacterController>();
                if (controller != null && controller.enabled)
                {
                    var contactRepair = motor.GetComponent<SplatVisualRepair>();
                    // Prefer independent, weighted foot seating for the Meshy rig.
                    // The whole rendered bounds include Splat's round body and can
                    // falsely position his feet over a slope or narrow platform.
                    if (contactRepair == null
                        || !contactRepair.AlignMeshyGroundedSolesAfterPose())
                    {
                        AlignVisualBottomToSupport(controller.bounds.min.y - groundedSoleInset);
                    }
                }
            }
        }

        private void OnDisable()
        {
            GameEvents.LevelCompleted -= HandleLevelCompleted;
            GameEvents.GatewaySignalReceived -= HandleGatewaySignalReceived;
            GameEvents.SecretWonderClaimed -= HandleSecretWonderClaimed;
            GameEvents.BuildRefused -= HandleBuildRefused;
            UnbindMotor();
        }

        private void HandleLevelCompleted()
        {
            // Hold an attentive pose while the repaired gateway answers; the
            // celebration belongs to Vesper's reply, not the boss's defeat.
            TriggerListen();
        }

        private void HandleGatewaySignalReceived() => TriggerVictory();

        private void HandleSecretWonderClaimed(string _) => TriggerDiscovery();

        private void HandleBuildRefused() => TriggerOops();

        private void Update()
        {
            CacheAnimator();
            if (_animatedTransforms.Length == 0 && animator != null)
            {
                CacheRestScales();
            }
            if (motor == null)
            {
                BindMotor();
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            var speed = motor != null ? motor.PlanarVelocity.magnitude : 0f;
            // The controller's authored blend thresholds are Idle=0, Walk=0.45,
            // Run=2. Map the measured walking pace to the Walk threshold directly;
            // a single linear 0..2 map left ordinary 1.2 m/s motion mostly blended
            // with Idle and delayed full Walk until about 1.74 m/s. Keep Run at 2
            // when the motor reaches its configured fullRunSpeed.
            var walkSpeed = Mathf.Max(0.01f, fullWalkSpeed);
            var runSpeed = Mathf.Max(walkSpeed + 0.01f, fullRunSpeed);
            const float walkBlendThreshold = 0.45f;
            const float runBlendThreshold = 2f;
            var normalizedBlend = speed <= walkSpeed
                ? Mathf.Lerp(0f, walkBlendThreshold, Mathf.Clamp01(speed / walkSpeed))
                : Mathf.Lerp(walkBlendThreshold, runBlendThreshold,
                    Mathf.Clamp01((speed - walkSpeed) / (runSpeed - walkSpeed)));
            if (HasParameter(MoveSpeedId, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(MoveSpeedId, normalizedBlend, speedDampTime, Time.deltaTime);
            }

            var grounded = motor == null || motor.IsGrounded;
            if (!_hasGroundedState)
            {
                _hasGroundedState = true;
                _wasGrounded = grounded;
            }
            else if (_wasGrounded && !grounded)
            {
                if (_preferInventionActionOnNextLaunch)
                {
                    // The invention launch already chose a specific character
                    // action. Do not replace it with the generic jump on the
                    // same grounded-to-airborne edge.
                    _preferInventionActionOnNextLaunch = false;
                }
                else
                {
                    TriggerJump();
                }
            }
            else if (grounded)
            {
                _preferInventionActionOnNextLaunch = false;
            }

            _wasGrounded = grounded;
        }

        private void CacheAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
                _skinnedRenderers = animator != null
                    ? animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    : System.Array.Empty<SkinnedMeshRenderer>();
            }
        }

        /// <summary>
        /// Re-seats the skinned silhouette after Animator evaluation without
        /// scaling the body or moving the gameplay collider. Used by the editor
        /// preview to sample how grounded poses fit a flat support plane.
        /// </summary>
        public float AlignVisualBottomToSupport(float supportY)
        {
            if ((_skinnedRenderers == null || _skinnedRenderers.Length == 0) && animator != null)
            {
                _skinnedRenderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            }

            if (_skinnedRenderers == null || _skinnedRenderers.Length == 0)
            {
                return 0f;
            }

            var lowestSole = float.PositiveInfinity;
            for (var i = 0; i < _skinnedRenderers.Length; i++)
            {
                var renderer = _skinnedRenderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                lowestSole = Mathf.Min(lowestSole, renderer.bounds.min.y);
            }

            if (float.IsInfinity(lowestSole))
            {
                return 0f;
            }

            var correction = Mathf.Clamp(supportY - lowestSole,
                -maxGroundCorrection, maxGroundCorrection);
            transform.position += Vector3.up * correction;
            return correction;
        }

        private void CacheRestScales()
        {
            if (animator == null)
            {
                _animatedTransforms = System.Array.Empty<Transform>();
                _restScales = System.Array.Empty<Vector3>();
                _skinnedRenderers = System.Array.Empty<SkinnedMeshRenderer>();
                _idleHead = null;
                _idleLeftArm = null;
                _idleRightArm = null;
                return;
            }

            _animatedTransforms = animator.GetComponentsInChildren<Transform>(true);
            _skinnedRenderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _idleHead = FindRigTransform("Head");
            _idleLeftArm = FindRigTransform("LeftArm");
            _idleRightArm = FindRigTransform("RightArm");
            _restScales = new Vector3[_animatedTransforms.Length];
            for (var i = 0; i < _animatedTransforms.Length; i++)
            {
                _restScales[i] = _animatedTransforms[i].localScale;
            }
        }

        private Transform FindRigTransform(string boneName)
        {
            for (var i = 0; i < _animatedTransforms.Length; i++)
            {
                if (_animatedTransforms[i] != null && _animatedTransforms[i].name == boneName)
                {
                    return _animatedTransforms[i];
                }
            }

            return null;
        }

        private void ApplyGroundedIdleLife()
        {
            var idle = false;
            if (animator != null && animator.runtimeAnimatorController != null && motor != null && motor.IsGrounded
                && motor.PlanarVelocity.sqrMagnitude <= 0.0144f && !animator.IsInTransition(0))
            {
                idle = animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion");
            }

            // Blend the small secondary motion in/out with the base state so
            // takeoff, landing, walking, and the Friendly Hello keep ownership
            // of the same rig bones without a pose pop.
            _idleLifeWeight = Mathf.MoveTowards(_idleLifeWeight, idle ? 1f : 0f,
                Time.deltaTime * 3f);
            if (_idleLifeWeight <= 0f)
            {
                return;
            }

            // Readable curious head tilt and shoulder counter-sway, layered on
            // the genuine Meshy idle pose. Keep the secondary motion in the
            // upper rig: the round shell, root, hips and feet retain their
            // authored scales and grounded pose.
            var phase = Time.time * (Mathf.PI * 2f / 3.4f);
            var yaw = (Mathf.Sin(phase) * 12f + Mathf.Sin(phase * 0.5f + 0.4f) * 2f) * _idleLifeWeight;
            var pitch = Mathf.Sin(phase * 0.5f + 1.1f) * 4.5f * _idleLifeWeight;
            var roll = Mathf.Sin(phase * 0.5f + 0.25f) * 3f * _idleLifeWeight;
            var armSway = Mathf.Sin(phase + 0.75f) * 10f * _idleLifeWeight;

            if (_idleHead != null)
            {
                _idleHead.localRotation *= Quaternion.Euler(pitch, yaw, roll);
            }

            if (_idleLeftArm != null)
            {
                _idleLeftArm.localRotation *= Quaternion.Euler(0f, 0f, armSway);
            }

            if (_idleRightArm != null)
            {
                _idleRightArm.localRotation *= Quaternion.Euler(0f, 0f, -armSway);
            }
        }

        private void BindMotor()
        {
            var found = GetComponentInParent<SplatMotor>();
            if (found == motor)
            {
                return;
            }

            UnbindMotor();
            motor = found;
            if (motor == null)
            {
                return;
            }

            motor.Jumped += HandleJumped;
            motor.Landed += HandleLanded;
        }

        private void UnbindMotor()
        {
            if (motor == null)
            {
                return;
            }

            motor.Jumped -= HandleJumped;
            motor.Landed -= HandleLanded;
            motor = null;
        }

        private void HandleJumped()
        {
            if (_preferInventionActionOnNextLaunch)
            {
                // A Boing-O-Matic launch already selected its invention action.
                // Consume the synchronous motor event here so it cannot also set
                // the generic Jump trigger and win the controller's transition race.
                _preferInventionActionOnNextLaunch = false;
                _hasGroundedState = true;
                _wasGrounded = false;
                return;
            }

            TriggerJump();
        }

        private void HandleLanded()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                ResetTriggerIfPresent(JumpId);
                SetTriggerIfPresent(LandId);
            }

            _hasGroundedState = true;
            _wasGrounded = true;
        }

        private void TriggerJump()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                ResetTriggerIfPresent(LandId);
                SetTriggerIfPresent(JumpId);
            }

            _hasGroundedState = true;
            _wasGrounded = false;
        }

        public void TriggerVictory()
        {
            _reactionPoseKind = string.Empty;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                SetTriggerIfPresent(VictoryId);
            }
        }

        public void TriggerDiscovery() => TriggerReaction(DiscoveryId, "Discover");

        public void TriggerListen() => TriggerReaction(ListenId, "Listen");

        public void TriggerOops() => TriggerReaction(OopsId, "Oops");

        private void TriggerReaction(int triggerId, string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                CacheAnimator();
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            _reactionPoseKind = stateName;
            _reactionPoseStartedAt = Time.time;

            // A reaction should respond even if a jump/landing transition is
            // already in progress. Trigger-only Any State transitions can be
            // deferred by that blend, so crossfade into the authored same-rig
            // reaction directly while retaining the controller's exit behavior.
            ResetTriggerIfPresent(triggerId);
            var fullStateHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (animator.HasState(0, fullStateHash))
            {
                animator.CrossFadeInFixedTime(fullStateHash, 0.08f, 0, 0f);
            }
            else
            {
                SetTriggerIfPresent(triggerId);
            }
        }

        private void ApplyReactionPose()
        {
            if (string.IsNullOrEmpty(_reactionPoseKind)) return;

            var elapsed = Mathf.Max(0f, Time.time - _reactionPoseStartedAt);
            var heldPose = _reactionPoseKind == "Listen";
            const float oneShotDuration = 0.95f;
            if (!heldPose && elapsed >= oneShotDuration)
            {
                _reactionPoseKind = string.Empty;
                return;
            }

            var weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.10f));
            if (!heldPose)
            {
                weight *= 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((elapsed - (oneShotDuration - 0.18f)) / 0.18f));
            }

            var head = Vector3.zero;
            var leftArm = 0f;
            var rightArm = 0f;
            switch (_reactionPoseKind)
            {
                case "Oops":
                    // Sheepish inward flinch with a brief downward head dip.
                    head = new Vector3(10f, 0f, -11f);
                    leftArm = 24f;
                    rightArm = -24f;
                    break;
                case "Discover":
                    // Curious head tilt and open arms, visibly distinct from refusal.
                    head = new Vector3(-3f, 0f, 14f);
                    leftArm = 38f;
                    rightArm = -38f;
                    break;
                case "Listen":
                    // Hold one hand near the ear while Splat tilts toward the signal.
                    head = new Vector3(0f, 0f, 17f);
                    leftArm = 5f;
                    rightArm = -54f;
                    break;
            }

            if (_idleHead != null && head.sqrMagnitude > 0f)
                _idleHead.localRotation *= Quaternion.Euler(head * weight);
            if (_idleLeftArm != null && Mathf.Abs(leftArm) > 0.01f)
                _idleLeftArm.localRotation *= Quaternion.Euler(0f, 0f, leftArm * weight);
            if (_idleRightArm != null && Mathf.Abs(rightArm) > 0.01f)
                _idleRightArm.localRotation *= Quaternion.Euler(0f, 0f, rightArm * weight);
        }

        public void TriggerInteraction()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                CacheAnimator();
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                // The existing reviewed one-arm hello doubles as a friendly
                // acknowledgement when Splat interacts with a character or prop.
                if (HasParameter(PersonalityIdleId, AnimatorControllerParameterType.Int))
                {
                    animator.SetInteger(PersonalityIdleId, 1);
                }

                ResetTriggerIfPresent(PersonalityTriggerId);
                SetTriggerIfPresent(PersonalityTriggerId);
            }
        }

        /// <summary>Play the same-rig authored pickup gesture when Splat collects an item.</summary>
        public void TriggerPickup()
        {
            TriggerOneShot(PickupId);
        }

        /// <summary>Play the controller's same-rig invention-use action for a crafted launch.</summary>
        public void TriggerUseInvention()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                CacheAnimator();
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            _preferInventionActionOnNextLaunch = true;
            TriggerOneShot(UseInventionId);
        }

        /// <summary>Optional action hooks for future authored traversal/combat inputs.</summary>
        public void TriggerDodge() => TriggerOneShot(DodgeId);
        public void TriggerAirAttack() => TriggerOneShot(AirAttackId);
        public void TriggerRunJumpRoll() => TriggerOneShot(RunJumpRollId);
        public void TriggerHitReaction() => TriggerOneShot(HitId);

        private void TriggerOneShot(int triggerId)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                CacheAnimator();
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            ResetTriggerIfPresent(triggerId);
            SetTriggerIfPresent(triggerId);
        }

        public void ResetAfterRespawn()
        {
            _reactionPoseKind = string.Empty;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                CacheAnimator();
            }

            if (animator == null || animator.runtimeAnimatorController == null) return;
            ResetTriggerIfPresent(JumpId);
            ResetTriggerIfPresent(LandId);
            ResetTriggerIfPresent(PersonalityTriggerId);
            ResetTriggerIfPresent(VictoryId);
            ResetTriggerIfPresent(DiscoveryId);
            ResetTriggerIfPresent(ListenId);
            ResetTriggerIfPresent(OopsId);
            if (HasParameter(MoveSpeedId, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(MoveSpeedId, 0f);
            }

            animator.Play("Locomotion", 0, 0f);
        }

        private bool HasParameter(int id, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                _parameterController = null;
                _cachedParameters = System.Array.Empty<AnimatorControllerParameter>();
                return false;
            }

            var currentController = animator.runtimeAnimatorController;
            if (currentController != _parameterController)
            {
                _parameterController = currentController;
                _cachedParameters = animator.parameters;
            }

            for (var i = 0; i < _cachedParameters.Length; i++)
            {
                if (_cachedParameters[i].nameHash == id && _cachedParameters[i].type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetTriggerIfPresent(int id)
        {
            if (HasParameter(id, AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger(id);
            }
        }

        private void SetTriggerIfPresent(int id)
        {
            if (HasParameter(id, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(id);
            }
        }

    }
}
