using UnityEngine;
using Unity.Profiling;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Drives the imported Meshy spider clips from the existing ScrapMite AI.
    /// This drives the compatible imported rig used by the RainbowSpider
    /// candidate and the single promoted World 02 ScrapMite encounter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RainbowSpiderAnimatorDriver : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("RainbowSpiderAnimatorDriver.Update");
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
        private static readonly int WalkRateId = Animator.StringToHash("WalkRate");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DefeatId = Animator.StringToHash("Defeat");
        private static readonly int IdleStateId = Animator.StringToHash("Base Layer.Idle");
        private static readonly int AttackStateId = Animator.StringToHash("Base Layer.Attack");
        private static readonly int HitStateId = Animator.StringToHash("Base Layer.Hit");

        [SerializeField] private Animator animator;
        [SerializeField] private ScrapMite mite;
        [SerializeField, Min(0f)] private float speedDampSeconds = 0.10f;
        [SerializeField, Range(0f, 1f)] private float hitTriggerThreshold = 0.55f;

        // The commissioned spider has no authored wind-up clip. Blend a rig-local
        // front-leg brace during the AI telegraph so the player gets an early
        // directional tell before the imported Attack clip is triggered. Alternate
        // the lead leg on repeated lunges to avoid a mirrored, mechanical pose.
        // Offsets are applied after Mecanim poses the real bones; no mesh scaling
        // or root movement is involved.
        [SerializeField, Range(0f, 28f)] private float anticipationUpperDegrees = 24f;
        [SerializeField, Range(0f, 40f)] private float anticipationLowerDegrees = 35f;
        [SerializeField, Range(0f, 24f)] private float anticipationFootDegrees = 18f;
        [SerializeField, Range(0f, 16f)] private float anticipationBodyPitchDegrees = 8f;
        [SerializeField, Range(0f, 18f)] private float attackBodyPitchDegrees = 12f;

        private Transform _motionRoot;
        private Transform _bodyBone;
        private readonly Transform[] _frontUpper = new Transform[2];
        private readonly Transform[] _frontLower = new Transform[2];
        private readonly Transform[] _frontFoot = new Transform[2];
        private Vector3 _lastPosition;
        private float _previousAttackElapsed = -1f;
        private float _previousHitWeight;
        private float _attackAnticipationWeight;
        private float _attackBodyPitchWeight;
        private float _hitBodyRecoilWeight;
        private float _hitStartedAt = -1f;
        private float _idleScanPhase;
        private float _idleAwarenessYawDegrees;
        private bool _leadLeftLeg = true;
        private bool _defeatSent;
        private float _defeatStartedAt = -1f;

        public bool DrivesImportedRig => isActiveAndEnabled && animator != null && animator.isActiveAndEnabled
            && animator.runtimeAnimatorController != null;
        public float AttackAnticipationWeightForDiagnostics => _attackAnticipationWeight;
        public float AttackBodyPitchWeightForDiagnostics => _attackBodyPitchWeight;
        public float HitBodyRecoilWeightForDiagnostics => _hitBodyRecoilWeight;
        public float IdleAwarenessYawDegreesForDiagnostics => _idleAwarenessYawDegrees;
        public string ImportedStateForDiagnostics
        {
            get
            {
                if (!gameObject.activeInHierarchy) return $"driver-object-inactive:{gameObject.name}";
                if (!enabled) return $"driver-disabled:{gameObject.name}";
                if (animator == null)
                {
                    var candidates = GetComponentsInChildren<Animator>(true);
                    return candidates.Length == 0
                        ? $"animator-null:{gameObject.name},children=0"
                        : $"animator-null:{gameObject.name},children={candidates.Length},first={candidates[0].name},firstController={candidates[0].runtimeAnimatorController?.name ?? "null"}";
                }
                if (!animator.gameObject.activeInHierarchy) return $"object-inactive:{animator.name}";
                if (!animator.enabled) return $"component-disabled:{animator.name}";
                if (animator.runtimeAnimatorController == null) return $"controller-null:{animator.name}";
                if (animator.layerCount == 0) return $"no-layers:{animator.name}";
                var state = animator.GetCurrentAnimatorStateInfo(0);
                var name = state.IsName("Base Layer.Idle") ? "Idle"
                    : state.IsName("Base Layer.Walk") ? "Walk"
                    : state.IsName("Base Layer.Attack") ? "Attack"
                    : state.IsName("Base Layer.Hit") ? "Hit"
                    : state.IsName("Base Layer.Defeat") ? "Defeat" : "Other";
                if (animator.IsInTransition(0))
                {
                    var next = animator.GetNextAnimatorStateInfo(0);
                    var nextName = next.IsName("Base Layer.Idle") ? "Idle"
                        : next.IsName("Base Layer.Walk") ? "Walk"
                        : next.IsName("Base Layer.Attack") ? "Attack"
                        : next.IsName("Base Layer.Hit") ? "Hit"
                        : next.IsName("Base Layer.Defeat") ? "Defeat" : "Other";
                    name += $"->{nextName}";
                }

                return $"{name}@{state.normalizedTime:F2},move={animator.GetFloat(MoveSpeedId):F2},controller={animator.runtimeAnimatorController.name}";
            }
        }
        public float DefeatClipDurationSeconds
        {
            get
            {
                if (animator == null || animator.runtimeAnimatorController == null) return 0.9f;
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip != null && clip.name.IndexOf("Defeat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return Mathf.Max(0.1f, clip.length / Mathf.Max(0.01f, animator.speed));
                return 0.9f;
            }
        }
        public bool IsDefeatPlaybackComplete
        {
            get
            {
                if (!_defeatSent || _defeatStartedAt < 0f) return false;
                if (!DrivesImportedRig || animator.layerCount == 0)
                    return Time.time - _defeatStartedAt >= DefeatClipDurationSeconds;
                if (!animator.IsInTransition(0)
                    && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Defeat")
                    && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                    return true;
                return Time.time - _defeatStartedAt >= DefeatClipDurationSeconds + 0.25f;
            }
        }

        private void Awake()
        {
            CacheReferences();
            ResetMotionSample();
        }

        private void OnEnable()
        {
            CacheReferences();
            ResetMotionSample();
            // This component may be enabled before an instantiated visual has
            // been parented under ScrapMite. CacheReferences leaves _motionRoot
            // pointing at this transform in that case, so seed from the safe
            // fallback and let normal movement binding happen in Update.
            var phaseOrigin = _motionRoot != null ? _motionRoot.position : transform.position;
            _idleScanPhase = Mathf.Repeat(
                Mathf.Abs(phaseOrigin.x * 17.1f + phaseOrigin.z * 31.7f) * 0.6180339f,
                1f) * (Mathf.PI * 2f);
            _previousAttackElapsed = -1f;
            _previousHitWeight = 0f;
            _defeatSent = false;
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            // A prefab may be instantiated before it is parented under the AI,
            // so Awake can run while the Mite reference is still unavailable.
            if (mite == null)
            {
                mite = GetComponentInParent<ScrapMite>();
                if (mite != null)
                {
                    _motionRoot = mite.transform;
                    _lastPosition = _motionRoot.position;
                }
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                CacheReferences();
                if (animator == null || animator.runtimeAnimatorController == null) return;
            }

            if (_motionRoot == null) _motionRoot = mite != null ? mite.transform : transform;
            var position = _motionRoot.position;
            var planarDelta = position - _lastPosition;
            planarDelta.y = 0f;
            _lastPosition = position;

            // The AI already knows its movement phase relative to its own top
            // speed. Reuse it when bound; otherwise support preview objects moved
            // by an external review driver without inventing gait animation.
            var moveAmount = mite != null
                ? mite.IsDefeated ? 0f : Mathf.Clamp01(mite.CommissionedGaitWeight)
                : Mathf.Clamp01(planarDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) / 2.2f);
            animator.SetFloat(MoveSpeedId, moveAmount, speedDampSeconds, Time.deltaTime);
            var walkRate = mite != null
                ? mite.IsDefeated ? 0f : mite.CommissionedGaitRate
                : Mathf.Clamp(planarDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) / 2.2f,
                    0.35f, 1.75f);
            animator.SetFloat(WalkRateId, walkRate, speedDampSeconds, Time.deltaTime);

            if (mite == null) return;
            var attackElapsed = mite.AttackStrokeElapsedForDiagnostics;
            var hitWeight = mite.ContactFlinchWeightForDiagnostics;
            if (!_defeatSent && mite.IsDefeated)
            {
                animator.ResetTrigger(AttackId);
                animator.ResetTrigger(HitId);
                _hitStartedAt = -1f;
                animator.SetFloat(MoveSpeedId, 0f);
                animator.SetTrigger(DefeatId);
                _defeatSent = true;
                _defeatStartedAt = Time.time;
            }
            else if (!_defeatSent)
            {
                if (hitWeight >= hitTriggerThreshold && _previousHitWeight < hitTriggerThreshold)
                {
                    animator.ResetTrigger(AttackId);
                    animator.SetTrigger(HitId);
                    _hitStartedAt = Time.time;
                }
                // Do not let a fresh AI attack-cycle trigger cut the imported
                // Hit clip short. The reaction should own the rig through its
                // authored exit transition before another strike can begin.
                else if (!IsPlayingOrEnteringHit()
                    && hitWeight < 0.1f && attackElapsed >= 0f
                    && (_previousAttackElapsed < 0f || attackElapsed + 0.02f < _previousAttackElapsed))
                {
                    _leadLeftLeg = !_leadLeftLeg;
                    animator.SetTrigger(AttackId);
                }
            }

            _previousAttackElapsed = attackElapsed;
            _previousHitWeight = hitWeight;
        }

        private bool IsPlayingOrEnteringHit()
        {
            if (animator == null || animator.layerCount == 0) return false;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HitStateId) return true;
            return animator.IsInTransition(0)
                && animator.GetNextAnimatorStateInfo(0).fullPathHash == HitStateId;
        }

        private void LateUpdate()
        {
            ApplyHitBodyRecoil();
            ApplyIdleAwarenessPose();
            ApplyAttackAnticipationPose();
            ApplyAttackStrokePose();
            // Animator has posed the imported bones by LateUpdate. Measure the
            // deformed skin here, after the clip update, so support seating uses
            // this frame's feet rather than last frame's bounds or importer data.
            if (mite == null) mite = GetComponentInParent<ScrapMite>();
            if (mite != null && DrivesImportedRig)
                mite.AlignCommissionedVisualAfterPose();
        }

        private void ApplyIdleAwarenessPose()
        {
            _idleAwarenessYawDegrees = 0f;
            if (!DrivesImportedRig || mite == null || mite.IsDefeated
                || mite.IsAttackTelegraphingForDiagnostics
                || animator.layerCount == 0 || animator.IsInTransition(0)
                || animator.GetCurrentAnimatorStateInfo(0).fullPathHash != IdleStateId) return;
            if (_bodyBone == null)
                _bodyBone = FindDescendant(animator.transform, "Body");
            if (_bodyBone == null) return;

            // The imported Idle clip moves the spider only slightly. Give its
            // rigged Body a deliberate glance, with brief holds at each side so
            // the attention shift reads at camera distance instead of reversing
            // continuously. Keep this in-place and scale-free; no root bobbing.
            var phase = Mathf.Repeat(Time.time / 5.8f + _idleScanPhase / (Mathf.PI * 2f), 1f);
            var yaw = EvaluateIdleAwarenessYaw(phase);
            _idleAwarenessYawDegrees = yaw;
            _bodyBone.localRotation *= Quaternion.Euler(0f, yaw, 0f);
        }

        private static float EvaluateIdleAwarenessYaw(float phase)
        {
            if (phase < 0.18f)
                return 12f * Mathf.SmoothStep(0f, 1f, phase / 0.18f);
            if (phase < 0.36f) return 12f;
            if (phase < 0.54f)
                return 12f * (1f - Mathf.SmoothStep(0f, 1f, (phase - 0.36f) / 0.18f));
            if (phase < 0.72f)
                return -10f * Mathf.SmoothStep(0f, 1f, (phase - 0.54f) / 0.18f);
            if (phase < 0.88f) return -10f;
            return -10f * (1f - Mathf.SmoothStep(0f, 1f, (phase - 0.88f) / 0.12f));
        }

        private void ApplyHitBodyRecoil()
        {
            _hitBodyRecoilWeight = 0f;
            if (!DrivesImportedRig || mite == null || mite.IsDefeated
                || !IsPlayingOrEnteringHit()) return;
            if (_bodyBone == null)
                _bodyBone = FindDescendant(animator.transform, "Body");
            if (_bodyBone == null || _hitStartedAt < 0f) return;

            // The imported Hit clip changes the legs only slightly at gameplay
            // scale. Add one small, directional recoil to its actual Body bone;
            // critically damp it so the face and legs settle without mesh-scale
            // pulses or an extra whole-root bounce.
            var elapsed = Mathf.Max(0f, Time.time - _hitStartedAt);
            var decayTime = elapsed * 5f;
            var weight = (1f + decayTime) * Mathf.Exp(-decayTime);
            _hitBodyRecoilWeight = weight;
            var away = mite.ContactFlinchDirectionLocal;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.back;
            var pitch = Mathf.Clamp(away.z * 18f, -18f, 18f) * weight;
            var roll = Mathf.Clamp(-away.x * 14f, -14f, 14f) * weight;
            _bodyBone.localRotation *= Quaternion.Euler(pitch, 0f, roll);
        }

        private void ApplyAttackAnticipationPose()
        {
            _attackAnticipationWeight = 0f;
            if (!DrivesImportedRig || mite == null || mite.IsDefeated
                || IsPlayingOrEnteringHit()) return;
            CacheFrontLegBones();

            var weight = mite.IsAttackTelegraphingForDiagnostics
                ? Mathf.Clamp01(mite.AttackTelegraphWeightForDiagnostics) : 0f;
            // Release the brace into the commissioned strike instead of layering
            // two attack poses on top of each other.
            var strokeAge = mite.AttackStrokeElapsedForDiagnostics;
            if (strokeAge >= 0f)
                weight *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(strokeAge / 0.14f));
            _attackAnticipationWeight = weight;
            if (weight <= 0.001f) return;

            for (var i = 0; i < _frontUpper.Length; i++)
            {
                var upper = _frontUpper[i];
                var lower = _frontLower[i];
                var foot = _frontFoot[i];
                if (upper == null || lower == null || foot == null) continue;
                var side = i == 0 ? -1f : 1f;
                // Keep the partner leg mostly in its support pose so the lead
                // reads as a deliberate, directional tell at gameplay distance.
                var leadWeight = (i == 0) == _leadLeftLeg ? 1f : 0.22f;
                upper.localRotation *= Quaternion.Euler(anticipationUpperDegrees * weight * leadWeight,
                    0f, side * anticipationUpperDegrees * 0.35f * weight * leadWeight);
                lower.localRotation *= Quaternion.Euler(-anticipationLowerDegrees * weight * leadWeight,
                    0f, side * anticipationLowerDegrees * 0.2f * weight * leadWeight);
                foot.localRotation *= Quaternion.Euler(anticipationFootDegrees * weight * leadWeight,
                    0f, -side * anticipationFootDegrees * 0.2f * weight * leadWeight);
            }

            // Load the same rigged body backward while the front legs brace.
            // The existing stroke then pitches forward, making the wind-up and
            // release readable as one attack instead of a stationary leg pose
            // followed by a sudden imported clip. This stays bone-local and does
            // not scale the mesh or move the AI/collision root.
            if (_bodyBone == null)
                _bodyBone = FindDescendant(animator.transform, "Body");
            if (_bodyBone != null)
                _bodyBone.localRotation *= Quaternion.Euler(-anticipationBodyPitchDegrees * weight, 0f, 0f);
        }

        private void ApplyAttackStrokePose()
        {
            _attackBodyPitchWeight = 0f;
            if (!DrivesImportedRig || mite == null || mite.IsDefeated
                || IsPlayingOrEnteringHit() || animator.layerCount == 0) return;

            var state = animator.GetCurrentAnimatorStateInfo(0);
            var enteringAttack = animator.IsInTransition(0)
                && animator.GetNextAnimatorStateInfo(0).fullPathHash == AttackStateId;
            if (state.fullPathHash != AttackStateId && !enteringAttack) return;

            // The imported strike has a restrained body silhouette at game scale.
            // Add one short, scale-free pitch to the existing Body bone, driven by
            // the same eased AI pulse as the leg tell. Animator rewrites the pose
            // every frame, so this post-clip offset does not accumulate.
            var weight = Mathf.Clamp01(mite.AttackStrokeWeightForDiagnostics);
            _attackBodyPitchWeight = weight;
            if (weight <= 0.001f) return;
            if (_bodyBone == null)
                _bodyBone = FindDescendant(animator.transform, "Body");
            if (_bodyBone != null)
                _bodyBone.localRotation *= Quaternion.Euler(attackBodyPitchDegrees * weight, 0f, 0f);
        }

        private void CacheFrontLegBones()
        {
            if (_frontUpper[0] != null && _frontUpper[1] != null) return;
            var root = animator != null ? animator.transform : transform;
            _frontUpper[0] = FindDescendant(root, "Leg_L_0_Upper");
            _frontUpper[1] = FindDescendant(root, "Leg_R_0_Upper");
            _frontLower[0] = FindDescendant(root, "Leg_L_0_Lower");
            _frontLower[1] = FindDescendant(root, "Leg_R_0_Lower");
            _frontFoot[0] = FindDescendant(root, "Leg_L_0_Foot");
            _frontFoot[1] = FindDescendant(root, "Leg_R_0_Foot");
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
        }

        /// <summary>Allows encounter code to explicitly cue the imported one-shot clips.</summary>
        public void PlayAttack()
        {
            if (CanPlayOneShot()) animator.SetTrigger(AttackId);
        }

        public void PlayHit()
        {
            if (!CanPlayOneShot()) return;
            animator.SetTrigger(HitId);
            _hitStartedAt = Time.time;
        }

        public void PlayDefeat()
        {
            if (!CanPlayOneShot() || _defeatSent) return;
            animator.ResetTrigger(AttackId);
            animator.ResetTrigger(HitId);
            animator.SetFloat(MoveSpeedId, 0f);
            animator.SetTrigger(DefeatId);
            _defeatSent = true;
            _defeatStartedAt = Time.time;
        }

        private bool CanPlayOneShot() => animator != null
            && animator.runtimeAnimatorController != null && !_defeatSent;

        private void CacheReferences()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                var animators = GetComponentsInChildren<Animator>(true);
                Animator firstWithController = null;
                Animator firstAnimator = null;
                for (var i = 0; i < animators.Length; i++)
                {
                    var candidate = animators[i];
                    if (candidate == null) continue;
                    firstAnimator ??= candidate;
                    if (candidate.runtimeAnimatorController == null) continue;
                    firstWithController ??= candidate;
                    if (candidate.isActiveAndEnabled)
                    {
                        animator = candidate;
                        break;
                    }
                }

                animator ??= firstWithController ?? firstAnimator;
            }

            if (mite == null) mite = GetComponentInParent<ScrapMite>();
            if (_motionRoot == null) _motionRoot = mite != null ? mite.transform : transform;
        }

        private void ResetMotionSample()
        {
            if (_motionRoot == null) _motionRoot = mite != null ? mite.transform : transform;
            _lastPosition = _motionRoot != null ? _motionRoot.position : transform.position;
        }
    }
}
