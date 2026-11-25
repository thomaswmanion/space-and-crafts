using System.Collections;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Hovering scrap foe — figure-8 drift + dive bump. Second archetype beside ScrapMite.
    /// </summary>
    public sealed class ScrapFloater : MonoBehaviour
    {
        [SerializeField] private float orbitRadius = 1.8f;
        [SerializeField] private float orbitSpeed = 1.1f;
        [SerializeField] private float hoverAmp = 0.14f;
        [SerializeField] private float diveSpeed = 6.5f;
        [SerializeField] private float noticeRadius = 5.5f;
        [SerializeField] private float bumpCooldown = 1.1f;
        [SerializeField] private float diveWindupDuration = 0.42f;

        private const float DiveRecoveryDuration = 0.60f;

        private Vector3 _origin;
        private float _phase;
        private float _nextBump;
        private bool _diving;
        private bool _recoveringFromDive;
        private float _diveRecoveryStartedAt;
        private Vector3 _diveRecoveryStartPosition;
        private bool _windingUp;
        private float _windupStartedAt;
        private float _windupUntil;
        private Vector3 _windupPosition;
        private Vector3 _diveTarget;
        private Transform _splat;
        private bool _defeated;
        private Transform _ring;
        private Vector3 _ringRest;
        private Transform _animatedVisual;
        private Vector3 _animatedVisualRestScale;
        private Quaternion _animatedVisualRestRotation;
        private bool _aggroReported;

        public bool IsWindingUpForDiagnostics => _windingUp;
        public bool IsDivingForDiagnostics => _diving;
        public bool IsRecoveringForDiagnostics => _recoveringFromDive;
        public Vector3 RootEulerForDiagnostics => transform.eulerAngles;
        public Vector3 VisualEulerForDiagnostics => _animatedVisual != null
            ? _animatedVisual.localRotation.eulerAngles : Vector3.zero;

        public void Configure(Vector3 origin, float radius, float speed)
        {
            _origin = origin;
            orbitRadius = radius;
            orbitSpeed = speed;
            transform.position = origin;
        }

        private void Awake()
        {
            _origin = transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_defeated)
            {
                return;
            }

            EnsureSplat();
            _phase += Time.deltaTime * orbitSpeed;

            var inNotice = _splat != null
                && (_splat.position - transform.position).sqrMagnitude < noticeRadius * noticeRadius;

            // The player can be destroyed during a scene transition. Do not leave
            // the tell frozen forever with its ring enlarged in that case.
            if (_splat == null)
            {
                _windingUp = false;
                _diving = false;
                _recoveringFromDive = false;
            }

            ReportAggro(inNotice || _windingUp || _diving || _recoveringFromDive);

            var windupProgress = _windingUp
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - _windupStartedAt) / Mathf.Max(0.01f, diveWindupDuration)))
                : 0f;
            if (_windingUp && _splat != null)
            {
                transform.position = _windupPosition;
                if (Time.time >= _windupUntil)
                {
                    _windingUp = false;
                    _diving = true;
                }
            }

            if (_diving && _splat != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, _diveTarget, diveSpeed * Time.deltaTime);
                if ((transform.position - _diveTarget).sqrMagnitude < 0.2f)
                {
                    _diving = false;
                    _recoveringFromDive = true;
                    _diveRecoveryStartedAt = Time.time;
                    _diveRecoveryStartPosition = transform.position;
                }
            }
            else if (_recoveringFromDive)
            {
                // Return smoothly to the orbit instead of snapping to a distant
                // point on the figure-eight as soon as the dive connects.
                var recovery = Mathf.Clamp01((Time.time - _diveRecoveryStartedAt) / DiveRecoveryDuration);
                var x = Mathf.Sin(_phase) * orbitRadius;
                var z = Mathf.Sin(_phase * 2f) * (orbitRadius * 0.55f);
                var y = Mathf.Sin(_phase * 1.7f) * hoverAmp;
                var orbitPosition = _origin + new Vector3(x, 1.2f + y, z);
                // Give the dive a buoyant rebound before the return settles into
                // the figure-eight. A straight glide back made the hit recovery
                // resemble idle movement; the small arc adds a distinct recovery
                // beat while keeping the body scale and final orbit unchanged.
                var recoveryLift = Mathf.Sin(recovery * Mathf.PI) * 0.14f;
                transform.position = Vector3.Lerp(_diveRecoveryStartPosition, orbitPosition,
                    Mathf.SmoothStep(0f, 1f, recovery)) + Vector3.up * recoveryLift;
                if (recovery >= 1f)
                {
                    _recoveringFromDive = false;
                }
            }
            else if (!_windingUp)
            {
                var x = Mathf.Sin(_phase) * orbitRadius;
                var z = Mathf.Sin(_phase * 2f) * (orbitRadius * 0.55f);
                var y = Mathf.Sin(_phase * 1.7f) * hoverAmp;
                transform.position = _origin + new Vector3(x, 1.2f + y, z);

                if (inNotice && Time.time >= _nextBump)
                {
                    _windingUp = true;
                    _windupStartedAt = Time.time;
                    _windupUntil = Time.time + Mathf.Max(0.1f, diveWindupDuration);
                    _windupPosition = transform.position;
                    _diveTarget = _splat.position + Vector3.up * 0.6f;
                    _nextBump = _windupUntil + bumpCooldown;
                    TempSfxPlayer.PlayWarn();
                }
            }

            if ((_windingUp || _diving || inNotice) && _splat != null)
            {
                var toPlayer = _splat.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    // Keep the navigation/collision root upright. Pitching it
                    // and the visual child applied the anticipation/dive lean
                    // twice, over-rotating the hover enemy and its hitbox.
                    var facing = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, facing,
                        1f - Mathf.Exp(-8f * Time.deltaTime));
                }
            }
            else
            {
                transform.Rotate(0f, 20f * Time.deltaTime, 0f, Space.World);
            }

            // Readable telegraph: the existing ring swells through anticipation and attack.
            if (_ring == null)
            {
                var found = FindVisualChild("ART_FloaterRing");
                if (found != null)
                {
                    _ring = found;
                    _ringRest = _ring.localScale;
                }
            }

            if (_ring != null)
            {
                var recoveryProgress = _recoveringFromDive
                    ? Mathf.Clamp01((Time.time - _diveRecoveryStartedAt) / DiveRecoveryDuration) : 0f;
                var attackTell = _windingUp ? Mathf.Lerp(1.12f, 1.55f, windupProgress)
                    : _diving ? 1.65f : _recoveringFromDive ? Mathf.Lerp(1.28f, 1f, recoveryProgress) : 1f;
                var target = _ringRest * attackTell;
                _ring.localScale = Vector3.Lerp(_ring.localScale, target, 14f * Time.deltaTime);
            }

            if (_animatedVisual == null)
            {
                _animatedVisual = FindVisualChild("ART_MeshyEnemyVisual") ?? FindVisualChild("ART_FloaterBody");
                if (_animatedVisual != null)
                {
                    _animatedVisualRestScale = _animatedVisual.localScale;
                    _animatedVisualRestRotation = _animatedVisual.localRotation;
                }
            }

            if (_animatedVisual != null)
            {
                // Keep the floaty's body proportions fixed. The earlier 8% squash
                // and stretch made the same sphere look like jelly; use a clear
                // backward coil and forward dive lean to carry the action instead.
                // The orbit moves the enemy root; this rotates only the render child.
                var recoveryProgress = _recoveringFromDive
                    ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - _diveRecoveryStartedAt) / DiveRecoveryDuration)) : 0f;
                var idlePose = Quaternion.Euler(Mathf.Sin(_phase * 1.3f) * 8f,
                    Mathf.Sin(_phase * 0.55f) * 6f, Mathf.Sin(_phase * 1.7f) * 10f);
                var visualPose = _windingUp
                    ? Quaternion.Euler(-18f * windupProgress, 0f, 6f * windupProgress)
                    : _diving
                        ? Quaternion.Euler(24f, 0f, -7f)
                        : _recoveringFromDive
                            ? Quaternion.Slerp(Quaternion.Euler(-10f, 0f, 9f), idlePose, recoveryProgress)
                            : idlePose;
                // The attack is shorter than the idle sway cycle. Let the visual
                // body catch its tell/dive pose quickly enough to read before the
                // root reaches the target, while keeping orbit motion gently eased.
                var poseResponse = _windingUp || _diving ? 18f : 9f;
                _animatedVisual.localRotation = Quaternion.Slerp(_animatedVisual.localRotation,
                    _animatedVisualRestRotation * visualPose, 1f - Mathf.Exp(-poseResponse * Time.deltaTime));
            }
        }

        private Transform FindVisualChild(string childName)
        {
            foreach (var child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
        private void OnTriggerEnter(Collider other)
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
            if (descending || motor.PlanarVelocity.sqrMagnitude > 80f)
            {
                Defeat();
                motor.ApplyLaunch(Vector3.up * 7f);
                return;
            }

            if (Time.time < _nextBump)
            {
                return;
            }

            var respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null || respawn.IsInvulnerable)
            {
                return;
            }

            _nextBump = Time.time + bumpCooldown;
            var push = other.transform.position - transform.position;
            push.y = 0.4f;
            respawn.ApplyKnockback(push);
            TempSfxPlayer.PlayOuch();
            GameplayFeedback.Toast("Ow — pop the floater from above!");
            var voice = motor.GetComponent<CharacterVoicePlayer>();
            if (voice != null)
            {
                voice.PlayHurt();
            }
        }

        private void Defeat()
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

            TempSfxPlayer.PlayExpose();
            GameplayFeedback.Toast("Floater popped!");
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            // Freeze the idle/telegraph overlay while the existing enemy body
            // tumbles out. Scaling the whole floater into a pancake read as jelly
            // and distorted its collision root; keep its silhouette intact.
            var visualTell = GetComponent<EnemyVisualTell>();
            if (visualTell != null) visualTell.enabled = false;
            if (_animatedVisual == null)
            {
                _animatedVisual = FindVisualChild("ART_MeshyEnemyVisual") ?? FindVisualChild("ART_FloaterBody");
                if (_animatedVisual != null)
                {
                    _animatedVisualRestScale = _animatedVisual.localScale;
                    _animatedVisualRestRotation = _animatedVisual.localRotation;
                }
            }

            StartCoroutine(DefeatRoutine());
        }

        private IEnumerator DefeatRoutine()
        {
            const float duration = 0.65f;
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var away = _splat != null ? transform.position - _splat.position : transform.right;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = transform.right;
            away.Normalize();
            var rollAxis = Vector3.Cross(Vector3.up, away).normalized;
            if (rollAxis.sqrMagnitude < 0.001f) rollAxis = Vector3.right;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                // Let the whole enemy lose lift and tumble away, so defeat reads
                // as a hit reaction instead of a brief hover followed by vanish.
                var hopAndDrop = 0.12f * Mathf.Sin(Mathf.PI * t) - 0.28f * t * t;
                transform.position = startPosition + away * (0.12f * t) + Vector3.up * hopAndDrop;
                transform.rotation = Quaternion.AngleAxis(96f * t, rollAxis) * startRotation;

                if (_animatedVisual != null)
                {
                    _animatedVisual.localScale = Vector3.Lerp(_animatedVisual.localScale,
                        _animatedVisualRestScale, 1f - Mathf.Exp(-12f * Time.deltaTime));
                }
                if (_ring != null)
                {
                    _ring.localScale = _ringRest * (1f - t);
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void ReportAggro(bool active)
        {
            if (active == _aggroReported)
            {
                return;
            }

            _aggroReported = active;
            var director = EnemyCombatDirector.Ensure();
            if (active)
            {
                director.NotifyAggroEnter(this);
            }
            else
            {
                director.NotifyAggroExit(this);
            }
        }

        private void EnsureSplat()
        {
            if (_splat != null)
            {
                return;
            }

            var motor = FindAnyObjectByType<SplatMotor>();
            if (motor != null)
            {
                _splat = motor.transform;
            }
        }
    }
}
