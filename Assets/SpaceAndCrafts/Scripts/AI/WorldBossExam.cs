using System.Collections;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Per-world final exam: invention-gated hits with telegraph + multi-phase enrage.
    /// TryRegisterHit stays available for smoke/systems; phases never gate hits.
    /// </summary>
    public sealed class WorldBossExam : MonoBehaviour
    {
        [SerializeField] private string requiredInventionId = "vine_spooler";
        [SerializeField] private int hitsToClear = 3;
        [SerializeField] private string clearToast = "Gateway stirs…";
        [SerializeField] private string introToast = "The exam stirs — craft ready?";
        [SerializeField] private string phaseTwoToast = "It adapts — stay sharp!";
        [SerializeField] private string phaseThreeToast = "Final fury!";
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform dangerRing;
        [SerializeField] private float telegraphDuration = 0.85f;
        [SerializeField] private float vulnerableDuration = 1.8f;
        [SerializeField] private float recoverDuration = 0.9f;
        [SerializeField] private float slamRadius = 2.8f;
        [SerializeField] private float introDuration = 1.35f;
        [SerializeField] private float introTriggerRadius = 11f;

        private int _hits;
        private int _phase = 1;
        private bool _cleared;
        private bool _introPlayed;
        private Vector3 _bodyBaseScale = Vector3.one;
        private Vector3 _bodyBaseLocalPosition;

        public int CurrentPhase => _phase;

        private void Awake()
        {
            if (bodyRoot == null)
            {
                bodyRoot = transform;
            }

            _bodyBaseScale = bodyRoot.localScale;
            _bodyBaseLocalPosition = bodyRoot.localPosition;
            SetDangerRing(0f, false);
        }

        private void OnEnable()
        {
            StartCoroutine(IntroWatch());
            StartCoroutine(TelegraphLoop());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Player.SplatMotor>() == null)
            {
                return;
            }

            TryRegisterHit();
        }

        /// <summary>
        /// Systems/smoke entry: one exam hit when the required invention is unlocked.
        /// Intro/phases never gate hits.
        /// </summary>
        public bool TryRegisterHit()
        {
            if (_cleared)
            {
                return false;
            }

            var session = FindAnyObjectByType<LevelSession>();
            if (session == null || !session.HasInvention(requiredInventionId))
            {
                GameplayFeedback.Toast("Need the world invention for this fight!");
                return false;
            }

            _hits++;
            GameplayFeedback.Toast($"Boss pressure {_hits}/{hitsToClear}");
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 1.2f, 8, 0.35f);
            var cam = FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(0.16f + (_phase - 1) * 0.04f);
            }

            UpdatePhaseFromHits();
            var music = FindAnyObjectByType<WorldMusicDirector>();
            if (music != null)
            {
                music.SetCombatIntensity(Mathf.Clamp01(_hits / (float)Mathf.Max(1, hitsToClear)));
            }

            if (_hits >= hitsToClear)
            {
                _cleared = true;
                SetDangerRing(0f, false);
                GameplayFeedback.Toast(clearToast);
                BossSpectacleDirector.PlayClearFanfare(transform);
                GameEvents.RaiseLevelCompleted();
            }

            return true;
        }

        private void UpdatePhaseFromHits()
        {
            // Phase thresholds scale with hitsToClear (3→6 across worlds).
            var p2 = Mathf.Max(1, hitsToClear / 3);
            var p3 = Mathf.Max(p2 + 1, (hitsToClear * 2) / 3);
            var next = 1;
            if (_hits >= p3)
            {
                next = 3;
            }
            else if (_hits >= p2)
            {
                next = 2;
            }

            if (next <= _phase)
            {
                return;
            }

            _phase = next;
            if (_phase == 2 && !string.IsNullOrEmpty(phaseTwoToast))
            {
                GameplayFeedback.Toast(phaseTwoToast);
                TempSfxPlayer.PlayExpose();
            }
            else if (_phase == 3 && !string.IsNullOrEmpty(phaseThreeToast))
            {
                GameplayFeedback.Toast(phaseThreeToast);
                TempSfxPlayer.PlayWarn();
            }

            if (HasDedicatedBodyVisual)
            {
                var tint = _phase == 3 ? 1.25f : 1.12f;
                bodyRoot.localScale = _bodyBaseScale * tint;
                _bodyBaseScale = bodyRoot.localScale;
            }

            BossSpectacleDirector.PlayPhaseSurge(transform, _phase);
        }

        private float PhaseTelegraph() => telegraphDuration * (_phase == 1 ? 1f : _phase == 2 ? 0.75f : 0.55f);

        private float PhaseVulnerable() => vulnerableDuration * (_phase == 1 ? 1f : _phase == 2 ? 0.85f : 0.7f);

        private float PhaseRecover() => recoverDuration * (_phase == 1 ? 1f : _phase == 2 ? 0.7f : 0.45f);

        private float PhaseSlamRadius() => slamRadius * (_phase == 1 ? 1f : _phase == 2 ? 1.2f : 1.4f);

        private bool HasDedicatedBodyVisual => bodyRoot != null && bodyRoot != transform;

        private IEnumerator IntroWatch()
        {
            while (!_introPlayed && !_cleared)
            {
                var motor = FindAnyObjectByType<Player.SplatMotor>();
                if (motor != null)
                {
                    var dist = Vector3.Distance(motor.transform.position, transform.position);
                    if (dist <= introTriggerRadius)
                    {
                        yield return PlayIntro();
                        yield break;
                    }
                }

                yield return null;
            }
        }

        private IEnumerator PlayIntro()
        {
            if (_introPlayed)
            {
                yield break;
            }

            _introPlayed = true;
            if (!string.IsNullOrEmpty(introToast))
            {
                GameplayFeedback.Toast(introToast);
            }

            TempSfxPlayer.PlayWarn();
            var cam = FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.GuideLookAt(transform.position + Vector3.up * 1.5f, introDuration, 14f);
                cam.Punch(0.12f);
            }

            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 1.5f, 10, 0.4f);
            yield return new WaitForSeconds(introDuration);
        }

        private IEnumerator TelegraphLoop()
        {
            while (!_cleared)
            {
                TempSfxPlayer.PlayWarn();
                var elapsed = 0f;
                var tel = PhaseTelegraph();
                var radius = PhaseSlamRadius();
                SetDangerRing(0.25f, true);
                while (elapsed < tel && !_cleared)
                {
                    elapsed += Time.deltaTime;
                    var t = elapsed / tel;
                    SetDangerRing(Mathf.Lerp(0.4f, radius, t), true);
                    if (HasDedicatedBodyVisual)
                    {
                        // Load the attack with a small brace and rise; keep the model's
                        // silhouette and proportions intact instead of stretching it.
                        var brace = Mathf.Clamp01(t / 0.22f);
                        var lift = Mathf.Clamp01((t - 0.22f) / 0.78f);
                        var localY = Mathf.Lerp(0f, -0.10f, Mathf.SmoothStep(0f, 1f, brace));
                        localY = Mathf.Lerp(localY, 0.20f, Mathf.SmoothStep(0f, 1f, lift));
                        bodyRoot.localPosition = _bodyBaseLocalPosition + Vector3.up * localY;
                    }

                    yield return null;
                }

                if (_cleared)
                {
                    if (HasDedicatedBodyVisual)
                    {
                        bodyRoot.localPosition = _bodyBaseLocalPosition;
                    }

                    yield break;
                }

                TempSfxPlayer.PlaySlam();
                TempLandDustBurst.Spawn(transform.position + Vector3.up * 0.05f);
                if (_phase >= 2)
                {
                    ApplyPhaseKnockback(radius);
                }

                if (HasDedicatedBodyVisual)
                {
                    var impactPosition = _bodyBaseLocalPosition + Vector3.down * 0.28f;
                    bodyRoot.localPosition = impactPosition;
                    var settleElapsed = 0f;
                    const float settleDuration = 0.16f;
                    while (settleElapsed < settleDuration && !_cleared)
                    {
                        settleElapsed += Time.deltaTime;
                        var settle = Mathf.SmoothStep(0f, 1f, settleElapsed / settleDuration);
                        bodyRoot.localPosition = Vector3.Lerp(impactPosition, _bodyBaseLocalPosition, settle);
                        yield return null;
                    }

                    bodyRoot.localPosition = _bodyBaseLocalPosition;
                }

                yield return new WaitForSeconds(PhaseVulnerable());
                SetDangerRing(0f, false);
                yield return new WaitForSeconds(PhaseRecover());
            }
        }

        private void ApplyPhaseKnockback(float radius)
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
                push.y = 0.45f;
                if (push.sqrMagnitude < 0.01f)
                {
                    push = Vector3.forward + Vector3.up;
                }

                respawn.ApplyKnockback(push, 10f + _phase * 2f);
            }
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

            dangerRing.localPosition = new Vector3(0f, -1.35f, 0f);
            var thickness = Mathf.Lerp(0.08f, 0.16f, Mathf.Clamp01(radius / Mathf.Max(0.01f, PhaseSlamRadius())));
            dangerRing.localScale = new Vector3(radius * 2f, thickness, radius * 2f);
        }
    }
}
