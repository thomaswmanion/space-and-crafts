using System.Collections;
using System.Collections.Generic;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Arrival mini-boss final exam for Boing-O-Matic.
    /// Readable slam → expose high weak point → require launch hits → complete level.
    /// </summary>
    public sealed class ScrapStormSentinel : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("ScrapStormSentinel.Update");
        private const string CommissionedMiteVisualResource = "SpaceAndCrafts/Enemies/ScrapMite_Rigged";

        [SerializeField] private Transform body;
        [SerializeField] private BossWeakPoint weakPoint;
        [SerializeField] private Transform dangerRing;
        [SerializeField] private int hitsToDefeat = 3;
        [SerializeField] private float telegraphDuration = 1.15f;
        [SerializeField] private float slamDuration = 0.3f;
        [SerializeField] private float recoverDuration = 1.0f;
        [SerializeField] private float weakPointWindow = 3.2f;
        [SerializeField] private float slamHeight = 2.6f;
        [SerializeField] private float slamRadius = 3.4f;
        [SerializeField] private float slamForce = 14f;
        [SerializeField, Min(1f)] private float combatStartDistance = 18f;
        [SerializeField] private string requiredInventionId = "boing_o_matic";
        [SerializeField] private GameObject commissionedPhaseMiteVisualPrefab;

        private int _hits;
        private int _phase = 1;
        private bool _busy;
        private bool _defeated;
        private bool _combatStarted;
        private SplatMotor _playerMotor;
        private Coroutine _combatLoop;
        private Vector3 _bodyBase;
        private Quaternion _bodyBaseRotation;
        private float _idleMotionPhase;
        private bool _usingCommissionedVisual;
        private Animator _visualAnimator;
        private bool _hasInvention;
        private bool _hintedBoing;
        private float _weakPointPoseWeight;
        private Transform _clawArmL;
        private Transform _clawArmR;
        private Quaternion _clawLBase;
        private Quaternion _clawRBase;
        private readonly List<GameObject> _phaseMites = new();

        public Vector3 AnimatedBodyLocalPositionForDiagnostics => body != null ? body.localPosition : Vector3.zero;
        public Vector3 AnimatedBodyLocalEulerForDiagnostics => body != null ? body.localEulerAngles : Vector3.zero;
        public Vector3 AnimatedBodyLocalScaleForDiagnostics => body != null ? body.localScale : Vector3.one;
        public bool UsesCommissionedVisualForDiagnostics => _usingCommissionedVisual;
        public bool IsDefeatedForDiagnostics => _defeated;
        public bool UsesRiggedVisualForDiagnostics => _visualAnimator != null
            && _visualAnimator.enabled && _visualAnimator.runtimeAnimatorController != null;
        public float WeakPointPoseWeightForDiagnostics => _weakPointPoseWeight;
        public string VisualAnimatorStateForDiagnostics
        {
            get
            {
                if (!UsesRiggedVisualForDiagnostics || _visualAnimator.layerCount == 0)
                    return "unavailable";

                var current = _visualAnimator.GetCurrentAnimatorStateInfo(0);
                var currentName = DescribeAnimatorState(current);
                if (!_visualAnimator.IsInTransition(0))
                    return $"{currentName}:{current.normalizedTime:F2}";

                var next = _visualAnimator.GetNextAnimatorStateInfo(0);
                return $"{currentName}->{DescribeAnimatorState(next)}:{next.normalizedTime:F2}";
            }
        }

        private static string DescribeAnimatorState(AnimatorStateInfo state)
        {
            if (state.IsName("Base Layer.Idle")) return "Idle";
            if (state.IsName("Base Layer.Walk")) return "Walk";
            if (state.IsName("Base Layer.Run")) return "Run";
            if (state.IsName("Base Layer.Attack")) return "Attack";
            if (state.IsName("Base Layer.Hit")) return "Hit";
            if (state.IsName("Base Layer.Knockdown")) return "Knockdown";
            return $"state-{state.fullPathHash:X8}";
        }

        private void Awake()
        {
            if (body == null)
            {
                body = transform;
            }
            else if (!body.gameObject.activeInHierarchy)
            {
                // MeshyCriticalAssetsBootstrap hides the organic ART_ shell when
                // it swaps in the commissioned model. Keep the slam attached to
                // the visible model instead of animating the disabled shell.
                var commissionedVisual = transform.Find("ART_MeshyBossVisual");
                if (commissionedVisual != null && commissionedVisual.gameObject.activeInHierarchy)
                {
                    body = commissionedVisual;
                    _usingCommissionedVisual = true;
                    var skinnedRenderers = commissionedVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    for (var i = 0; i < skinnedRenderers.Length; i++)
                        skinnedRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            _bodyBase = body.localPosition;
            _bodyBaseRotation = body.localRotation;
            _visualAnimator = body.GetComponentInChildren<Animator>(true);
            _idleMotionPhase = Random.Range(0f, Mathf.PI * 2f);
            CacheClawArms();
            if (weakPoint != null)
            {
                weakPoint.SetExposed(false);
            }

            SetDangerRing(0f, false);
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            // When a compatible commissioned rig is assigned as the visible
            // boss, its Idle clip owns the character pose. Keep the legacy
            // scale-free sway only for the original static Meshy shell.
            if (UsesRiggedVisualForDiagnostics)
            {
                return;
            }

            // Rattle's commissioned chassis has no idle clip. Give the visible
            // body a slow, scale-free weight shift between combat beats so the
            // boss does not freeze like a posed prop. Keep the AI root/colliders
            // fixed, and leave all coroutine-owned anticipation/recovery poses
            // untouched.
            if (_defeated || _busy || body == null || body == transform
                || _weakPointPoseWeight > 0.001f)
            {
                return;
            }

            var breathPhase = Time.time * 1.15f + _idleMotionPhase;
            var weightPhase = Time.time * 0.72f + _idleMotionPhase * 0.61f;
            body.localPosition = _bodyBase + Vector3.up * (0.045f * Mathf.Sin(breathPhase));
            body.localRotation = _bodyBaseRotation * Quaternion.Euler(
                3f * Mathf.Sin(weightPhase),
                0f,
                2f * Mathf.Sin(weightPhase * 0.83f + 0.7f));
        }

        private void CacheClawArms()
        {
            if (body == null)
            {
                return;
            }

            _clawArmL = body.Find("ART_ScrapStormClawArmL");
            _clawArmR = body.Find("ART_ScrapStormClawArmR");
            if (_clawArmL != null)
            {
                _clawLBase = _clawArmL.localRotation;
            }

            if (_clawArmR != null)
            {
                _clawRBase = _clawArmR.localRotation;
            }
        }

        private void OnEnable()
        {
            GameEvents.BuildCompleted += HandleBuildCompleted;
            var session = FindAnyObjectByType<LevelSession>();
            if (session != null && session.HasInvention(requiredInventionId))
            {
                _hasInvention = true;
            }

            _combatLoop = StartCoroutine(CombatLoop());
        }

        private void OnDisable()
        {
            GameEvents.BuildCompleted -= HandleBuildCompleted;
            if (_combatLoop != null)
            {
                StopCoroutine(_combatLoop);
                _combatLoop = null;
            }

            _busy = false;
            if (!_defeated)
            {
                // A toggled, non-defeated arena restarts from phase one because
                // its temporary phase adds are cleared below.
                _combatStarted = false;
                _hits = 0;
                _phase = 1;
                _hintedBoing = false;
                _weakPointPoseWeight = 0f;
                if (weakPoint != null) weakPoint.SetExposed(false);
                if (body != null)
                {
                    body.localPosition = _bodyBase;
                    body.localRotation = _bodyBaseRotation;
                    ResetClawPose();
                }
                SetDangerRing(0f, false);
            }
            ClearPhaseMites();
        }

        public void NotifyWeakPointHit()
        {
            if (_defeated || !_hasInvention)
            {
                return;
            }

            _hits++;
            TriggerVisual("Hit");
            TempSfxPlayer.PlayRelayHit();
            GameplayFeedback.Toast($"Relay hit! Rattle's gate clamp weakens ({_hits}/{hitsToDefeat}).");
            PoseClawsLoosening();
            if (weakPoint != null)
            {
                TempCraftSparkBurst.Spawn(weakPoint.transform.position, 12, 0.45f);
            }

            EscalatePhaseFromHits();
            var music = FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.SetCombatIntensity(Mathf.Clamp01(_hits / (float)Mathf.Max(1, hitsToDefeat)));
            }

            if (_hits >= hitsToDefeat)
            {
                StartCoroutine(DefeatRoutine());
            }
        }

        private void EscalatePhaseFromHits()
        {
            var next = 1;
            if (_hits >= 2)
            {
                next = 3;
            }
            else if (_hits >= 1)
            {
                next = 2;
            }

            if (next <= _phase)
            {
                return;
            }

            _phase = next;
            if (_phase == 2)
            {
                GameplayFeedback.Toast("Rattle braces the gate — wider slam!");
                TempSfxPlayer.PlayExpose();
                SpawnPhaseMites(2);
            }
            else if (_phase == 3)
            {
                GameplayFeedback.Toast("Rattle's clamp slips — double slam!");
                TempSfxPlayer.PlayWarn();
                SpawnPhaseMites(3);
            }

            BossSpectacleDirector.PlayPhaseSurge(transform, _phase);
        }

        private void SpawnPhaseMites(int count)
        {
            // Prefer the existing skinned Rainbow spider candidate; its Animator
            // and RainbowSpiderAnimatorDriver already consume ScrapMite AI state.
            // Retain the legacy commissioned resource and organic builder only as
            // fallbacks for scenes/projects without the rigged visual reference.
            var commissionedMiteVisual = commissionedPhaseMiteVisualPrefab != null
                ? commissionedPhaseMiteVisualPrefab
                : Resources.Load<GameObject>(CommissionedMiteVisualResource);
            for (var i = 0; i < count; i++)
            {
                var mite = new GameObject($"ART_ScrapMite_Phase{_phase}_{i}");
                mite.transform.position = transform.position + new Vector3((i - (count - 1) * 0.5f) * 2.2f, 0.7f, -2.5f);
                var a = new GameObject("PatrolA");
                a.transform.SetParent(mite.transform, false);
                a.transform.localPosition = new Vector3(-1.4f, 0f, 0f);
                var b = new GameObject("PatrolB");
                b.transform.SetParent(mite.transform, false);
                b.transform.localPosition = new Vector3(1.4f, 0f, 0f);
                var ai = mite.AddComponent<ScrapMite>();
                if (commissionedMiteVisual != null)
                {
                    var visual = Instantiate(commissionedMiteVisual, mite.transform, false);
                    visual.name = "ART_MeshyEnemyVisual";
                }
                else
                {
                    OrganicScrapMiteBuilder.Build(mite.transform, ApplyRuntimeMat);
                }
                ai.ConfigurePatrol(a.transform, b.transform, 2.6f + _phase * 0.2f);
                _phaseMites.Add(mite);
            }
        }

        private static void ApplyRuntimeMat(GameObject go, string assetName, Color color, float smoothness, float metallic)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = assetName, color = color };
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            renderer.sharedMaterial = mat;
        }

        private void HandleBuildCompleted(string inventionId)
        {
            if (InventionCompatibility.Satisfies(requiredInventionId, inventionId))
            {
                _hasInvention = true;
            }
        }

        private IEnumerator CombatLoop()
        {
            while (!_combatStarted && !_defeated)
            {
                if (_playerMotor == null)
                {
                    _playerMotor = FindAnyObjectByType<SplatMotor>();
                }

                if (_playerMotor != null)
                {
                    var toPlayer = _playerMotor.transform.position - transform.position;
                    var startDistance = Mathf.Max(1f, combatStartDistance);
                    _combatStarted = toPlayer.sqrMagnitude <= startDistance * startDistance;
                }

                if (!_combatStarted)
                {
                    // Keep the boss dormant during the long approach so its slam
                    // telegraphs and weak-point windows cannot expire offscreen.
                    yield return new WaitForSeconds(0.2f);
                }
            }

            if (_combatStarted && !_defeated)
            {
                // The player approaches the gateway boss from the terrace. The
                // Regent's authored chest target is on its forward side, so turn
                // toward Splat before the first tell instead of exposing the
                // target on the far side of the model.
                if (_playerMotor != null)
                {
                    var toPlayer = _playerMotor.transform.position - transform.position;
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude > 0.01f)
                    {
                        var startRotation = transform.rotation;
                        var faceRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                        const float turnDuration = 0.4f;
                        var turnElapsed = 0f;
                        while (turnElapsed < turnDuration && !_defeated)
                        {
                            turnElapsed += Time.deltaTime;
                            transform.rotation = Quaternion.Slerp(startRotation, faceRotation,
                                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(turnElapsed / turnDuration)));
                            yield return null;
                        }

                        if (!_defeated)
                        {
                            transform.rotation = faceRotation;
                        }
                    }
                }
            }

            while (!_defeated)
            {
                if (_busy)
                {
                    yield return null;
                    continue;
                }

                yield return SlamCycle();
                if (_phase >= 3 && !_defeated)
                {
                    // Final phase: immediate follow-up slam before weak window.
                    yield return SlamCycle();
                }

                if (_hasInvention && !_defeated)
                {
                    if (weakPoint != null)
                    {
                        weakPoint.SetExposed(true);
                        // Telegraph the punish window with the existing claw pivots
                        // so the player can read the opening before a projectile lands.
                        PoseClawsLoosening();
                        TempSfxPlayer.PlayExpose();
                        GameplayFeedback.Toast(_phase >= 2
                            ? "Rattle's relay is exposed — Boing it fast!"
                            : "Rattle's relay is exposed — Boing it!");
                    }

                    yield return WeakPointOpenPose(PhaseWeakWindow());
                    if (weakPoint != null && weakPoint.IsExposed)
                    {
                        weakPoint.SetExposed(false);
                    }
                }
                else if (!_hasInvention && !_hintedBoing)
                {
                    _hintedBoing = true;
                    GameplayFeedback.Toast("It shrugs off everything… need a bigger launch!");
                }

                // Let the chassis finish its recovery before the next tell. The
                // no-pause loop made Rattle chain attack poses like a mechanism
                // stuck in a cycle, leaving no readable neutral beat to reset.
                if (!_defeated)
                {
                    yield return new WaitForSeconds(0.7f);
                }
            }
        }

        private IEnumerator WeakPointOpenPose(float duration)
        {
            if (body == null || weakPoint == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration && weakPoint.IsExposed && !_defeated)
            {
                elapsed += Time.deltaTime;
                _weakPointPoseWeight = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / 0.2f));
                // The commissioned FBX is a single unrigged mesh and has no
                // claw-arm transforms to open. Give its existing chassis a
                // restrained scale-free recoil so the punish window reads on
                // the model that actually appears in gameplay.
                body.localPosition = _bodyBase + Vector3.up * (0.10f * _weakPointPoseWeight);
                body.localRotation = _bodyBaseRotation
                    * Quaternion.Euler(-9f * _weakPointPoseWeight, 0f, 0f);
                yield return null;
            }

            if (_defeated)
            {
                // DefeatRoutine owns the body transform from this point onward.
                // Do not blend the weak-point pose back to combat rest over it.
                _weakPointPoseWeight = 0f;
                yield break;
            }

            var recoveryStart = body.localPosition;
            var recoveryRotation = body.localRotation;
            var recoveryElapsed = 0f;
            const float recoveryDuration = 0.18f;
            while (recoveryElapsed < recoveryDuration)
            {
                recoveryElapsed += Time.deltaTime;
                var recover = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(recoveryElapsed / recoveryDuration));
                body.localPosition = Vector3.Lerp(recoveryStart, _bodyBase, recover);
                body.localRotation = Quaternion.Slerp(recoveryRotation, _bodyBaseRotation, recover);
                _weakPointPoseWeight = 1f - recover;
                yield return null;
            }

            body.localPosition = _bodyBase;
            body.localRotation = _bodyBaseRotation;
            _weakPointPoseWeight = 0f;
        }

        private float PhaseTelegraph() => telegraphDuration * (_phase == 1 ? 1f : _phase == 2 ? 0.8f : 0.65f);

        private float PhaseRecover() => recoverDuration * (_phase == 1 ? 1f : _phase == 2 ? 0.75f : 0.55f);

        private float PhaseSlamRadius() => slamRadius * (_phase == 1 ? 1f : _phase == 2 ? 1.2f : 1.35f);

        private float PhaseSlamForce() => slamForce * (_phase == 1 ? 1f : _phase == 2 ? 1.15f : 1.3f);

        private float PhaseWeakWindow() => weakPointWindow * (_phase == 1 ? 1f : _phase == 2 ? 0.95f : 0.9f);

        private IEnumerator SlamCycle()
        {
            _busy = true;
            TriggerVisual("Attack");
            body.localPosition = _bodyBase;
            body.localRotation = _bodyBaseRotation;

            var tel = PhaseTelegraph();
            var radius = PhaseSlamRadius();
            const float anticipationPortion = 0.26f;
            const float anticipationDip = 0.10f;
            var elapsed = 0f;
            TempSfxPlayer.PlayWarn();
            SetDangerRing(0.2f, true);
            while (elapsed < tel)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / tel));
                // Load the frame with a short body dip before lift-off. Raising the
                // entire boss from the first telegraph frame looked like it simply
                // floated upward instead of bracing for a slam.
                var bodyOffset = t < anticipationPortion
                    ? -anticipationDip * Mathf.Sin(Mathf.PI * t / anticipationPortion)
                    : slamHeight * Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01((t - anticipationPortion) / (1f - anticipationPortion)));
                body.localPosition = _bodyBase + Vector3.up * bodyOffset;
                var loaded = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(t / anticipationPortion));
                var liftProgress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((t - anticipationPortion) / (1f - anticipationPortion)));
                // A compact backward load followed by a forward drive makes the
                // existing armored chassis communicate the slam through weight
                // and rotation. Keep it on the visual body; collision and scale
                // remain owned by the upright enemy root.
                var bodyPitch = -10f * loaded + 4f * liftProgress;
                var bodyRoll = Mathf.Sin(t * Mathf.PI) * 2.5f;
                body.localRotation = _bodyBaseRotation * Quaternion.Euler(bodyPitch, 0f, bodyRoll);
                SetDangerRing(Mathf.Lerp(0.4f, radius, t), true);
                var clawWindUp = t < anticipationPortion
                    ? Mathf.SmoothStep(0f, 0.55f, Mathf.Clamp01(t / anticipationPortion))
                    : Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((t - anticipationPortion) / (1f - anticipationPortion)));
                PoseClawsWindUp(clawWindUp);
                yield return null;
            }

            elapsed = 0f;
            var start = body.localPosition;
            var startRotation = body.localRotation;
            var impactRotation = _bodyBaseRotation * Quaternion.Euler(14f, 0f, 0f);
            while (elapsed < slamDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slamDuration));
                body.localPosition = Vector3.Lerp(start, _bodyBase, t);
                body.localRotation = Quaternion.Slerp(startRotation, impactRotation, t);
                PoseClawsWindUp(1f - t);
                yield return null;
            }

            body.localPosition = _bodyBase;
            body.localRotation = impactRotation;
            ResetClawPose();
            TempSfxPlayer.PlaySlam();
            TempLandDustBurst.Spawn(transform.position + Vector3.up * 0.1f);
            var cam = FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(0.28f + (_phase - 1) * 0.06f);
            }

            ApplySlamKnockback(radius, PhaseSlamForce());
            SetDangerRing(0f, false);

            var recoverDuration = PhaseRecover();
            elapsed = 0f;
            while (elapsed < recoverDuration)
            {
                elapsed += Time.deltaTime;
                var recoverT = Mathf.Clamp01(elapsed / recoverDuration);
                // A single shallow recoil makes the heavy chassis settle after
                // impact without reintroducing a whole-body squash or repeated
                // jelly bounce.
                body.localPosition = _bodyBase + Vector3.up * (Mathf.Sin(recoverT * Mathf.PI) * 0.08f);
                body.localRotation = Quaternion.Slerp(impactRotation, _bodyBaseRotation, recoverT)
                    * Quaternion.Euler(0f, 0f, -3f * Mathf.Sin(recoverT * Mathf.PI));
                yield return null;
            }

            body.localPosition = _bodyBase;
            body.localRotation = _bodyBaseRotation;
            _busy = false;
        }

        private void SetDangerRing(float radius, bool active)
        {
            if (dangerRing == null)
            {
                return;
            }

            dangerRing.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            dangerRing.localPosition = new Vector3(0f, 0.05f, 0f);
            var maxR = PhaseSlamRadius();
            var thickness = Mathf.Lerp(0.08f, 0.18f, Mathf.Clamp01(radius / maxR));
            dangerRing.localScale = new Vector3(radius * 2f, thickness, radius * 2f);
        }

        private void PoseClawsWindUp(float t)
        {
            t = Mathf.Clamp01(t);
            if (_clawArmL != null)
            {
                _clawArmL.localRotation = _clawLBase * Quaternion.Euler(-55f * t, 0f, 25f * t);
            }

            if (_clawArmR != null)
            {
                _clawArmR.localRotation = _clawRBase * Quaternion.Euler(-55f * t, 0f, -25f * t);
            }
        }

        private void ResetClawPose()
        {
            if (_clawArmL != null)
            {
                _clawArmL.localRotation = _clawLBase;
            }

            if (_clawArmR != null)
            {
                _clawArmR.localRotation = _clawRBase;
            }
        }

        private void PoseClawsLoosening()
        {
            var release = Mathf.Clamp01(_hits / (float)Mathf.Max(1, hitsToDefeat));
            if (_clawArmL != null)
            {
                _clawArmL.localRotation = _clawLBase * Quaternion.Euler(-8f - 14f * release,
                    0f, 30f + 24f * release);
            }

            if (_clawArmR != null)
            {
                _clawArmR.localRotation = _clawRBase * Quaternion.Euler(-8f - 14f * release,
                    0f, -30f - 24f * release);
            }
        }

        private void ApplySlamKnockback(float radius, float force)
        {
            var hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var respawn = hits[i].GetComponentInParent<Player.PlayerRespawn>();
                if (respawn == null || respawn.IsInvulnerable)
                {
                    continue;
                }

                var push = hits[i].transform.position - transform.position;
                push.y = 0.55f;
                if (push.sqrMagnitude < 0.01f)
                {
                    push = Vector3.forward + Vector3.up;
                }

                respawn.ApplyKnockback(push, force);
            }
        }

        private IEnumerator DefeatRoutine()
        {
            _defeated = true;
            TriggerVisual("Defeat");
            ClearPhaseMites();
            SetDangerRing(0f, false);
            if (weakPoint != null)
            {
                weakPoint.SetExposed(false);
            }

            GameplayFeedback.Toast("Rattle lets go of the relay.");
            BossSpectacleDirector.PlayClearFanfare(transform);
            var bodyStart = body.localPosition;
            var bodyStartRotation = body.localRotation;
            var bodyFallRotation = bodyStartRotation * Quaternion.Euler(22f, 0f, 22f);
            var clawLeftStart = _clawArmL != null ? _clawArmL.localRotation : Quaternion.identity;
            var clawRightStart = _clawArmR != null ? _clawArmR.localRotation : Quaternion.identity;
            var clawLeftRelease = _clawLBase * Quaternion.Euler(-16f, 0f, 60f);
            var clawRightRelease = _clawRBase * Quaternion.Euler(-16f, 0f, -60f);
            const float releaseDuration = 1.15f;
            var elapsed = 0f;
            while (elapsed < releaseDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / releaseDuration));
                body.localPosition = Vector3.Lerp(bodyStart, _bodyBase - Vector3.up * 0.28f, t);
                body.localRotation = Quaternion.Slerp(bodyStartRotation, bodyFallRotation, t);
                if (_clawArmL != null)
                {
                    _clawArmL.localRotation = Quaternion.Slerp(clawLeftStart, clawLeftRelease, t);
                }

                if (_clawArmR != null)
                {
                    _clawArmR.localRotation = Quaternion.Slerp(clawRightStart, clawRightRelease, t);
                }

                yield return null;
            }

            // Keep the commissioned visual folded down after defeat instead of
            // snapping upright before the exit delay. Its collision root remains
            // upright and the mesh uses no squash or stretch.
            body.localPosition = _bodyBase - Vector3.up * 0.28f;
            body.localRotation = bodyFallRotation;
            if (_clawArmL != null) _clawArmL.localRotation = clawLeftRelease;
            if (_clawArmR != null) _clawArmR.localRotation = clawRightRelease;
            // Hold the release pose long enough to read before the gateway
            // response replaces the dialogue and reframes the camera.
            yield return new WaitForSeconds(0.9f);
            var music = FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.SetCombatIntensity(0f);
            }

            GameEvents.RaiseLevelCompleted();
            // Deactivating this GameObject stops its coroutines. Raise the
            // progression event first so the completion cannot be swallowed
            // by the coroutine being terminated with the boss object.
            gameObject.SetActive(false);
        }

        private void ClearPhaseMites()
        {
            for (var i = 0; i < _phaseMites.Count; i++)
            {
                var mite = _phaseMites[i];
                if (mite == null)
                {
                    continue;
                }

                var miteAi = mite.GetComponent<ScrapMite>();
                if (miteAi != null)
                {
                    miteAi.DismissAfterBossDefeat();
                }
                else
                {
                    mite.SetActive(false);
                    Destroy(mite);
                }
            }

            _phaseMites.Clear();
        }

        private void TriggerVisual(string parameterName)
        {
            if (!UsesRiggedVisualForDiagnostics)
            {
                return;
            }

            var parameters = _visualAnimator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Trigger
                    && parameters[i].name == parameterName)
                {
                    _visualAnimator.SetTrigger(parameterName);
                    return;
                }
            }
        }
    }
}
