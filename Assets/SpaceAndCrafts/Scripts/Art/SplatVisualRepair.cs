using System.Collections.Generic;
using SpaceAndCrafts.Player;
using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Procedural SoftBlob meshes often serialize with zero bounds after reload.
    /// Rebuild missing/empty meshes at runtime so Splat is actually visible.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SplatVisualRepair : MonoBehaviour
    {
        [SerializeField] private bool rebuildIfMissing = true;
        // Target the sampled support plane directly. Earlier inset candidates
        // (including the 12 mm review in Iter1226) did not produce a clear seam
        // improvement and can read as hover on thin platforms.
        // Keep the visual soles a few millimetres inside the physics support:
        // staged mesh/collider comparison found up to 10 mm of collider stand-off
        // on the shallow leaf surface. This is visual-only and never moves the
        // gameplay root or controller.
        private const float GroundVisualContactInset = 0.007f;
        private const float MaxGroundVisualCorrection = 0.10f;
        // Let grounded foot pivots close a one-frame Idle-transition gap in
        // 50 mm steps at 60 fps; this remains joint-only visual correction and
        // does not move the gameplay root or collider.
        private const float MaxGroundVisualCorrectionSpeed = 3.0f;
        // Run stance should close the sampled swing-to-plant gap promptly so the new support foot does not hover across several frames.
        private const float RunGroundVisualCorrectionSpeed = 3.6f;
        // Close most of the measured touchdown sole gap on the first grounded
        // frame, while the per-frame cap keeps the correction from snapping.
        private const float LandingGroundVisualCorrectionSpeed = 6.0f;
        private const float LandingGroundVisualCorrectionMaxStep = 0.10f;
        private const float MinimumLegWeightForFootContact = 0.75f;
        private const float MinimumFootWeightForContact = 0.55f;

        private static readonly HashSet<EntityId> RepairedRoots = new HashSet<EntityId>();
        private static readonly HashSet<EntityId> BrightenedSkinnedRoots = new HashSet<EntityId>();
        private static readonly HashSet<EntityId> MeshyModeRoots = new HashSet<EntityId>();
        private Transform _groundContactVisual;
        private Renderer[] _groundContactRenderers = System.Array.Empty<Renderer>();
        private Renderer[] _groundContactBootRenderers = System.Array.Empty<Renderer>();
        private Renderer[] _groundContactLeftFootRenderers = System.Array.Empty<Renderer>();
        private Renderer[] _groundContactRightFootRenderers = System.Array.Empty<Renderer>();
        private Vector3[][] _groundContactLeftFootVertices = System.Array.Empty<Vector3[]>();
        private Vector3[][] _groundContactRightFootVertices = System.Array.Empty<Vector3[]>();
        private SkinnedMeshRenderer[] _groundContactSkinnedRenderers = System.Array.Empty<SkinnedMeshRenderer>();
        private Transform _groundContactMeshyLeftFootPivot;
        private Transform _groundContactMeshyRightFootPivot;
        private int[][] _groundContactLeftLegVertices = System.Array.Empty<int[]>();
        private int[][] _groundContactRightLegVertices = System.Array.Empty<int[]>();
        private float[][] _groundContactLeftLegWeights = System.Array.Empty<float[]>();
        private float[][] _groundContactRightLegWeights = System.Array.Empty<float[]>();
        private Vector3[][] _groundContactSourceVertices = System.Array.Empty<Vector3[]>();
        private BoneWeight[][] _groundContactSourceWeights = System.Array.Empty<BoneWeight[]>();
        private Matrix4x4[][] _groundContactBindPoses = System.Array.Empty<Matrix4x4[]>();
        private Transform[][] _groundContactBones = System.Array.Empty<Transform[]>();
        private bool[] _groundContactCanEvaluateSamples = System.Array.Empty<bool>();
        private readonly RaycastHit[] _groundContactHits = new RaycastHit[16];
        private readonly List<Vector3> _groundContactBakedVertices = new List<Vector3>(2048);
        private Mesh _groundContactBakedMesh;
        private SplatSkeletonAnimator _poseAnimator;
        private SplatMotor _groundMotor;
        private string _groundContactCacheDiagnostics = "not cached";
        private string _leftFootSampleEvaluationDiagnostics = "not sampled";
        private string _rightFootSampleEvaluationDiagnostics = "not sampled";
        public string SoleContactDiagnostics { get; private set; } = "not sampled";
        public Vector3 LeftSoleWorldPointForDiagnostics { get; private set; }
        public Vector3 RightSoleWorldPointForDiagnostics { get; private set; }
        private static readonly Vector2[] GroundProbeOffsets =
        {
            Vector2.zero,
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            // A planted shoe can land with its sole over a diagonal platform
            // corner even when the center and cardinal probes miss the deck.
            // Add normalized corner samples so edge contact can use the actual
            // support surface instead of the controller-height fallback.
            new Vector2(-0.70710678f, -0.70710678f),
            new Vector2(-0.70710678f, 0.70710678f),
            new Vector2(0.70710678f, -0.70710678f),
            new Vector2(0.70710678f, 0.70710678f),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRepairGuard()
        {
            RepairedRoots.Clear();
            BrightenedSkinnedRoots.Clear();
            MeshyModeRoots.Clear();
        }

        private void Awake()
        {
            _poseAnimator = SplatMeshyVisualMode.IsEnabledOn(transform)
                ? null : GetComponent<SplatSkeletonAnimator>();
            _groundMotor = GetComponent<SplatMotor>();
            if (SplatMeshyVisualMode.IsEnabledOn(transform))
            {
                Repair(transform);
                return;
            }

            if (!rebuildIfMissing)
            {
                return;
            }

            Repair(transform);
        }

        private void LateUpdate()
        {
            if (SplatMeshyVisualMode.IsEnabledOn(transform))
            {
                _poseAnimator = null;
                var meshRoot = transform.Find(SplatMeshyVisualMode.MeshyVisualRootName);
                if (!MeshyModeRoots.Contains(transform.GetEntityId()) || meshRoot == null
                    || !meshRoot.gameObject.activeSelf)
                {
                    Repair(transform);
                }
                return;
            }

            if (MeshyModeRoots.Contains(transform.GetEntityId()))
            {
                // The opt-in marker can be disabled or removed to restore the
                // inactive Organic child and its original pose driver.
                Repair(transform);
            }

            KeepSingleVisualRoot(transform);
            if (_poseAnimator == null)
            {
                _poseAnimator = GetComponent<SplatSkeletonAnimator>();
            }

            // The pose driver calls AlignGroundedSolesAfterPose after its own
            // LateUpdate. Avoid baking the same character twice in one frame.
            if (_poseAnimator == null)
            {
                KeepSkinnedSolesSeated(transform);
            }
        }

        private void OnDestroy()
        {
            if (_groundContactBakedMesh != null)
            {
                if (Application.isPlaying) Object.Destroy(_groundContactBakedMesh);
                else Object.DestroyImmediate(_groundContactBakedMesh);
                _groundContactBakedMesh = null;
            }
        }

        /// <summary>
        /// Reapply grounded sole alignment after the animation rig has finished
        /// its LateUpdate foot locks, so the rendered pose is the one measured.
        /// </summary>
        public Vector3 AlignGroundedSolesAfterPose()
        {
            if (_poseAnimator == null)
            {
                _poseAnimator = GetComponent<SplatSkeletonAnimator>();
            }

            var root = transform;
            var visual = ResolveSplatVisualRoot(root);
            if (visual == null)
            {
                return Vector3.zero;
            }

            var positionBeforeAlignment = visual.position;
            KeepSkinnedSolesSeated(transform);
            return visual.position - positionBeforeAlignment;
        }

        /// <summary>
        /// Lets the animator driver fall back to whole-visual bounds seating for
        /// the imported Meshy rig. Its broad leg weights make joint translation
        /// visibly stretch the lower-body silhouette.
        /// </summary>
        public bool AlignMeshyGroundedSolesAfterPose()
        {
            if (!SplatMeshyVisualMode.IsEnabledOn(transform)) return false;
            // The Iter1858 mesh distributes its lower-leg skin weights broadly
            // across the round torso. Translating the leg parents enough to close
            // a measured sole gap visibly stretches both legs. Let the animator
            // driver use its whole-visual bounds fallback instead; that seats the
            // imported silhouette without deforming the rig.
            return false;
        }

        private void KeepSkinnedSolesSeated(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var motor = root.GetComponent<SplatMotor>();
            if (_groundMotor != motor) _groundMotor = motor;
            var controller = root.GetComponent<CharacterController>();
            var meshMode = SplatMeshyVisualMode.IsEnabledOn(root);
            _poseAnimator = meshMode ? null : ( _poseAnimator != null ? _poseAnimator : root.GetComponent<SplatSkeletonAnimator>() );
            var visual = ResolveSplatVisualRoot(root);
            if (motor == null || controller == null || !controller.enabled || visual == null)
            {
                return;
            }

            if (!motor.IsGrounded)
            {
                // Do not leave the last grounded sole measurement in place while
                // the character is airborne; capture diagnostics must not imply
                // that the feet remain seated through the jump.
                SoleContactDiagnostics = $"airborne; no grounded foot correction (action={_poseAnimator?.CurrentAction.ToString() ?? "unknown"})";
                return;
            }

            if (_groundContactVisual != visual || _groundContactRenderers == null)
            {
                _groundContactVisual = visual;
                _groundContactRenderers = visual.GetComponentsInChildren<Renderer>(true);
                _groundContactSkinnedRenderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var bootRenderers = new List<Renderer>();
                var leftFootRenderers = new List<Renderer>();
                var rightFootRenderers = new List<Renderer>();
                for (var i = 0; i < _groundContactRenderers.Length; i++)
                {
                    var renderer = _groundContactRenderers[i];
                    if (renderer == null) continue;
                    var objectName = renderer.gameObject.name;
                    if (objectName.IndexOf("Boot", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || objectName.IndexOf("Sole", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || objectName.IndexOf("Foot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bootRenderers.Add(renderer);
                    }

                    if (HasFootSideToken(objectName, true)) leftFootRenderers.Add(renderer);
                    else if (HasFootSideToken(objectName, false)) rightFootRenderers.Add(renderer);
                }

                // Use dedicated boot meshes when the authored rig provides them.
                // A whole-body skin bound is not a sole: Splat's rounded body extends
                // below its boots and used to pull the visual down while the feet floated.
                _groundContactBootRenderers = bootRenderers.ToArray();
                _groundContactLeftFootRenderers = leftFootRenderers.ToArray();
                _groundContactRightFootRenderers = rightFootRenderers.ToArray();
                _groundContactLeftFootVertices = CacheRigidFootVertices(_groundContactLeftFootRenderers);
                _groundContactRightFootVertices = CacheRigidFootVertices(_groundContactRightFootRenderers);
                CacheLegWeightedVertices();
                _groundContactMeshyLeftFootPivot = FindMeshyFootPivot(_groundContactSkinnedRenderers, true);
                _groundContactMeshyRightFootPivot = FindMeshyFootPivot(_groundContactSkinnedRenderers, false);
            }

            var lowestSole = float.PositiveInfinity;
            var lowestSolePosition = Vector3.zero;
            var soleRenderers = _groundContactBootRenderers.Length > 0
                ? _groundContactBootRenderers : _groundContactRenderers;
            Renderer lowestSoleRenderer = null;
            var useDedicatedBoots = _groundContactBootRenderers.Length > 0;
            var useLegWeightedVertices = !useDedicatedBoots && HasLegWeightedVertices();
            var leftFootBottom = float.PositiveInfinity;
            var rightFootBottom = float.PositiveInfinity;
            var leftFootContactPosition = Vector3.zero;
            var rightFootContactPosition = Vector3.zero;
            var leftFootInfluence = 1f;
            var rightFootInfluence = 1f;

            // The approved authored Splat is built from overlapping, individually
            // posed soft meshes rather than a skinned Meshy import. A whole-visual
            // offset cannot correct two separate stance soles, and moving the root
            // after a foot has been locked would otherwise leave that foot hovering.
            // Resolve each named foot against its own support and move its pivot plus
            // the run anchor together; leave swing legs alone.
            // Decide the contact solver from the boot renderers themselves, not
            // from unrelated skinned parts such as Splat's two-bone arms. A mixed
            // rig with rigid feet must still anchor each stance foot independently;
            // treating the entire visual as skinned translates the body to chase
            // whichever foot happens to be lowest and makes the planted shoe float.
            if (_poseAnimator != null && useDedicatedBoots && HasRigidFootRenderers())
            {
                var leftContact = CorrectRigidFootContact(root, controller, true);
                var rightContact = CorrectRigidFootContact(root, controller, false);
                SoleContactDiagnostics = $"method=independent rigid-foot pivots, leftStance={_poseAnimator.IsLeftRunStanceForDiagnostics}, "
                    + $"rightStance={_poseAnimator.IsRightRunStanceForDiagnostics}, left={leftContact}, right={rightContact}";
                return;
            }

            if (!useDedicatedBoots && _groundContactSkinnedRenderers.Length > 0)
            {
                // Renderer.bounds can include a wide conservative margin on this
                // skinned rig. It placed the visible feet about 5 cm above the pad
                // even when the reported bound was 2 mm below it. Bake the posed
                // skin and prefer leg-weighted vertices so belly or antenna vertices
                // cannot stand in for the boot soles. Fall back to the full skin when
                // this rig variant does not expose named leg weights.
                if (_groundContactBakedMesh == null)
                {
                    _groundContactBakedMesh = new Mesh
                    {
                        name = "SplatGroundContactBake",
                        hideFlags = HideFlags.DontSave
                    };
                }

                for (var rendererIndex = 0; rendererIndex < _groundContactSkinnedRenderers.Length; rendererIndex++)
                {
                    var skinned = _groundContactSkinnedRenderers[rendererIndex];
                    if (skinned == null || !skinned.enabled || !skinned.gameObject.activeInHierarchy
                        || skinned.sharedMesh == null)
                    {
                        continue;
                    }

                    // When the rig has dedicated leg-weighted contact samples,
                    // other skinned renderers (face/details/accessories) cannot
                    // affect either foot estimate. Avoid CPU-baking their meshes
                    // every grounded frame; the full-skin fallback below remains
                    // unchanged for rigs without usable leg weights.
                    if (useLegWeightedVertices
                        && (_groundContactLeftLegVertices[rendererIndex]?.Length ?? 0) == 0
                        && (_groundContactRightLegVertices[rendererIndex]?.Length ?? 0) == 0)
                    {
                        continue;
                    }

                    var canEvaluateSamples = rendererIndex < _groundContactCanEvaluateSamples.Length
                        && _groundContactCanEvaluateSamples[rendererIndex];
                    if (!useLegWeightedVertices || !canEvaluateSamples)
                    {
                        _groundContactBakedMesh.Clear();
                        skinned.BakeMesh(_groundContactBakedMesh);
                        _groundContactBakedVertices.Clear();
                        _groundContactBakedMesh.GetVertices(_groundContactBakedVertices);
                    }
                    if (useLegWeightedVertices)
                    {
                        AccumulateWeightedFoot(rendererIndex, skinned,
                            _groundContactLeftLegVertices[rendererIndex],
                            _groundContactLeftLegWeights[rendererIndex], true, ref leftFootBottom,
                            ref leftFootInfluence, ref leftFootContactPosition,
                            ref lowestSole, ref lowestSolePosition, ref lowestSoleRenderer);
                        AccumulateWeightedFoot(rendererIndex, skinned,
                            _groundContactRightLegVertices[rendererIndex],
                            _groundContactRightLegWeights[rendererIndex], false, ref rightFootBottom,
                            ref rightFootInfluence, ref rightFootContactPosition,
                            ref lowestSole, ref lowestSolePosition, ref lowestSoleRenderer);
                    }
                    else
                    {
                        for (var vertexIndex = 0; vertexIndex < _groundContactBakedVertices.Count; vertexIndex++)
                        {
                            ConsiderBakedVertex(skinned, _groundContactBakedVertices[vertexIndex],
                                ref lowestSole, ref lowestSolePosition, ref lowestSoleRenderer);
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < soleRenderers.Length; i++)
                {
                    var renderer = soleRenderers[i];
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (renderer.bounds.min.y < lowestSole)
                    {
                        lowestSole = renderer.bounds.min.y;
                        lowestSolePosition = renderer.bounds.center;
                        lowestSoleRenderer = renderer;
                    }
                }
            }

            // Meshy Splat has no SplatSkeletonAnimator foot-lock pivots. Use the
            // imported rig's own left/right ankle/foot bones instead: each foot
            // is measured from its weighted rendered vertices and corrected toward
            // the walkable surface under that foot. The visual root and controller
            // stay fixed, so slopes no longer make one foot chase the other.
            if (meshMode && useLegWeightedVertices && _poseAnimator == null
                && _groundContactMeshyLeftFootPivot != null
                && _groundContactMeshyRightFootPivot != null)
            {
                var leftCorrection = CorrectMeshyWeightedFoot(root, controller, true,
                    _groundContactMeshyLeftFootPivot, ref leftFootBottom, ref leftFootInfluence,
                    ref leftFootContactPosition, out var leftHasSurface, out var leftSupportName,
                    out var leftTargetY, out var leftProbeOffset);
                var rightCorrection = CorrectMeshyWeightedFoot(root, controller, false,
                    _groundContactMeshyRightFootPivot, ref rightFootBottom, ref rightFootInfluence,
                    ref rightFootContactPosition, out var rightHasSurface, out var rightSupportName,
                    out var rightTargetY, out var rightProbeOffset);
                TryMeasureWeightedFoot(true, out var leftPostY, out _, out var leftPostPoint);
                TryMeasureWeightedFoot(false, out var rightPostY, out _, out var rightPostPoint);
                LeftSoleWorldPointForDiagnostics = leftPostPoint;
                RightSoleWorldPointForDiagnostics = rightPostPoint;
                SoleContactDiagnostics = $"method=Meshy weighted foot pivots, leftPre={(float.IsInfinity(leftFootBottom) ? "n/a" : leftFootBottom.ToString("F3"))}, rightPre={(float.IsInfinity(rightFootBottom) ? "n/a" : rightFootBottom.ToString("F3"))}, "
                    + $"leftPost={(float.IsInfinity(leftPostY) ? "n/a" : leftPostY.ToString("F3"))}, rightPost={(float.IsInfinity(rightPostY) ? "n/a" : rightPostY.ToString("F3"))}, "
                    + $"leftTarget={(leftHasSurface ? leftTargetY.ToString("F3") : "n/a")}, rightTarget={(rightHasSurface ? rightTargetY.ToString("F3") : "n/a")}, "
                    + $"leftSupport={(leftHasSurface ? leftSupportName : "none")}, rightSupport={(rightHasSurface ? rightSupportName : "none")}, "
                    + $"leftProbe=({leftProbeOffset.x:F3},{leftProbeOffset.y:F3}), rightProbe=({rightProbeOffset.x:F3},{rightProbeOffset.y:F3}), "
                    + $"leftCorrection={leftCorrection:F3}, rightCorrection={rightCorrection:F3}, "
                    + $"leftPivot={_groundContactMeshyLeftFootPivot.name}@{_groundContactMeshyLeftFootPivot.position.y:F3}, "
                    + $"rightPivot={_groundContactMeshyRightFootPivot.name}@{_groundContactMeshyRightFootPivot.position.y:F3}, "
                    + $"leftInfluence={leftFootInfluence:F3}, rightInfluence={rightFootInfluence:F3}, "
                    + $"rootFixed={root.position.ToString("F3")}";
                return;
            }

            // During a run, align only the locked support foot. Choosing the
            // lowest of both feet lets the swinging leg move the whole visual
            // root and creates a body bob. Apply the measured correction to the
            // stance-leg pivot and its world anchor instead.
            if (useLegWeightedVertices && _poseAnimator != null
                && _poseAnimator.CurrentAction == SplatSkeletonAnimator.Action.Run)
            {
                var leftStance = _poseAnimator.IsLeftRunStanceForDiagnostics
                    && !float.IsInfinity(leftFootBottom);
                var rightStance = _poseAnimator.IsRightRunStanceForDiagnostics
                    && !float.IsInfinity(rightFootBottom);
                if (!leftStance && !rightStance)
                {
                    SoleContactDiagnostics = $"renderers={_groundContactRenderers.Length}, boots=0, "
                        + $"method=weighted stance-foot vertices, leftFoot={(float.IsInfinity(leftFootBottom) ? "n/a" : leftFootBottom.ToString("F3"))}, "
                        + $"rightFoot={(float.IsInfinity(rightFootBottom) ? "n/a" : rightFootBottom.ToString("F3"))}, supportFoot=none";
                    return;
                }

                var leftHasRunSurface = false;
                var rightHasRunSurface = false;
                var leftRunSurfaceY = 0f;
                var rightRunSurfaceY = 0f;
                var leftRunSurfaceName = string.Empty;
                var rightRunSurfaceName = string.Empty;
                var leftRunProbeOffset = Vector2.zero;
                var rightRunProbeOffset = Vector2.zero;
                var leftRequestedFootCorrection = 0f;
                var rightRequestedFootCorrection = 0f;
                var runFootCorrectionStepLimit = RunGroundVisualCorrectionSpeed
                    * Mathf.Max(0f, Time.deltaTime);
                var leftAppliedFootCorrection = 0f;
                var rightAppliedFootCorrection = 0f;
                if (leftStance)
                {
                    leftHasRunSurface = TryGetGroundSurfaceY(root, controller, leftFootContactPosition,
                        out leftRunSurfaceY, out leftRunSurfaceName, out leftRunProbeOffset, out var leftRunNormal);
                    if (leftStance && leftHasRunSurface
                        && _poseAnimator.AlignGroundedFootToSurface(true, leftRunNormal))
                    {
                        TryMeasureWeightedFoot(true, out leftFootBottom, out leftFootInfluence,
                            out leftFootContactPosition);
                        leftHasRunSurface = TryGetGroundSurfaceY(root, controller, leftFootContactPosition,
                            out leftRunSurfaceY, out leftRunSurfaceName, out leftRunProbeOffset, out _);
                    }
                    var leftSupportY = leftHasRunSurface
                        ? leftRunSurfaceY - GroundVisualContactInset
                        : controller.bounds.min.y - GroundVisualContactInset;
                    leftRequestedFootCorrection = Mathf.Clamp((leftSupportY - leftFootBottom)
                        / MinimumCorrectionInfluence(leftFootInfluence),
                        -MaxGroundVisualCorrection, MaxGroundVisualCorrection);
                    leftAppliedFootCorrection = Mathf.Clamp(leftRequestedFootCorrection,
                        -runFootCorrectionStepLimit, runFootCorrectionStepLimit);
                }

                if (rightStance)
                {
                    rightHasRunSurface = TryGetGroundSurfaceY(root, controller, rightFootContactPosition,
                        out rightRunSurfaceY, out rightRunSurfaceName, out rightRunProbeOffset, out var rightRunNormal);
                    if (rightStance && rightHasRunSurface
                        && _poseAnimator.AlignGroundedFootToSurface(false, rightRunNormal))
                    {
                        TryMeasureWeightedFoot(false, out rightFootBottom, out rightFootInfluence,
                            out rightFootContactPosition);
                        rightHasRunSurface = TryGetGroundSurfaceY(root, controller, rightFootContactPosition,
                            out rightRunSurfaceY, out rightRunSurfaceName, out rightRunProbeOffset, out _);
                    }
                    var rightSupportY = rightHasRunSurface
                        ? rightRunSurfaceY - GroundVisualContactInset
                        : controller.bounds.min.y - GroundVisualContactInset;
                    rightRequestedFootCorrection = Mathf.Clamp((rightSupportY - rightFootBottom)
                        / MinimumCorrectionInfluence(rightFootInfluence),
                        -MaxGroundVisualCorrection, MaxGroundVisualCorrection);
                    rightAppliedFootCorrection = Mathf.Clamp(rightRequestedFootCorrection,
                        -runFootCorrectionStepLimit, runFootCorrectionStepLimit);
                }

                // Run stance windows can overlap, especially at walking speed.
                // Each planted leg owns its own world-space anchor, so correct
                // both independently when both are planted. Choosing only the
                // lower sole leaves the other foot floating on sloped supports.
                // Swing legs remain untouched and the visual root never moves.
                if (leftStance)
                {
                    _poseAnimator.CorrectRunStanceFootHeight(true, leftAppliedFootCorrection);
                }

                if (rightStance)
                {
                    _poseAnimator.CorrectRunStanceFootHeight(false, rightAppliedFootCorrection);
                }

                var leftPostCorrectionEstimate = leftStance && !float.IsInfinity(leftFootBottom)
                    ? leftFootBottom + leftFootInfluence * leftAppliedFootCorrection
                    : float.NaN;
                var rightPostCorrectionEstimate = rightStance && !float.IsInfinity(rightFootBottom)
                    ? rightFootBottom + rightFootInfluence * rightAppliedFootCorrection
                    : float.NaN;

                SoleContactDiagnostics = $"renderers={_groundContactRenderers.Length}, boots=0, "
                    + $"method=weighted planted-foot vertices, leftStance={leftStance}, rightStance={rightStance}, "
                    + $"cache={_groundContactCacheDiagnostics}, "
                    + $"leftFoot={(float.IsInfinity(leftFootBottom) ? "n/a" : leftFootBottom.ToString("F3"))}, "
                    + $"rightFoot={(float.IsInfinity(rightFootBottom) ? "n/a" : rightFootBottom.ToString("F3"))}, "
                    + $"leftPostCorrectionEstimate={(float.IsNaN(leftPostCorrectionEstimate) ? "n/a" : leftPostCorrectionEstimate.ToString("F3"))}, "
                    + $"rightPostCorrectionEstimate={(float.IsNaN(rightPostCorrectionEstimate) ? "n/a" : rightPostCorrectionEstimate.ToString("F3"))}, "
                    + $"leftTarget={(leftStance ? (leftHasRunSurface ? leftRunSurfaceY - GroundVisualContactInset : controller.bounds.min.y - GroundVisualContactInset).ToString("F3") : "n/a")}, "
                    + $"rightTarget={(rightStance ? (rightHasRunSurface ? rightRunSurfaceY - GroundVisualContactInset : controller.bounds.min.y - GroundVisualContactInset).ToString("F3") : "n/a")}, "
                    + $"leftSupport={(leftHasRunSurface ? leftRunSurfaceName : leftStance ? "controller-fallback" : "swing")}, "
                    + $"rightSupport={(rightHasRunSurface ? rightRunSurfaceName : rightStance ? "controller-fallback" : "swing")}, "
                    + $"leftProbeOffset=({leftRunProbeOffset.x:F3},{leftRunProbeOffset.y:F3}), "
                    + $"rightProbeOffset=({rightRunProbeOffset.x:F3},{rightRunProbeOffset.y:F3}), "
                    + $"leftRequestedCorrection={leftRequestedFootCorrection:F3}, rightRequestedCorrection={rightRequestedFootCorrection:F3}, "
                    + $"leftCorrection={leftAppliedFootCorrection:F3}, rightCorrection={rightAppliedFootCorrection:F3}, "
                    + $"runStepLimit={runFootCorrectionStepLimit:F3}";
                return;
            }

            // Idle and landing have two support points. Correct each leg pivot
            // from its own weighted sole and the surface directly beneath it;
            // translating the whole visual from only the lowest foot could leave
            // the other boot visibly suspended on a sloped or narrow pad.
            if (useLegWeightedVertices && _poseAnimator != null
                && _poseAnimator.CurrentAction != SplatSkeletonAnimator.Action.Jump)
            {
                var leftCorrection = CorrectGroundedWeightedFoot(root, controller,
                    true, ref leftFootBottom, ref leftFootInfluence, ref leftFootContactPosition,
                    out var leftHasSurface, out var leftSupportName, out var leftTargetY,
                    out var leftProbeOffset);
                var rightCorrection = CorrectGroundedWeightedFoot(root, controller,
                    false, ref rightFootBottom, ref rightFootInfluence, ref rightFootContactPosition,
                    out var rightHasSurface, out var rightSupportName, out var rightTargetY,
                    out var rightProbeOffset);
                // Report the weighted sole's estimated post-correction height as
                // well as the pre-correction bake. Logging only the pre-correction
                // minimum made a successful same-frame pivot correction look like
                // a one-frame hover in capture telemetry.
                var leftPostCorrection = float.IsInfinity(leftFootBottom)
                    ? float.NaN : leftFootBottom + leftFootInfluence * leftCorrection;
                var rightPostCorrection = float.IsInfinity(rightFootBottom)
                    ? float.NaN : rightFootBottom + rightFootInfluence * rightCorrection;
                SoleContactDiagnostics = $"renderers={_groundContactRenderers.Length}, boots=0, "
                    + $"method=weighted independent foot pivots, footBones={_poseAnimator.HasFootPivots}, leftEuler={_poseAnimator.BoundLeftFootEuler.ToString("F1")}, rightEuler={_poseAnimator.BoundRightFootEuler.ToString("F1")}, leftPreCorrection={(float.IsInfinity(leftFootBottom) ? "n/a" : leftFootBottom.ToString("F3"))}, "
                    + $"cache={_groundContactCacheDiagnostics}, "
                    + $"rightPreCorrection={(float.IsInfinity(rightFootBottom) ? "n/a" : rightFootBottom.ToString("F3"))}, "
                    + $"leftInfluence={leftFootInfluence:F2}, rightInfluence={rightFootInfluence:F2}, "
                    + $"samplesL=({_leftFootSampleEvaluationDiagnostics}), samplesR=({_rightFootSampleEvaluationDiagnostics}), "
                    + $"leftPostCorrectionEstimate={(float.IsNaN(leftPostCorrection) ? "n/a" : leftPostCorrection.ToString("F3"))}, "
                    + $"rightPostCorrectionEstimate={(float.IsNaN(rightPostCorrection) ? "n/a" : rightPostCorrection.ToString("F3"))}, "
                    + $"leftPoint={leftFootContactPosition.ToString("F3")}, rightPoint={rightFootContactPosition.ToString("F3")}, "
                    + $"leftSupport={(leftHasSurface ? leftSupportName : "controller-fallback")}, rightSupport={(rightHasSurface ? rightSupportName : "controller-fallback")}, "
                    + $"leftProbeOffset=({leftProbeOffset.x:F3},{leftProbeOffset.y:F3}), rightProbeOffset=({rightProbeOffset.x:F3},{rightProbeOffset.y:F3}), "
                    + $"leftTarget={leftTargetY:F3}, rightTarget={rightTargetY:F3}, "
                    + $"leftCorrection={leftCorrection:F3}, rightCorrection={rightCorrection:F3}";
                return;
            }

            if (float.IsInfinity(lowestSole))
            {
                return;
            }

            // Align the boot surface to the actual support under that boot. The body
            // skin extends below the boots, so its aggregate bounds cannot represent
            // foot contact. CharacterController bounds also include a skin margin.
            // Keep a controller-derived fallback for unusual contacts where the
            // short non-allocating ray misses the support.
            var probe = lowestSoleRenderer != null ? lowestSolePosition : controller.bounds.center;
            var targetSole = TryGetGroundSurfaceY(root, controller, probe,
                out var surfaceY, out _, out _, out _)
                ? surfaceY - GroundVisualContactInset
                : controller.bounds.min.y - GroundVisualContactInset;
            // The measured points are now leg-weighted or dedicated boot vertices,
            // so allow enough visual-only correction to seat tucked landing feet
            // immediately without changing the CharacterController or gameplay root.
            var requestedCorrection = Mathf.Clamp(targetSole - lowestSole,
                -MaxGroundVisualCorrection, MaxGroundVisualCorrection);
            // The jump-to-Land pose can move its weighted foot vertices by several
            // centimetres in one frame. Applying the whole correction to the visual
            // root caused a visible snap on the touchdown frame and a counter-snap
            // as the pose settled. Converge over a few frames so the landing reads
            // as a short settle while keeping the gameplay collider untouched.
            var correctionSpeed = _poseAnimator != null
                && _poseAnimator.CurrentAction == SplatSkeletonAnimator.Action.Land
                ? LandingGroundVisualCorrectionSpeed
                : MaxGroundVisualCorrectionSpeed;
            var maximumFrameCorrection = correctionSpeed * Mathf.Max(0f, Time.deltaTime);
            if (_poseAnimator != null
                && _poseAnimator.CurrentAction == SplatSkeletonAnimator.Action.Land)
            {
                maximumFrameCorrection = Mathf.Min(maximumFrameCorrection,
                    LandingGroundVisualCorrectionMaxStep);
            }
            var correction = Mathf.Clamp(requestedCorrection,
                -maximumFrameCorrection, maximumFrameCorrection);
            SoleContactDiagnostics = $"renderers={_groundContactRenderers.Length}, boots={_groundContactBootRenderers.Length}, "
                + $"method={(useDedicatedBoots ? "boot renderer bounds" : useLegWeightedVertices ? "baked leg-weighted vertices" : _groundContactSkinnedRenderers.Length > 0 ? "baked skinned vertices" : "renderer bounds")}, "
                + $"measured={(lowestSoleRenderer != null ? lowestSoleRenderer.name : "none")}, "
                + $"bottom={lowestSole:F3}, leftFoot={(float.IsInfinity(leftFootBottom) ? "n/a" : leftFootBottom.ToString("F3"))}, "
                + $"rightFoot={(float.IsInfinity(rightFootBottom) ? "n/a" : rightFootBottom.ToString("F3"))}, "
                + $"target={targetSole:F3}, requestedCorrection={requestedCorrection:F3}, correction={correction:F3}";
            if (Mathf.Abs(correction) > 0.001f)
            {
                visual.position += Vector3.up * correction;
            }
        }

        private float CorrectGroundedWeightedFoot(Transform root, CharacterController controller,
            bool leftFoot, ref float footBottom, ref float legInfluence, ref Vector3 soleContactPosition,
            out bool hasSurface, out string supportName, out float targetY,
            out Vector2 supportProbeOffset)
        {
            hasSurface = false;
            supportName = string.Empty;
            targetY = 0f;
            supportProbeOffset = Vector2.zero;
            if (float.IsInfinity(footBottom)) return 0f;
            // Probe beneath the lowest actually rendered sole vertex. A leg pivot
            // can sit well above and behind the foot, which selected the wrong
            // surface on sloped platforms and narrow ledges.
            hasSurface = TryGetGroundSurfaceY(root, controller, soleContactPosition,
                out var surfaceY, out supportName, out supportProbeOffset, out var surfaceNormal);
            if (hasSurface && _poseAnimator.AlignGroundedFootToSurface(leftFoot, surfaceNormal))
            {
                TryMeasureWeightedFoot(leftFoot, out footBottom, out legInfluence, out soleContactPosition);
                hasSurface = TryGetGroundSurfaceY(root, controller, soleContactPosition,
                    out surfaceY, out supportName, out supportProbeOffset, out _);
            }
            targetY = hasSurface
                ? surfaceY - GroundVisualContactInset
                : controller.bounds.min.y - GroundVisualContactInset;
            // A weighted vertex follows its leg pivot only by that bone's share
            // of the skin weight. Compensate for the measured vertex influence so
            // a 75%-weighted sole moves far enough to close the actual gap.
            var requested = Mathf.Clamp((targetY - footBottom)
                / MinimumCorrectionInfluence(legInfluence),
                -MaxGroundVisualCorrection, MaxGroundVisualCorrection);
            var correctionSpeed = _poseAnimator.CurrentAction == SplatSkeletonAnimator.Action.Land
                ? LandingGroundVisualCorrectionSpeed : MaxGroundVisualCorrectionSpeed;
            var maxStep = correctionSpeed * Mathf.Max(0f, Time.deltaTime);
            if (_poseAnimator.CurrentAction == SplatSkeletonAnimator.Action.Land)
                maxStep = Mathf.Min(maxStep, LandingGroundVisualCorrectionMaxStep);
            var correction = Mathf.Clamp(requested, -maxStep, maxStep);
            _poseAnimator.CorrectGroundedFootHeight(leftFoot, correction);
            return correction;
        }

        private string CorrectRigidFootContact(Transform root, CharacterController controller, bool leftFoot)
        {
            if (_poseAnimator == null) return "animator missing";
            var action = _poseAnimator.CurrentAction;
            if (action != SplatSkeletonAnimator.Action.Idle
                && action != SplatSkeletonAnimator.Action.Run
                && action != SplatSkeletonAnimator.Action.Land)
                return $"ignored action={action}";
            if (action == SplatSkeletonAnimator.Action.Run
                && (leftFoot ? !_poseAnimator.IsLeftRunStanceForDiagnostics
                    : !_poseAnimator.IsRightRunStanceForDiagnostics))
                return "swing foot";

            var renderers = leftFoot ? _groundContactLeftFootRenderers : _groundContactRightFootRenderers;
            if (!TryGetRendererBounds(renderers, out var bounds, out _)) return "foot renderer missing";
            var localFootVertices = leftFoot ? _groundContactLeftFootVertices : _groundContactRightFootVertices;
            var hasSoleVertex = TryGetLowestFootVertex(renderers, localFootVertices, out var solePoint);
            var probe = hasSoleVertex ? solePoint : bounds.center;
            var hasSurface = TryGetGroundSurfaceY(root, controller, probe,
                out var surfaceY, out var surfaceName, out var selectedProbeOffset, out var surfaceNormal);
            if (hasSurface && _poseAnimator.AlignGroundedFootToSurface(leftFoot, surfaceNormal)
                && TryGetRendererBounds(renderers, out bounds, out _))
            {
                hasSoleVertex = TryGetLowestFootVertex(renderers, localFootVertices, out solePoint);
                probe = hasSoleVertex ? solePoint : bounds.center;
                hasSurface = TryGetGroundSurfaceY(root, controller, probe,
                    out surfaceY, out surfaceName, out selectedProbeOffset, out _);
            }

            var targetY = hasSurface ? surfaceY - GroundVisualContactInset
                : controller.bounds.min.y - GroundVisualContactInset;
            // A rotated renderer AABB's minimum Y may belong to a downhill
            // corner, while bounds.center samples a different support height.
            // Align the actual lowest mesh point to the surface directly below
            // it so a correct AABB does not leave the visible sole floating.
            var currentSoleY = hasSoleVertex ? solePoint.y : bounds.min.y;
            var requested = Mathf.Clamp(targetY - currentSoleY,
                -MaxGroundVisualCorrection, MaxGroundVisualCorrection);
            var stepSpeed = action == SplatSkeletonAnimator.Action.Run
                ? RunGroundVisualCorrectionSpeed
                : action == SplatSkeletonAnimator.Action.Land
                    ? LandingGroundVisualCorrectionSpeed : MaxGroundVisualCorrectionSpeed;
            var stepLimit = stepSpeed * Mathf.Max(0f, Time.deltaTime);
            if (action == SplatSkeletonAnimator.Action.Land)
                stepLimit = Mathf.Min(stepLimit, LandingGroundVisualCorrectionMaxStep);
            var correction = Mathf.Clamp(requested, -stepLimit, stepLimit);
            if (action == SplatSkeletonAnimator.Action.Run)
                _poseAnimator.CorrectRunStanceFootHeight(leftFoot, correction);
            else
                _poseAnimator.CorrectGroundedFootHeight(leftFoot, correction);
            var hasCorrectedSole = TryGetLowestFootVertex(renderers, localFootVertices, out var correctedSolePoint);
            if (hasCorrectedSole)
            {
                if (leftFoot) LeftSoleWorldPointForDiagnostics = correctedSolePoint;
                else RightSoleWorldPointForDiagnostics = correctedSolePoint;
            }
            return $"action={action}, support={(hasSurface ? surfaceName : "capsule")}, "
                + $"surface={(hasSurface ? surfaceY.ToString("F3") : "n/a")}, supportProbeOffset={selectedProbeOffset:F3}, "
                + $"sole={(hasSoleVertex ? currentSoleY : bounds.min.y):F3}, target={targetY:F3}, request={requested:F3}, "
                + $"step={correction:F3}, remaining~={(requested - correction):F3}m, "
                + $"correctedSole={(hasCorrectedSole ? correctedSolePoint.ToString("F3") : "bounds fallback")}";
        }

        private static Vector3[][] CacheRigidFootVertices(Renderer[] renderers)
        {
            var cached = new Vector3[renderers != null ? renderers.Length : 0][];
            for (var i = 0; i < cached.Length; i++)
            {
                var filter = renderers[i] != null
                    ? renderers[i].GetComponent<MeshFilter>() : null;
                var mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || !mesh.isReadable)
                {
                    continue;
                }

                try
                {
                    cached[i] = mesh.vertices;
                }
                catch
                {
                    // Keep the renderer-bounds fallback for non-readable or
                    // engine-owned mesh variants.
                }
            }

            return cached;
        }

        private static bool TryGetLowestFootVertex(Renderer[] renderers,
            Vector3[][] localVertices, out Vector3 lowestPoint)
        {
            lowestPoint = Vector3.zero;
            var lowestY = float.PositiveInfinity;
            if (renderers == null || localVertices == null)
            {
                return false;
            }

            for (var rendererIndex = 0; rendererIndex < renderers.Length
                && rendererIndex < localVertices.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var vertices = localVertices[rendererIndex];
                if (renderer == null || vertices == null || vertices.Length == 0
                    || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    var worldPoint = renderer.transform.TransformPoint(vertices[vertexIndex]);
                    if (worldPoint.y < lowestY)
                    {
                        lowestY = worldPoint.y;
                        lowestPoint = worldPoint;
                    }
                }
            }

            return !float.IsPositiveInfinity(lowestY);
        }

        private static bool TryGetRendererBounds(Renderer[] renderers, out Bounds bounds, out string name)
        {
            bounds = default;
            name = string.Empty;
            var found = false;
            if (renderers == null) return false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    name = renderer.name;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                    name += "," + renderer.name;
                }
            }
            return found;
        }

        private bool HasRigidFootRenderers() => AreRigid(_groundContactLeftFootRenderers)
            && AreRigid(_groundContactRightFootRenderers);

        private static bool AreRigid(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return false;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is SkinnedMeshRenderer) return false;
            }
            return true;
        }

        private static string RigidFootDiagnostic(Renderer[] renderers)
        {
            return TryGetRendererBounds(renderers, out var bounds, out var name)
                ? $"{name}:sole={bounds.min.y:F3}m" : "missing";
        }

        private static bool HasFootSideToken(string objectName, bool left)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            var side = left ? "L" : "R";
            return objectName.IndexOf("Foot" + side, System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Boot" + side, System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Sole" + side, System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Foot_" + (left ? "Left" : "Right"), System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Boot_" + (left ? "Left" : "Right"), System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryMeasureWeightedFoot(bool leftFoot, out float footBottom,
            out float footInfluence, out Vector3 footContactPosition)
        {
            footBottom = float.PositiveInfinity;
            footInfluence = 1f;
            footContactPosition = Vector3.zero;
            Renderer overallRenderer = null;
            var overallBottom = float.PositiveInfinity;
            var overallPosition = Vector3.zero;
            for (var rendererIndex = 0; rendererIndex < _groundContactSkinnedRenderers.Length; rendererIndex++)
            {
                var skinned = _groundContactSkinnedRenderers[rendererIndex];
                if (skinned == null || !skinned.enabled || !skinned.gameObject.activeInHierarchy
                    || skinned.sharedMesh == null)
                    continue;

                var contactVertices = leftFoot
                    ? _groundContactLeftLegVertices[rendererIndex]
                    : _groundContactRightLegVertices[rendererIndex];
                if (contactVertices == null || contactVertices.Length == 0)
                    continue;

                var canEvaluateSamples = rendererIndex < _groundContactCanEvaluateSamples.Length
                    && _groundContactCanEvaluateSamples[rendererIndex];
                if (!canEvaluateSamples)
                {
                    _groundContactBakedMesh.Clear();
                    skinned.BakeMesh(_groundContactBakedMesh);
                    _groundContactBakedVertices.Clear();
                    _groundContactBakedMesh.GetVertices(_groundContactBakedVertices);
                }
                if (leftFoot)
                {
                    AccumulateWeightedFoot(rendererIndex, skinned, _groundContactLeftLegVertices[rendererIndex],
                        _groundContactLeftLegWeights[rendererIndex], true, ref footBottom,
                        ref footInfluence, ref footContactPosition, ref overallBottom,
                        ref overallPosition, ref overallRenderer);
                }
                else
                {
                    AccumulateWeightedFoot(rendererIndex, skinned, _groundContactRightLegVertices[rendererIndex],
                        _groundContactRightLegWeights[rendererIndex], false, ref footBottom,
                        ref footInfluence, ref footContactPosition, ref overallBottom,
                        ref overallPosition, ref overallRenderer);
                }
            }
            return !float.IsInfinity(footBottom);
        }

        private float CorrectMeshyWeightedFoot(Transform root, CharacterController controller,
            bool leftFoot, Transform footPivot, ref float footBottom, ref float legInfluence,
            ref Vector3 soleContactPosition, out bool hasSurface, out string supportName,
            out float targetY, out Vector2 supportProbeOffset)
        {
            hasSurface = false;
            supportName = string.Empty;
            targetY = 0f;
            supportProbeOffset = Vector2.zero;
            if (footPivot == null || float.IsInfinity(footBottom)) return 0f;

            hasSurface = TryGetGroundSurfaceY(root, controller, soleContactPosition,
                out var surfaceY, out supportName, out supportProbeOffset, out var surfaceNormal);
            if (!hasSurface) return 0f;

            // Keep the imported ankle's animated bend, but orient its sole to
            // this foot's own walkable contact normal. Animator evaluation resets
            // the pose next frame, so the correction cannot accumulate.
            if (surfaceNormal.sqrMagnitude > 0.5f)
            {
                var alignedRotation = Quaternion.FromToRotation(footPivot.up, surfaceNormal.normalized)
                    * footPivot.rotation;
                if (Quaternion.Angle(footPivot.rotation, alignedRotation) > 0.15f)
                {
                    footPivot.rotation = alignedRotation;
                    TryMeasureWeightedFoot(leftFoot, out footBottom, out legInfluence,
                        out soleContactPosition);
                    hasSurface = TryGetGroundSurfaceY(root, controller, soleContactPosition,
                        out surfaceY, out supportName, out supportProbeOffset, out _);
                    if (!hasSurface) return 0f;
                }
            }

            targetY = surfaceY - GroundVisualContactInset;
            var gap = targetY - footBottom;
            var isMoving = _groundMotor != null && _groundMotor.PlanarVelocity.sqrMagnitude > 0.0144f;
            // A swing foot can be well above the platform while Splat is running.
            // Never drag that foot down to the ground; correct only near-contact
            // penetration/hover on its short stance window.
            if (isMoving && gap < -0.075f) return 0f;

            var requested = Mathf.Clamp(gap / MinimumCorrectionInfluence(legInfluence),
                -MaxGroundVisualCorrection, MaxGroundVisualCorrection);
            // This is a pose-space correction re-applied after Animator every
            // frame, not a persistent transform lerp: Animator restores the
            // authored joint pose before the next correction. A deltaTime step
            // therefore never converges and leaves the same residual hover on
            // every frame. Apply the bounded weighted-vertex solution directly;
            // the existing MaxGroundVisualCorrection cap prevents large snaps.
            var applied = requested;
            if (Mathf.Abs(applied) <= 0.0001f) return 0f;

            footPivot.position += Vector3.up * applied;
            TryMeasureWeightedFoot(leftFoot, out footBottom, out legInfluence,
                out soleContactPosition);
            if (leftFoot) LeftSoleWorldPointForDiagnostics = soleContactPosition;
            else RightSoleWorldPointForDiagnostics = soleContactPosition;
            return applied;
        }

        private static Transform FindMeshyFootPivot(SkinnedMeshRenderer[] renderers, bool leftFoot)
        {
            // The production Meshy skin weights contact geometry across the
            // lower leg, foot, and toe bones. Move the lower-leg parent first so
            // the full weighted chain follows the same correction; adjusting a
            // foot leaf alone can leave the measured sole unchanged when its
            // lowest vertices are carried by ToeBase or Leg influences.
            var preferred = leftFoot
                ? new[] { "LeftLeg", "Bone_LegL", "LeftFoot", "Bone_FootL", "LeftToeBase" }
                : new[] { "RightLeg", "Bone_LegR", "RightFoot", "Bone_FootR", "RightToeBase" };
            if (renderers == null) return null;
            for (var nameIndex = 0; nameIndex < preferred.Length; nameIndex++)
            {
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var bones = renderers[rendererIndex] != null ? renderers[rendererIndex].bones : null;
                    if (bones == null) continue;
                    for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {
                        if (bones[boneIndex] != null && bones[boneIndex].name == preferred[nameIndex])
                            return bones[boneIndex];
                    }
                }
            }
            return null;
        }

        private static Transform ResolveSplatVisualRoot(Transform root)
        {
            if (root == null) return null;
            if (SplatMeshyVisualMode.IsEnabledOn(root))
                return SplatMeshyVisualMode.ResolveVisual(root);
            return root.Find(OrganicSplatBuilder.VisualRootName);
        }

        private float MinimumCorrectionInfluence(float measuredInfluence)
        {
            var minimum = _poseAnimator != null && _poseAnimator.HasFootPivots
                ? MinimumFootWeightForContact : MinimumLegWeightForFootContact;
            return Mathf.Max(minimum, measuredInfluence);
        }

        private void CacheLegWeightedVertices()
        {
            var diagnostics = new List<string>();
            _groundContactLeftLegVertices = new int[_groundContactSkinnedRenderers.Length][];
            _groundContactRightLegVertices = new int[_groundContactSkinnedRenderers.Length][];
            _groundContactLeftLegWeights = new float[_groundContactSkinnedRenderers.Length][];
            _groundContactRightLegWeights = new float[_groundContactSkinnedRenderers.Length][];
            _groundContactSourceVertices = new Vector3[_groundContactSkinnedRenderers.Length][];
            _groundContactSourceWeights = new BoneWeight[_groundContactSkinnedRenderers.Length][];
            _groundContactBindPoses = new Matrix4x4[_groundContactSkinnedRenderers.Length][];
            _groundContactBones = new Transform[_groundContactSkinnedRenderers.Length][];
            _groundContactCanEvaluateSamples = new bool[_groundContactSkinnedRenderers.Length];
            for (var rendererIndex = 0; rendererIndex < _groundContactSkinnedRenderers.Length; rendererIndex++)
            {
                var skinned = _groundContactSkinnedRenderers[rendererIndex];
                var mesh = skinned != null ? skinned.sharedMesh : null;
                var bones = skinned != null ? skinned.bones : null;
                if (mesh == null || bones == null)
                {
                    diagnostics.Add($"{skinned?.name ?? "<null>"}:mesh={mesh != null},bones={bones?.Length ?? 0}");
                    continue;
                }

                // Cache the source skin data once. On ordinary four-influence
                // meshes without blend shapes, the contact pass can transform
                // only the leg-weighted vertices through their four bone matrices
                // instead of baking every vertex of the full character every
                // grounded frame. Keep BakeMesh for other mesh types below.
                var sourceWeights = mesh.boneWeights;
                var bindPoses = mesh.bindposes;
                // Mesh.vertices is empty when Read/Write is disabled in the model
                // importer, even though vertexCount and boneWeights remain populated.
                // In that case the cached-sample path would silently skip every
                // contact vertex; use BakeMesh below, which can read the posed skin.
                var canEvaluateSamples = mesh.isReadable && mesh.blendShapeCount == 0
                    && mesh.vertexCount == sourceWeights.Length
                    && bindPoses.Length == bones.Length && bones.Length > 0
                    && UsesAtMostFourSkinInfluences(mesh);
                _groundContactCanEvaluateSamples[rendererIndex] = canEvaluateSamples;
                _groundContactSourceVertices[rendererIndex] = canEvaluateSamples
                    ? mesh.vertices : System.Array.Empty<Vector3>();
                _groundContactSourceWeights[rendererIndex] = canEvaluateSamples
                    ? sourceWeights : System.Array.Empty<BoneWeight>();
                _groundContactBindPoses[rendererIndex] = canEvaluateSamples
                    ? bindPoses : System.Array.Empty<Matrix4x4>();
                _groundContactBones[rendererIndex] = canEvaluateSamples
                    ? bones : System.Array.Empty<Transform>();

                var leftBones = FindContactBoneIndices(bones, true);
                var rightBones = FindContactBoneIndices(bones, false);
                if (leftBones.Length == 0 && rightBones.Length == 0)
                {
                    diagnostics.Add($"{skinned.name}:v={mesh.vertexCount},w={sourceWeights.Length},bind={bindPoses.Length},bones={bones.Length},footBones=missing");
                    continue;
                }

                var weights = sourceWeights;
                var leftVertices = new List<int>();
                var rightVertices = new List<int>();
                var leftWeights = new List<float>();
                var rightWeights = new List<float>();
                for (var vertexIndex = 0; vertexIndex < weights.Length; vertexIndex++)
                {
                    var weight = weights[vertexIndex];
                    // Contact samples should come from vertices primarily carried
                    // by the leg, not broad hip/body skin with only a small leg
                    // influence. Moving the leg pivot cannot seat a 20%-weighted
                    // torso vertex by the same amount as the actual boot sole.
                    var leftIsFootBone = leftBones.Length == 1 && bones[leftBones[0]] != null
                        && bones[leftBones[0]].name == "Bone_FootL";
                    var rightIsFootBone = rightBones.Length == 1 && bones[rightBones[0]] != null
                        && bones[rightBones[0]].name == "Bone_FootR";
                    var leftThreshold = leftIsFootBone
                        ? MinimumFootWeightForContact : MinimumLegWeightForFootContact;
                    var rightThreshold = rightIsFootBone
                        ? MinimumFootWeightForContact : MinimumLegWeightForFootContact;
                    var leftWeight = WeightForBones(weight, leftBones);
                    var rightWeight = WeightForBones(weight, rightBones);
                    if (leftWeight >= leftThreshold)
                    {
                        leftVertices.Add(vertexIndex);
                        leftWeights.Add(leftWeight);
                    }
                    if (rightWeight >= rightThreshold)
                    {
                        rightVertices.Add(vertexIndex);
                        rightWeights.Add(rightWeight);
                    }
                }

                _groundContactLeftLegVertices[rendererIndex] = leftVertices.ToArray();
                _groundContactRightLegVertices[rendererIndex] = rightVertices.ToArray();
                _groundContactLeftLegWeights[rendererIndex] = leftWeights.ToArray();
                _groundContactRightLegWeights[rendererIndex] = rightWeights.ToArray();
                diagnostics.Add($"{skinned.name}:v={mesh.vertexCount},w={sourceWeights.Length},bind={bindPoses.Length},bones={bones.Length},leftBones={FormatBoneNames(bones, leftBones)}[{leftVertices.Count}],rightBones={FormatBoneNames(bones, rightBones)}[{rightVertices.Count}],eval={canEvaluateSamples}");
            }
            _groundContactCacheDiagnostics = string.Join(";", diagnostics);
        }

        private static int[] FindContactBoneIndices(Transform[] bones, bool leftFoot)
        {
            if (bones == null) return System.Array.Empty<int>();
            var side = leftFoot ? "Left" : "Right";
            var authoredFoot = leftFoot ? "Bone_FootL" : "Bone_FootR";
            var authoredLeg = leftFoot ? "Bone_LegL" : "Bone_LegR";
            var authoredIndex = System.Array.FindIndex(bones,
                bone => bone != null && bone.name == authoredFoot);
            if (authoredIndex >= 0) return new[] { authoredIndex };
            authoredIndex = System.Array.FindIndex(bones,
                bone => bone != null && bone.name == authoredLeg);
            if (authoredIndex >= 0) return new[] { authoredIndex };

            // Meshy humanoid exports use weighted foot/ankle/toe chains rather
            // than the project's Bone_FootL/Bone_LegL names. Sum only the distal
            // side-specific chain, so shared hips or torso weights cannot be
            // mistaken for sole samples. This also handles soles split across
            // LeftLeg and LeftToeBase (as found in the Iter1337 candidate).
            var aliases = new[] { side + "Foot", side + "ToeBase", side + "Leg" };
            var indices = new List<int>(aliases.Length);
            for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
            {
                var boneIndex = System.Array.FindIndex(bones,
                    bone => bone != null && bone.name == aliases[aliasIndex]);
                if (boneIndex >= 0) indices.Add(boneIndex);
            }
            return indices.ToArray();
        }

        private static float WeightForBones(BoneWeight weight, int[] boneIndices)
        {
            var total = 0f;
            for (var i = 0; i < boneIndices.Length; i++)
                total += WeightForBone(weight, boneIndices[i]);
            return total;
        }

        private static string FormatBoneNames(Transform[] bones, int[] indices)
        {
            if (indices == null || indices.Length == 0) return "missing";
            var names = new string[indices.Length];
            for (var i = 0; i < indices.Length; i++)
                names[i] = indices[i] >= 0 && indices[i] < bones.Length && bones[indices[i]] != null
                    ? bones[indices[i]].name : "?";
            return string.Join("+", names);
        }

        private bool HasLegWeightedVertices()
        {
            for (var i = 0; i < _groundContactLeftLegVertices.Length; i++)
            {
                if ((_groundContactLeftLegVertices[i]?.Length ?? 0) > 0
                    || (_groundContactRightLegVertices[i]?.Length ?? 0) > 0)
                    return true;
            }
            return false;
        }

        private void AccumulateWeightedFoot(int rendererIndex, SkinnedMeshRenderer skinned,
            int[] indices, float[] contactWeights, bool leftFoot, ref float footBottom, ref float footInfluence,
            ref Vector3 footContactPosition,
            ref float overallBottom,
            ref Vector3 overallPosition, ref Renderer overallRenderer)
        {
            if (indices == null || contactWeights == null || skinned.sharedMesh == null) return;
            var footPosition = Vector3.zero;
            Renderer footRenderer = null;
            var finiteSamples = 0;
            var nonFiniteSamples = 0;
            var skippedWeightSamples = 0;
            var skippedSourceSamples = 0;
            var skippedBakedSamples = 0;
            var minimumSampleY = float.PositiveInfinity;
            var maximumSampleY = float.NegativeInfinity;
            for (var i = 0; i < indices.Length; i++)
            {
                var vertexIndex = indices[i];
                if (i >= contactWeights.Length)
                {
                    skippedWeightSamples++;
                    continue;
                }
                var canEvaluateSamples = rendererIndex < _groundContactCanEvaluateSamples.Length
                    && _groundContactCanEvaluateSamples[rendererIndex];
                Vector3 worldVertex;
                if (canEvaluateSamples)
                {
                    if (vertexIndex < 0 || vertexIndex >= _groundContactSourceVertices[rendererIndex].Length)
                    {
                        skippedSourceSamples++;
                        continue;
                    }
                    worldVertex = EvaluateSkinnedVertexWorld(rendererIndex, vertexIndex);
                }
                else
                {
                    if (vertexIndex < 0 || vertexIndex >= _groundContactBakedVertices.Count)
                    {
                        skippedBakedSamples++;
                        continue;
                    }
                    worldVertex = skinned.transform.position
                        + skinned.transform.rotation * _groundContactBakedVertices[vertexIndex];
                }
                if (float.IsNaN(worldVertex.x) || float.IsNaN(worldVertex.y) || float.IsNaN(worldVertex.z)
                    || float.IsInfinity(worldVertex.x) || float.IsInfinity(worldVertex.y) || float.IsInfinity(worldVertex.z))
                {
                    nonFiniteSamples++;
                    continue;
                }
                finiteSamples++;
                minimumSampleY = Mathf.Min(minimumSampleY, worldVertex.y);
                maximumSampleY = Mathf.Max(maximumSampleY, worldVertex.y);
                if (worldVertex.y < footBottom)
                {
                    footBottom = worldVertex.y;
                    footInfluence = contactWeights[i];
                    footContactPosition = worldVertex;
                    footPosition = worldVertex;
                    footRenderer = skinned;
                }
            }

            if (footBottom < overallBottom)
            {
                overallBottom = footBottom;
                overallPosition = footPosition;
                overallRenderer = footRenderer;
            }
            var samples = $"vertices={indices.Length},weights={contactWeights.Length},finite={finiteSamples},nonfinite={nonFiniteSamples},"
                + $"skipWeight={skippedWeightSamples},skipSource={skippedSourceSamples},skipBaked={skippedBakedSamples},"
                + $"minY={(float.IsInfinity(minimumSampleY) ? "n/a" : minimumSampleY.ToString("F3"))},"
                + $"maxY={(float.IsInfinity(maximumSampleY) ? "n/a" : maximumSampleY.ToString("F3"))}";
            if (leftFoot) _leftFootSampleEvaluationDiagnostics = samples;
            else _rightFootSampleEvaluationDiagnostics = samples;
        }

        private Vector3 EvaluateSkinnedVertexWorld(int rendererIndex, int vertexIndex)
        {
            var vertex = _groundContactSourceVertices[rendererIndex][vertexIndex];
            var weight = _groundContactSourceWeights[rendererIndex][vertexIndex];
            var bindPoses = _groundContactBindPoses[rendererIndex];
            var bones = _groundContactBones[rendererIndex];
            var world = Vector3.zero;
            AccumulateSkinInfluence(vertex, weight.boneIndex0, weight.weight0, bindPoses, bones, ref world);
            AccumulateSkinInfluence(vertex, weight.boneIndex1, weight.weight1, bindPoses, bones, ref world);
            AccumulateSkinInfluence(vertex, weight.boneIndex2, weight.weight2, bindPoses, bones, ref world);
            AccumulateSkinInfluence(vertex, weight.boneIndex3, weight.weight3, bindPoses, bones, ref world);
            return world;
        }

        private static bool UsesAtMostFourSkinInfluences(Mesh mesh)
        {
            if (mesh == null) return false;
            var influencesPerVertex = mesh.GetBonesPerVertex();
            try
            {
                for (var i = 0; i < influencesPerVertex.Length; i++)
                {
                    if (influencesPerVertex[i] > 4) return false;
                }
                return true;
            }
            finally
            {
                influencesPerVertex.Dispose();
            }
        }

        private static void AccumulateSkinInfluence(Vector3 vertex, int boneIndex, float weight,
            Matrix4x4[] bindPoses, Transform[] bones, ref Vector3 world)
        {
            if (weight <= 0f || boneIndex < 0 || boneIndex >= bindPoses.Length
                || boneIndex >= bones.Length || bones[boneIndex] == null)
                return;

            var bindSpaceVertex = bindPoses[boneIndex].MultiplyPoint3x4(vertex);
            world += bones[boneIndex].localToWorldMatrix.MultiplyPoint3x4(bindSpaceVertex) * weight;
        }

        private static void ConsiderBakedVertex(SkinnedMeshRenderer skinned, Vector3 vertex,
            ref float lowest, ref Vector3 lowestPosition, ref Renderer lowestRenderer)
        {
            // BakeMesh returns vertices relative to the renderer transform and
            // includes its scale by default. Apply rotation and translation once.
            var worldVertex = skinned.transform.position + skinned.transform.rotation * vertex;
            if (worldVertex.y >= lowest) return;
            lowest = worldVertex.y;
            lowestPosition = worldVertex;
            lowestRenderer = skinned;
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var total = 0f;
            if (weight.boneIndex0 == boneIndex) total += weight.weight0;
            if (weight.boneIndex1 == boneIndex) total += weight.weight1;
            if (weight.boneIndex2 == boneIndex) total += weight.weight2;
            if (weight.boneIndex3 == boneIndex) total += weight.weight3;
            return total;
        }


        private bool TryGetGroundSurfaceY(Transform root, CharacterController controller,
            Vector3 probePosition, out float surfaceY, out string surfaceName,
            out Vector2 selectedProbeOffset, out Vector3 surfaceNormal)
        {
            surfaceY = 0f;
            surfaceName = string.Empty;
            selectedProbeOffset = Vector2.zero;
            surfaceNormal = root != null ? root.up : Vector3.up;
            var bounds = controller.bounds;
            var distance = bounds.extents.y + 0.25f;
            var offsetRadius = controller.radius * 0.65f;
            // Pick the support closest to the controller's actual foot plane.
            // Taking the highest nearby hit can pull Splat upward beside a ledge
            // or a stacked pad, leaving both feet visibly above the surface.
            var expectedSupportY = bounds.min.y;
            var bestScore = float.PositiveInfinity;
            var closest = float.NegativeInfinity;
            var closestNormal = surfaceNormal;
            var found = false;
            for (var probeIndex = 0; probeIndex < GroundProbeOffsets.Length; probeIndex++)
            {
                var offset = GroundProbeOffsets[probeIndex] * offsetRadius;
                var origin = new Vector3(probePosition.x + offset.x, bounds.center.y + 0.05f,
                    probePosition.z + offset.y);
                var count = Physics.RaycastNonAlloc(origin, Vector3.down, _groundContactHits, distance,
                    _groundMotor != null ? _groundMotor.GroundSupportMask
                        : SplatMotor.DefaultWalkableGroundMask,
                    QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                {
                    var hit = _groundContactHits[i];
                    var collider = hit.collider;
                    if (collider == null || hit.normal.y < 0.65f || collider.transform == root
                        || collider.transform.IsChildOf(root)
                        || collider.GetComponentInParent<SplatMotor>() != null)
                    {
                        continue;
                    }

                    // A grounded controller may be on a raised pad while a foot
                    // probe near its edge sees the floor underneath. The fixed
                    // margin rejects that lower flat floor. On an incline,
                    // however, bounds.min.y is
                    // the bottom of the controller's world AABB, not the support
                    // height at this foot. Allow the surface-height difference
                    // across that horizontal radius using the candidate normal;
                    // a flat lower floor still receives no extra allowance.
                    var normalY = Mathf.Max(0.001f, hit.normal.y);
                    var horizontalNormal = Mathf.Sqrt(Mathf.Max(0f, 1f - normalY * normalY));
                    var slopeHeightAllowance = Mathf.Max(bounds.extents.x, bounds.extents.z)
                        * horizontalNormal / normalY;
                    var minimumSupportY = expectedSupportY - controller.skinWidth - 0.04f
                        - slopeHeightAllowance;
                    if (hit.point.y < minimumSupportY)
                    {
                        continue;
                    }

                    var verticalError = Mathf.Abs(hit.point.y - expectedSupportY);
                    // The center probe wins close ties, while a nearby support
                    // still works when the lowest visible foot is near an edge.
                    var horizontalPenalty = offset.sqrMagnitude * 0.08f;
                    var score = verticalError + horizontalPenalty;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        closest = hit.point.y;
                        closestNormal = hit.normal;
                        surfaceName = collider.name;
                        selectedProbeOffset = offset;
                        found = true;
                    }
                }

                // A nearby support sample can differ in height by centimetres
                // on an incline, even when the measured sole point has valid
                // ground directly beneath it. Prefer that exact point first;
                // retain the surrounding probes as edge/fallback support.
                if (probeIndex == 0 && found)
                {
                    surfaceY = closest;
                    surfaceNormal = closestNormal;
                    return true;
                }
            }

            if (found)
            {
                surfaceY = closest;
                surfaceNormal = closestNormal;
            }
            return found;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookSceneLoaded()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            AutoRepairPlayer();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode _)
        {
            AutoRepairPlayer();
        }

        private static void AutoRepairPlayer()
        {
            var splat = GameObject.Find("Splat_TEMP");
            if (splat == null)
            {
                return;
            }

            if (SplatMeshyVisualMode.IsEnabledOn(splat.transform))
            {
                Repair(splat.transform);
                return;
            }

            // Keep the authored skinned character visible when both it and the static
            // Meshy render are serialized into the Arrival scene. Otherwise the bone
            // animator poses a hidden character while the visible hero stays rigid.
            var visual = splat.transform.Find(OrganicSplatBuilder.VisualRootName);
            if (HasUsableSkinnedCharacter(visual))
            {
                if (OrganicSplatBuilder.ContainsBone(visual, OrganicSplatBuilder.FaceVersionMarkerName))
                {
                    PrepareSkinnedCharacter(splat.transform, visual);
                }
                else
                {
                    Repair(splat.transform);
                }
                return;
            }

            // Use the Meshy hero as a visual fallback only when no usable skinned Splat exists.
            if (HasReadableMeshyHero(splat.transform))
            {
                HideOrganicRenderers(splat.transform);
                return;
            }

            if (splat.GetComponent<SplatVisualRepair>() == null)
            {
                splat.AddComponent<SplatVisualRepair>();
            }
            else
            {
                Repair(splat.transform);
            }

            // Player silhouette was reading as a tiny statue — bump visual scale.
            visual = splat.transform.Find(OrganicSplatBuilder.VisualRootName);
            if (visual != null && !HasUsableSkinnedCharacter(visual) && visual.localScale.x < 1.4f)
            {
                visual.localScale = Vector3.one * 1.55f;
            }
        }

        public static void Repair(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var rootId = root.GetEntityId();
            if (SplatMeshyVisualMode.IsEnabledOn(root))
            {
                MeshyModeRoots.Add(rootId);
                PrepareMeshyVisual(root);
                return;
            }

            if (MeshyModeRoots.Remove(rootId))
            {
                RepairedRoots.Remove(rootId);
                HideMeshyRenderers(root);
                var organicVisual = root.Find(OrganicSplatBuilder.VisualRootName);
                if (organicVisual != null)
                {
                    organicVisual.gameObject.SetActive(true);
                }

                var organicAnimator = root.GetComponent<SplatSkeletonAnimator>();
                if (organicAnimator != null)
                {
                    organicAnimator.enabled = true;
                }
            }

            if (!RepairedRoots.Add(rootId))
            {
                return;
            }

            var existingVisual = root.Find(OrganicSplatBuilder.VisualRootName);
            var hasCurrentFace = OrganicSplatBuilder.ContainsBone(existingVisual,
                OrganicSplatBuilder.FaceVersionMarkerName);
            var footRigCandidateAvailable = Resources.Load<GameObject>(
                "Characters/Splat_Rigged_Candidate_Iter1137") != null;
            var existingHasFootRig = OrganicSplatBuilder.ContainsBone(existingVisual, "Bone_FootL")
                && OrganicSplatBuilder.ContainsBone(existingVisual, "Bone_FootR");
            if (HasUsableSkinnedCharacter(existingVisual) && hasCurrentFace
                && (!footRigCandidateAvailable || existingHasFootRig))
            {
                PrepareSkinnedCharacter(root, existingVisual);
                return;
            }

            if (!HasUsableSkinnedCharacter(existingVisual) && HasReadableMeshyHero(root))
            {
                HideOrganicRenderers(root);
                return;
            }

            var riggedPrefab = Resources.Load<GameObject>("Characters/Splat_Rigged_Candidate_Iter241")
                ?? Resources.Load<GameObject>("Characters/Splat_Rigged");
            if (riggedPrefab != null && (!HasUsableSkinnedCharacter(existingVisual)
                || !hasCurrentFace || (footRigCandidateAvailable && !existingHasFootRig)))
            {
                OrganicSplatBuilder.BuildVisualRoot(root, ApplyLit);
                var installedVisual = root.Find(OrganicSplatBuilder.VisualRootName);
                if (HasUsableSkinnedCharacter(installedVisual))
                {
                    PrepareSkinnedCharacter(root, installedVisual);
                }
                BrightenMaterials(root);
                Debug.Log($"[SplatVisualRepair] Installed the skinned Splat visual on {root.name}#{root.GetEntityId()}; visualRoots={CountVisualRoots(root)}.");
                return;
            }

            var filters = existingVisual != null
                ? existingVisual.GetComponentsInChildren<MeshFilter>(true)
                : System.Array.Empty<MeshFilter>();
            var broken = 0;
            foreach (var filter in filters)
            {
                if (filter == null)
                {
                    continue;
                }

                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount < 3 || mesh.bounds.size.sqrMagnitude < 1e-6f)
                {
                    broken++;
                }
            }

            var staleFace = existingVisual != null
                && !OrganicSplatBuilder.ContainsBone(existingVisual, OrganicSplatBuilder.FaceVersionMarkerName);
            if (broken == 0 && existingVisual != null && !staleFace)
            {
                BrightenMaterials(root);
                return;
            }

            // Remove every previous visual root. Older repair paths could create
            // siblings when startup and scene-load callbacks both ran in one frame.
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name != OrganicSplatBuilder.VisualRootName)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    child.name = OrganicSplatBuilder.VisualRootName + "_Retired";
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            OrganicSplatBuilder.BuildVisualRoot(root, ApplyLit);
            BrightenMaterials(root);
            Debug.Log($"[SplatVisualRepair] Rebuilt Splat root {root.name}#{root.GetEntityId()}; visualRoots={CountVisualRoots(root)}.");
        }

        private static void PrepareMeshyVisual(Transform root)
        {
            var meshy = SplatMeshyVisualMode.IsEnabledOn(root)
                ? SplatMeshyVisualMode.ResolveVisual(root)
                : root.Find(SplatMeshyVisualMode.MeshyVisualRootName);
            if (meshy == null)
            {
                // If the opt-in marker is serialized before its child asset, keep
                // the existing Organic visual visible as a safe fallback.
                return;
            }

            meshy.gameObject.SetActive(true);
            var renderers = meshy.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            HideCompetingSkinnedRigs(root, meshy);

            var organic = root.Find(OrganicSplatBuilder.VisualRootName);
            if (organic != null && organic != meshy)
            {
                organic.gameObject.SetActive(false);
            }

            // The procedural Organic animator uses a different bone layout.
            // Leave the component serialized for rollback, but keep it off while
            // the explicit Meshy mode marker is active.
            var organicAnimator = root.GetComponent<SplatSkeletonAnimator>();
            if (organicAnimator != null)
            {
                organicAnimator.enabled = false;
            }

            var animator = meshy.GetComponent<Animator>() ?? meshy.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
            }

            var driver = meshy.GetComponent<SplatMeshyAnimatorDriver>() ??
                         meshy.GetComponentInChildren<SplatMeshyAnimatorDriver>(true);
            if (driver == null)
            {
                driver = meshy.gameObject.AddComponent<SplatMeshyAnimatorDriver>();
            }

            driver.enabled = true;

            // The approved Meshy controller includes a short friendly hello.
            // Play it occasionally during a true grounded idle so Splat reads
            // like a curious little adventurer even when the player pauses.
            // Keep it on the Meshy visual only: the organic fallback uses a
            // different skeleton and must never receive these rig gestures.
            var personalityDriver = meshy.GetComponent<SplatMeshyIdlePersonalityDriver>() ??
                                    meshy.GetComponentInChildren<SplatMeshyIdlePersonalityDriver>(true);
            if (personalityDriver == null)
            {
                personalityDriver = meshy.gameObject.AddComponent<SplatMeshyIdlePersonalityDriver>();
            }

            personalityDriver.enabled = true;
        }

        private static void HideCompetingSkinnedRigs(Transform root, Transform selected)
        {
            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                var child = root.GetChild(childIndex);
                if (child == selected || child.name == OrganicSplatBuilder.VisualRootName)
                    continue;
                var skins = child.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length == 0)
                    continue;
                for (var skinIndex = 0; skinIndex < skins.Length; skinIndex++)
                    if (skins[skinIndex] != null) skins[skinIndex].enabled = false;
                foreach (var animator in child.GetComponentsInChildren<Animator>(true))
                    if (animator != null) animator.enabled = false;
                foreach (var driver in child.GetComponentsInChildren<SplatMeshyAnimatorDriver>(true))
                    if (driver != null) driver.enabled = false;
                foreach (var personality in child.GetComponentsInChildren<SplatMeshyIdlePersonalityDriver>(true))
                    if (personality != null) personality.enabled = false;
            }
        }

        private static void KeepSingleVisualRoot(Transform root)
        {
            if (SplatMeshyVisualMode.IsEnabledOn(root))
            {
                return;
            }

            Transform keep = null;
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name != OrganicSplatBuilder.VisualRootName)
                {
                    continue;
                }

                if (keep == null || (!OrganicSplatBuilder.ContainsBone(keep, OrganicSplatBuilder.FaceVersionMarkerName)
                    && OrganicSplatBuilder.ContainsBone(child, OrganicSplatBuilder.FaceVersionMarkerName)))
                {
                    keep = child;
                }
            }

            if (keep == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name != OrganicSplatBuilder.VisualRootName || child == keep)
                {
                    continue;
                }

                // Hide immediately so a startup duplicate can never double the face
                // for a frame; destruction then removes its generated meshes.
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
        }

        private static int CountVisualRoots(Transform root)
        {
            var count = 0;
            for (var i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).name == OrganicSplatBuilder.VisualRootName)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasUsableSkinnedCharacter(Transform visual)
        {
            if (visual == null)
            {
                return false;
            }

            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.vertexCount >= 3
                    && renderer.bones != null && renderer.bones.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasReadableMeshyHero(Transform root)
        {
            var meshy = SplatMeshyVisualMode.IsEnabledOn(root)
                ? SplatMeshyVisualMode.ResolveVisual(root)
                : root.Find(SplatMeshyVisualMode.MeshyVisualRootName);
            if (meshy == null || !meshy.gameObject.activeInHierarchy)
            {
                return false;
            }

            foreach (var r in meshy.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static void HideOrganicRenderers(Transform root)
        {
            var organic = root.Find(OrganicSplatBuilder.VisualRootName);
            if (organic == null)
            {
                return;
            }

            foreach (var r in organic.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null)
                {
                    r.enabled = false;
                }
            }
        }

        private static void HideMeshyRenderers(Transform root)
        {
            var meshy = SplatMeshyVisualMode.ResolveVisual(root);
            if (meshy == null)
            {
                return;
            }

            foreach (var renderer in meshy.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        private static void PrepareSkinnedCharacter(Transform root, Transform visual)
        {
            HideMeshyRenderers(root);

            // The Arrival scene can start with an already-serialized skinned rig,
            // which skips Repair's fallback AddComponent path. Keep the runtime
            // contact solver on every rig so the animator can re-seat the rendered
            // soles after each pose, rather than relying only on this one-time bind.
            if (root.GetComponent<SplatVisualRepair>() == null)
            {
                // This path has already accepted a valid skinned rig. Mark that
                // setup complete before AddComponent invokes Awake, otherwise the
                // new component could recursively re-enter Repair and replace the
                // accepted visual while checking its fallback candidate.
                RepairedRoots.Add(root.GetEntityId());
                root.gameObject.AddComponent<SplatVisualRepair>();
            }

            var animator = root.GetComponent<SplatSkeletonAnimator>();
            if (animator == null)
            {
                // Several world bootstrap fallback paths build Splat at runtime
                // without copying every component from the saved prefab. A visible
                // skinned rig without its pose driver was therefore a statue.
                animator = root.gameObject.AddComponent<SplatSkeletonAnimator>();
            }

            if (!animator.IsBoundToVisibleSkin(visual))
            {
                // The serialized SplatVisual may be cached by the animator before
                // this repair replaces it. Rebind so action poses drive the visible rig.
                // Repeated scene repair callbacks must not recache rest pose after the
                // animator has already posed the same skin; that turns an action pose
                // into a new bind baseline and compounds deformation.
                animator.Bind(root.GetComponent<SplatMotor>(), visual);
            }

            AlignSkinnedSolesToController(root, visual);

            if (visual != null && BrightenedSkinnedRoots.Add(root.GetEntityId()))
            {
                BrightenMaterials(visual);
            }
        }

        private static void AlignSkinnedSolesToController(Transform root, Transform visual)
        {
            if (root == null || visual == null)
            {
                return;
            }

            var controller = root.GetComponent<CharacterController>();
            if (controller == null || !controller.enabled)
            {
                return;
            }

            // Match SplatMotor.Awake's support-pad margin before moving the skin,
            // including when scene-load repair runs before the motor's Awake.
            if (controller.skinWidth > SplatMotor.PlatformContactSkinWidth)
            {
                controller.skinWidth = SplatMotor.PlatformContactSkinWidth;
            }

            // The approved Splat rig mixes skinned arms with rigid, independently
            // posed feet. Aligning this whole visual from every skinned renderer's
            // bounds treats an arm as a sole and can translate the body below the
            // floor. Leave these rigs at their authored bind height; the grounded
            // per-foot solver below owns contact for the rigid shoes.
            if (HasRigidNamedFootRenderers(visual))
            {
                return;
            }

            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var hasBounds = false;
            var lowestSole = float.PositiveInfinity;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer.sharedMesh == null)
                {
                    continue;
                }

                lowestSole = Mathf.Min(lowestSole, renderer.bounds.min.y);
                hasBounds = true;
            }

            if (!hasBounds || float.IsInfinity(lowestSole))
            {
                return;
            }

            // Derive contact from the actual skinned silhouette and collider, rather
            // than a model-specific offset that can leave the boots floating or sunk.
            // CharacterController.bounds.min already represents the walkable
            // capsule bottom; subtracting skinWidth lowered the visible sole
            // beneath the surface on fallback contacts.
            const float soleClearance = 0f;
            var floorY = controller.bounds.min.y + soleClearance;
            var adjustment = floorY - lowestSole;
            if (Mathf.Abs(adjustment) > 0.001f)
            {
                visual.position += Vector3.up * adjustment;
            }
        }

        private static bool HasRigidNamedFootRenderers(Transform visual)
        {
            if (visual == null) return false;
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            var hasLeft = false;
            var hasRight = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy
                    || renderer is SkinnedMeshRenderer) continue;
                if (HasFootSideToken(renderer.gameObject.name, true)) hasLeft = true;
                else if (HasFootSideToken(renderer.gameObject.name, false)) hasRight = true;
                if (hasLeft && hasRight) return true;
            }
            return false;
        }

        private static void BrightenMaterials(Transform root)
        {
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null)
                {
                    continue;
                }

                // Keep the new face, boots, and paint marks their own colors.
                var n = rend.gameObject.name;
                if (n.Contains("Iris") || n.Contains("Pupil") || n.Contains("EyeGlint")
                    || n.Contains("SmileTeeth") || n.Contains("AntennaTip") || n.Contains("Boot")
                    || n.Contains("PaintSpiral"))
                {
                    continue;
                }

                // OrganicSplatBuilder names its white eyeball mesh ART_Eye, while
                // imported/repaired variants may use an explicit EyeWhite name.
                // Recognize both so the fallback's sclera does not get the purple
                // skin tint from this material pass.
                var isEyeWhite = n.Contains("EyeWhite") || n == "ART_Eye";
                var isCheek = n.Contains("Cheek");
                var isBrow = n.Contains("Brow");
                var isSmile = n.Contains("Smile");
                var isMapmakerGold = n.Contains("Maker") || n.Contains("MapmakerSpark");
                var isSplatSkin = n.Contains("ART_SplatBody");
                var isApprovedPurple = n.Contains("PurplePaw") || n.Contains("PurpleLeg")
                    || n.Contains("PurpleFoot") || n.Contains("BackwardsCap");

                // sharedMaterials in edit mode — materials property instantiates leaks.
                var mats = Application.isPlaying ? rend.materials : rend.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null)
                    {
                        continue;
                    }

                    if (!Application.isPlaying)
                    {
                        mat = new Material(mat) { name = mat.name + "_SplatBright" };
                        mats[i] = mat;
                    }

                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        c.a = 1f;
                        if (isEyeWhite)
                        {
                            c = Color.Lerp(c, new Color(1f, 0.99f, 0.94f, 1f), 0.28f);
                        }
                        else if (isCheek)
                        {
                            c = Color.Lerp(c, new Color(1f, 0.58f, 0.69f, 1f), 0.42f);
                        }
                        else if (isBrow)
                        {
                            c = Color.Lerp(c, new Color(0.5f, 0.28f, 0.67f, 1f), 0.56f);
                        }
                        else if (isSmile)
                        {
                            c = Color.Lerp(c, new Color(0.38f, 0.13f, 0.5f, 1f), 0.18f);
                        }
                        else if (isMapmakerGold)
                        {
                            c = Color.Lerp(c, new Color(1f, 0.78f, 0.3f, 1f), 0.26f);
                        }
                        else if (!isApprovedPurple)
                        {
                            // Keep the skin purple and readable, with a soft painted finish.
                            c = Color.Lerp(c, new Color(0.78f, 0.42f, 1f, 1f), 0.18f);
                        }

                        mat.SetColor("_BaseColor", c);
                    }

                    if (mat.HasProperty("_Metallic"))
                    {
                        mat.SetFloat("_Metallic", 0f);
                    }

                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", Mathf.Min(mat.GetFloat("_Smoothness"), 0.14f));
                    }

                    if (mat.HasProperty("_SpecularHighlights"))
                    {
                        mat.SetFloat("_SpecularHighlights", 0f);
                    }

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (isEyeWhite)
                        {
                            mat.SetColor("_EmissionColor", new Color(1f, 0.93f, 0.78f) * 0.16f);
                        }
                        else if (isCheek)
                        {
                            mat.SetColor("_EmissionColor", new Color(1f, 0.28f, 0.4f) * 0.06f);
                        }
                        else if (isMapmakerGold)
                        {
                            mat.SetColor("_EmissionColor", new Color(1f, 0.58f, 0.12f) * 0.08f);
                        }
                        else if (isSplatSkin)
                        {
                            // Keep a modest neutral lift so colored world lighting
                            // doesn't tint the approved white body. The previous
                            // 0.62 fill flattened the skin shading in the World 1
                            // gameplay capture; let the key/fill lights describe
                            // the round form instead of washing it out.
                            // Keep the stronger neutral fill in the green-lit
                            // Corkscrew Canopy; only the Rainbow Arrival frame
                            // showed the high fill flattening the white body.
                            var inRainbowArrival = rend.gameObject.scene.name == "RainbowWorld_Arrival";
                            var whiteBodyFill = inRainbowArrival ? 0.30f : 0.62f;
                            mat.SetColor("_EmissionColor", Color.white * whiteBodyFill);
                        }
                        else
                        {
                            mat.SetColor("_EmissionColor", new Color(0.35f, 0.12f, 0.55f) * 0.025f);
                        }
                    }
                }

                if (!Application.isPlaying)
                {
                    rend.sharedMaterials = mats;
                }
            }
        }

        private static void ApplyLit(GameObject go, string matName, Color color, float metallic, float smoothness)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null)
            {
                return;
            }

            var shader = matName == "Mat_SplatEyeGlint"
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var mat = new Material(shader) { name = matName };
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.color = color;
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            rend.sharedMaterial = mat;
        }
    }
}
