using System;
using System.Reflection;
using UnityEngine;
using SpaceAndCrafts.Player;

namespace SpaceAndCrafts.Art.ReviewOnly
{
    /// <summary>
    /// Capture-only experiment for the Iter1835 Meshy Generic run clips. The production
    /// contact pass deliberately rejects swing feet above 75 mm; this candidate chooses
    /// one low, non-rising foot and lowers its leg parent by at most 120 mm. It must only
    /// be attached by the named Unity review capture and must not be serialized to a prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class SplatRunContactCorrectionReviewIter1836 : MonoBehaviour
    {
        private const float MaximumCorrection = 0.12f;
        private const float SoleInset = 0.004f;
        private const float MaxFootHeightForCandidate = 0.24f;
        private const float SwitchHeightMargin = 0.025f;
        private const float MaximumRisingSpeed = 0.025f;
        private const float WalkableNormalY = 0.65f;

        private SplatMotor _motor;
        private Animator _animator;
        private SplatVisualRepair _contactRepair;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private MethodInfo _measureWeightedFoot;
        private float _previousLeftY;
        private float _previousRightY;
        private bool _hasPreviousSample;
        private int _stanceFoot = -1;

        public string Diagnostics { get; private set; } = "not sampled";

        private void Awake()
        {
            _motor = GetComponent<SplatMotor>() ?? GetComponentInChildren<SplatMotor>(true);
            _contactRepair = GetComponent<SplatVisualRepair>() ?? GetComponentInChildren<SplatVisualRepair>(true);
            // The old visual is destroyed deferred during the capture; search the
            // renamed/new active child explicitly so this review component cannot
            // bind to the stale production Animator/mesh for this frame.
            var visual = transform.Find(SplatMeshyVisualMode.MeshyVisualRootName);
            _animator = visual != null
                ? visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true)
                : GetComponentInChildren<Animator>(true);
            var searchRoot = visual != null ? visual : transform;
            var bones = searchRoot.GetComponentsInChildren<Transform>(true);
            _leftLeg = FindLeg(bones, true);
            _rightLeg = FindLeg(bones, false);
            _measureWeightedFoot = typeof(SplatVisualRepair).GetMethod("TryMeasureWeightedFoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            enabled = true;
        }

        public void SampleAndCorrectNow()
        {
            if (_motor == null || !_motor.IsGrounded || _contactRepair == null
                || _leftLeg == null || _rightLeg == null)
            {
                Diagnostics = $"inactive: motor={_motor != null}, grounded={(_motor != null && _motor.IsGrounded)}, contactSampler={_contactRepair != null}, leftLeg={_leftLeg != null}, rightLeg={_rightLeg != null}";
                return;
            }

            var leftPoint = _contactRepair.LeftSoleWorldPointForDiagnostics;
            var rightPoint = _contactRepair.RightSoleWorldPointForDiagnostics;
            if (!TryGetSupport(leftPoint, out var leftSupportY, out var leftSupportName)
                || !TryGetSupport(rightPoint, out var rightSupportY, out var rightSupportName))
            {
                Diagnostics = "inactive: no valid walkable surface beneath both measured soles";
                return;
            }

            var dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var leftRiseSpeed = _hasPreviousSample ? (leftPoint.y - _previousLeftY) / dt : 0f;
            var rightRiseSpeed = _hasPreviousSample ? (rightPoint.y - _previousRightY) / dt : 0f;
            _previousLeftY = leftPoint.y;
            _previousRightY = rightPoint.y;
            _hasPreviousSample = true;

            var leftHeight = leftPoint.y - leftSupportY;
            var rightHeight = rightPoint.y - rightSupportY;
            if (_stanceFoot < 0)
            {
                _stanceFoot = leftHeight <= rightHeight ? 0 : 1;
            }
            else
            {
                var heldHeight = _stanceFoot == 0 ? leftHeight : rightHeight;
                var otherHeight = _stanceFoot == 0 ? rightHeight : leftHeight;
                var otherRiseSpeed = _stanceFoot == 0 ? rightRiseSpeed : leftRiseSpeed;
                if (otherHeight + SwitchHeightMargin < heldHeight
                    && otherRiseSpeed <= MaximumRisingSpeed)
                {
                    _stanceFoot = 1 - _stanceFoot;
                }
            }

            var selectedPoint = _stanceFoot == 0 ? leftPoint : rightPoint;
            var selectedSupportY = _stanceFoot == 0 ? leftSupportY : rightSupportY;
            var selectedHeight = _stanceFoot == 0 ? leftHeight : rightHeight;
            var selectedRiseSpeed = _stanceFoot == 0 ? leftRiseSpeed : rightRiseSpeed;
            var selectedLeg = _stanceFoot == 0 ? _leftLeg : _rightLeg;
            var requested = selectedSupportY + SoleInset - selectedPoint.y;
            var applied = 0f;

            // Don't pull a rising swing foot down. The lower-foot/hysteresis test
            // chooses a plausible stance candidate; this velocity gate preserves lift.
            if (_motor.PlanarVelocity.sqrMagnitude > 0.04f
                && selectedHeight >= 0f && selectedHeight <= MaxFootHeightForCandidate
                && selectedRiseSpeed <= MaximumRisingSpeed)
            {
                applied = Mathf.Clamp(requested, -MaximumCorrection, 0f);
                if (Mathf.Abs(applied) > 0.0001f)
                    selectedLeg.position += Vector3.up * applied;
            }

            // Re-evaluate the actual skin-weighted vertices after moving the
            // lower-leg parent. The pre-correction point above came from the
            // production sampler's earlier pass and cannot prove the correction
            // made it into the rendered pose. Reflection keeps this measurement
            // capture-only without changing SplatVisualRepair's gameplay API.
            var leftPostSupportY = 0f;
            var rightPostSupportY = 0f;
            var hasLeftPost = TryMeasureWeightedFoot(true, out var leftPostBottom, out var leftPostPoint)
                && TryGetSupport(leftPostPoint, out leftPostSupportY, out _);
            var hasRightPost = TryMeasureWeightedFoot(false, out var rightPostBottom, out var rightPostPoint)
                && TryGetSupport(rightPostPoint, out rightPostSupportY, out _);
            var leftPostGap = hasLeftPost ? leftPostBottom - leftPostSupportY : float.NaN;
            var rightPostGap = hasRightPost ? rightPostBottom - rightPostSupportY : float.NaN;

            Diagnostics = $"stanceCandidate={( _stanceFoot == 0 ? "left" : "right")}, "
                + $"leftHeight={leftHeight:F3}, rightHeight={rightHeight:F3}, "
                + $"leftRise={leftRiseSpeed:F3}, rightRise={rightRiseSpeed:F3}, "
                + $"requested={requested:F3}, applied={applied:F3}, "
                + $"truePostGapLeft={(hasLeftPost ? leftPostGap.ToString("F3") : "n/a")}, truePostGapRight={(hasRightPost ? rightPostGap.ToString("F3") : "n/a")}, "
                + $"leftSupport={leftSupportName}, rightSupport={rightSupportName}, "
                + $"rootFixed={_motor.transform.position:F3}, animator={_animator?.runtimeAnimatorController?.name ?? "missing"}";
        }

        private bool TryMeasureWeightedFoot(bool leftFoot, out float footBottom, out Vector3 solePoint)
        {
            footBottom = float.PositiveInfinity;
            solePoint = Vector3.zero;
            if (_measureWeightedFoot == null || _contactRepair == null) return false;
            var arguments = new object[] { leftFoot, footBottom, 0f, solePoint };
            var success = _measureWeightedFoot.Invoke(_contactRepair, arguments) is bool measured && measured;
            footBottom = (float)arguments[1];
            solePoint = (Vector3)arguments[3];
            return success && !float.IsInfinity(footBottom);
        }

        private void LateUpdate() => SampleAndCorrectNow();

        private bool TryGetSupport(Vector3 solePoint, out float supportY, out string supportName)
        {
            supportY = 0f;
            supportName = string.Empty;
            var origin = solePoint + Vector3.up * 0.28f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 0.85f, ~0,
                QueryTriggerInteraction.Ignore);
            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform == transform
                    || hit.collider.transform.IsChildOf(transform)
                    || hit.normal.y < WalkableNormalY || hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                supportY = hit.point.y;
                supportName = hit.collider.name;
            }
            return !float.IsPositiveInfinity(bestDistance);
        }

        private static Transform FindLeg(Transform[] bones, bool left)
        {
            var names = left
                ? new[] { "LeftLeg", "Bone_LegL", "LeftFoot", "Bone_FootL" }
                : new[] { "RightLeg", "Bone_LegR", "RightFoot", "Bone_FootR" };
            if (bones == null) return null;
            for (var n = 0; n < names.Length; n++)
            {
                for (var b = 0; b < bones.Length; b++)
                {
                    if (bones[b] != null && bones[b].name == names[n])
                        return bones[b];
                }
            }
            return null;
        }
    }
}
