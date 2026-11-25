using System.Collections;
using System.Collections.Generic;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Simple Arrival foe: patrols, bumps Splat, flattens when stomped or launched into.
    /// TEMP ART mesh expected on the same object.
    /// </summary>
    public sealed class ScrapMite : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("ScrapMite.Update");
        private const int WalkableGroundMask = 1 << 0; // Project Default layer.
        private const float PursuitStandOffMeters = 0.9f;
        private const float AttackTelegraphEnterRangeMeters = 2.85f;
        private const float AttackTelegraphExitRangeMeters = 3.2f;
        private const float AttackStrokeDurationSeconds = 0.42f;
        private const float AttackStrokeCycleSeconds = 0.92f;
        private static readonly List<ScrapMite> ActiveMites = new List<ScrapMite>(16);
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float bumpCooldown = 0.8f;
        [SerializeField, Min(0f)] private float contactKnockbackSpeed = 10f;
        [SerializeField] private float bobAmplitude = 0.025f;
        [SerializeField] private float defeatSquashDuration = 0.22f;
        [SerializeField] private float noticeRadius = 4.5f;
        [SerializeField] private float startledSpeedMul = 1.55f;
        [SerializeField] private bool limitConcurrentPursuers;
        [SerializeField, Min(1)] private int concurrentPursuerLimit = 2;

        private Vector3 _patrolPointA;
        private Vector3 _patrolPointB;
        private Vector3 _target;
        private float _nextBumpTime;
        private bool _defeated;
        private bool _destroyAfterDefeat;
        private float _baseY;
        private Transform _splat;
        private bool _aggroReported;
        private Transform _body;
        private Transform _commissionedVisual;
        private ScrapMiteCommissionedAnimator _commissionedRigAnimator;
        private RainbowSpiderAnimatorDriver _commissionedMeshyAnimatorDriver;
        private bool _commissionedVisualUsesPivotRig;
        private Renderer[] _commissionedVisualRenderers = System.Array.Empty<Renderer>();
        private MeshFilter[] _commissionedVisualMeshFilters = System.Array.Empty<MeshFilter>();
        private static readonly Dictionary<Mesh, Vector3[]> MeshVertexCache = new Dictionary<Mesh, Vector3[]>();
        private Mesh _skinnedBoundsScratchMesh;
        private readonly List<Vector3> _skinnedBoundsScratchVertices = new List<Vector3>(4096);
        private Vector3 _commissionedVisualRestPosition;
        private Quaternion _commissionedVisualRestRotation;
        private Transform _shell;
        private Transform _antenna;
        private readonly Transform[] _eyes = new Transform[2];
        private readonly Transform[] _pupils = new Transform[2];
        private readonly Vector3[] _pupilRestPositions = new Vector3[2];
        private readonly Vector3[] _eyeRestScales = new Vector3[2];
        private int _eyeCount;
        private int _pupilCount;
        private float _nextEyeBlinkAt;
        private float _eyeBlinkStartedAt = -1f;
        private float _nextAntennaTwitchAt;
        private float _antennaTwitchStartedAt = -1f;
        private float _antennaTwitchDirection = 1f;
        private float _antennaTwitchWeight;
        private readonly Transform[] _legs = new Transform[6];
        private readonly Renderer[][] _legRenderers = new Renderer[6][];
        private readonly Quaternion[] _legRestRotations = new Quaternion[6];
        private readonly Vector3[] _legRestPositions = new Vector3[6];
        private readonly Vector3[] _legStanceWorldPositions = new Vector3[6];
        private readonly bool[] _legStanceLocked = new bool[6];
        private Quaternion _bodyRestRotation;
        private Vector3 _bodyRestPosition;
        private Quaternion _shellRestRotation;
        private Quaternion _antennaRestRotation;
        private float _stridePhase;
        private float _commissionedGaitWeight;
        private float _commissionedGaitRate = 1f;
        private float _motionPhaseOffset;
        private float _groundY;
        private float _rootGroundOffset;
        private bool _hasGroundSupport;
        private float _contactFlinchStartedAt = -1f;
        private Vector3 _contactFlinchDirectionLocal;
        private readonly RaycastHit[] _groundSupportHits = new RaycastHit[12];
        private float _alertBlend;
        private float _attackTelegraphBlend;
        private bool _isAttackTelegraphing;
        private float _attackStrokeStartedAt = -1f;
        private float _attackStrokeWeight;
        private bool _isPursuingPlayerForDiagnostics;

        private void OnDestroy()
        {
            ActiveMites.Remove(this);
            if (_skinnedBoundsScratchMesh == null) return;
            Destroy(_skinnedBoundsScratchMesh);
            _skinnedBoundsScratchMesh = null;
        }

        private void OnEnable()
        {
            if (!ActiveMites.Contains(this)) ActiveMites.Add(this);
        }

        private void OnDisable()
        {
            ActiveMites.Remove(this);
            _isPursuingPlayerForDiagnostics = false;
        }

        public float AlertWeightForDiagnostics => _alertBlend;
        public bool IsPursuingPlayerForDiagnostics => _isPursuingPlayerForDiagnostics;
        public Vector3 BodyLocalEulerForDiagnostics => _body != null ? _body.localEulerAngles : Vector3.zero;
        public Vector3 LeftFrontLegLocalEulerForDiagnostics => _legs[0] != null ? _legs[0].localEulerAngles : Vector3.zero;
        public Vector3 RightFrontLegLocalEulerForDiagnostics => _legs[3] != null ? _legs[3].localEulerAngles : Vector3.zero;
        public Vector3 AntennaLocalEulerForDiagnostics => _antenna != null ? _antenna.localEulerAngles : Vector3.zero;
        public float AttackTelegraphWeightForDiagnostics => _attackTelegraphBlend;
        public float AttackStrokeWeightForDiagnostics => _attackStrokeWeight;
        public float AttackSettleWeightForDiagnostics
        {
            get
            {
                if (!_isAttackTelegraphing || _attackStrokeStartedAt < 0f) return 0f;
                var elapsed = Time.time - _attackStrokeStartedAt;
                if (elapsed <= AttackStrokeDurationSeconds || elapsed >= AttackStrokeCycleSeconds) return 0f;
                return 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(AttackStrokeDurationSeconds, AttackStrokeCycleSeconds, elapsed));
            }
        }
        public float AttackStrokeElapsedForDiagnostics => _attackStrokeStartedAt >= 0f
            ? Time.time - _attackStrokeStartedAt : -1f;
        public bool IsAttackTelegraphingForDiagnostics => _isAttackTelegraphing;
        public float CommissionedGaitPhase => _stridePhase;
        public float CommissionedGaitWeight => _commissionedGaitWeight;
        public float CommissionedGaitRate => _commissionedGaitRate;
        public float AntennaTwitchWeightForDiagnostics => _antennaTwitchWeight;
        public float ContactFlinchWeightForDiagnostics => GetContactFlinchWeight();
        public float ContactKnockbackSpeedForDiagnostics => contactKnockbackSpeed;
        public bool IsDefeated => _defeated;
        public float DefeatPoseWeight => !_defeated || _contactFlinchStartedAt < 0f
            ? 0f
            : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - _contactFlinchStartedAt) / 0.36f));
        public Vector3 ContactFlinchDirectionLocal => _contactFlinchDirectionLocal;
        public float GroundYForVisuals => _hasGroundSupport ? _groundY : transform.position.y;
        public Vector3 LeftPupilGazeOffsetForDiagnostics => _pupils[0] != null
            ? _pupils[0].localPosition - _pupilRestPositions[0] : Vector3.zero;
        public Vector3 RightPupilGazeOffsetForDiagnostics => _pupils[1] != null
            ? _pupils[1].localPosition - _pupilRestPositions[1] : Vector3.zero;
        public float EyeBlinkWeightForDiagnostics => GetEyeBlinkWeight();
        public Vector3 CommissionedVisualLocalPositionForDiagnostics => _commissionedVisual != null
            ? _commissionedVisual.localPosition : Vector3.zero;
        public Vector3 CommissionedVisualLocalEulerForDiagnostics => _commissionedVisual != null
            ? _commissionedVisual.localRotation.eulerAngles : Vector3.zero;
        public float CommissionedVisualGroundGapForDiagnostics
        {
            get
            {
                if (!_hasGroundSupport || _commissionedVisualRenderers == null) return float.NaN;
                var lowest = FindCommissionedVisualBoundsMinimumY();
                return float.IsInfinity(lowest) ? float.NaN : lowest - _groundY;
            }
        }
        public bool UsesCommissionedVisualForDiagnostics => _commissionedVisual != null
            && _commissionedVisual.gameObject.activeInHierarchy;

        public void TriggerContactFlinch(Vector3 awayDirection)
        {
            awayDirection.y = 0f;
            if (awayDirection.sqrMagnitude < 0.001f)
            {
                awayDirection = -transform.forward;
            }

            _contactFlinchDirectionLocal = transform.InverseTransformDirection(awayDirection.normalized);
            _contactFlinchStartedAt = Time.time;
        }

        public void ConfigurePatrol(Transform a, Transform b, float speed)
        {
            pointA = a;
            pointB = b;
            moveSpeed = speed;
            CachePatrolPoints();
            CacheVisualRig();
            AlignFeetToGround();
            _baseY = transform.position.y;
            _target = _patrolPointB;
        }

        public void DismissAfterBossDefeat()
        {
            if (_defeated)
            {
                // A phase Mite may already be finishing its stomp defeat pose.
                // Let that coroutine destroy it at the end; if it was already
                // deactivated, there is no remaining pose to preserve.
                _destroyAfterDefeat = true;
                if (!gameObject.activeInHierarchy)
                {
                    Destroy(gameObject);
                }
                return;
            }

            _defeated = true;
            _destroyAfterDefeat = true;
            if (_aggroReported)
            {
                EnemyCombatDirector.Ensure().NotifyEnemyDefeated(this);
                _aggroReported = false;
            }

            // Boss cleanup used to deactivate and destroy its phase Mites in
            // the same frame, skipping the authored defeat fall used by stomps.
            // Reuse that pose silently so the arena clear still reads in-world.
            TriggerContactFlinch(-transform.forward);
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            StopAllCoroutines();
            StartCoroutine(DefeatRoutine());
        }

        private void Awake()
        {
            EnsureContactTrigger();
            // Give each mite its own gait start so a group does not march or
            // idle-step in lockstep like duplicated toys.
            _motionPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            _stridePhase = _motionPhaseOffset;
            _nextAntennaTwitchAt = Time.time + Random.Range(2.0f, 2.8f);
            CacheVisualRig();
            AlignFeetToGround();
            _baseY = transform.position.y;
            CachePatrolPoints();
            _target = _patrolPointB;
        }

        private void OnTransformChildrenChanged()
        {
            // Commissioned Meshy visuals are attached after dynamically spawned
            // Mites run Awake. Rebind once the child arrives so its shadow policy
            // is applied even when the legacy organic rig was already cached.
            CacheVisualRig();
        }

        private void CachePatrolPoints()
        {
            // Encounter spawners sometimes parent waypoint markers to the Mite.
            // Read their world positions once so the patrol targets stay fixed
            // when the root moves instead of drifting away with the enemy.
            _patrolPointA = pointA != null
                ? pointA.position : transform.position - Vector3.right * 2f;
            _patrolPointB = pointB != null
                ? pointB.position : transform.position + Vector3.right * 2f;
        }

        private void EnsureContactTrigger()
        {
            // Some saved world Mites contain only their AI and visual hierarchy,
            // so their trigger callbacks for bump damage and stomp defeat never
            // run. Add the same contact volume used by the Arrival/boss spawners
            // when a scene omitted it.
            var contact = GetComponent<Collider>();
            if (contact == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.35f, 0f);
                box.size = new Vector3(1.2f, 0.9f, 1.2f);
                box.isTrigger = true;
                return;
            }

            // These AI roots move directly and should report overlap without
            // pushing the CharacterController or changing patrol movement.
            contact.isTrigger = true;
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            if (_defeated)
            {
                return;
            }

            // Some world builders attach the organic mesh after this component's
            // Awake; keep looking until that authored visual hierarchy exists.
            if (_body == null && _legs[0] == null)
            {
                CacheVisualRig();
            }

            var startled = IsStartled();
            var pursuingPlayer = startled
                && (!limitConcurrentPursuers || HasPursuitSlot());
            _isPursuingPlayerForDiagnostics = pursuingPlayer;
            var contactFlinch = GetContactFlinchWeight();
            _alertBlend = Mathf.MoveTowards(_alertBlend, startled ? 1f : 0f,
                Time.deltaTime * (startled ? 8f : 4f));
            var playerDistance = _splat != null
                ? Flat(_splat.position - transform.position).magnitude
                : float.PositiveInfinity;
            // Begin the tell while the Mite still has room to approach; the old
            // 2.15 m gate left less than half a second before contact at chase speed.
            var attackRange = _attackTelegraphBlend > 0.01f
                ? AttackTelegraphExitRangeMeters : AttackTelegraphEnterRangeMeters;
            // Keep a hit reaction distinct from renewed attack anticipation. Without
            // this gate, the Mite immediately leans back into its brace while its
            // contact flinch is still visible, making the impact read like a pose
            // glitch instead of recoil followed by recovery.
            var recoveringFromContact = contactFlinch > 0.05f;
            var supportTelegraph = limitConcurrentPursuers
                && playerDistance <= AttackTelegraphExitRangeMeters;
            var telegraphing = !recoveringFromContact && startled
                && (playerDistance <= attackRange || supportTelegraph);
            var enteredAttackTelegraph = telegraphing && !_isAttackTelegraphing;
            _isAttackTelegraphing = telegraphing;
            if (enteredAttackTelegraph)
            {
                // Pair the visible brace with one warning cue when it begins;
                // do not repeat it every frame or during the lunge itself.
                TempSfxPlayer.PlayWarn();
            }
            var telegraphStep = telegraphing
                ? Mathf.Min(Time.deltaTime * 4.5f, 0.14f)
                : Time.deltaTime * 5f;
            var previousAttackTelegraphBlend = _attackTelegraphBlend;
            _attackTelegraphBlend = Mathf.MoveTowards(_attackTelegraphBlend,
                telegraphing ? 1f : 0f, telegraphStep);
            // Finish the loaded brace before beginning the body stroke. Starting
            // it at the first few percent made the imported one-piece Mite lunge
            // before its tell, then settle into a long frozen-looking lean.
            if (previousAttackTelegraphBlend < 0.92f && _attackTelegraphBlend >= 0.92f)
            {
                _attackStrokeStartedAt = Time.time;
            }
            else if (_attackTelegraphBlend <= 0.01f)
            {
                _attackStrokeStartedAt = -1f;
            }
            else if (telegraphing && _attackTelegraphBlend >= 0.92f
                && _attackStrokeStartedAt >= 0f
                && Time.time - _attackStrokeStartedAt >= AttackStrokeCycleSeconds)
            {
                // If Splat stays inside the close-range threat zone, do not leave
                // the Mite frozen in its loaded pose after the first lunge. Repeat
                // the scale-free strike with a short settle between strokes; a hit
                // flinch or leaving range still interrupts the cycle immediately.
                _attackStrokeStartedAt = Time.time;
            }
            ReportAggro(startled);
            var a = _patrolPointA;
            var b = _patrolPointB;
            var speed = moveSpeed * (startled ? startledSpeedMul : 1f);
            var previous = transform.position;
            // Alert used to increase patrol speed but keep following the same
            // waypoint, so a nearby player could trigger the enemy without it
            // actually approaching them. Chase in the horizontal plane while
            // alert; retain `_target` as the saved patrol waypoint for when the
            // player leaves its notice radius.
            var movementTarget = _target;
            if (recoveringFromContact)
            {
                // Let the recoil transforms read before pursuit starts again;
                // the normal support pass continues to seat the feet meanwhile.
                movementTarget = previous;
                speed = 0f;
            }
            else if (startled && _splat != null && !pursuingPlayer)
            {
                // Keep support Mites moving along their authored patrol while
                // they telegraph, instead of freezing them inside the warning
                // band or letting the whole group converge on one contact point.
                movementTarget = _target;
                speed = moveSpeed * 0.65f;
            }
            else if (pursuingPlayer && _splat != null)
            {
                var toPlayer = Flat(_splat.position - previous);
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    // Stop at contact range instead of running through Splat's
                    // center. The existing trigger then delivers the bump while
                    // the Mite can hold its braced anticipation pose.
                    movementTarget = _splat.position
                        - toPlayer.normalized * PursuitStandOffMeters;
                    movementTarget.y = _baseY;
                }
                else
                {
                    movementTarget = previous;
                }
            }
            var next = Vector3.MoveTowards(previous, movementTarget, speed * Time.deltaTime);
            // Mites are ground walkers; root bob made their feet visibly float.
            // Follow the walkable surface under each patrol step, rather than
            // freezing the spawn height and hovering over raised/lowered pads.
            if (!UpdateGroundSupport(next))
            {
                // A patrol gap is a boundary, not a reason to keep gliding at the
                // last cached height. Turn back at the final supported step.
                // During pursuit, hold this safe edge without overwriting the
                // patrol waypoint; once alert ends, the Mite can resume its loop.
                if (!startled)
                {
                    var targetOnB = Flat(_target - b).sqrMagnitude < Flat(_target - a).sqrMagnitude;
                    _target = targetOnB ? a : b;
                }
                next = previous;
            }
            next.y = _baseY;
            transform.position = next;

            var planarDelta = Flat(next - previous);
            var planarSpeed = planarDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            var facingDelta = planarDelta;
            if (facingDelta.sqrMagnitude <= 0.0001f && startled && _splat != null)
            {
                facingDelta = Flat(_splat.position - transform.position);
            }

            if (facingDelta.sqrMagnitude > 0.0001f)
            {
                var facing = Quaternion.LookRotation(facingDelta.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
            }

            AnimateLocomotion(planarSpeed, startled);

            var flat = transform.position;
            flat.y = 0f;
            var targetFlat = _target;
            targetFlat.y = 0f;
            if (!startled && (flat - targetFlat).sqrMagnitude < 0.05f)
            {
                _target = (targetFlat - Flat(a)).sqrMagnitude < (targetFlat - Flat(b)).sqrMagnitude ? b : a;
            }
        }

        private void LateUpdate()
        {
            // The imported-rig driver performs this seat after its animation
            // overlays in LateUpdate. Keep this pass as a fallback for static or
            // un-driven commissioned visuals, but don't bake the same skinned
            // meshes twice per frame when the driver owns the visible rig.
            if (_commissionedVisual != null && _commissionedVisual.gameObject.activeInHierarchy
                && (_commissionedMeshyAnimatorDriver == null
                    || !_commissionedMeshyAnimatorDriver.DrivesImportedRig))
                AlignCommissionedVisualToGround();
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private bool HasPursuitSlot()
        {
            if (_splat == null)
            {
                return false;
            }

            var selfDistance = Flat(_splat.position - transform.position).sqrMagnitude;
            var closerPursuers = 0;
            var limit = Mathf.Max(1, concurrentPursuerLimit);
            for (var i = 0; i < ActiveMites.Count; i++)
            {
                var other = ActiveMites[i];
                if (other == null || other == this || other._defeated
                    || !other.isActiveAndEnabled || other._splat != _splat)
                {
                    continue;
                }

                var otherDistance = Flat(_splat.position - other.transform.position).sqrMagnitude;
                if (otherDistance > other.noticeRadius * other.noticeRadius)
                {
                    continue;
                }

                var tieBreak = Mathf.Abs(otherDistance - selfDistance) <= 0.0001f
                    && string.CompareOrdinal(other.name, name) < 0;
                if (otherDistance < selfDistance || tieBreak)
                {
                    closerPursuers++;
                    if (closerPursuers >= limit)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void AlignCommissionedVisualToGround()
        {
            if (!_hasGroundSupport || _commissionedVisual == null
                || _commissionedVisualRenderers == null || _commissionedVisualRenderers.Length == 0)
            {
                return;
            }

            var lowestVisiblePoint = FindCommissionedVisualBoundsMinimumY();

            if (float.IsInfinity(lowestVisiblePoint)) return;

            // Reseat only the render model; the AI root and contact trigger stay
            // on the sampled support. Imported skinned rigs are measured from a
            // baked pose below; static and pivot-authored visuals use mesh bounds.
            const float footClearance = 0.005f;
            // Pivoted commissioned meshes and imported rigs may have a deeper
            // authored origin than the legacy art and need a larger seat limit.
            // This value is recomputed from the rest pose every frame, so the old
            // cap left the same lower vertices buried on every frame instead of
            // converging over time.
            // Keep a finite guard against a bad ground sample, but allow the
            // measured commissioned-rig offset to reach its support surface.
            // World 01 patrol capture 1592 measured a persistent 0.30-0.36 m
            // foot gap with the previous 0.42 m cap, so allow this visual-only
            // imported rig correction to converge across a deeper authored root
            // offset. The AI root and collision/contact remain unchanged.
            var maxVisualSeatCorrection = _commissionedMeshyAnimatorDriver != null
                && _commissionedMeshyAnimatorDriver.DrivesImportedRig
                    ? 1.0f
                    : _commissionedVisualUsesPivotRig ? 0.24f : 0.14f;
            var correction = Mathf.Clamp(_groundY + footClearance - lowestVisiblePoint,
                -maxVisualSeatCorrection, maxVisualSeatCorrection);
            if (Mathf.Abs(correction) > 0.0005f)
            {
                _commissionedVisual.position += Vector3.up * correction;
            }
        }

        /// <summary>
        /// Re-seat the active commissioned visual after its child animator has
        /// finished posing pivots, using this enemy's already-sampled support.
        /// </summary>
        public void AlignCommissionedVisualAfterPose()
        {
            AlignCommissionedVisualToGround();
        }

        private float FindCommissionedVisualBoundsMinimumY()
        {
            var lowest = float.PositiveInfinity;

            // SkinnedMeshRenderer.localBounds can be a stale importer envelope,
            // not the feet in the current animated pose. Bake the actual pose so
            // ground seating and its diagnostics use the vertices the player sees.
            for (var rendererIndex = 0; rendererIndex < _commissionedVisualRenderers.Length; rendererIndex++)
            {
                if (!(_commissionedVisualRenderers[rendererIndex] is SkinnedMeshRenderer skinned)
                    || !skinned.enabled || !skinned.gameObject.activeInHierarchy || skinned.sharedMesh == null)
                    continue;

                if (_skinnedBoundsScratchMesh == null)
                {
                    _skinnedBoundsScratchMesh = new Mesh { name = "ScrapMite_SkinnedBoundsScratch" };
                    _skinnedBoundsScratchMesh.MarkDynamic();
                }

                skinned.BakeMesh(_skinnedBoundsScratchMesh, true);
                _skinnedBoundsScratchVertices.Clear();
                _skinnedBoundsScratchMesh.GetVertices(_skinnedBoundsScratchVertices);
                var skinnedTransform = skinned.transform;
                for (var vertexIndex = 0; vertexIndex < _skinnedBoundsScratchVertices.Count; vertexIndex++)
                {
                    lowest = Mathf.Min(lowest,
                        skinnedTransform.TransformPoint(_skinnedBoundsScratchVertices[vertexIndex]).y);
                }
            }

            for (var filterIndex = 0; filterIndex < _commissionedVisualMeshFilters.Length; filterIndex++)
            {
                var meshFilter = _commissionedVisualMeshFilters[filterIndex];
                if (meshFilter == null || !meshFilter.gameObject.activeInHierarchy) continue;
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;

                if (!mesh.isReadable)
                {
                    var unreadableRenderer = meshFilter.GetComponent<Renderer>();
                    if (unreadableRenderer != null && unreadableRenderer.enabled)
                    {
                        lowest = Mathf.Min(lowest, FindRendererLocalBoundsMinimumY(unreadableRenderer));
                    }
                    continue;
                }

                if (!MeshVertexCache.TryGetValue(mesh, out var vertices))
                {
                    vertices = mesh.vertices;
                    MeshVertexCache.Add(mesh, vertices);
                }

                var meshTransform = meshFilter.transform;
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    lowest = Mathf.Min(lowest, meshTransform.TransformPoint(vertices[vertexIndex]).y);
                }
            }

            if (!float.IsInfinity(lowest)) return lowest;

            for (var rendererIndex = 0; rendererIndex < _commissionedVisualRenderers.Length; rendererIndex++)
            {
                var renderer = _commissionedVisualRenderers[rendererIndex];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                // Keep a useful fallback for renderer types without a readable mesh.
                lowest = Mathf.Min(lowest, FindRendererLocalBoundsMinimumY(renderer));
            }

            return lowest;
        }

        private static float FindRendererLocalBoundsMinimumY(Renderer renderer)
        {
            var lowest = float.PositiveInfinity;
            var bounds = renderer.localBounds;
            var min = bounds.min;
            var max = bounds.max;
            for (var corner = 0; corner < 8; corner++)
            {
                var localPoint = new Vector3((corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y, (corner & 4) == 0 ? min.z : max.z);
                lowest = Mathf.Min(lowest, renderer.transform.TransformPoint(localPoint).y);
            }
            return lowest;
        }

        private bool UpdateGroundSupport(Vector3 nextPosition)
        {
            // Follow route height changes over several frames. At 30 fps, the old
            // 12 m/s rate could move the root nearly 0.4 m in one update, which
            // read as a vertical pop even after support feet were allowed to replant.
            const float followSpeed = 4.8f;
            if (!TryGetWalkableGroundY(nextPosition, 1.5f, 4f, out var targetGroundY)) return false;
            if (!_hasGroundSupport)
            {
                _rootGroundOffset = _baseY - targetGroundY;
                _groundY = targetGroundY;
                _hasGroundSupport = true;
                return true;
            }

            var step = followSpeed * Time.deltaTime;
            _groundY = Mathf.MoveTowards(_groundY, targetGroundY, step);
            _baseY = Mathf.MoveTowards(_baseY, _groundY + _rootGroundOffset, step);
            _hasGroundSupport = true;
            return true;
        }

        private bool TryGetWalkableGroundY(Vector3 position, float rayStart, float rayDistance, out float groundY)
        {
            groundY = 0f;
            var origin = position + Vector3.up * rayStart;
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _groundSupportHits, rayDistance,
                WalkableGroundMask, QueryTriggerInteraction.Ignore);
            var bestDistance = float.PositiveInfinity;
            var found = false;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _groundSupportHits[i];
                if (hit.collider == null || hit.normal.y < 0.65f
                    || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<SplatMotor>() != null
                    || hit.distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = hit.distance;
                groundY = hit.point.y;
                found = true;
            }

            return found;
        }

        private void AlignFeetToGround()
        {
            // Encounter builders historically placed mites 0.7 m above the
            // route. Their root is at the feet, so preserve the patrol pose
            // while seating the authored leg tips just above the walk surface.
            const float rayStart = 1.5f;
            const float rayDistance = 4f;
            const float maxCorrection = 1.5f;
            const float footClearance = 0.005f;
            var origin = transform.position + Vector3.up * rayStart;
            var hits = Physics.RaycastAll(origin, Vector3.down, rayDistance, WalkableGroundMask,
                QueryTriggerInteraction.Ignore);
            var bestDistance = float.PositiveInfinity;
            var bestY = float.NaN;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.normal.y < 0.65f) continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<SplatMotor>() != null) continue;
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestY = hit.point.y;
                }
            }

            if (float.IsNaN(bestY)) return;
            _groundY = bestY;
            var lowestVisualY = float.PositiveInfinity;
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
                {
                    lowestVisualY = Mathf.Min(lowestVisualY, renderers[i].bounds.min.y);
                }
            }

            // Seat the visible mesh from its actual feet instead of assuming the
            // AI root is at the bottom of every authored/procedural variant.
            var correction = float.IsInfinity(lowestVisualY)
                ? bestY + 0.055f - transform.position.y
                : bestY + footClearance - lowestVisualY;
            if (Mathf.Abs(correction) > maxCorrection) return;
            var position = transform.position;
            position.y += correction;
            transform.position = position;
            _baseY = position.y;
            _rootGroundOffset = _baseY - _groundY;
            _hasGroundSupport = true;
        }

        private void CacheVisualRig()
        {
            _eyeCount = 0;
            _pupilCount = 0;
            System.Array.Clear(_eyes, 0, _eyes.Length);
            System.Array.Clear(_pupils, 0, _pupils.Length);
            var children = GetComponentsInChildren<Transform>(true);
            // Arrival visuals may wrap the imported Meshy model in one or more
            // authoring transforms. A direct-child-only lookup left the active
            // one-piece model unbound, so none of the scale-free fallback motion
            // below reached the thing the player could actually see.
            Transform commissionedVisual = null;
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] != transform && children[i].name == "ART_MeshyEnemyVisual"
                    && children[i].gameObject.activeInHierarchy)
                {
                    commissionedVisual = children[i];
                    break;
                }
            }
            if (_commissionedVisual != commissionedVisual)
            {
                _commissionedVisual = commissionedVisual;
                _commissionedRigAnimator = _commissionedVisual != null
                    ? _commissionedVisual.GetComponent<ScrapMiteCommissionedAnimator>() : null;
                _commissionedMeshyAnimatorDriver = _commissionedVisual != null
                    ? _commissionedVisual.GetComponentInChildren<RainbowSpiderAnimatorDriver>(true) : null;
                if (_commissionedVisual != null)
                {
                    _commissionedVisualRestPosition = _commissionedVisual.localPosition;
                    _commissionedVisualRestRotation = _commissionedVisual.localRotation;
                    _commissionedVisualRenderers = _commissionedVisual.GetComponentsInChildren<Renderer>(true);
                    // The commissioned spider is a high-poly gameplay mesh. Its
                    // real-time shadow pass costs more than the entire character
                    // color pass in the Arrival profile; keep the model lit and
                    // animated while avoiding a second full-resolution draw.
                    for (var rendererIndex = 0; rendererIndex < _commissionedVisualRenderers.Length; rendererIndex++)
                    {
                        if (_commissionedVisualRenderers[rendererIndex] is SkinnedMeshRenderer)
                            _commissionedVisualRenderers[rendererIndex].shadowCastingMode = ShadowCastingMode.Off;
                    }
                    _commissionedVisualMeshFilters = _commissionedVisual.GetComponentsInChildren<MeshFilter>(true);
                    _commissionedVisualUsesPivotRig = _commissionedVisualMeshFilters.Length > 1;
                }
                else
                {
                    _commissionedVisualRenderers = System.Array.Empty<Renderer>();
                    _commissionedVisualMeshFilters = System.Array.Empty<MeshFilter>();
                    _commissionedVisualUsesPivotRig = false;
                }
            }

            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                // Meshy bootstrap keeps the legacy organic hierarchy disabled
                // underneath the commissioned visual. Do not bind the animator
                // to hidden duplicate bones; use the active visible rig instead.
                if (child == transform || !child.gameObject.activeInHierarchy) continue;
                if (child.name == "ART_MiteBody")
                {
                    _body = child;
                    _bodyRestRotation = child.localRotation;
                    _bodyRestPosition = child.localPosition;
                }
                else if (child.name == "ART_MiteShell")
                {
                    _shell = child;
                    _shellRestRotation = child.localRotation;
                }
                else if (child.name == "ART_MiteAntenna")
                {
                    _antenna = child;
                    _antennaRestRotation = child.localRotation;
                }
                else if (child.name == "ART_MiteEye" && _eyeCount < _eyes.Length)
                {
                    _eyes[_eyeCount] = child;
                    _eyeRestScales[_eyeCount] = child.localScale;
                    _eyeCount++;
                }
                else if (child.name == "ART_MitePupil" && _pupilCount < _pupils.Length)
                {
                    _pupils[_pupilCount] = child;
                    _pupilRestPositions[_pupilCount] = child.localPosition;
                    _pupilCount++;
                }
                else if (child.name.StartsWith("ART_MiteLeg_"))
                {
                    if (int.TryParse(child.name.Substring("ART_MiteLeg_".Length), out var index)
                        && index >= 0 && index < _legs.Length)
                    {
                        // Refresh scene-baked legacy visuals in memory. Their coiled
                        // spring geometry obscures the support stance and reads as
                        // a floating prop; keep the same pivots/materials but replace
                        // only those old leg meshes with one continuous tapered limb.
                        var filter = child.GetComponent<MeshFilter>();
                        if (filter != null && filter.sharedMesh != null
                            && filter.sharedMesh.name.StartsWith("Organic_SpringHelix", System.StringComparison.Ordinal))
                        {
                            var side = index < 3 ? -1f : 1f;
                            filter.sharedMesh = OrganicMeshFactory.MiteSupportLeg(side, index % 3 - 1f);
                        }
                        _legs[index] = child;
                        _legRestRotations[index] = child.localRotation;
                        _legRestPositions[index] = child.localPosition;
                        _legStanceLocked[index] = false;
                    }
                }
            }

            for (var i = 0; i < _legs.Length; i++)
            {
                _legRenderers[i] = _legs[i] != null
                    ? _legs[i].GetComponentsInChildren<Renderer>(true)
                    : null;
            }

            // Pupil children are visited immediately after each eye in this
            // authored hierarchy. If a rebuilt visual arrives later, refresh the
            // rest offsets along with its transforms.
            for (var i = 0; i < _pupils.Length; i++)
            {
                if (_pupils[i] != null) _pupilRestPositions[i] = _pupils[i].localPosition;
            }
            if (_eyeCount > 0 && _nextEyeBlinkAt <= 0f)
            {
                _nextEyeBlinkAt = Time.time + Random.Range(1.1f, 2.1f);
            }
        }

        private void AnimateLocomotion(float planarSpeed, bool startled)
        {
            var moving = planarSpeed > 0.08f;
            var contactFlinch = GetContactFlinchWeight();
            // Ease out of each lunge during the pause before the next one.
            // The threat tell stays partially visible, but the Mite no longer
            // holds the same locked brace through every repeated attack cycle.
            var attackSettle = AttackSettleWeightForDiagnostics;
            var attackBrace = _attackTelegraphBlend * (1f - attackSettle * 0.55f);
            var stationaryAttackBrace = moving ? 0f : attackBrace;
            if (moving)
            {
                // Advance the gait by distance, so the support legs track patrol
                // travel instead of cycling independently and visibly skating.
                // Keep each support phase within the small creature's leg reach.
                // The old 0.55× patrol-speed stride let the root travel over half
                // a metre while a foot was still supposed to be planted.
                var strideLength = Mathf.Max(0.5f, moveSpeed * 0.28f);
                _stridePhase = Mathf.Repeat(_stridePhase
                    + planarSpeed * Time.deltaTime * (Mathf.PI * 2f / strideLength), Mathf.PI * 2f);
            }
            var gait = moving ? Mathf.Clamp01(planarSpeed / Mathf.Max(moveSpeed, 0.01f)) : 0f;
            _commissionedGaitWeight = gait;
            // Preserve the actual pace for the imported walk cycle. GaitWeight
            // is intentionally clamped for the procedural pose amplitudes, but
            // that made a startled Mite travel at 1.55x speed while its candidate
            // Meshy legs kept stepping at their normal rate.
            _commissionedGaitRate = moving
                ? Mathf.Clamp(planarSpeed / Mathf.Max(moveSpeed, 0.01f), 0.35f, 1.75f)
                : 0f;
            var idlePhase = Time.time * (startled ? 3.6f : 2.4f) + _motionPhaseOffset;
            var quietWeight = (1f - gait) * (1f - Mathf.Clamp01(Mathf.Max(_alertBlend,
                Mathf.Max(_attackTelegraphBlend, contactFlinch))));
            if (_antennaTwitchStartedAt < 0f && quietWeight > 0.55f
                && Time.time >= _nextAntennaTwitchAt)
            {
                _antennaTwitchStartedAt = Time.time;
                _antennaTwitchDirection = Random.value < 0.5f ? -1f : 1f;
                _nextAntennaTwitchAt = Time.time + Random.Range(3.2f, 5.0f);
            }
            if (_antennaTwitchStartedAt >= 0f)
            {
                var twitchAge = Time.time - _antennaTwitchStartedAt;
                var twitchAccent = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.08f, twitchAge))
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.16f, 0.48f, twitchAge)));
                _antennaTwitchWeight = twitchAccent * quietWeight;
                if (twitchAge >= 0.48f) _antennaTwitchStartedAt = -1f;
            }
            else
            {
                _antennaTwitchWeight = 0f;
            }
            AnimateEyeTracking();
            // Compute the stroke once for both commissioned-pivot and static
            // visuals. Previously this lived only in the static-mesh fallback,
            // so the active articulated rig never received its attack release.
            var strokeElapsed = _attackStrokeStartedAt >= 0f
                ? Time.time - _attackStrokeStartedAt : float.PositiveInfinity;
            var strokeProgress = Mathf.Clamp01(strokeElapsed / AttackStrokeDurationSeconds);
            var strokePulse = strokeElapsed < AttackStrokeDurationSeconds
                ? Mathf.Sin(strokeProgress * Mathf.PI) : 0f;
            _attackStrokeWeight = _attackTelegraphBlend * strokePulse;

            // Meshy swaps disable the procedural ART_ shell (and its articulated
            // legs), but the imported static mesh has no animation clips. Drive
            // the cohesive visible model from the same gait/brace/flinch signals
            // so its anticipation and locomotion do not continue on hidden bones.
            if (_commissionedVisual != null && _commissionedVisual.gameObject.activeInHierarchy)
            {
                var importedAnimatorOwnsPose = _commissionedMeshyAnimatorDriver != null
                    && _commissionedMeshyAnimatorDriver.DrivesImportedRig;
                if ((_commissionedRigAnimator != null && _commissionedRigAnimator.enabled)
                    || importedAnimatorOwnsPose)
                {
                    // Let the articulated pivot driver show its limb gait and
                    // attack pose without adding the old whole-mesh jelly wobble.
                    _commissionedVisual.localPosition = _commissionedVisualRestPosition;
                    _commissionedVisual.localRotation = _commissionedVisualRestRotation;
                }
                else
                {
                    var visualPhase = moving ? _stridePhase : idlePhase * 0.82f;
                    // The unrigged commissioned mesh cannot articulate its legs;
                    // use a short forward body stroke during the held attack tell
                    // so the player sees a windup/release beat without body scale.
                    var attackStroke = _attackStrokeWeight;
                    var visualBob = moving
                        ? Mathf.Sin(visualPhase * 2f - 0.35f) * 0.022f * gait
                        : Mathf.Sin(idlePhase * 0.5f) * 0.028f;
                    visualBob += _attackTelegraphBlend * 0.014f - attackStroke * 0.008f
                        - contactFlinch * 0.010f;
                    var visualPitch = moving
                        ? Mathf.Sin(visualPhase) * 3.5f * gait
                        : Mathf.Sin(idlePhase * 0.5f) * 2.8f;
                    // The static imported mesh has no articulated feet; keep its
                    // whole-body pitch small so both ends stay close to the route.
                    // Coil the intact Meshy silhouette slightly away from the
                    // player during the held tell, then pitch into the attack.
                    // This keeps the one-piece model's action readable without
                    // fake squash/stretch or hidden-bone movement.
                    visualPitch += _alertBlend * 1.5f - attackBrace * 8f
                        + attackStroke * 18f - contactFlinch * 4f;
                    var visualRoll = moving
                        ? Mathf.Sin(visualPhase + Mathf.PI * 0.5f) * 5f * gait
                        : Mathf.Sin(idlePhase) * 5f;
                    visualRoll -= _contactFlinchDirectionLocal.x * contactFlinch * 6f;
                    // The imported Mite has no neck or limb rig. Add a slow,
                    // restrained scan to its intact silhouette while it is calm
                    // so idle reads as awareness instead of only a looping bob.
                    // Suppress the scan during locomotion, alert, attack, and hit
                    // recovery; the AI root and its collision shape stay untouched.
                    var idleScanYaw = Mathf.Sin(idlePhase * 0.3f) * 14f * quietWeight;
                    _commissionedVisual.localPosition = _commissionedVisualRestPosition
                        + Vector3.up * visualBob
                        - Vector3.forward * (attackBrace * 0.06f)
                        + Vector3.forward * (attackStroke * 0.18f)
                        + _contactFlinchDirectionLocal * (contactFlinch * 0.10f);
                    _commissionedVisual.localRotation = _commissionedVisualRestRotation
                        * Quaternion.Euler(visualPitch, idleScanYaw, visualRoll);
                }
                // The imported rig must be re-seated after Animator has updated
                // its pose in LateUpdate. Its dedicated driver performs that
                // single post-animation correction; here we only keep legacy
                // pivot rigs and unrigged commissioned art aligned.
                if (_commissionedMeshyAnimatorDriver == null
                    || !_commissionedMeshyAnimatorDriver.DrivesImportedRig)
                {
                    AlignCommissionedVisualToGround();
                }
            }

            if ((_commissionedRigAnimator != null && _commissionedRigAnimator.enabled)
                || (_commissionedMeshyAnimatorDriver != null
                    && _commissionedMeshyAnimatorDriver.DrivesImportedRig))
            {
                // Eye tracking/blinks above still respond to Splat. The dedicated
                // pivot animator owns the remaining articulated body parts.
                return;
            }

            for (var i = 0; i < _legs.Length; i++)
            {
                if (_legs[i] == null) continue;
                // Hexapods travel in alternating tripods: left front/rear plus
                // right middle, then their opposing three. The old per-row offsets
                // made pairs drift in an uncoordinated wave.
                var tripodA = i == 0 || i == 2 || i == 4;
                var phase = moving
                    ? _stridePhase + (tripodA ? 0f : Mathf.PI)
                    // Idle feet used to complete one lift-and-set cycle in almost
                    // six seconds. At gameplay distance that read as a frozen,
                    // hovering prop. A quicker alternating tripod keeps one set
                    // clearly supporting the body while the other makes a small,
                    // readable curious step.
                    : idlePhase * 0.82f + (tripodA ? 0f : Mathf.PI);
                // Make the idle posture read through the feet: the old 13-degree
                // twitch disappeared at gameplay scale, so the creature looked
                // frozen even though its body and shell were moving.
                var swing = Mathf.Sin(phase) * (moving ? 24f * gait : 33f);
                // Set the opposing tripods into a visible brace before the bump.
                // The body's round silhouette hides most pitch, so the legs carry
                // the attack anticipation without stretching or inflating the body.
                if (!moving)
                {
                    var idleSwing = swing + Mathf.Sin(idlePhase * 0.7f + i * 1.7f) * 5f;
                    swing = idleSwing * (1f - stationaryAttackBrace);
                }
                swing += stationaryAttackBrace * (tripodA ? -8f : 8f);
                // Let the idle pose breathe through the feet: alternate a small
                // tripod lift while the opposite three legs remain grounded.
                // Previously idle motion rotated the legs but then seated every
                // foot, hiding most of the animation at normal camera distance.
                var step = Mathf.Max(0f, Mathf.Sin(phase));
                // Keep the swing clear of the ground without lifting the tiny
                // curled feet so far that the Mite reads as floating. Idle weight
                // shifts stay close to the floor; the shell and antenna carry idle
                // motion while the opposite tripod supports the body.
                var lift = moving
                    ? step * 0.045f * gait
                    : step * step * 0.024f * (1f - stationaryAttackBrace);
                var stanceBrace = Mathf.Sign(_legRestPositions[i].x)
                    * (0.045f + stationaryAttackBrace * 0.13f + contactFlinch * 0.03f);
                _legs[i].localPosition = _legRestPositions[i]
                    + Vector3.up * lift + Vector3.right * stanceBrace;
                _legs[i].localRotation = _legRestRotations[i] * Quaternion.Euler(swing, 0f, 0f);
            }

            if (_body != null)
            {
                // Give each stride one small body counter-beat. The former
                // 3 mm double-bounce was almost invisible, leaving the planted
                // tripod legs to carry the entire gait at gameplay distance.
                var nod = moving ? Mathf.Sin(_stridePhase) * 4.5f : Mathf.Sin(idlePhase) * (startled ? 6f : 4.2f);
                // A short forward brace makes the notice state readable before
                // the mite reaches Splat; the attack still comes from contact.
                // At close range, load the front of the body before contact so
                // the bump has a visible anticipation beat, distinct from alert.
                // The organic gameplay Mite used to hold a planted brace while
                // the attack pulse only moved the commissioned mesh fallback.
                // Let the shell make a short, scale-free release beat as well.
                nod += _alertBlend * 9f + attackBrace * 34f
                    + _attackStrokeWeight * 17f - contactFlinch * 16f;
                _body.localRotation = _bodyRestRotation * Quaternion.Euler(nod, 0f, moving ? Mathf.Sin(_stridePhase) * 1.5f : 0f);
                var bodyBob = moving
                    ? Mathf.Sin(_stridePhase - 0.35f) * bobAmplitude * 0.5f
                    : Mathf.Sin(idlePhase * 0.5f) * (startled ? 0.016f : 0.014f);
                bodyBob -= attackBrace * 0.05f;
                // Keep the shell carried by its planted tripods while shortening
                // the measured underside gap that made the body look detached
                // from its support stance. A larger settle value lowers the body
                // because it is subtracted here. Re-seat legs afterward so feet
                // remain on the sampled ground surface.
                var bodySettle = moving ? 0.125f : 0.135f;
                _body.localPosition = _bodyRestPosition + Vector3.up * (bodyBob - bodySettle
                    + attackBrace * 0.065f + contactFlinch * 0.02f)
                    + Vector3.forward * (_attackStrokeWeight * 0.12f)
                    + _contactFlinchDirectionLocal * (contactFlinch * 0.13f);
            }

            // The legs are children of the bobbing body. Seat support feet after
            // applying body motion; doing it before the bob let the body lift
            // their roots again and created a visible gap over traversal pads.
            for (var i = 0; i < _legs.Length; i++)
            {
                if (_legs[i] == null) continue;
                var tripodA = i == 0 || i == 2 || i == 4;
                var phase = moving
                    ? _stridePhase + (tripodA ? 0f : Mathf.PI)
                    : idlePhase * 0.82f + (tripodA ? 0f : Mathf.PI);
                var supporting = stationaryAttackBrace >= 0.65f || Mathf.Sin(phase) <= 0f;
                if (!supporting)
                {
                    _legStanceLocked[i] = false;
                    continue;
                }

                // Keep a planted foot fixed in world space while its tripod carries
                // the moving body. If the support leg has reached a different-height
                // walkable surface, release the old anchor so the foot can step up
                // instead of stretching behind under the rising body.
                var posedFootPosition = _legs[i].position;
                var posedGroundY = TryGetWalkableGroundY(posedFootPosition, 0.5f, 1.25f, out var sampledPosedGroundY)
                    ? sampledPosedGroundY
                    : _groundY;
                if (_legStanceLocked[i]
                    && Mathf.Abs(posedGroundY - _legStanceWorldPositions[i].y) > 0.16f)
                {
                    _legStanceLocked[i] = false;
                }

                if (!_legStanceLocked[i])
                {
                    _legStanceWorldPositions[i] = posedFootPosition;
                    _legStanceLocked[i] = true;
                }
                else
                {
                    _legs[i].position = _legStanceWorldPositions[i];
                }

                SeatLegOnGround(i);
                // Surface correction may move the pivot a few millimetres; retain
                // that seated position as the planted anchor for the next frame.
                _legStanceWorldPositions[i] = _legs[i].position;
            }

            if (_shell != null)
            {
                _shell.localRotation = _shellRestRotation * Quaternion.Euler(_alertBlend * 5f - _attackTelegraphBlend * 14f
                    + _attackStrokeWeight * 7f + contactFlinch * 20f,
                    moving ? Mathf.Sin(_stridePhase - 0.7f) * 4f : Mathf.Sin(idlePhase * 0.65f) * 10f,
                    (moving ? Mathf.Sin(_stridePhase + 0.35f) * 3.5f : Mathf.Sin(idlePhase) * 4f)
                    - _contactFlinchDirectionLocal.x * contactFlinch * 14f);
            }

            if (_antenna != null)
            {
                var alert = startled ? 26f : 18f;
                _antenna.localRotation = _antennaRestRotation * Quaternion.Euler(_alertBlend * 24f
                    + _attackTelegraphBlend * 24f + contactFlinch * 38f,
                    Mathf.Sin(Time.time * 1.1f) * alert,
                    Mathf.Sin(Time.time * 1.7f) * alert - _contactFlinchDirectionLocal.x * contactFlinch * 18f
                    + _antennaTwitchDirection * _antennaTwitchWeight * 25f);
            }
        }

        private void AnimateEyeTracking()
        {
            var blend = 1f - Mathf.Exp(-Time.deltaTime * 10f);
            var target = _splat != null ? _splat.position + Vector3.up * 1.05f : Vector3.zero;
            // Keep the commissioned pupils seated and readable through the close
            // combat tell. Narrowing this model's eye shells while the body braces
            // made the unskinned pupils look like they had popped out of the head.
            var calmFace = _alertBlend < 0.15f && _attackTelegraphBlend < 0.15f
                && GetContactFlinchWeight() < 0.15f;
            if (!calmFace && _eyeBlinkStartedAt >= 0f)
            {
                _eyeBlinkStartedAt = -1f;
                _nextEyeBlinkAt = Time.time + Random.Range(1.4f, 2.2f);
            }
            if (_eyeCount > 0 && calmFace && _eyeBlinkStartedAt < 0f
                && Time.time >= _nextEyeBlinkAt)
            {
                _eyeBlinkStartedAt = Time.time;
            }
            var blink = GetEyeBlinkWeight();
            if (_eyeBlinkStartedAt >= 0f && Time.time - _eyeBlinkStartedAt >= 0.16f)
            {
                _eyeBlinkStartedAt = -1f;
                _nextEyeBlinkAt = Time.time + Random.Range(2.4f, 4.8f);
                blink = 0f;
            }
            for (var i = 0; i < _eyes.Length; i++)
            {
                var eye = _eyes[i];
                if (eye == null) continue;
                var restScale = _eyeRestScales[i];
                // Focus the existing eyes during the attack tell so the face
                // joins the warning instead of leaving the ground ring to carry
                // the whole anticipation cue. The smoothed telegraph also eases
                // the eyes back open when the Mite disengages.
                var attackFocus = Mathf.Lerp(1f, 0.82f, _attackTelegraphBlend);
                eye.localScale = new Vector3(restScale.x,
                    restScale.y * (1f - blink * 0.92f) * attackFocus, restScale.z);
            }

            for (var i = 0; i < _pupils.Length; i++)
            {
                var pupil = _pupils[i];
                if (pupil == null) continue;
                var targetPosition = _pupilRestPositions[i];
                var eye = i < _eyes.Length ? _eyes[i] : null;
                if (eye != null && _splat != null)
                {
                    // Eye scale drives the attack squint and blink. InverseTransformPoint
                    // divides by that animated scale, which can fling a pupil out of its
                    // socket while the eye is narrowed. Measure gaze in the unscaled
                    // parent frame, then rotate it into the eye's local axes.
                    var eyeParent = eye.parent;
                    var direction = eyeParent != null
                        ? Quaternion.Inverse(eye.localRotation)
                            * (eyeParent.InverseTransformPoint(target) - eye.localPosition)
                        : eye.InverseTransformPoint(target);
                    direction.z = 0f;
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        direction.Normalize();
                        targetPosition += new Vector3(direction.x * 0.024f, direction.y * 0.018f, 0f);
                    }
                }

                pupil.localPosition = Vector3.Lerp(pupil.localPosition, targetPosition, blend);
            }
        }

        private float GetEyeBlinkWeight()
        {
            if (_eyeBlinkStartedAt < 0f) return 0f;
            var elapsed = Time.time - _eyeBlinkStartedAt;
            if (elapsed < 0f || elapsed >= 0.16f) return 0f;
            return elapsed < 0.08f
                ? Mathf.SmoothStep(0f, 1f, elapsed / 0.08f)
                : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - 0.08f) / 0.08f);
        }

        private float GetContactFlinchWeight()
        {
            if (_contactFlinchStartedAt < 0f)
            {
                return 0f;
            }

            var elapsed = Time.time - _contactFlinchStartedAt;
            return elapsed >= 0f && elapsed < 0.52f ? Mathf.Exp(-elapsed * 5.5f) : 0f;
        }

        private void SeatLegOnGround(int index)
        {
            var renderers = _legRenderers[index];
            if (renderers == null || _legs[index] == null) return;

            var lowest = float.PositiveInfinity;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    lowest = Mathf.Min(lowest, renderer.bounds.min.y);
                }
            }

            if (float.IsInfinity(lowest)) return;
            // Seat each planted foot to the actual surface below that leg. Using
            // only the root's center ray made the far leg float on tilted pads.
            var footGroundY = TryGetWalkableGroundY(_legs[index].position, 0.5f, 1.25f, out var sampledGroundY)
                ? sampledGroundY
                : _groundY;
            var correction = footGroundY + 0.005f - lowest;
            if (Mathf.Abs(correction) > 0.25f) return;
            _legs[index].position += Vector3.up * correction;
        }

        private bool IsStartled()
        {
            if (_splat == null)
            {
                var motor = FindAnyObjectByType<SplatMotor>();
                if (motor != null)
                {
                    _splat = motor.transform;
                }
            }

            if (_splat == null)
            {
                return false;
            }

            var delta = _splat.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= noticeRadius * noticeRadius;
        }

        private void OnTriggerEnter(Collider other) => HandleContact(other);

        private void OnTriggerStay(Collider other) => HandleContact(other);

        private void HandleContact(Collider other)
        {
            if (_defeated)
            {
                return;
            }

            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            var descending = !motor.IsGrounded && other.bounds.center.y > transform.position.y + 0.2f;
            if (descending || motor.IsGroundPounding || motor.PlanarVelocity.sqrMagnitude > 80f)
            {
                Defeat(transform.position - other.transform.position);
                return;
            }

            if (Time.time < _nextBumpTime)
            {
                return;
            }

            var respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null || respawn.IsInvulnerable)
            {
                return;
            }

            var push = other.transform.position - transform.position;
            push.y = 0.4f;
            respawn.ApplyKnockback(push, contactKnockbackSpeed);
            TriggerContactFlinch(-push);
            TempSfxPlayer.PlayOuch();
            // Tell grammar: combat flash + knockback own the read; no toast spam.
            _nextBumpTime = Time.time + bumpCooldown;
        }

        private void Defeat(Vector3 impactDirection)
        {
            if (_defeated)
            {
                return;
            }

            _defeated = true;
            if (_aggroReported)
            {
                EnemyCombatDirector.Ensure().NotifyEnemyDefeated(this);
                _aggroReported = false;
            }

            TriggerContactFlinch(impactDirection);
            TempSfxPlayer.PlaySlam();
            GameplayFeedback.Toast("ScrapMite flattened!");
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.3f, 10, 0.4f);
            StartCoroutine(DefeatRoutine());
        }

        private void ReportAggro(bool startled)
        {
            if (startled == _aggroReported)
            {
                return;
            }

            _aggroReported = startled;
            var director = EnemyCombatDirector.Ensure();
            if (startled)
            {
                director.NotifyAggroEnter(this);
            }
            else
            {
                director.NotifyAggroExit(this);
            }
        }

        private IEnumerator DefeatRoutine()
        {
            var elapsed = 0f;
            // The commissioned visual's LateUpdate animator owns the visible
            // body, antenna, and leg pivots. Keep this coroutine responsible
            // for the legacy visual path and the shell accent only, rather than
            // writing the same transforms earlier in every frame.
            var commissionedRigOwnsPose = _commissionedVisual != null
                && _commissionedVisual.gameObject.activeInHierarchy
                && ((_commissionedRigAnimator != null && _commissionedRigAnimator.enabled)
                    || (_commissionedMeshyAnimatorDriver != null
                        && _commissionedMeshyAnimatorDriver.DrivesImportedRig));
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            var bodyStartRotation = _body != null ? _body.localRotation : Quaternion.identity;
            var bodyStartPosition = _body != null ? _body.localPosition : Vector3.zero;
            var shellStartRotation = _shell != null ? _shell.localRotation : Quaternion.identity;
            var antennaStartRotation = _antenna != null ? _antenna.localRotation : Quaternion.identity;
            var legStartRotations = new Quaternion[_legs.Length];
            for (var i = 0; i < _legs.Length; i++)
            {
                legStartRotations[i] = _legs[i] != null ? _legs[i].localRotation : Quaternion.identity;
            }

            var away = _contactFlinchDirectionLocal;
            if (away.sqrMagnitude < 0.001f) away = -Vector3.forward;
            away.Normalize();
            var fallAxis = Vector3.Cross(Vector3.up, away).normalized;
            if (fallAxis.sqrMagnitude < 0.001f) fallAxis = Vector3.right;
            // Keep serialized tuning values from older scenes, but give the new
            // fall a visible half-second arc. The previous 0.22 s global squash
            // stretched the whole enemy sideways and made it vanish like jelly.
            var duration = Mathf.Max(0.52f, defeatSquashDuration * 2.4f);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                if (!commissionedRigOwnsPose && _body != null)
                {
                    _body.localRotation = bodyStartRotation * Quaternion.AngleAxis(58f * t, fallAxis);
                    _body.localPosition = bodyStartPosition + away * (0.08f * t) + Vector3.up * (-0.025f * t);
                }
                if (_shell != null)
                {
                    _shell.localRotation = shellStartRotation * Quaternion.Euler(12f * t, 0f,
                        -Mathf.Sign(away.x) * 10f * t);
                }
                if (!commissionedRigOwnsPose && _antenna != null)
                {
                    _antenna.localRotation = antennaStartRotation * Quaternion.Euler(28f * t, 0f,
                        -Mathf.Sign(away.x) * 16f * t);
                }
                for (var i = 0; !commissionedRigOwnsPose && i < _legs.Length; i++)
                {
                    if (_legs[i] == null) continue;
                    var side = Mathf.Sign(_legRestPositions[i].x);
                    _legs[i].localRotation = legStartRotations[i]
                        * Quaternion.Euler(-22f * t, 0f, side * 12f * t);
                }
                yield return null;
            }

            // Leave the final collapsed pose on screen long enough to read after
            // the impact spark and camera motion have settled. The old 0.18 s
            // hold removed the enemy almost immediately after its fall, so the
            // defeated pose was easy to miss in the middle of a real encounter.
            yield return new WaitForSeconds(0.55f);
            if (_commissionedMeshyAnimatorDriver != null
                && _commissionedMeshyAnimatorDriver.DrivesImportedRig)
            {
                var waitLimit = _commissionedMeshyAnimatorDriver.DefeatClipDurationSeconds + 0.25f;
                var waitElapsed = 0f;
                while (waitElapsed < waitLimit && !_commissionedMeshyAnimatorDriver.IsDefeatPlaybackComplete)
                {
                    waitElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            if (_destroyAfterDefeat)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
