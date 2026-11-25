using System.Collections;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Gameplay;
using UnityEngine;
using Unity.Profiling;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Drives Splat's named skeleton for gameplay actions:
    /// Idle / Run / Jump / Land / Interact / Craft / Listen / Celebrate.
    /// Poses the skinned character rig with compatible authored locomotion clips
    /// when available, and keeps procedural posing for expressive gameplay actions
    /// and as a fallback. Commissioned Meshy character exports contain no clips.
    /// </summary>
    public sealed class SplatSkeletonAnimator : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("SplatSkeletonAnimator.Update");
        private static readonly ProfilerMarker LateUpdateMarker = new ProfilerMarker("SplatSkeletonAnimator.LateUpdate");
        public enum Action
        {
            Idle,
            Run,
            Jump,
            Land,
            Interact,
            Discover,
            Oops,
            Craft,
            Listen,
            Celebrate,
            Hit,
        }

        public enum DiscoveryKind
        {
            Other,
            Spring,
            Dish,
            Crystal,
            Wonder,
        }

        [SerializeField] private SplatMotor motor;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float runSpeedThreshold = 0.35f;
        [SerializeField] private float poseBlendSpeed = 18f;
        [SerializeField] private bool useAuthoredLocomotionClips = true;

        private SplatInteractor _interactor;
        private SplatVisualRepair _visualRepair;
        private Transform _hips;
        private Transform _spine;
        private Transform _chest;
        private Transform _head;
        private Transform _antenna;
        private Transform _makerPouch;
        private Transform _makerPack;
        private Transform _armL;
        private Transform _armR;
        private Transform _armTipL;
        private Transform _armTipR;
        private Transform _legL;
        private Transform _legR;
        private Transform _footL;
        private Transform _footR;
        private SkinnedMeshRenderer _paintedFaceRenderer;
        private MaterialPropertyBlock _paintedFaceProperties;
        private Transform _eyeRootL;
        private Transform _eyeRootR;
        private Transform _irisL;
        private Transform _irisR;
        private Transform _smile;
        private Transform _authoredRigRoot;
        private Transform[] _authoredRigTransforms;
        private Vector3[] _authoredPosePositions;
        private Quaternion[] _authoredPoseRotations;
        private Vector3[] _authoredPoseScales;
        private Vector3[] _authoredLastPosePositions;
        private Quaternion[] _authoredLastPoseRotations;
        private Vector3[] _authoredTransitionFromPositions;
        private Quaternion[] _authoredTransitionFromRotations;
        private bool _hasAuthoredLastPose;
        private bool _authoredTransitionActive;
        private float _authoredTransitionStartedAt;
        private float _authoredTransitionDurationSeconds = AuthoredActionTransitionSeconds;
        private AnimationClip _authoredIdleClip;
        private AnimationClip _authoredRunClip;
        private AnimationClip _authoredJumpClip;
        private AnimationClip _authoredLandClip;
        private bool _authoredClipsLoadAttempted;
        private float _authoredLocomotionBlend = 1f;
        private const float AuthoredActionTransitionSeconds = 0.14f;

        private Quaternion _hipsRest;
        private Quaternion _spineRest;
        private Quaternion _chestRest;
        private Quaternion _headRest;
        private Quaternion _antennaRest;
        private Quaternion _armLRest;
        private Quaternion _armRRest;
        private Quaternion _armTipLRest;
        private Quaternion _armTipRRest;
        private Quaternion _legLRest;
        private Quaternion _legRRest;
        private Quaternion _footLRest;
        private Quaternion _footRRest;
        private Vector3 _hipsRestPos;
        private Vector3 _legLRestPos;
        private Vector3 _legRRestPos;
        private Vector3 _footLRestPos;
        private Vector3 _footRRestPos;
        private Vector3 _legLAuthoredRestPos;
        private Vector3 _legRAuthoredRestPos;
        private bool _hasAuthoredLegRestPositions;
        private Vector3 _eyeRestScaleL;
        private Vector3 _eyeRestScaleR;
        private Vector3 _eyeExpressionScale = Vector3.one;
        private Vector3 _irisRestPosL;
        private Vector3 _irisRestPosR;
        private Vector3 _smileRestScale = Vector3.one;
        private Vector3 _makerPouchRestScale = Vector3.one;
        private Vector3 _makerPackRestScale = Vector3.one;

        private Action _action = Action.Idle;
        private DiscoveryKind _discoveryKind = DiscoveryKind.Other;
        private float _actionUntil;
        private float _actionStartedAt;
        private float _jumpStartedAt;
        private float _idleEnteredAt;
        private float _nextIdleGreetingAt;
        private float _idleGreetingStartedAt = -1f;
        private float _idleGreetingDuration = 1.1f;
        private bool _idleGreetingRightArm = true;
        private float _nextIdleLookAt;
        private Vector3 _hitRecoilLocalDirection;
        private float _idleLookStartedAt = -1f;
        private float _idleLookDuration;
        private float _idleLookYaw;
        private Action _lastTrackedAction;
        private float _phase;
        private float _runPhase;
        // A shorter support beat gives the recovery leg more time in the air,
        // making each stride read clearly at gameplay distance while LateUpdate
        // keeps the opposite foot planted through the stance interval.
        // Keep most of each slow waddle step under load while shortening the
        // planted interval enough to avoid pulling this single-segment leg skin
        // into a long tapered shape behind the body.
        private const float SlowRunStanceFraction = 0.56f;
        private const float FastRunStanceFraction = 0.52f;
        private const float MinimumRunCadenceHz = 1.6f;
        private const float RunExitFootBlendSeconds = 0.18f;
        private const float CraftActionSeconds = 1.1f;
        private const float CraftStrokeCycles = 2f;
        private const float LandingPrepDistance = 1.6f;
        private const float IdleGlanceMaxOffset = 0.022f;
        private const float IdleGlanceSmoothTime = 0.14f;
        private const float MaxIdleHeadYawDegrees = 6f;
        private const float MaxGroundContactOffset = 0.10f;
        private bool _leftRunStance;
        private bool _rightRunStance;
        private bool _wasRunning;
        private bool _leftRunExitPlant;
        private bool _rightRunExitPlant;
        private float _runExitStartedAt;
        private Vector3 _leftRunPlantWorldPosition;
        private Vector3 _rightRunPlantWorldPosition;
        private Quaternion _leftRunExitRotation;
        private Quaternion _rightRunExitRotation;
        private Quaternion _leftRunPlantRootLocalRotation;
        private Quaternion _rightRunPlantRootLocalRotation;
        private float _poseBlend;
        private Vector2 _runAccelerationLeanDegrees;
        private Vector3 _runAccelerationLocalForDiagnostics;
        private Vector3 _previousPlanarVelocity;
        private bool _hasPreviousPlanarVelocity;
        private float _landingImpactStrength;
        private float _landingTipFollowThrough;
        private float _landingTipCounterbalance;
        // Let the antenna trail Splat's authored pose targets by a few frames.
        // This adds secondary motion without scaling the body or changing the
        // feet's planted support path.
        private float _antennaPitch;
        private float _antennaPitchVelocity;
        private float _antennaRoll;
        private float _antennaRollVelocity;
        // Secondary flipper motion follows the actual jump arc instead of
        // free-running sine waves, which made the airborne pose wobble even
        // when Splat's vertical motion had already settled near the apex.
        private float _jumpWingFollow;
        private float _jumpWingFollowVelocity;
        private float _jumpTipFollow;
        private float _jumpTipFollowVelocity;
        private const float AntennaFollowTime = 0.085f;
        private float _runLandingStartedAt = -1f;
        private float _landingPrepWeight;
        // Visual grounding offsets must survive AnimationClip.SampleAnimation, which
        // writes the authored local pose again every Update. Keep the accumulated
        // correction separately, then reapply it after each sampled pose so the
        // contact solver can converge over several frames instead of restarting
        // from the same airborne foot position forever.
        private float _leftGroundContactOffsetY;
        private float _rightGroundContactOffsetY;
        private readonly RaycastHit[] _landingGroundHits = new RaycastHit[12];
        private float _nextBlinkAt;
        private float _blinkStartedAt;
        private bool _blinkActive;
        private float _nextGlanceAt;
        private float _glanceUntil;
        private float _glanceDirection;
        private Vector2 _gazeOffset;
        private Vector2 _gazeVelocity;
        private float _paintedBlink;
        private Transform _celebrationLookTarget;
        private float _makerPouchPulseStartedAt = -10f;
        private bool _motorEventsSubscribed;
        private const float MakerPouchPulseDuration = 0.52f;
        private static readonly int EyeBlinkProperty = Shader.PropertyToID("_EyeBlink");
        private static readonly int EyeExpressionProperty = Shader.PropertyToID("_EyeExpression");
        private static readonly int EyeGazeProperty = Shader.PropertyToID("_EyeGaze");

        public Action CurrentAction => _action;
        public DiscoveryKind CurrentDiscoveryKind => _discoveryKind;
        public float PhaseForDiagnostics => _phase;
        public float LandingImpactStrengthForDiagnostics => _landingImpactStrength;
        public float LandingPrepWeightForDiagnostics => _landingPrepWeight;
        public Vector2 AntennaPoseForDiagnostics => new Vector2(_antennaPitch, _antennaRoll);
        public Vector2 RunAccelerationLeanDegreesForDiagnostics => _runAccelerationLeanDegrees;
        public Vector3 RunAccelerationLocalForDiagnostics => _runAccelerationLocalForDiagnostics;
        public Vector3 HipsRestLocalPositionForDiagnostics => _hipsRestPos;
        public float IdleGreetingWeightForDiagnostics => IdleGreetingWeight();
        public bool IdleGreetingRightArmForDiagnostics => _idleGreetingRightArm;
        public float IdleGreetingNextAtForDiagnostics => _nextIdleGreetingAt;
        public float IdleGreetingStartedAtForDiagnostics => _idleGreetingStartedAt;
        public float AuthoredLocomotionTransitionWeightForDiagnostics => !_authoredTransitionActive
            ? 1f
            : Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((Time.time - _authoredTransitionStartedAt)
                    / Mathf.Max(0.01f, _authoredTransitionDurationSeconds)));
        public int FootPlantCountForDiagnostics { get; private set; }
        public bool UseAuthoredLocomotionForDiagnostics
        {
            get => useAuthoredLocomotionClips;
            set => useAuthoredLocomotionClips = value;
        }

        public event System.Action FootPlanted;
        public bool IsBlinking => _blinkActive;
        public bool IsBoundToVisibleVisual(Transform visual) => _armL != null && visual != null && _armL.IsChildOf(visual);
        public bool IsBoundToVisibleSkin(Transform visual)
        {
            if (_armL == null || visual == null) return false;
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin == null || skin.bones == null) continue;
                for (var i = 0; i < skin.bones.Length; i++)
                {
                    if (skin.bones[i] == _armL) return true;
                }
            }
            return false;
        }
        public Vector3 BoundLeftArmEuler => _armL != null ? _armL.localRotation.eulerAngles : Vector3.zero;
        public Vector3 BoundRightArmEuler => _armR != null ? _armR.localRotation.eulerAngles : Vector3.zero;
        public Vector3 BoundLeftArmRestEuler => _armLRest.eulerAngles;
        public Quaternion BoundLeftArmRestLocalRotation => _armLRest;
        public Vector3 BoundRightArmTipEuler => _armTipR != null ? _armTipR.localRotation.eulerAngles : Vector3.zero;
        public Vector3 BoundLeftArmTipEuler => _armTipL != null ? _armTipL.localRotation.eulerAngles : Vector3.zero;
        public Vector3 BoundLeftLegEuler => _legL != null ? _legL.localRotation.eulerAngles : Vector3.zero;
        public Vector3 BoundLeftLegLocalPosition => _legL != null ? _legL.localPosition : Vector3.zero;
        public Vector3 BoundRightLegLocalPosition => _legR != null ? _legR.localPosition : Vector3.zero;
        public Vector3 BoundLeftLegWorldPosition => _legL != null ? _legL.position : Vector3.zero;
        public Vector3 BoundRightLegWorldPosition => _legR != null ? _legR.position : Vector3.zero;
        public bool HasFootPivots => _footL != null && _footR != null;
        public Vector3 BoundLeftFootEuler => _footL != null ? _footL.localRotation.eulerAngles : Vector3.zero;
        public Vector3 BoundRightFootEuler => _footR != null ? _footR.localRotation.eulerAngles : Vector3.zero;
        public float VisualRootWorldYForDiagnostics => visualRoot != null ? visualRoot.position.y : float.NaN;
        public Vector3 VisualRootLossyScaleForDiagnostics => visualRoot != null ? visualRoot.lossyScale : Vector3.zero;
        public Vector2 GazeOffsetForDiagnostics => _gazeOffset;
        public bool IsLeftRunStanceForDiagnostics => _action == Action.Run && _leftRunStance;
        public bool IsRightRunStanceForDiagnostics => _action == Action.Run && _rightRunStance;

        public void CorrectRunStanceFootHeight(bool leftFoot, float worldYOffset)
        {
            if (_action != Action.Run || Mathf.Abs(worldYOffset) < 0.0001f) return;
            var pivot = GroundedFootPivot(leftFoot);
            if (leftFoot && _leftRunStance && pivot != null)
            {
                _leftRunPlantWorldPosition.y += worldYOffset;
                pivot.position += Vector3.up * worldYOffset;
            }
            else if (!leftFoot && _rightRunStance && pivot != null)
            {
                _rightRunPlantWorldPosition.y += worldYOffset;
                pivot.position += Vector3.up * worldYOffset;
            }
        }

        public void CorrectGroundedFootHeight(bool leftFoot, float worldYOffset)
        {
            if (Mathf.Abs(worldYOffset) < 0.0001f) return;
            var pivot = GroundedFootPivot(leftFoot);
            if (pivot == null) return;
            if (leftFoot)
            {
                _leftGroundContactOffsetY = Mathf.Clamp(
                    _leftGroundContactOffsetY + worldYOffset,
                    -MaxGroundContactOffset, MaxGroundContactOffset);
            }
            else
            {
                _rightGroundContactOffsetY = Mathf.Clamp(
                    _rightGroundContactOffsetY + worldYOffset,
                    -MaxGroundContactOffset, MaxGroundContactOffset);
            }
            pivot.position += Vector3.up * worldYOffset;
            if (_action == Action.Run)
            {
                if (leftFoot && _leftRunStance) _leftRunPlantWorldPosition.y += worldYOffset;
                else if (!leftFoot && _rightRunStance) _rightRunPlantWorldPosition.y += worldYOffset;
            }
        }

        public bool AlignGroundedFootToSurface(bool leftFoot, Vector3 surfaceNormal)
        {
            var foot = leftFoot ? _footL : _footR;
            if (foot == null || surfaceNormal.sqrMagnitude < 0.5f) return false;

            // Preserve the animated parent (and therefore the ankle pose), but
            // rotate the sole's local up axis onto its own walkable support.
            // This is applied after the action pose each frame, so it cannot
            // accumulate or alter the gameplay/collision root.
            var legRest = leftFoot ? _legLRest : _legRRest;
            var footRest = leftFoot ? _footLRest : _footRRest;
            var rootLocalRest = _hipsRest * legRest * footRest;
            var targetWorld = Quaternion.FromToRotation(transform.up, surfaceNormal.normalized)
                * (transform.rotation * rootLocalRest);
            if (_action == Action.Run)
            {
                // LateUpdate reapplies the stance foot's world-space plant after
                // visual contact repair. Keep that anchor's rotation aligned with
                // this same walkable surface or the late lock restores its flat
                // bind angle and lifts one edge of the sole on sloped platforms.
                var targetRootLocalRotation = Quaternion.Inverse(transform.rotation) * targetWorld;
                if (leftFoot && _leftRunStance)
                    _leftRunPlantRootLocalRotation = targetRootLocalRotation;
                else if (!leftFoot && _rightRunStance)
                    _rightRunPlantRootLocalRotation = targetRootLocalRotation;
            }
            var change = Quaternion.Angle(foot.rotation, targetWorld);
            if (change <= 0.1f) return false;
            foot.rotation = targetWorld;
            return change > 1f;
        }

        private Transform GroundedFootPivot(bool leftFoot)
        {
            return leftFoot ? (_footL != null ? _footL : _legL)
                : (_footR != null ? _footR : _legR);
        }
        public Vector3 BoundHipsWorldPosition => _hips != null ? _hips.position : Vector3.zero;
        public float BoundLeftLegWorldUpOffset => _legL != null && _legL.parent != null
            ? _legL.parent.TransformVector(_legL.localPosition - _legLRestPos).y : 0f;

        public void ForceEyesOpenForCapture()
        {
            if (_eyeRootL == null || _eyeRootR == null)
            {
                CacheBones();
            }

            if (_paintedFaceRenderer != null && (_eyeRootL == null || _eyeRootR == null))
            {
                _blinkActive = false;
                _paintedBlink = 0f;
                _nextBlinkAt = Time.time + 10f;
                _eyeExpressionScale = EyeExpressionScale(_action);
                ApplyPaintedFaceProperties();
                return;
            }

            if (_eyeRootL == null || _eyeRootR == null)
            {
                return;
            }

            _blinkActive = false;
            _nextBlinkAt = Time.time + 10f;
            _eyeExpressionScale = EyeExpressionScale(_action);
            ApplyEyeScale(_eyeExpressionScale);
        }

        public void Bind(SplatMotor motorRef, Transform visual)
        {
            UnsubscribeMotorEvents();
            motor = motorRef;
            visualRoot = visual;
            CacheBones();
            if (isActiveAndEnabled)
            {
                SubscribeMotorEvents();
            }
        }

        private void Awake()
        {
            _visualRepair = GetComponent<SplatVisualRepair>();
            _idleEnteredAt = Time.time;
            _lastTrackedAction = _action;
            CacheBones();
        }

        private void OnEnable()
        {
            SubscribeMotorEvents();

            GameEvents.CollectibleAcquired += HandleCollect;
            GameEvents.SecretWonderClaimed += HandleWonderClaimed;
            GameEvents.BuildRefused += HandleBuildRefused;
            GameEvents.BuildCompleted += HandleCraft;
            GameEvents.LevelCompleted += HandleLevelCompleted;
            GameEvents.GatewaySignalReceived += HandleGatewaySignalReceived;
        }

        private void OnDisable()
        {
            UnsubscribeMotorEvents();

            GameEvents.CollectibleAcquired -= HandleCollect;
            GameEvents.SecretWonderClaimed -= HandleWonderClaimed;
            GameEvents.BuildRefused -= HandleBuildRefused;
            GameEvents.BuildCompleted -= HandleCraft;
            GameEvents.LevelCompleted -= HandleLevelCompleted;
            GameEvents.GatewaySignalReceived -= HandleGatewaySignalReceived;
        }

        private void SubscribeMotorEvents()
        {
            if (_motorEventsSubscribed || motor == null)
            {
                return;
            }

            motor.Jumped += HandleJumped;
            motor.Landed += HandleLanded;
            _motorEventsSubscribed = true;
        }

        private void UnsubscribeMotorEvents()
        {
            if (!_motorEventsSubscribed || motor == null)
            {
                return;
            }

            motor.Jumped -= HandleJumped;
            motor.Landed -= HandleLanded;
            _motorEventsSubscribed = false;
        }

        private void Update()
        {
            using var markerScope = UpdateMarker.Auto();
            // Exponential smoothing keeps pose transitions consistent at 30/60/120
            // Hz and prevents a low-frame hitch from making the rig overshoot.
            _poseBlend = 1f - Mathf.Exp(-Mathf.Max(0f, poseBlendSpeed) * Time.deltaTime);
            if (_hips == null)
            {
                CacheBones();
                if (_hips == null)
                {
                    return;
                }
            }

            // A late grounded callback can arrive after the player has already
            // started moving (for example, when the controller settles onto a
            // thin pad). Do not let its short Land lock suppress the run gait
            // while Splat is decisively grounded and travelling.
            if (_action == Action.Land && motor != null && motor.IsGrounded
                && motor.PlanarVelocity.magnitude >= Mathf.Max(0f, runSpeedThreshold))
            {
                _action = Action.Run;
                _actionUntil = Time.time;
            }

            if (Time.time >= _actionUntil)
            {
                if (motor != null && !motor.IsGrounded)
                {
                    _action = Action.Jump;
                }
                else if (motor != null)
                {
                    // Use a lower release point than the entry point. Small
                    // changes in analog input or surface drag around a single
                    // cutoff used to toggle the entire run pose, interrupting
                    // its gait and foot-plant phase. The 72% release band keeps
                    // a slow start/stop in one action until speed clearly settles.
                    var enterRunAt = Mathf.Max(0f, runSpeedThreshold);
                    var exitRunAt = enterRunAt * 0.72f;
                    var runAt = _action == Action.Run ? exitRunAt : enterRunAt;
                    _action = motor.PlanarVelocity.magnitude >= runAt ? Action.Run : Action.Idle;
                }
                else
                {
                    _action = Action.Idle;
                }
            }

            if (_action != _lastTrackedAction)
            {
                BeginAuthoredLocomotionTransition(_action);
                _authoredLocomotionBlend = 0f;
                if (_action == Action.Idle)
                {
                    _authoredLocomotionBlend = 0f;
                    _idleEnteredAt = Time.time;
                    _idleGreetingStartedAt = -1f;
                    // Show a distinct character beat soon after the player stops;
                    // a long first wait let short pauses look like a frozen idle.
                    _nextIdleGreetingAt = Time.time + 0.35f + Random.Range(0.65f, 1.0f);
                    _idleLookStartedAt = -1f;
                    _nextIdleLookAt = Time.time + Random.Range(1.8f, 2.6f);
                }
                else if (_lastTrackedAction == Action.Idle)
                {
                    _authoredLocomotionBlend = 0f;
                    _idleGreetingStartedAt = -1f;
                    _nextIdleGreetingAt = 0f;
                    _idleLookStartedAt = -1f;
                }
                if (_action == Action.Jump)
                {
                    _authoredLocomotionBlend = 0f;
                    // Ground-contact correction can shift the cached rest pivots
                    // independently to match an uneven surface. Those offsets
                    // belong to the grounded pose; carrying them into takeoff
                    // made the airborne tuck inherit a terrain-specific stance.
                    // Ease from the current feet toward the authored pose through
                    // the normal pose blend instead of stretching the body.
                    RestoreAuthoredLegRestPositions();
                    ResetJumpWingFollow();
                }
                _lastTrackedAction = _action;
            }

            _phase += Time.deltaTime;
            UpdateRunAccelerationLean();
            if (_action == Action.Run && motor != null)
            {
                var planarSpeed = motor.PlanarVelocity.magnitude;
                var strideLength = RunStrideLength(planarSpeed);
                var stanceFraction = RunStanceFractionForSpeed(planarSpeed);
                // Match stride cadence to root travel so a stance foot reaches its
                // rearward step limit just as the stance interval ends. The former
                // fixed 6–9.2 rad/s cadence left the pivot anchored while Splat ran
                // several metres, stretching the skinned leg into a long spike.
                var cadenceHz = planarSpeed > 0.05f
                    // Keep cadence matched through the 1.55x teach-pad boost.
                    // With a support interval at or above half the cycle, this
                    // avoids airborne gaps while the higher ceiling preserves
                    // the authored leg reach at boosted root speeds.
                    ? Mathf.Clamp(planarSpeed * stanceFraction / (2f * strideLength), MinimumRunCadenceHz, 11.5f)
                    : MinimumRunCadenceHz;
                _runPhase = Mathf.Repeat(_runPhase + Time.deltaTime * cadenceHz * (Mathf.PI * 2f), Mathf.PI * 2f);
            }

            PoseForAction(_action);
            SampleAuthoredLocomotionClip();
            ApplyGroundedContactOffsets();
            AnimateMakerPouch();
            AnimateSmile();
            AnimateBlink();
            AnimateGaze();
            ApplyPaintedFaceProperties();
        }

        private void ApplyGroundedContactOffsets()
        {
            if (motor != null && !motor.IsGrounded)
            {
                _leftGroundContactOffsetY = 0f;
                _rightGroundContactOffsetY = 0f;
                return;
            }

            // Run uses its own world-space stance anchors; the additive offsets
            // belong to the two-foot Idle/Land contact solver only.
            if (_action != Action.Idle && _action != Action.Land)
            {
                _leftGroundContactOffsetY = 0f;
                _rightGroundContactOffsetY = 0f;
                return;
            }

            var left = GroundedFootPivot(true);
            var right = GroundedFootPivot(false);
            if (left != null && Mathf.Abs(_leftGroundContactOffsetY) > 0.0001f)
                left.position += Vector3.up * _leftGroundContactOffsetY;
            if (right != null && Mathf.Abs(_rightGroundContactOffsetY) > 0.0001f)
                right.position += Vector3.up * _rightGroundContactOffsetY;
        }

        private void UpdateRunAccelerationLean()
        {
            var planarVelocity = motor != null ? motor.PlanarVelocity : Vector3.zero;
            if (!_hasPreviousPlanarVelocity)
            {
                _previousPlanarVelocity = planarVelocity;
                _hasPreviousPlanarVelocity = true;
            }

            var dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var worldAcceleration = (planarVelocity - _previousPlanarVelocity) / dt;
            _previousPlanarVelocity = planarVelocity;
            var localAcceleration = transform.InverseTransformDirection(worldAcceleration);
            localAcceleration.y = 0f;
            _runAccelerationLocalForDiagnostics = localAcceleration;

            // Let the torso lead a quick start and counter the lean while braking
            // or turning. Keep the response small and eased: the feet remain on
            // their planted anchors, and the body keeps its original silhouette.
            var target = new Vector2(
                Mathf.Clamp(-localAcceleration.x * 1.0f, -6f, 6f),
                Mathf.Clamp(localAcceleration.z * 2.0f, -8f, 8f));
            // Ground acceleration reaches top speed in only a few motor steps.
            // Use a quick attack so that short impulse produces a readable lean,
            // then a slower release so the torso settles naturally after input
            // stops instead of immediately snapping upright.
            var attacking = target.sqrMagnitude >= _runAccelerationLeanDegrees.sqrMagnitude;
            var responseRate = attacking ? 24f : 4f;
            var blend = 1f - Mathf.Exp(-responseRate * dt);
            _runAccelerationLeanDegrees = Vector2.Lerp(_runAccelerationLeanDegrees, target, blend);
        }

        private void LateUpdate()
        {
            using var markerScope = LateUpdateMarker.Auto();
            // SplatMotor moves the character root during Update. Keep the planted
            // boot pivots fixed in world space after that movement so the stance
            // phase does not skate across narrow traversal platforms.
            var leftPlant = GroundedFootPivot(true);
            var rightPlant = GroundedFootPivot(false);
            if (_action == Action.Run && _leftRunStance && leftPlant != null)
            {
                leftPlant.SetPositionAndRotation(_leftRunPlantWorldPosition,
                    transform.rotation * _leftRunPlantRootLocalRotation);
            }

            if (_action == Action.Run && _rightRunStance && rightPlant != null)
            {
                rightPlant.SetPositionAndRotation(_rightRunPlantWorldPosition,
                    transform.rotation * _rightRunPlantRootLocalRotation);
            }

            // Keep the last support foot steady as the run pose eases into idle.
            // Dropping the world-space lock on the first idle frame let SoftPos
            // pull a planted shoe back under the hips immediately, which looked
            // like a short skate at the end of each stop. Fade the old anchor into
            // the already-blending idle pose so the foot releases without a snap.
            if (_action == Action.Idle && (motor == null || motor.IsGrounded)
                && Time.time - _runExitStartedAt < RunExitFootBlendSeconds)
            {
                var release = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((Time.time - _runExitStartedAt) / RunExitFootBlendSeconds));
                BlendRunExitFoot(leftPlant, _leftRunExitPlant, _leftRunPlantWorldPosition,
                    _leftRunExitRotation, release);
                BlendRunExitFoot(rightPlant, _rightRunExitPlant, _rightRunPlantWorldPosition,
                    _rightRunExitRotation, release);
            }
            else
            {
                _leftRunExitPlant = false;
                _rightRunExitPlant = false;
            }

            // SplatVisualRepair runs before this animator's LateUpdate. Recheck
            // the final posed skin for every action: idle breathing and jump/land
            // transitions can move the sole bounds even when Run foot locks are
            // inactive, leaving Splat visibly hovering or sunk on a platform.
            // The scene repair can be added after this animator already bound its
            // visible rig. Refresh the reference here so those runtime-created
            // skinned characters still receive the final grounded contact pass.
            if (_visualRepair == null)
            {
                _visualRepair = GetComponent<SplatVisualRepair>();
            }
            var groundedVisualCorrection = _visualRepair != null
                ? _visualRepair.AlignGroundedSolesAfterPose()
                : Vector3.zero;
            if (groundedVisualCorrection.sqrMagnitude > 0.000001f)
            {
                // Visual sole seating translates the whole skinned rig after the
                // run stance has already been locked in world space. Move each
                // active plant anchor by the same correction so the grounding
                // pass does not fight its own foot lock and leave the shoe high.
                if (_action == Action.Run)
                {
                    if (_leftRunStance && leftPlant != null)
                    {
                        _leftRunPlantWorldPosition += groundedVisualCorrection;
                        leftPlant.SetPositionAndRotation(_leftRunPlantWorldPosition,
                            transform.rotation * _leftRunPlantRootLocalRotation);
                    }

                    if (_rightRunStance && rightPlant != null)
                    {
                        _rightRunPlantWorldPosition += groundedVisualCorrection;
                        rightPlant.SetPositionAndRotation(_rightRunPlantWorldPosition,
                            transform.rotation * _rightRunPlantRootLocalRotation);
                    }
                }
                else if (_action == Action.Idle
                    && Time.time - _runExitStartedAt < RunExitFootBlendSeconds)
                {
                    if (_leftRunExitPlant && leftPlant != null)
                    {
                        _leftRunPlantWorldPosition += groundedVisualCorrection;
                        leftPlant.position += groundedVisualCorrection;
                    }

                    if (_rightRunExitPlant && rightPlant != null)
                    {
                        _rightRunPlantWorldPosition += groundedVisualCorrection;
                        rightPlant.position += groundedVisualCorrection;
                    }
                }
            }

            if (_action == Action.Land)
            {
                // Carry Jump's lowered descent into contact. The short authored
                // Land clip can restore a wide arm spread after PoseLand; using
                // the same lowered shoulders as grounded Idle here keeps the
                // last visible landing frames from snapping back to a T-pose.
                if (_armL != null)
                    _armL.localRotation = _armLRest * Quaternion.Euler(-34f, 0f, 54f);
                if (_armR != null)
                    _armR.localRotation = _armRRest * Quaternion.Euler(-34f, 0f, -54f);
            }

            CacheAuthoredPoseForNextTransition();
        }

        public void PlayAction(Action action, float duration)
        {
            if (_action == Action.Celebrate && Time.time < _actionUntil
                && action != Action.Celebrate && action != Action.Hit)
            {
                return;
            }

            _action = action;
            _actionStartedAt = Time.time;
            _actionUntil = Time.time + Mathf.Max(0.05f, duration);
            if (action == Action.Jump)
            {
                _jumpStartedAt = Time.time;
            }
            if (action == Action.Hit)
            {
                StopAllCoroutines();
            }
            if (action == Action.Land)
            {
                _landingImpactStrength = motor != null
                    ? Mathf.InverseLerp(3.5f, 14.7f, motor.LastLandingImpactSpeed)
                    : 0f;
                _runLandingStartedAt = Time.time;
            }
            else if (action == Action.Jump)
            {
                ResetJumpWingFollow();
            }
            if (action == Action.Celebrate)
            {
                StopAllCoroutines();
            }

            if (action == Action.Interact || action == Action.Craft)
            {
                StopAllCoroutines();
                StartCoroutine(OneShotRoutine(action, duration));
            }
        }

        private IEnumerator OneShotRoutine(Action action, float duration)
        {
            var end = Time.time + duration;
            while (Time.time < end)
            {
                PoseForAction(action);
                yield return null;
            }
        }

        private void PoseForAction(Action action)
        {
            if (action != Action.Jump) _landingPrepWeight = 0f;
            if (action == Action.Run)
            {
                _wasRunning = true;
                _leftRunExitPlant = false;
                _rightRunExitPlant = false;
            }
            else
            {
                if (_wasRunning)
                {
                    var leftPivot = GroundedFootPivot(true);
                    var rightPivot = GroundedFootPivot(false);
                    _leftRunExitPlant = _leftRunStance && leftPivot != null;
                    _rightRunExitPlant = _rightRunStance && rightPivot != null;
                    _runExitStartedAt = Time.time;
                    _leftRunExitRotation = leftPivot != null ? leftPivot.rotation : Quaternion.identity;
                    _rightRunExitRotation = rightPivot != null ? rightPivot.rotation : Quaternion.identity;
                    _wasRunning = false;
                }
                _leftRunStance = false;
                _rightRunStance = false;
            }

            // Jump and Land own their leg positions. Resetting to rest before
            // posing them every frame fought the target and cut their foot lift
            // roughly in half; the other actions can still settle legs to rest.
            if (action != Action.Run && action != Action.Jump && action != Action.Land
                && action != Action.Hit)
            {
                ResetLegPositions();
            }

            switch (action)
            {
                case Action.Run:
                    PoseRun();
                    break;
                case Action.Jump:
                    PoseJump();
                    break;
                case Action.Land:
                    PoseLand();
                    break;
                case Action.Hit:
                    PoseHit();
                    break;
                case Action.Interact:
                    PoseInteract();
                    break;
                case Action.Oops:
                    PoseOops();
                    break;
                case Action.Discover:
                    PoseDiscover();
                    break;
                case Action.Craft:
                    PoseCraft();
                    break;
                case Action.Listen:
                    PoseListen();
                    break;
                case Action.Celebrate:
                    PoseCelebrate();
                    break;
                default:
                    PoseIdle();
                    break;
            }

            PoseArmTips(action);
        }

        private void PoseArmTips(Action action)
        {
            if (_armTipL == null && _armTipR == null)
            {
                return;
            }

            var left = 0f;
            var right = 0f;
            var greeting = IdleGreetingWeight();
            var jumpTipCounterRoll = 0f;
            switch (action)
            {
                case Action.Run:
                    var speed = motor != null ? Mathf.Clamp01(motor.PlanarVelocity.magnitude / 7.5f) : 0.5f;
                    // The shoulder gives the run its broad pump. Increase the
                    // smaller tip follow-through as well so the rounded flippers
                    // keep a readable alternating edge at ordinary run speed.
                    // Keep the movement in rotation only; the boot path and body
                    // proportions stay fixed for the planted gait.
                    var swing = Mathf.Sin(_runPhase) * Mathf.Lerp(24f, 34f, speed);
                    left = swing;
                    right = -swing;
                    break;
                case Action.Jump:
                    // PoseJump already drives the tips from a damped, velocity-
                    // shaped spring. Keep that same target here: this final tip
                    // pass runs after PoseJump and used to overwrite its follow
                    // with an unrelated free-running sine, adding a second wobble
                    // to every jump and landing approach.
                    var wingRoll = _jumpWingFollow * 0.35f;
                    var tipBrace = _landingPrepWeight * 18f;
                    left = 6f + _jumpTipFollow + tipBrace;
                    right = 6f - _jumpTipFollow + tipBrace;
                    // Preserve PoseJump's small counter-roll so the distal
                    // flippers remain attached to the same flight beat.
                    jumpTipCounterRoll = wingRoll * 0.2f;
                    break;
                case Action.Land:
                    // PoseLand computes one damped tip follow-through alongside
                    // the body settle. Reuse those values here: this final pass
                    // used to replace its soft landing pose with an unrelated
                    // positive arm lift and a second counterbalance.
                    left = -5f + _landingTipFollowThrough;
                    right = -5f - _landingTipFollowThrough;
                    break;
                case Action.Hit:
                    var hitFollow = Mathf.Exp(-Mathf.Max(0f, Time.time - _actionStartedAt) * 13f) * 10f;
                    left = hitFollow;
                    right = hitFollow;
                    break;
                case Action.Interact:
                    right = -14f;
                    break;
                case Action.Discover:
                    left = -12f;
                    right = -12f;
                    break;
                case Action.Oops:
                    left = 14f;
                    right = -8f;
                    break;
                case Action.Craft:
                    var spin = Mathf.Sin(CraftActionElapsed * CraftStrokeCycles
                        * (Mathf.PI * 2f / CraftActionSeconds)) * 18f;
                    left = -8f + spin;
                    right = -8f - spin;
                    break;
                case Action.Listen:
                    left = 10f;
                    break;
                case Action.Celebrate:
                    left = Mathf.Sin(_phase * 3f) * 12f;
                    right = Mathf.Sin(_phase * 3f + 0.7f) * 6f;
                    break;
                default:
                    // A wider distal beat makes the raised flipper read as a
                    // greeting instead of one static arm lift from gameplay range.
                    var greetingWave = greeting * IdleGreetingWave() * 46f;
                    left = -3f + (_idleGreetingRightArm ? 0f : greetingWave);
                    // Let the wave travel through the soft flipper at a readable
                    // cadence instead of the fast tremble that made it look like
                    // incidental jitter from the gameplay camera.
                    // The staged far-side trial clears more of the torso silhouette.
                    // Its larger distal arc is visible in the capture, but still
                    // reads as a side extension rather than a polished wave.
                    right = 3f + (_idleGreetingRightArm ? greetingWave : 0f);
                    break;
            }

            // Add a small side-to-side roll through the distal flipper so the
            // idle hello reads as a wave, rather than an arm held out with only
            // a hinge-like up/down wag.
            var greetingTipRoll = greeting * IdleGreetingWave() * 18f + jumpTipCounterRoll;
            var leftTipRoll = -6f + (_idleGreetingRightArm ? 0f : greetingTipRoll);
            var rightTipRoll = 6f + (_idleGreetingRightArm ? greetingTipRoll : 0f);
            if (action == Action.Jump)
            {
                leftTipRoll = -7f - jumpTipCounterRoll;
                rightTipRoll = 7f + jumpTipCounterRoll;
            }
            else if (action == Action.Land)
            {
                leftTipRoll = -5f - _landingTipCounterbalance * 0.35f;
                rightTipRoll = 5f + _landingTipCounterbalance * 0.35f;
            }
            SoftRot(_armTipL, _armTipLRest * Quaternion.Euler(left, 0f, leftTipRoll));
            SoftRot(_armTipR, _armTipRRest * Quaternion.Euler(right, 0f, rightTipRoll));
        }

        private void PoseIdle()
        {
            var breath = Mathf.Sin(_phase * 1.35f);
            var chestBreath = Mathf.Sin(_phase * 1.35f - 0.38f);
            var shoulderSway = Mathf.Sin(_phase * 1.15f);
            var weightShift = Mathf.Sin(_phase * 0.92f + 0.3f);
            var greeting = IdleGreetingWeight();
            var curiousLook = IdleCuriousLookYaw(greeting);
            var curiousArmLift = Mathf.Clamp01(Mathf.Abs(curiousLook) / 19f) * 16f;
            var curiousLeftArmLift = curiousLook < 0f ? curiousArmLift : 0f;
            var curiousRightArmLift = curiousLook > 0f ? curiousArmLift : 0f;
            var lookYaw = Mathf.Clamp(Mathf.Sin(_phase * 0.75f) * 14f + _gazeOffset.x * 360f
                + curiousLook, -34f, 34f)
                * (1f - greeting * 0.85f);
            // Keep the pelvis centered over both planted feet during idle. Previous
            // lateral weight shifts translated both leg roots across narrow pads;
            // retain the character in the upper-body sway instead.
            SoftPos(_hips, _hipsRestPos);
            // Retain a small part of the smoothed run lean after the speed
            // threshold switches the pose to Idle. Without this, braking tilt
            // vanished on the exact frame the gait stopped and read as a snap.
            // The signal decays in UpdateRunAccelerationLean; this only rotates
            // the upper body and leaves the idle pelvis position and foot roots
            // planted.
            SoftRot(_hips, _hipsRest);
            SoftRot(_spine, _spineRest * Quaternion.Euler(
                breath * 6f + _runAccelerationLeanDegrees.y * 0.55f,
                -lookYaw * 0.2f,
                shoulderSway * 6f - weightShift * 2f + greeting * 2.5f + _runAccelerationLeanDegrees.x * 0.55f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(
                chestBreath * 7f + _runAccelerationLeanDegrees.y * 0.2f,
                -lookYaw * 0.1f,
                shoulderSway * 7.5f - weightShift * 2.8f + greeting * 4f + _runAccelerationLeanDegrees.x * 0.3f));
            // Let Splat visibly notice the world while staying planted. The earlier
            // four-degree head sway and eye-only glance disappeared at gameplay scale.
            // Let the head acknowledge the raised flipper so the greeting reads
            // as a whole-character action rather than a loose arm waggle. Keep
            // the small lean above the planted pelvis and feet.
            var lookTilt = Mathf.Sin(_phase * 0.55f) * 8f - weightShift * 1.5f
                + greeting * 6f - curiousLook * 0.11f;
            // Splat's fused eyes and face visibly skew in same-rig pose reviews
            // once head yaw reaches about 14 degrees. Keep the head almost
            // forward-facing and let the pupils plus upper-body counter-sway
            // carry idle curiosity instead of rotating the fused face.
            SoftRot(_head, _headRest * Quaternion.Euler(
                -_runAccelerationLeanDegrees.y * 0.2f,
                Mathf.Clamp(lookYaw, -MaxIdleHeadYawDegrees, MaxIdleHeadYawDegrees),
                lookTilt - _runAccelerationLeanDegrees.x * 0.25f));
            var antennaGreeting = greeting * IdleGreetingWave() * 14f;
            SoftAntenna(0f, Mathf.Sin(_phase * 1.4f) * 20f + antennaGreeting);
            // The arm bones point out along local X, so rotations around that
            // axis only twist the flippers. Rotate around local Z to lower the
            // mirrored arms toward the ground and swing only one upward to greet.
            var greetingWavePhase = IdleGreetingWave();
            var greetingWave = greetingWavePhase * greeting * 3f;
            var relaxSway = breath * 1.5f + shoulderSway * 1.5f;
            var leftArmRoll = 54f + relaxSway
                - (_idleGreetingRightArm ? 0f : greeting * (124f + greetingWave));
            var rightArmRoll = -54f - relaxSway
                + (_idleGreetingRightArm ? greeting * (124f + greetingWave) : 0f);
            SoftRot(_armL, _armLRest * Quaternion.Euler(-34f + shoulderSway * 2f
                + curiousLeftArmLift * 0.2f, 0f, leftArmRoll));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-34f - shoulderSway * 2f
                + curiousRightArmLift * 0.2f, 0f, rightArmRoll));
            ResetLegs();
        }

        private float IdleCuriousLookYaw(float greeting)
        {
            if (_action != Action.Idle || greeting > 0.05f)
            {
                _idleLookStartedAt = -1f;
                return 0f;
            }

            if (_idleLookStartedAt < 0f)
            {
                if (_nextIdleLookAt <= 0f)
                    _nextIdleLookAt = Time.time + Random.Range(1.4f, 2.2f);
                if (Time.time < _nextIdleLookAt) return 0f;

                _idleLookStartedAt = Time.time;
                _idleLookDuration = Random.Range(0.72f, 0.96f);
                _idleLookYaw = Random.value < 0.5f ? -19f : 19f;
            }

            var elapsed = Time.time - _idleLookStartedAt;
            if (elapsed >= _idleLookDuration)
            {
                _idleLookStartedAt = -1f;
                _nextIdleLookAt = Time.time + Random.Range(2.4f, 4.0f);
                return 0f;
            }

            var rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.2f, elapsed));
            var fallStart = _idleLookDuration - 0.22f;
            var fall = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(fallStart, _idleLookDuration, elapsed));
            return _idleLookYaw * rise * fall;
        }

        private float IdleGreetingWeight()
        {
            if (_action != Action.Idle)
            {
                _idleGreetingStartedAt = -1f;
                return 0f;
            }

            if (_idleGreetingStartedAt < 0f)
            {
                if (_nextIdleGreetingAt <= 0f)
                {
                    _nextIdleGreetingAt = Mathf.Max(_idleEnteredAt + 0.35f,
                        Time.time + Random.Range(0.65f, 1.0f));
                }
                if (Time.time < _nextIdleGreetingAt) return 0f;
                _idleGreetingStartedAt = Time.time;
                _idleGreetingDuration = Random.Range(1.0f, 1.3f);
                _idleGreetingRightArm = IsRightArmFartherFromCamera();
            }

            var elapsed = Time.time - _idleGreetingStartedAt;
            if (elapsed >= _idleGreetingDuration)
            {
                _idleGreetingStartedAt = -1f;
                _nextIdleGreetingAt = Time.time + Random.Range(3.2f, 4.4f);
                return 0f;
            }

            var rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.22f, elapsed));
            var fallStart = _idleGreetingDuration - 0.24f;
            var fall = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(fallStart, _idleGreetingDuration, elapsed));
            return rise * fall;
        }

        private float IdleGreetingWave()
        {
            return _idleGreetingStartedAt >= 0f
                // A slower, broader beat reads as an intentional hello from the
                // gameplay camera instead of a fast wrist twitch.
                ? Mathf.Sin((Time.time - _idleGreetingStartedAt) * 5.2f)
                : 0f;
        }

        private bool IsRightArmFartherFromCamera()
        {
            var camera = Camera.main;
            if (camera == null || _armL == null || _armR == null)
            {
                return true;
            }

            var cameraPosition = camera.transform.position;
            // Select the farther flipper: the near-side arm is mostly occluded
            // by the torso in the gameplay-facing greeting capture.
            return (cameraPosition - _armR.position).sqrMagnitude
                >= (cameraPosition - _armL.position).sqrMagnitude;
        }

        private void PoseRun()
        {
            var planarSpeed = motor != null ? motor.PlanarVelocity.magnitude : 3.75f;
            var speed = Mathf.Clamp01(planarSpeed / 7.5f);
            var stride = Mathf.Sin(_runPhase);
            // The short arm bones point out along local X. Keep the same
            // downward local-Z bias as Idle, then add a smaller alternating
            // pump so the gait does not fan both flippers into a T-pose.
            // Keep the short arms closer to the torso during the stride. The
            // previous 14–24 degree counter-swing repeatedly opened both arms
            // into a wing-like silhouette at the gameplay camera distance.
            var swing = stride * (9f + speed * 7f);
            // Add a small alternating shoulder yaw so each flipper sweeps through
            // the direction of travel as its partner recovers. The older single-
            // axis pump moved both hands mostly sideways, which read as a broad
            // flap rather than a run. Keep this cross-axis beat restrained so it
            // does not lift the arms into a T-pose or disturb the planted gait.
            var armYaw = Mathf.Cos(_runPhase) * (6f + speed * 5f);
            // Give the soft body one compact 3 cm pulse per stride. Two full
            // vertical bounces per stride read as a high-frequency jelly wobble.
            var bob = (0.5f - 0.5f * Mathf.Cos(_runPhase)) * 0.03f * speed;
            var bodySway = stride * Mathf.Lerp(2f, 6f, speed);
            var landingAge = Time.time - _runLandingStartedAt;
            var landingSettleTime = Mathf.Max(0f, landingAge) * 7f;
            var runLandingBeat = _runLandingStartedAt >= 0f && landingAge < 0.36f
                ? (1f + landingSettleTime) * Mathf.Exp(-landingSettleTime)
                    * (1f - Mathf.SmoothStep(0.30f, 0.36f, landingAge))
                : 0f;
            var landingCompression = (0.075f + _landingImpactStrength * 0.025f) * runLandingBeat;
            var stanceFraction = RunStanceFractionForSpeed(planarSpeed);
            // Use the same travel-matched stride as the cadence calculation.
            // A separate full-run length here made the swing leg reach too far
            // at analog walking speeds even though the support pivot was locked.
            var strideLength = RunStrideLength(planarSpeed);
            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, bob - landingCompression));
            // Give each leg a real support interval: its foot stays at floor height
            // and rolls back under the body before lifting into a shorter recovery
            // arc. The old sine lift moved the supporting foot too, which read as
            // Splat hovering across the thin traversal pads.
            var leftCycle = Mathf.Repeat(_runPhase / (Mathf.PI * 2f), 1f);
            var rightCycle = Mathf.Repeat(leftCycle + 0.5f, 1f);
            var leftInStance = leftCycle < stanceFraction;
            var rightInStance = rightCycle < stanceFraction;
            var leftPlant = GroundedFootPivot(true);
            var rightPlant = GroundedFootPivot(false);
            SoftRot(_footL, _footLRest * Quaternion.Euler(leftInStance ? 0f : -12f, 0f, 0f));
            SoftRot(_footR, _footRRest * Quaternion.Euler(rightInStance ? 0f : -12f, 0f, 0f));
            if (leftInStance && !_leftRunStance && leftPlant != null)
            {
                _leftRunPlantWorldPosition = leftPlant.position;
                _leftRunPlantRootLocalRotation = CaptureRestFootRotationInRootSpace(true);
                if (motor != null && motor.IsGrounded)
                {
                    FootPlantCountForDiagnostics++;
                    FootPlanted?.Invoke();
                }
            }

            if (rightInStance && !_rightRunStance && rightPlant != null)
            {
                _rightRunPlantWorldPosition = rightPlant.position;
                _rightRunPlantRootLocalRotation = CaptureRestFootRotationInRootSpace(false);
                if (motor != null && motor.IsGrounded)
                {
                    FootPlantCountForDiagnostics++;
                    FootPlanted?.Invoke();
                }
            }

            _leftRunStance = leftInStance;
            _rightRunStance = rightInStance;
            EvaluateRunFoot(leftCycle, strideLength, stanceFraction,
                speed, out var leftFootLift, out var leftFootStride, out var leftLegPitch);
            EvaluateRunFoot(rightCycle, strideLength, stanceFraction,
                speed, out var rightFootLift, out var rightFootStride, out var rightLegPitch);
            // The legs descend from the animated pelvis, so pelvis bob also lifts
            // both feet. Cancel that shared motion at each foot root; only the
            // swing foot should leave the ground while the support foot stays planted.
            SoftRunLegPos(_legL, _legLRestPos + LocalWorldUpOffset(_legL, leftFootLift - bob + landingCompression)
                + LocalWorldForwardOffset(_legL, leftFootStride), speed);
            SoftRunLegPos(_legR, _legRRestPos + LocalWorldUpOffset(_legR, rightFootLift - bob + landingCompression)
                + LocalWorldForwardOffset(_legR, rightFootStride), speed);
            // Counter-rotate the torso and head across the leg cycle so the gait
            // reads as a planted waddle instead of a loose whole-body wobble.
            SoftRot(_hips, _hipsRest * Quaternion.Euler(2f * speed,
                stride * 1.8f, -bodySway + _runAccelerationLeanDegrees.x * 0.35f));
            SoftRot(_spine, _spineRest * Quaternion.Euler(2f * speed + _runAccelerationLeanDegrees.y,
                -stride * 1.2f, bodySway * 0.55f + _runAccelerationLeanDegrees.x)
                * Quaternion.Euler(10f * runLandingBeat, 0f, 0f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(_runAccelerationLeanDegrees.y * 0.35f,
                swing * 0.18f, -bodySway * 0.42f + _runAccelerationLeanDegrees.x * 0.5f)
                * Quaternion.Euler(6f * runLandingBeat, 0f, 0f));
            SoftRot(_head, _headRest * Quaternion.Euler(-2f * speed - _runAccelerationLeanDegrees.y * 0.45f,
                -stride * 1.2f, -bodySway * 0.48f - _runAccelerationLeanDegrees.x * 0.35f)
                * Quaternion.Euler(-3f * runLandingBeat, 0f, 0f));
            SoftAntenna(0f,
                Mathf.Sin(_runPhase - 0.42f) * (2f + speed * 2f) + 18f * runLandingBeat);
            // Keep a visibly lowered base pose, then counter-swing in forward/back
            // rotation instead of repeatedly lifting both flippers into a T-shape.
            var leftRunArm = Quaternion.Euler(-34f, armYaw, 62f + swing);
            var rightRunArm = Quaternion.Euler(-34f, -armYaw, -62f - swing);
            var landingBraceBlend = runLandingBeat * 0.8f;
            var leftLandingBrace = Quaternion.Euler(-34f, armYaw * 0.2f, 62f);
            var rightLandingBrace = Quaternion.Euler(-34f, -armYaw * 0.2f, -62f);
            SoftRot(_armL, _armLRest * Quaternion.Slerp(leftRunArm, leftLandingBrace, landingBraceBlend));
            SoftRot(_armR, _armRRest * Quaternion.Slerp(rightRunArm, rightLandingBrace, landingBraceBlend));
            // Animate the existing distal flipper bones as the shoulders pump.
            // Keeping them at bind pose made the arms move like two rigid paddles;
            // this counter-swing adds follow-through without changing body scale
            // or the support-foot path.
            var armTipLag = Mathf.Sin(_runPhase - 0.72f) * Mathf.Lerp(10f, 20f, speed);
            var armTipFold = 7f + speed * 8f;
            SoftRot(_armTipL, _armTipLRest * Quaternion.Euler(armTipFold + armTipLag,
                0f, -6f - armYaw * 0.15f));
            SoftRot(_armTipR, _armTipRRest * Quaternion.Euler(armTipFold - armTipLag,
                0f, 6f + armYaw * 0.15f));
            SoftRot(_legL, _legLRest * Quaternion.Euler(-leftLegPitch, 0f, 0f));
            SoftRot(_legR, _legRRest * Quaternion.Euler(rightLegPitch, 0f, 0f));

        }

        private static float RunStanceFractionForSpeed(float planarSpeed)
        {
            var speedBlend = Mathf.Clamp01(planarSpeed / 7.5f);
            return Mathf.Lerp(SlowRunStanceFraction, FastRunStanceFraction, speedBlend * speedBlend);
        }

        private static void EvaluateRunFoot(float cycle, float strideLength, float stanceFraction,
            float speedBlend,
            out float lift, out float stride, out float pitch)
        {
            // Keep the recovery foot clear without lifting Splat into a floaty
            // high-step. The animation is a light waddle, not a run in deep snow.
            // A full jog arc looked like a hop at walking pace on the narrow pads.
            // Scale only the swing clearance with pace; the planted interval,
            // stride length, and faster-run silhouette remain unchanged.
            var swingLift = Mathf.Lerp(0.08f, 0.15f, Mathf.Clamp01(speedBlend));
            const float swingPitch = 10f;
            // The stance toe remains nearly level while the swing toe rolls through
            // a small recovery arc. Keep the planted sole broadly flat on the pad.
            const float stancePitch = 6f;

            if (cycle < stanceFraction)
            {
                var stance = cycle / stanceFraction;
                lift = 0f;
                stride = Mathf.Lerp(strideLength, -strideLength, stance);
                // Keep toe rotation small while this foot is meant to bear weight;
                // the earlier 42-degree arc let the visible sole roll off the pad.
                pitch = Mathf.Lerp(stancePitch, -stancePitch, stance);
                return;
            }

            var swing = (cycle - stanceFraction) / (1f - stanceFraction);
            // Ease the recovery foot at both ends of its swing. Linear travel
            // started moving at full speed on the exact frame the stance lock
            // released, then stopped just as abruptly when the next lock began;
            // that reversal made the short-legged gait read as a mechanical hop.
            // SmoothStep keeps the same endpoints and stance timing while easing
            // velocity to zero at lift-off and touchdown.
            var swingTravel = Mathf.SmoothStep(0f, 1f, swing);
            lift = Mathf.Sin(swing * Mathf.PI) * swingLift;
            stride = Mathf.Lerp(-strideLength, strideLength, swingTravel);
            pitch = Mathf.Lerp(-stancePitch, swingPitch, swingTravel);
        }

        private static float RunStrideLength(float planarSpeed)
        {
            var speedBlend = Mathf.Clamp01(planarSpeed / 7.5f);
            // Keep ordinary-speed foot pivots close to Splat's short legs, but
            // preserve enough reach at boost speed for the capped gait cadence
            // to match root travel. Longer reaches flatten the single-segment
            // leg at walking pace; too little reach at top speed makes the planted
            // foot lag behind the body.
            var fullStrideLength = 0.14f + 0.14f * speedBlend * speedBlend;
            // Scale the stride with root travel until the ordinary run length
            // becomes appropriate, keeping analog-speed foot motion in phase.
            var walkingStrideLength = planarSpeed * RunStanceFractionForSpeed(planarSpeed)
                / (2f * MinimumRunCadenceHz);
            return Mathf.Min(fullStrideLength, Mathf.Max(0.06f, walkingStrideLength));
        }

        private Quaternion CaptureRestFootRotationInRootSpace(bool leftFoot)
        {
            // Bone_Hips is animated for the gait's counter-sway. Capture the boot's
            // bind orientation relative to Splat's moving root instead of retaining
            // the previous swing frame's pitch; a planted shoe should not land tilted.
            var hipsRoot = _hips != null && _hips.parent != null ? _hips.parent : transform;
            var legRest = leftFoot ? _legLRest : _legRRest;
            var footRest = leftFoot ? _footLRest : _footRRest;
            var restWorldRotation = hipsRoot.rotation * _hipsRest * legRest * footRest;
            return Quaternion.Inverse(transform.rotation) * restWorldRotation;
        }

        private static void BlendRunExitFoot(Transform leg, bool planted, Vector3 anchor,
            Quaternion anchorRotation, float release)
        {
            if (leg == null || !planted) return;
            var posedPosition = leg.position;
            var posedRotation = leg.rotation;
            leg.SetPositionAndRotation(Vector3.Lerp(anchor, posedPosition, release),
                Quaternion.Slerp(anchorRotation, posedRotation, release));
        }

        private void PoseJump()
        {
            // Let the visible jump pose follow Splat's actual arc. At takeoff he
            // tucks his feet and leans into the launch; near the apex the flippers
            // open into a readable float; on descent the feet extend for landing.
            // A fixed airborne T-pose made every part of the jump look weightless.
            var verticalSpeed = motor != null ? motor.VerticalVelocity : 0f;
            _landingPrepWeight = EvaluateLandingPrepWeight(verticalSpeed);
            var launch = Mathf.InverseLerp(-7f, 14.7f, verticalSpeed);
            var apexOpen = verticalSpeed >= 0f
                ? 1f - Mathf.Clamp01(verticalSpeed / 14.7f)
                : 1f - Mathf.Clamp01(-verticalSpeed / 7f);
            // Ease into and out of the glide instead of mapping vertical speed
            // directly to the shoulder pose. Speed changes nearly linearly
            // around the apex, so the raw mapping made the flippers reverse
            // direction at a constant rate; the eased envelope gives the pose
            // a brief, controlled hold without adding torso bounce or scale.
            apexOpen = Mathf.SmoothStep(0f, 1f, apexOpen);
            // The arms point along local X, so shoulder rotation around X only
            // twists the mesh and leaves the silhouette almost unchanged. Use
            // local Z to angle both flippers down at takeoff/descent and open
            // them outward near the apex; blend to a smaller landing brace.
            var jumpArmSpread = Mathf.Lerp(54f, 10f, apexOpen);
            // Begin a small reaching brace only after Splat is descending toward
            // a nearby walkable surface. The motor still owns touchdown and the
            // Land action; this provides a readable transition before contact.
            jumpArmSpread = Mathf.Lerp(jumpArmSpread, 32f, _landingPrepWeight);
            // A small spring trails the actual rise and fall. A free-running
            // oscillator kept flapping through the whole airtime and looked
            // disconnected from the jump; velocity-shaped targets make the
            // flippers counter the launch, settle at the apex, then follow the
            // descent into the brace.
            var wingFollowTarget = Mathf.Clamp(-verticalSpeed * 0.9f, -12f, 12f)
                * (1f - _landingPrepWeight * 0.75f);
            _jumpWingFollow = Mathf.SmoothDamp(_jumpWingFollow, wingFollowTarget,
                ref _jumpWingFollowVelocity, 0.10f, 100f, Time.deltaTime);
            var tipFollowTarget = Mathf.Clamp(_jumpWingFollow * 1.35f, -16f, 16f)
                * (1f - _landingPrepWeight * 0.75f);
            _jumpTipFollow = Mathf.SmoothDamp(_jumpTipFollow, tipFollowTarget,
                ref _jumpTipFollowVelocity, 0.13f, 120f, Time.deltaTime);
            var wingFollow = _jumpWingFollow;
            var wingRoll = wingFollow * 0.35f;
            // This imported skin's leg weights stretch into spikes when the long
            // leg bones rotate deeply. Keep the airborne tuck compact and let the
            // arms and torso carry most of the jump silhouette.
            var tuck = Mathf.Max(launch, apexOpen * 0.72f);
            var legCurl = Mathf.Lerp(-2f, 12f, tuck);
            legCurl = Mathf.Lerp(legCurl, -5f, _landingPrepWeight);
            // Keep the airborne legs compact: large root offsets tugged the
            // weighted lower-body skin downward and read like vertical stretching.
            // The open flippers and antenna already carry the jump silhouette.
            var alternatingTuck = Mathf.Sin(_phase * 4.2f) * (2f + apexOpen * 2f);
            // Keep the procedural leg lift compact so this layer does not pull
            // the shared lower-body skin into a tall silhouette. The authored
            // Jump clip also moves the leg roots, and its blend combines with
            // this pose; the combined deformation needs a current capture review.
            var legTuck = Mathf.Lerp(0.012f, 0.035f, Mathf.Max(launch, apexOpen));
            // Extend the rigged legs toward the detected support during the
            // final descent. A tilted-pad capture measured the right sole
            // 109 mm above its local target on the first Land frame, reaching
            // the 100 mm correction cap before settling on the next frame.
            // Keep the extra reach gated by the near-support/descent weight so
            // the airborne shape stays compact until landing is close.
            legTuck = Mathf.Lerp(legTuck, -0.075f, _landingPrepWeight);
            var legDepthCycle = Mathf.Sin(_phase * 3.6f) * 0.012f;
            // Retain only a small velocity-driven roll over the pose's clear
            // takeoff/glide/landing silhouette changes.
            var armBank = Mathf.Lerp(2f, 6f, apexOpen);
            // Keep the torso close to its bind alignment: this skin's midsection
            // weights are not built for a deep articulated bend and can look
            // vertically pulled in the takeoff pose.
            var bodyPitch = Mathf.Lerp(2f, -5f, launch);

            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, 0.008f + launch * 0.012f));
            SoftRot(_hips, _hipsRest);
            SoftPos(_legL, _legLRestPos + LocalWorldUpOffset(_legL, legTuck)
                + LocalWorldForwardOffset(_legL, legDepthCycle));
            SoftPos(_legR, _legRRestPos + LocalWorldUpOffset(_legR, legTuck)
                + LocalWorldForwardOffset(_legR, -legDepthCycle));
            SoftRot(_spine, _spineRest * Quaternion.Euler(bodyPitch, 0f, 0f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(bodyPitch * 0.4f, 0f, 0f));
            SoftRot(_head, _headRest * Quaternion.Euler(-bodyPitch * 0.55f, 0f, 0f));
            // Give the soft antenna a readable trailing arc through takeoff and
            // descent. Keep this isolated from body scale/pitch so jump squash
            // never turns into the old extreme vertical stretch.
            var antennaLag = Mathf.Clamp(-verticalSpeed * 1.65f, -22f, 22f);
            var antennaFlutter = Mathf.Sin(_phase * 6.2f) * 4f;
            SoftAntenna(0f, antennaLag + antennaFlutter);
            SoftRot(_armL, _armLRest * Quaternion.Euler(-34f + wingFollow * 0.35f, 0f,
                jumpArmSpread + armBank + wingRoll));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-34f - wingFollow * 0.35f, 0f,
                -jumpArmSpread - armBank - wingRoll));
            var tipFollow = _jumpTipFollow;
            var tipBrace = _landingPrepWeight * 18f;
            SoftRot(_armTipL, _armTipLRest * Quaternion.Euler(6f + tipFollow + tipBrace,
                0f, -7f - wingRoll * 0.2f));
            SoftRot(_armTipR, _armTipRRest * Quaternion.Euler(6f - tipFollow + tipBrace,
                0f, 7f + wingRoll * 0.2f));
            SoftRot(_legL, _legLRest * Quaternion.Euler(legCurl + alternatingTuck, 0f, 0f));
            SoftRot(_legR, _legRRest * Quaternion.Euler(legCurl - alternatingTuck, 0f, 0f));
            var footTuck = Mathf.Lerp(-10f, 3f, _landingPrepWeight);
            SoftRot(_footL, _footLRest * Quaternion.Euler(footTuck + alternatingTuck * 0.25f, 0f, 0f));
            SoftRot(_footR, _footRRest * Quaternion.Euler(footTuck - alternatingTuck * 0.25f, 0f, 0f));
        }

        private void ResetJumpWingFollow()
        {
            _jumpWingFollow = 0f;
            _jumpWingFollowVelocity = 0f;
            _jumpTipFollow = 0f;
            _jumpTipFollowVelocity = 0f;
        }

        private float EvaluateLandingPrepWeight(float verticalSpeed)
        {
            if (motor == null || motor.IsGrounded || verticalSpeed > -0.6f) return 0f;
            var controller = motor.GetComponent<CharacterController>();
            if (controller == null || !controller.enabled) return 0f;

            var feetBottom = controller.bounds.min.y;
            var origin = new Vector3(controller.bounds.center.x,
                feetBottom + 0.12f, controller.bounds.center.z);
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _landingGroundHits,
                LandingPrepDistance + 0.12f, motor.GroundSupportMask,
                QueryTriggerInteraction.Ignore);
            var nearestClearance = float.PositiveInfinity;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _landingGroundHits[i];
                if (hit.collider == null || hit.normal.y < 0.65f
                    || hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                var clearance = feetBottom - hit.point.y;
                if (clearance < -0.08f || clearance >= nearestClearance) continue;
                nearestClearance = clearance;
            }

            if (float.IsInfinity(nearestClearance)) return 0f;
            var distanceWeight = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(LandingPrepDistance, 0.12f,
                    Mathf.Clamp(nearestClearance, 0f, LandingPrepDistance)));
            var descentWeight = Mathf.InverseLerp(0.6f, 5.5f, -verticalSpeed);
            return distanceWeight * descentWeight;
        }

        private void PoseLand()
        {
            // Absorb the impact, give Splat one small rebound, then settle. Keep
            // the feet in place while the hips compress so the landing reads as
            // contact with the platform instead of a floating body squash.
            var duration = Mathf.Max(0.01f, _actionUntil - _actionStartedAt);
            var progress = Mathf.Clamp01((Time.time - _actionStartedAt) / duration);
            var elapsed = Mathf.Max(0f, Time.time - _actionStartedAt);
            // Use one compact, critically damped impact/recovery beat instead
            // of a cosine rebound that made the whole body feel jelly-like.
            // Keep the impact readable for a few frames after touchdown. The
            // previous 12/s decay had almost returned to idle by the first
            // gameplay frame we captured, so the landing looked like a frozen
            // pose despite being driven by the motor's Land event.
            // Keep the impact readable through the first recovery beat. The old
            // envelope dropped below half strength almost immediately, so the
            // landing could look like a static pose at normal gameplay distance.
            // A monotonic critically damped envelope keeps the compression visible
            // through touchdown while returning without a second vertical bounce.
            // The feet continue to counter the pelvis travel during that recovery.
            var settleTime = elapsed * 5.5f;
            var settle = (1f + settleTime) * Mathf.Exp(-settleTime);
            settle *= 1f - Mathf.SmoothStep(0.88f, 1f, progress);
            var impact = _landingImpactStrength;
            var maxLandingCompression = 0.085f + impact * 0.025f;
            var landingCompression = maxLandingCompression * settle;
            var armCounterbalance = Mathf.Exp(-elapsed * 4.5f) * 8f * impact;
            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, -landingCompression));
            SoftPos(_legL, _legLRestPos + LocalWorldUpOffset(_legL, landingCompression));
            SoftPos(_legR, _legRRestPos + LocalWorldUpOffset(_legR, landingCompression));
            SoftRot(_spine, _spineRest * Quaternion.Euler((10f + impact * 4f) * settle, 0f, 0f));
            SoftRot(_chest, _chestRest * Quaternion.Euler((4f + impact * 2f) * settle, 0f, 0f));
            SoftRot(_head, _headRest * Quaternion.Euler(2f * settle, 0f, 0f));
            SoftAntenna((18f + impact * 8f) * settle, 0f);
            SoftRot(_armL, _armLRest * Quaternion.Euler((24f + impact * 4f) * settle, 0f,
                (18f + impact * 3f) * settle - armCounterbalance * 0.25f));
            SoftRot(_armR, _armRRest * Quaternion.Euler((24f + impact * 4f) * settle, 0f,
                (-18f - impact * 3f) * settle + armCounterbalance * 0.25f));
            // Let the flipper tips lag once and settle with the body. A fast,
            // 24-degree sine made the landing visibly buzz, reinforcing the
            // gelatin motion the rest of this damped landing is meant to avoid.
            _landingTipFollowThrough = 7f * settle * impact;
            _landingTipCounterbalance = armCounterbalance;
            SoftRot(_legL, _legLRest * Quaternion.Euler(-7f * settle, 0f, 0f));
            SoftRot(_legR, _legRRest * Quaternion.Euler(-7f * settle, 0f, 0f));
            SoftRot(_footL, _footLRest * Quaternion.Euler(4f * settle, 0f, 0f));
            SoftRot(_footR, _footRRest * Quaternion.Euler(4f * settle, 0f, 0f));
        }

        /// <summary>Brief directional flinch layered before the normal airborne pose resumes.</summary>
        public void PlayHitReaction(Vector3 worldKnockbackDirection)
        {
            var recoil = -worldKnockbackDirection;
            recoil.y = 0f;
            _hitRecoilLocalDirection = recoil.sqrMagnitude > 0.001f
                ? transform.InverseTransformDirection(recoil.normalized)
                : Vector3.back;
            PlayAction(Action.Hit, 0.30f);
        }

        private void PoseHit()
        {
            // A short, critically damped flinch makes the impact readable without
            // adding a second bounce or changing any rig/body scale.
            var elapsed = Mathf.Max(0f, Time.time - _actionStartedAt);
            // Keep the recoil visible for more than a handful of frames at 30 fps.
            // The body remains scale-free; directional rotation and counterbalancing
            // flippers carry the impact instead of a jelly-like squash.
            var impulse = (1f + elapsed * 8f) * Mathf.Exp(-elapsed * 8f);
            var pitch = -_hitRecoilLocalDirection.z * 14f * impulse;
            var roll = _hitRecoilLocalDirection.x * 16f * impulse;
            SoftRot(_hips, _hipsRest);
            SoftRot(_spine, _spineRest * Quaternion.Euler(pitch, 0f, roll));
            SoftRot(_chest, _chestRest * Quaternion.Euler(pitch * 0.55f, 0f, roll * 0.65f));
            SoftRot(_head, _headRest * Quaternion.Euler(-pitch * 0.45f, 0f, -roll * 0.5f));
            SoftRot(_armL, _armLRest * Quaternion.Euler(-12f * impulse, 0f,
                -18f * impulse - roll * 0.25f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-12f * impulse, 0f,
                18f * impulse - roll * 0.25f));
            SoftRot(_armTipL, _armTipLRest * Quaternion.Euler(8f * impulse, 0f, -6f * impulse));
            SoftRot(_armTipR, _armTipRRest * Quaternion.Euler(8f * impulse, 0f, 6f * impulse));
            SoftPos(_legL, _legLRestPos + LocalWorldUpOffset(_legL, 0.018f * impulse));
            SoftPos(_legR, _legRRestPos + LocalWorldUpOffset(_legR, 0.018f * impulse));
            SoftRot(_legL, _legLRest * Quaternion.Euler(4f * impulse, 0f, 0f));
            SoftRot(_legR, _legRRest * Quaternion.Euler(4f * impulse, 0f, 0f));
            SoftRot(_footL, _footLRest * Quaternion.Euler(-2f * impulse, 0f, 0f));
            SoftRot(_footR, _footRRest * Quaternion.Euler(-2f * impulse, 0f, 0f));
            SoftAntenna(12f * impulse, -roll * 0.75f);
        }

        private void PoseInteract()
        {
            SoftRot(_spine, _spineRest * Quaternion.Euler(10f, 0f, 0f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(8f, 18f, 0f));
            SoftRot(_head, _headRest * Quaternion.Euler(-6f, 20f, 0f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-70f, 25f, 10f));
            SoftRot(_armL, _armLRest * Quaternion.Euler(15f, 0f, -12f));
            SoftAntenna(0f, 20f);
            ResetLegs();
        }

        private void PoseOops()
        {
            var elapsed = Mathf.Max(0f, Time.time - _actionStartedAt);
            var wobble = Mathf.Sin(elapsed * 5f) * Mathf.Clamp01(1f - elapsed / 0.78f);
            var shrug = Mathf.Sin(Mathf.Clamp01(elapsed / 0.2f) * Mathf.PI);
            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, 0.02f + shrug * 0.035f));
            SoftRot(_hips, _hipsRest * Quaternion.Euler(2f, 0f, 0f));
            SoftRot(_spine, _spineRest * Quaternion.Euler(4f, 0f, -wobble * 2f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(0f, wobble * 3f, -5f));
            SoftRot(_head, _headRest * Quaternion.Euler(-5f, wobble * 7f, -11f + wobble * 2f));
            SoftAntenna(0f, -28f + wobble * 12f);
            SoftRot(_armL, _armLRest * Quaternion.Euler(-42f, 0f, -46f - wobble * 3f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-20f, 0f, 42f + wobble * 3f));
            ResetLegs();
        }

        private void PoseCraft()
        {
            // Start from a repeatable anticipation beat and fit two rhythmic
            // left/right cycles into the actual build-complete response.
            var elapsed = CraftActionElapsed;
            var cycleAngle = elapsed * CraftStrokeCycles * (Mathf.PI * 2f / CraftActionSeconds);
            var assemblyBeat = Mathf.Sin(cycleAngle);
            var leftWork = Mathf.Max(0f, assemblyBeat);
            var rightWork = Mathf.Max(0f, -assemblyBeat);
            var spin = assemblyBeat * 10f;
            var anticipation = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.14f, elapsed));
            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips,
                0.003f + Mathf.Abs(assemblyBeat) * 0.005f));
            SoftRot(_spine, _spineRest * Quaternion.Euler(-8f - anticipation * 3f,
                assemblyBeat * 6f, -assemblyBeat * 3f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(0f, spin, 0f));
            SoftRot(_head, _headRest * Quaternion.Euler(-14f - anticipation * 4f,
                assemblyBeat * 11f, -assemblyBeat * 5f));
            SoftRot(_armL, _armLRest * Quaternion.Euler(-26f - leftWork * 46f + rightWork * 10f,
                assemblyBeat * 14f, -35f + assemblyBeat * 18f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-26f - rightWork * 46f + leftWork * 10f,
                -assemblyBeat * 14f, 35f + assemblyBeat * 18f));
            SoftAntenna(0f, Mathf.Sin(_phase * 8f) * 9f);
            ResetLegs();
        }

        private float CraftActionElapsed
        {
            get
            {
                var elapsed = Mathf.Max(0f, Time.time - _actionStartedAt);
                // Long staged captures repeat the action for review. The live
                // build event is 1.1 s and plays once before returning to idle.
                return _actionUntil - _actionStartedAt > CraftActionSeconds * 2f
                    ? Mathf.Repeat(elapsed, CraftActionSeconds)
                    : elapsed;
            }
        }

        private void PoseDiscover()
        {
            // Give every discovery a small character-specific response: Splat bounces
            // with the spring, inspects the dish, lifts the crystal, or beams at a wonder.
            var elapsed = Mathf.Max(0f, Time.time - _actionStartedAt);
            var reactionDuration = _discoveryKind == DiscoveryKind.Wonder ? 1.35f : 0.82f;
            var pulse = Mathf.Sin(elapsed * 6f) * Mathf.Clamp01(1f - elapsed / reactionDuration);
            var bounce = Mathf.Sin(Mathf.Clamp01(elapsed / 0.24f) * Mathf.PI);
            var hipsPop = 0.035f;
            var bodyPitch = -8f;
            var chestYaw = pulse * 6f;
            var headPitch = -10f;
            var antennaWiggle = Mathf.Sin(elapsed * 9f) * 10f;
            var leftArmLift = -48f;
            var rightArmLift = -58f;
            var leftArmSpread = -30f;
            var rightArmSpread = 32f;

            switch (_discoveryKind)
            {
                case DiscoveryKind.Spring:
                    hipsPop += bounce * 0.045f;
                    bodyPitch = -5f;
                    chestYaw = pulse * 10f;
                    antennaWiggle = Mathf.Sin(elapsed * 6f) * 8f;
                    leftArmLift = -58f;
                    rightArmLift = -72f;
                    break;
                case DiscoveryKind.Dish:
                    hipsPop += bounce * 0.035f;
                    bodyPitch = -15f;
                    chestYaw = Mathf.Sin(elapsed * 4.5f) * 7f;
                    headPitch = -18f;
                    antennaWiggle = Mathf.Sin(elapsed * 7f) * 7f;
                    leftArmLift = -18f;
                    rightArmLift = -72f;
                    leftArmSpread = -12f;
                    rightArmSpread = 20f;
                    break;
                case DiscoveryKind.Crystal:
                    hipsPop += bounce * 0.04f;
                    bodyPitch = -3f;
                    chestYaw = pulse * 5f;
                    headPitch = -15f;
                    antennaWiggle = Mathf.Sin(elapsed * 7f) * 8f;
                    leftArmLift = -62f;
                    rightArmLift = -68f;
                    leftArmSpread = -22f;
                    rightArmSpread = 24f;
                    break;
                case DiscoveryKind.Wonder:
                    hipsPop += Mathf.Abs(Mathf.Sin(elapsed * 4f)) * 0.025f
                        * Mathf.Clamp01(1f - elapsed / reactionDuration);
                    bodyPitch = -4f;
                    chestYaw = Mathf.Sin(elapsed * 4f) * 4f;
                    headPitch = -17f;
                    antennaWiggle = Mathf.Sin(elapsed * 7f) * 9f;
                    leftArmLift = -70f;
                    rightArmLift = -76f;
                    leftArmSpread = -40f;
                    rightArmSpread = 42f;
                    break;
            }

            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, hipsPop));
            SoftRot(_hips, _hipsRest * Quaternion.Euler(-5f, 0f, 0f));
            SoftRot(_spine, _spineRest * Quaternion.Euler(bodyPitch, 0f, pulse * 3f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(-4f, chestYaw, 0f));
            SoftRot(_head, _headRest * Quaternion.Euler(headPitch, pulse * 3f, -pulse * 2f));
            SoftAntenna(0f, antennaWiggle);
            SoftRot(_armL, _armLRest * Quaternion.Euler(leftArmLift, 0f, leftArmSpread - pulse * 5f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(rightArmLift, 0f, rightArmSpread + pulse * 5f));
            ResetLegs();
        }

        private void PoseCelebrate()
        {
            var bounce = Mathf.Abs(Mathf.Sin(_phase * 2.5f)) * 0.035f;
            var wave = Mathf.Sin(_phase * 3f) * 5f;
            var lookYaw = 0f;
            if (_celebrationLookTarget != null)
            {
                var towardBeacon = _celebrationLookTarget.position - transform.position;
                towardBeacon.y = 0f;
                if (towardBeacon.sqrMagnitude > 0.01f)
                {
                    lookYaw = Mathf.Clamp(Vector3.SignedAngle(transform.forward, towardBeacon.normalized, Vector3.up), -65f, 65f);
                }
            }

            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, bounce));
            SoftRot(_hips, _hipsRest * Quaternion.Euler(-5f, 0f, 0f));
            SoftRot(_spine, _spineRest * Quaternion.Euler(-8f, 0f, 0f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(-5f, lookYaw * 0.3f, Mathf.Sin(_phase * 3f) * 2f));
            SoftRot(_head, _headRest * Quaternion.Euler(-8f, lookYaw * 0.62f, Mathf.Sin(_phase * 2.5f) * 4f));
            SoftAntenna(0f, Mathf.Sin(_phase * 4f) * 6f);
            SoftRot(_armL, _armLRest * Quaternion.Euler(-92f + wave, 0f, -28f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(-92f - wave, 0f, 28f));
            var legCurl = Mathf.Abs(Mathf.Sin(_phase * 2.5f)) * 6f;
            SoftRot(_legL, _legLRest * Quaternion.Euler(legCurl, 0f, 0f));
            SoftRot(_legR, _legRRest * Quaternion.Euler(legCurl, 0f, 0f));
        }

        private void PoseListen()
        {
            var lookYaw = 0f;
            if (_celebrationLookTarget != null)
            {
                var towardBeacon = _celebrationLookTarget.position - transform.position;
                towardBeacon.y = 0f;
                if (towardBeacon.sqrMagnitude > 0.01f)
                {
                    lookYaw = Mathf.Clamp(Vector3.SignedAngle(transform.forward, towardBeacon.normalized, Vector3.up), -65f, 65f);
                }
            }

            var breath = Mathf.Sin(_phase * 2.8f) * 0.015f;
            SoftPos(_hips, _hipsRestPos + LocalWorldUpOffset(_hips, breath));
            SoftRot(_hips, _hipsRest);
            SoftRot(_spine, _spineRest * Quaternion.Euler(-5f, 0f, 0f));
            SoftRot(_chest, _chestRest * Quaternion.Euler(0f, lookYaw * 0.28f, -3f));
            SoftRot(_head, _headRest * Quaternion.Euler(-3f, lookYaw * 0.65f, -9f));
            SoftAntenna(0f, -18f + Mathf.Sin(_phase * 4f) * 3f);
            SoftRot(_armL, _armLRest * Quaternion.Euler(-34f, 0f, -20f));
            SoftRot(_armR, _armRRest * Quaternion.Euler(12f, 0f, 8f));
            ResetLegs();
        }

        private void SoftAntenna(float pitchDegrees, float rollDegrees)
        {
            // The current pose already supplies expressive angle targets. A
            // short critically damped response adds a soft lag on transitions
            // and direction changes while returning cleanly to each target.
            var deltaTime = Time.deltaTime;
            _antennaPitch = Mathf.SmoothDampAngle(_antennaPitch, pitchDegrees,
                ref _antennaPitchVelocity, AntennaFollowTime, Mathf.Infinity, deltaTime);
            _antennaRoll = Mathf.SmoothDampAngle(_antennaRoll, rollDegrees,
                ref _antennaRollVelocity, AntennaFollowTime, Mathf.Infinity, deltaTime);
            SoftRot(_antenna, _antennaRest * Quaternion.Euler(_antennaPitch, 0f, _antennaRoll));
        }

        private void SoftRot(Transform t, Quaternion target)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, target, _poseBlend);
        }

        private void SoftPos(Transform t, Vector3 target)
        {
            if (t == null) return;
            t.localPosition = Vector3.Lerp(t.localPosition, target, _poseBlend);
        }

        private void SoftRunLegPos(Transform t, Vector3 target, float speedBlend)
        {
            if (t == null) return;
            // A fast swing reaches its landing target in fewer frames. Increase
            // only leg-position response with travel speed so the planted anchor
            // is captured closer to the target; preserve softer torso easing.
            var blend = Mathf.Lerp(_poseBlend, 0.8f, speedBlend);
            t.localPosition = Vector3.Lerp(t.localPosition, target, blend);
        }


        private void AnimateBlink()
        {
            var hasMeshEyes = _eyeRootL != null && _eyeRootR != null;
            if (!hasMeshEyes && _paintedFaceRenderer == null)
            {
                return;
            }

            _eyeExpressionScale = Vector3.Lerp(
                _eyeExpressionScale,
                EyeExpressionScale(_action),
                1f - Mathf.Exp(-18f * Time.deltaTime));

            if (!_blinkActive && Time.time >= _nextBlinkAt)
            {
                _blinkActive = true;
                _blinkStartedAt = Time.time;
            }

            if (!_blinkActive)
            {
                _paintedBlink = 0f;
                if (hasMeshEyes)
                {
                    ApplyEyeScale(_eyeExpressionScale);
                }
                return;
            }

            const float closeSeconds = 0.075f;
            const float reopenSeconds = 0.12f;
            var elapsed = Time.time - _blinkStartedAt;
            var amount = elapsed < closeSeconds
                ? elapsed / closeSeconds
                : 1f - (elapsed - closeSeconds) / reopenSeconds;
            amount = Mathf.Clamp01(amount);
            _paintedBlink = Mathf.SmoothStep(0f, 1f, amount);
            var blinkScale = _eyeExpressionScale;
            blinkScale.y *= Mathf.Lerp(1f, 0.12f, Mathf.SmoothStep(0f, 1f, amount));
            if (hasMeshEyes)
            {
                ApplyEyeScale(blinkScale);
            }

            if (elapsed >= closeSeconds + reopenSeconds)
            {
                if (hasMeshEyes)
                {
                    ApplyEyeScale(_eyeExpressionScale);
                }
                _paintedBlink = 0f;
                _blinkActive = false;
                _nextBlinkAt = Time.time + Random.Range(2.8f, 5.2f);
            }
        }

        private void ApplyEyeScale(Vector3 expression)
        {
            _eyeRootL.localScale = Vector3.Scale(_eyeRestScaleL, expression);
            _eyeRootR.localScale = Vector3.Scale(_eyeRestScaleR, expression);
        }

        private Vector3 EyeExpressionScale(Action action)
        {
            var landingBlink = _landingImpactStrength
                // Match the eye follow-through to PoseLand's body settle so
                // the impact expression survives more than one rendered frame.
                * Mathf.Exp(-Mathf.Max(0f, Time.time - _actionStartedAt) * 5.5f);
            return action switch
            {
                Action.Jump => new Vector3(1.02f, 1.08f, 1f),
                Action.Hit => new Vector3(1.07f, 1.12f, 1f),
                Action.Land => new Vector3(1f + landingBlink * 0.04f,
                    1f - landingBlink * 0.42f, 1f),
                Action.Discover when _discoveryKind == DiscoveryKind.Wonder => new Vector3(1.07f, 1.18f, 1f),
                Action.Discover => new Vector3(1.06f, 1.13f, 1f),
                Action.Oops => new Vector3(1.02f, 0.94f, 1f),
                Action.Craft => new Vector3(1.02f, 0.94f, 1f),
                Action.Listen => new Vector3(1.01f, 0.92f, 1f),
                Action.Celebrate => new Vector3(1.07f, 0.78f, 1f),
                _ => Vector3.one,
            };
        }

        private void AnimateGaze()
        {
            var hasMeshIris = _irisL != null && _irisR != null;
            if (!hasMeshIris && _paintedFaceRenderer == null)
            {
                return;
            }

            var targetOffset = Vector2.zero;
            var focused = _interactor != null ? _interactor.CurrentFocus : null;
            if ((_action == Action.Idle || _action == Action.Interact) && focused != null && _head != null)
            {
                var towardTarget = _head.InverseTransformDirection(focused.transform.position - _head.position);
                if (towardTarget.z > 0f)
                {
                    // Keep the iris inside the soft white rim, especially vertically.
                    targetOffset = new Vector2(
                        Mathf.Clamp(towardTarget.x * 0.035f, -0.03f, 0.03f),
                        Mathf.Clamp(towardTarget.y * 0.02f, -0.015f, 0.015f));
                }
            }
            else if (_action == Action.Idle)
            {
                if (Time.time >= _nextGlanceAt)
                {
                    _glanceDirection = Random.value < 0.5f ? -1f : 1f;
                    _glanceUntil = Time.time + Random.Range(0.42f, 0.68f);
                    _nextGlanceAt = _glanceUntil + Random.Range(3.0f, 4.4f);
                }

                if (Time.time < _glanceUntil)
                {
                    targetOffset.x = _glanceDirection * IdleGlanceMaxOffset;
                }
            }
            else if (_action == Action.Craft)
            {
                // Let Splat watch the alternating flipper strokes instead of
                // staring past his work. Keep the pupils inside the painted rim.
                var gazeBeat = Mathf.Sin(CraftActionElapsed * CraftStrokeCycles
                    * (Mathf.PI * 2f / CraftActionSeconds));
                targetOffset = new Vector2(gazeBeat * 0.022f, -0.015f);
            }

            var smoothTime = _action == Action.Idle ? IdleGlanceSmoothTime : 0.11f;
            _gazeOffset = Vector2.SmoothDamp(_gazeOffset, targetOffset, ref _gazeVelocity, smoothTime);
            if (hasMeshIris)
            {
                var offset = new Vector3(_gazeOffset.x, _gazeOffset.y, 0f);
                _irisL.localPosition = _irisRestPosL + offset;
                _irisR.localPosition = _irisRestPosR + offset;
            }
        }

        private void ApplyPaintedFaceProperties()
        {
            if (_paintedFaceRenderer == null)
            {
                return;
            }

            _paintedFaceProperties ??= new MaterialPropertyBlock();
            _paintedFaceRenderer.GetPropertyBlock(_paintedFaceProperties);
            _paintedFaceProperties.SetFloat(EyeBlinkProperty, _paintedBlink);
            _paintedFaceProperties.SetVector(EyeExpressionProperty,
                new Vector4(_eyeExpressionScale.x, _eyeExpressionScale.y, 0f, 0f));
            _paintedFaceProperties.SetVector(EyeGazeProperty,
                new Vector4(_gazeOffset.x, _gazeOffset.y, 0f, 0f));
            _paintedFaceRenderer.SetPropertyBlock(_paintedFaceProperties);
        }

        private void AnimateSmile()
        {
            if (_smile == null)
            {
                return;
            }

            var expression = Vector3.one;
            switch (_action)
            {
                case Action.Run:
                    expression = new Vector3(1.04f, 1.08f, 1f);
                    break;
                case Action.Idle:
                    // Give the resting face a readable, warm smile without adding
                    // extra face pieces or changing the authored mouth mesh.
                    expression = new Vector3(1.18f, 1.3f, 1f);
                    break;
                case Action.Interact:
                    expression = new Vector3(1.12f, 1.2f, 1f);
                    break;
                case Action.Discover:
                    expression = _discoveryKind switch
                    {
                        DiscoveryKind.Spring => new Vector3(1.18f, 1.3f, 1f),
                        DiscoveryKind.Dish => new Vector3(1.12f, 1.17f, 1f),
                        DiscoveryKind.Crystal => new Vector3(1.2f, 1.32f, 1f),
                        DiscoveryKind.Wonder => new Vector3(1.24f, 1.38f, 1f),
                        _ => new Vector3(1.16f, 1.24f, 1f),
                    };
                    break;
                case Action.Craft:
                    expression = new Vector3(1.18f, 1.32f, 1f);
                    break;
                case Action.Oops:
                    expression = new Vector3(0.92f, 0.9f, 1f);
                    break;
                case Action.Celebrate:
                    expression = new Vector3(1.22f, 1.38f, 1f);
                    break;
            }

            var target = Vector3.Scale(_smileRestScale, expression);
            _smile.localScale = Vector3.Lerp(_smile.localScale, target, _poseBlend);
        }

        private void AnimateMakerPouch()
        {
            if (_makerPouch == null)
            {
                return;
            }

            var elapsed = Time.time - _makerPouchPulseStartedAt;
            var pulse = _action == Action.Craft
                ? Mathf.Max(0f, Mathf.Sin(elapsed * CraftStrokeCycles
                    * (Mathf.PI * 2f / CraftActionSeconds)))
                : elapsed >= 0f && elapsed < MakerPouchPulseDuration
                    ? Mathf.Sin(elapsed / MakerPouchPulseDuration * Mathf.PI)
                    : 0f;
            var playfulSquash = new Vector3(1f + pulse * 0.08f, 1f - pulse * 0.05f, 1f + pulse * 0.07f);
            var target = Vector3.Scale(_makerPouchRestScale, playfulSquash);
            _makerPouch.localScale = Vector3.Lerp(_makerPouch.localScale, target, _poseBlend);

            if (_makerPack != null)
            {
                var packBounce = new Vector3(1f + pulse * 0.04f, 1f - pulse * 0.025f, 1f + pulse * 0.05f);
                var packTarget = Vector3.Scale(_makerPackRestScale, packBounce);
                _makerPack.localScale = Vector3.Lerp(_makerPack.localScale, packTarget, _poseBlend);
            }
        }

        private void HandleJumped() => PlayAction(Action.Jump, 0.35f);

        private void HandleLanded() => PlayAction(Action.Land, 0.52f);

        private void HandleCollect(CollectibleDefinition definition)
        {
            _makerPouchPulseStartedAt = Time.time;
            if (definition != null && definition.Category == CollectibleCategory.Component)
            {
                _discoveryKind = definition.Id switch
                {
                    "component_spring" => DiscoveryKind.Spring,
                    "component_metal_dish" => DiscoveryKind.Dish,
                    "component_power_crystal" => DiscoveryKind.Crystal,
                    _ => DiscoveryKind.Other,
                };
                PlayAction(Action.Discover, 0.82f);
                return;
            }

            PlayAction(Action.Interact, 0.45f);
        }

        public void PlayPickupFlyIn(Vector3 pickupPosition)
        {
            if (_makerPouch == null || _makerPack == null)
            {
                CacheBones();
            }

            // Meshy Splat's approved silhouette does not require a visible
            // maker pack/pouch. Keep the pickup sparkle payoff by aiming at the
            // character's torso (then visual/root as a last resort) when those
            // optional accessories are absent.
            var visibleTarget = _makerPack != null ? _makerPack
                : _makerPouch != null ? _makerPouch
                : _hips != null ? _hips
                : visualRoot != null ? visualRoot
                : transform;
            TempCraftSparkBurst.SpawnPickupArc(pickupPosition, visibleTarget);
        }

        private void HandleWonderClaimed(string _)
        {
            _discoveryKind = DiscoveryKind.Wonder;
            PlayAction(Action.Discover, 1.35f);
        }

        private void HandleBuildRefused() => PlayAction(Action.Oops, 0.78f);

        public void PlayCraftAnimation(float duration)
        {
            _makerPouchPulseStartedAt = Time.time;
            PlayAction(Action.Craft, duration);
        }

        private void HandleCraft(string _) => PlayCraftAnimation(1.1f);

        private void HandleLevelCompleted()
        {
            var beacon = GameObject.Find("ART_GatewayBeacon");
            _celebrationLookTarget = beacon != null ? beacon.transform : null;
            PlayAction(Action.Listen, 8f);
        }

        private void HandleGatewaySignalReceived() => PlayAction(Action.Celebrate, 3.4f);

        private void SampleAuthoredLocomotionClip()
        {
            if (!useAuthoredLocomotionClips || _authoredRigRoot == null
                || (_action != Action.Idle && _action != Action.Run
                    && _action != Action.Jump && _action != Action.Land))
            {
                _authoredTransitionActive = false;
                return;
            }

            var clip = _action switch
            {
                Action.Run => _authoredRunClip,
                Action.Jump => _authoredJumpClip,
                Action.Land => _authoredLandClip,
                _ => _authoredIdleClip,
            };
            if (clip == null || _authoredRigTransforms == null || _authoredRigTransforms.Length == 0)
            {
                _authoredTransitionActive = false;
                return;
            }

            if (_authoredPosePositions == null || _authoredPosePositions.Length != _authoredRigTransforms.Length)
            {
                _authoredPosePositions = new Vector3[_authoredRigTransforms.Length];
                _authoredPoseRotations = new Quaternion[_authoredRigTransforms.Length];
                _authoredPoseScales = new Vector3[_authoredRigTransforms.Length];
            }
            EnsureAuthoredTransitionBuffers();

            var sampleTime = _action switch
            {
                Action.Run => Mathf.Repeat(_runPhase / (Mathf.PI * 2f), 1f) * clip.length,
                Action.Jump => Mathf.Clamp01((Time.time - _jumpStartedAt)
                    / Mathf.Max(0.1f, motor != null ? motor.EstimatedJumpFlightSeconds : 0.8f)) * clip.length,
                Action.Land => Mathf.Clamp01((Time.time - _actionStartedAt)
                    / Mathf.Max(0.05f, _actionUntil - _actionStartedAt)) * clip.length,
                _ => Mathf.Repeat(Time.time / Mathf.Max(0.1f, clip.length), 1f) * clip.length,
            };

            for (var i = 0; i < _authoredRigTransforms.Length; i++)
            {
                _authoredPosePositions[i] = _authoredRigTransforms[i].localPosition;
                _authoredPoseRotations[i] = _authoredRigTransforms[i].localRotation;
                _authoredPoseScales[i] = _authoredRigTransforms[i].localScale;
            }

            clip.SampleAnimation(_authoredRigRoot.gameObject, sampleTime);
            _authoredLocomotionBlend = Mathf.MoveTowards(_authoredLocomotionBlend, 1f,
                Time.deltaTime / 0.12f);
            var transitionWeight = AuthoredLocomotionTransitionWeightForDiagnostics;
            // The legacy authored Jump track opposes the live velocity-shaped
            // PoseJump apex. In matched captures, reducing its blend to zero
            // makes the articulated flight pose visibly open instead of canceling
            // the procedural shoulder arc. Keep this action on the existing rig
            // until an authored replacement is retimed to match that arc. Keep
            // Land partly procedural so its brief contact settle remains visible.
            var authoredPoseWeight = _action switch
            {
                Action.Jump => 0f,
                // Landing is a brief contact beat; let more of its existing
                // damped compression survive the clip blend so touchdown reads
                // as weight transfer rather than an upright pose change.
                Action.Land => 0.55f,
                _ => 1f,
            };
            for (var i = 0; i < _authoredRigTransforms.Length; i++)
            {
                var bone = _authoredRigTransforms[i];
                if (_action == Action.Idle && IsProceduralIdleDetail(bone))
                {
                    bone.localPosition = _authoredPosePositions[i];
                    bone.localRotation = _authoredPoseRotations[i];
                    continue;
                }
                var clipPosition = bone.localPosition;
                var clipRotation = bone.localRotation;
                var targetPosition = Vector3.Lerp(_authoredPosePositions[i], clipPosition,
                    _authoredLocomotionBlend * authoredPoseWeight);
                var targetRotation = Quaternion.Slerp(_authoredPoseRotations[i], clipRotation,
                    _authoredLocomotionBlend * authoredPoseWeight);
                if (_authoredTransitionActive)
                {
                    targetPosition = Vector3.Lerp(_authoredTransitionFromPositions[i],
                        targetPosition, transitionWeight);
                    targetRotation = Quaternion.Slerp(_authoredTransitionFromRotations[i],
                        targetRotation, transitionWeight);
                }
                bone.localPosition = targetPosition;
                bone.localRotation = targetRotation;
                // Imported clips may contain scale keys even when their authored
                // intent is pose-only. Preserve the current scale (including any
                // procedural expression already applied this frame) so a clip key
                // cannot stretch the skinned body or a parent bone by multiples.
                bone.localScale = _authoredPoseScales[i];
            }
            if (transitionWeight >= 1f) _authoredTransitionActive = false;
        }

        private void BeginAuthoredLocomotionTransition(Action nextAction)
        {
            var isLocomotion = nextAction == Action.Idle || nextAction == Action.Run
                || nextAction == Action.Jump || nextAction == Action.Land;
            if (!isLocomotion || !useAuthoredLocomotionClips || !_hasAuthoredLastPose
                || _authoredRigTransforms == null)
            {
                _authoredTransitionActive = false;
                return;
            }

            EnsureAuthoredTransitionBuffers();
            System.Array.Copy(_authoredLastPosePositions, _authoredTransitionFromPositions,
                _authoredRigTransforms.Length);
            System.Array.Copy(_authoredLastPoseRotations, _authoredTransitionFromRotations,
                _authoredRigTransforms.Length);
            _authoredTransitionStartedAt = Time.time;
            // Let a landed pose release into the planted idle more gently than
            // a run/jump state change. Splat's idle and landing clips can place
            // the shoe vertices at slightly different heights; a longer blend
            // gives the grounded foot solver time to follow that change without
            // making every turn or takeoff feel laggy.
            _authoredTransitionDurationSeconds = nextAction == Action.Idle
                && _lastTrackedAction == Action.Land
                ? 0.22f
                : AuthoredActionTransitionSeconds;
            _authoredTransitionActive = true;
        }

        private void CacheAuthoredPoseForNextTransition()
        {
            if (_authoredRigTransforms == null || _authoredRigTransforms.Length == 0) return;
            EnsureAuthoredTransitionBuffers();
            for (var i = 0; i < _authoredRigTransforms.Length; i++)
            {
                var bone = _authoredRigTransforms[i];
                _authoredLastPosePositions[i] = bone.localPosition;
                _authoredLastPoseRotations[i] = bone.localRotation;
            }
            _hasAuthoredLastPose = true;
        }

        private void EnsureAuthoredTransitionBuffers()
        {
            if (_authoredRigTransforms == null) return;
            var count = _authoredRigTransforms.Length;
            if (_authoredLastPosePositions == null || _authoredLastPosePositions.Length != count)
            {
                _authoredLastPosePositions = new Vector3[count];
                _authoredLastPoseRotations = new Quaternion[count];
                _authoredTransitionFromPositions = new Vector3[count];
                _authoredTransitionFromRotations = new Quaternion[count];
                _hasAuthoredLastPose = false;
                _authoredTransitionActive = false;
            }
        }

        private bool IsProceduralIdleDetail(Transform bone)
        {
            return bone == _armL || bone == _armR || bone == _armTipL || bone == _armTipR
                || bone == _eyeRootL || bone == _eyeRootR || bone == _irisL || bone == _irisR
                || bone == _smile;
        }

        private static AnimationClip LoadAuthoredClip(string resourcePath)
        {
            var clips = Resources.LoadAll<AnimationClip>(resourcePath);
            for (var i = 0; i < clips.Length; i++)
                if (clips[i] != null && !clips[i].name.StartsWith("__preview__"))
                    return clips[i];
            return null;
        }

        private void CacheBones()
        {
            if (_interactor == null)
            {
                _interactor = GetComponent<SplatInteractor>();
            }

            if (visualRoot == null || visualRoot == transform)
            {
                var found = transform.Find(OrganicSplatBuilder.VisualRootName);
                visualRoot = found != null ? found : transform;
            }

            _hips = FindBone(OrganicSplatBuilder.HipsName);
            _spine = FindBone(OrganicSplatBuilder.SpineName);
            _chest = FindBone(OrganicSplatBuilder.ChestName);
            _makerPouch = FindBone("ART_MakerPouch");
            _makerPack = FindBone("ART_MakerPack");
            _head = FindBone(OrganicSplatBuilder.HeadName);
            _antenna = FindBone(OrganicSplatBuilder.AntennaName);
            _armL = FindBone(OrganicSplatBuilder.ArmLName);
            _armR = FindBone(OrganicSplatBuilder.ArmRName);
            _armTipL = FindBone("Bone_ArmTipL");
            _armTipR = FindBone("Bone_ArmTipR");
            _legL = FindBone("Bone_LegL");
            _legR = FindBone("Bone_LegR");
            _footL = FindBone("Bone_FootL");
            _footR = FindBone("Bone_FootR");
            _eyeRootL = FindBone("ART_EyeRootL");
            _eyeRootR = FindBone("ART_EyeRootR");
            _irisL = FindBone("ART_IrisL") ?? FindBone("ART_Iris");
            _irisR = FindBone("ART_IrisR");
            _smile = FindBone("ART_SplatSmile");
            _authoredRigRoot = FindBone("Splat_SkinRig");
            if (!_authoredClipsLoadAttempted && _authoredRigRoot != null)
            {
                _authoredClipsLoadAttempted = true;
                const string resourceRoot = "Characters/SplatAnimationIter1161/";
                _authoredIdleClip = LoadAuthoredClip("Characters/SplatAnimationIter1301/Idle")
                    ?? LoadAuthoredClip(resourceRoot + "Idle");
                _authoredRunClip = LoadAuthoredClip(resourceRoot + "Run");
                _authoredJumpClip = LoadAuthoredClip(resourceRoot + "Jump");
                var iter1332LandClip = LoadAuthoredClip("Characters/SplatAnimationIter1332/Land");
                _authoredLandClip = iter1332LandClip
                    ?? LoadAuthoredClip("Characters/SplatAnimationIter1313/Land")
                    ?? LoadAuthoredClip("Characters/SplatAnimationIter1204/Land")
                    ?? LoadAuthoredClip("Characters/SplatAnimationIter1203/Land")
                    ?? LoadAuthoredClip(resourceRoot + "Land");
                _authoredRigTransforms = _authoredRigRoot.GetComponentsInChildren<Transform>(true);
                Debug.Log($"[Splat animation] authored locomotion clips: idle={_authoredIdleClip != null} ({_authoredIdleClip?.name ?? "none"}, {_authoredIdleClip?.length ?? 0f:F2}s), run={_authoredRunClip != null}, jump={_authoredJumpClip != null}, land={_authoredLandClip != null} ({_authoredLandClip?.name ?? "none"}, source={(iter1332LandClip != null ? "Iter1332" : "fallback")}), rigBones={_authoredRigTransforms.Length}.");
            }
            _paintedFaceRenderer = null;
            if (visualRoot != null)
            {
                var skinnedRenderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (var i = 0; i < skinnedRenderers.Length; i++)
                {
                    if (skinnedRenderers[i] != null && skinnedRenderers[i].name.Contains("ART_SplatBody_Iter"))
                    {
                        _paintedFaceRenderer = skinnedRenderers[i];
                        break;
                    }
                }
            }
            if (_irisR == null && _eyeRootR != null)
            {
                var rightEyeChildren = _eyeRootR.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < rightEyeChildren.Length; i++)
                {
                    if (rightEyeChildren[i].name == "ART_Iris")
                    {
                        _irisR = rightEyeChildren[i];
                        break;
                    }
                }
            }

            if (_eyeRootL != null) _eyeRestScaleL = _eyeRootL.localScale;
            if (_eyeRootR != null) _eyeRestScaleR = _eyeRootR.localScale;
            if (_irisL != null) _irisRestPosL = _irisL.localPosition;
            if (_irisR != null) _irisRestPosR = _irisR.localPosition;
            if (_smile != null) _smileRestScale = _smile.localScale;
            if (_makerPouch != null) _makerPouchRestScale = _makerPouch.localScale;
            if (_makerPack != null) _makerPackRestScale = _makerPack.localScale;
            if (_nextBlinkAt <= 0f) _nextBlinkAt = Time.time + Random.Range(2.2f, 4.4f);
            if (_nextGlanceAt <= 0f) _nextGlanceAt = Time.time + Random.Range(1.8f, 2.6f);
            if (_nextIdleGreetingAt <= 0f)
                _nextIdleGreetingAt = Time.time + 0.35f + Random.Range(0.65f, 1.0f);

            if (_hips != null)
            {
                _hipsRest = _hips.localRotation;
                _hipsRestPos = _hips.localPosition;
            }

            if (_spine != null) _spineRest = _spine.localRotation;
            if (_chest != null) _chestRest = _chest.localRotation;
            if (_head != null) _headRest = _head.localRotation;
            if (_antenna != null) _antennaRest = _antenna.localRotation;
            if (_armL != null) _armLRest = _armL.localRotation;
            if (_armR != null) _armRRest = _armR.localRotation;
            if (_armTipL != null) _armTipLRest = _armTipL.localRotation;
            if (_armTipR != null) _armTipRRest = _armTipR.localRotation;
            if (_legL != null) _legLRest = _legL.localRotation;
            if (_legR != null) _legRRest = _legR.localRotation;
            if (_footL != null) _footLRest = _footL.localRotation;
            if (_footR != null) _footRRest = _footR.localRotation;
            if (_legL != null) _legLRestPos = _legL.localPosition;
            if (_legR != null) _legRRestPos = _legR.localPosition;
            if (_footL != null) _footLRestPos = _footL.localPosition;
            if (_footR != null) _footRRestPos = _footR.localPosition;
            if (!_hasAuthoredLegRestPositions && _legL != null && _legR != null)
            {
                _legLAuthoredRestPos = _legL.localPosition;
                _legRAuthoredRestPos = _legR.localPosition;
                _hasAuthoredLegRestPositions = true;
            }
        }

        private void RestoreAuthoredLegRestPositions()
        {
            if (!_hasAuthoredLegRestPositions) return;
            _legLRestPos = _legLAuthoredRestPos;
            _legRRestPos = _legRAuthoredRestPos;
        }

        private void ResetLegs()
        {
            SoftRot(_legL, _legLRest);
            SoftRot(_legR, _legRRest);
            SoftRot(_footL, _footLRest);
            SoftRot(_footR, _footRRest);
            ResetLegPositions();
        }

        private void ResetLegPositions()
        {
            SoftPos(_legL, _legLRestPos);
            SoftPos(_legR, _legRRestPos);
            SoftPos(_footL, _footLRestPos);
            SoftPos(_footR, _footRRestPos);
        }

        private static Vector3 LocalWorldUpOffset(Transform bone, float distance)
        {
            if (bone == null || bone.parent == null || Mathf.Abs(distance) <= 0.0001f)
            {
                return Vector3.zero;
            }

            return bone.parent.InverseTransformVector(Vector3.up * distance);
        }

        private Vector3 LocalWorldForwardOffset(Transform bone, float distance)
        {
            if (bone == null || bone.parent == null || Mathf.Abs(distance) <= 0.0001f)
            {
                return Vector3.zero;
            }

            var characterForward = visualRoot != null ? visualRoot.forward : transform.forward;
            characterForward = Vector3.ProjectOnPlane(characterForward, Vector3.up).normalized;
            if (characterForward.sqrMagnitude <= 0.0001f)
            {
                characterForward = transform.forward;
            }

            return bone.parent.InverseTransformVector(characterForward * distance);
        }

        private Vector3 LocalWorldRightOffset(Transform bone, float distance)
        {
            if (bone == null || bone.parent == null || Mathf.Abs(distance) <= 0.0001f)
            {
                return Vector3.zero;
            }

            var characterRight = visualRoot != null ? visualRoot.right : transform.right;
            characterRight = Vector3.ProjectOnPlane(characterRight, Vector3.up).normalized;
            if (characterRight.sqrMagnitude <= 0.0001f)
            {
                characterRight = transform.right;
            }

            return bone.parent.InverseTransformVector(characterRight * distance);
        }

        private Transform FindBone(string name)
        {
            if (visualRoot == null)
            {
                return null;
            }

            var direct = visualRoot.Find(name);
            if (direct != null)
            {
                return direct;
            }

            var all = visualRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                {
                    return all[i];
                }
            }

            return null;
        }
    }
}
