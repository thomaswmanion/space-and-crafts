using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Ground scrap roller — third combat archetype. Charges in a line with a telegraph stretch.
    /// </summary>
    public sealed class ScrapRoller : MonoBehaviour
    {
        [SerializeField] private float noticeRadius = 6.5f;
        [SerializeField] private float chargeSpeed = 9.5f;
        [SerializeField] private float chargeDuration = 0.85f;
        [SerializeField] private float windupDuration = 0.48f;
        [SerializeField] private float windupPullback = 0.2f;
        [SerializeField] private float cooldown = 1.6f;
        [SerializeField] private float bumpForce = 7f;
        private const float DefeatAnimationDuration = 0.66f;

        private Vector3 _origin;
        private float _nextCharge;
        private float _chargeUntil;
        private float _chargeRollDegrees;
        private float _chargeRecoveryStartAngle;
        private float _chargeSpeedWeight = 1f;
        private float _windupStartedAt = -1f;
        private float _windupUntil;
        private Vector3 _windupStartPosition;
        private Vector3 _chargeDir;
        private bool _charging;
        private Transform _splat;
        private Quaternion _restRotation;
        private bool _defeated;
        private Transform _stripe;
        private Vector3 _stripeRest;
        private Quaternion _stripeRestRotation;
        private bool _aggroReported;
        private float _chargeEndedAt = -10f;
        private float _idlePhaseOffset;
        private float _defeatStartedAt = -1f;
        private Transform _animatedVisual;
        private Quaternion _animatedVisualRestRotation;
        private Quaternion _animatedVisualPose = Quaternion.identity;

        public bool IsWindingUpForDiagnostics => _windupStartedAt >= 0f;
        public bool IsChargingForDiagnostics => _charging;
        public float ChargeSpeedWeightForDiagnostics => _chargeSpeedWeight;
        public float ChargeRecoilForDiagnostics => 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((Time.time - _chargeEndedAt) / 0.32f));
        public float ChargeRollAngleForVisuals => _charging ? _chargeRollDegrees
            : Mathf.LerpAngle(_chargeRecoveryStartAngle, 0f, 1f - ChargeRecoilForDiagnostics);
        public float WindupProgressForDiagnostics => _windupStartedAt < 0f ? 0f
            : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                (Time.time - _windupStartedAt) / Mathf.Max(0.01f, windupDuration)));
        public float IdlePhaseOffsetForDiagnostics => _idlePhaseOffset;
        public Vector3 ChargeVisualLocalEulerForDiagnostics => _animatedVisual != null
            ? _animatedVisual.localRotation.eulerAngles : Vector3.zero;
        public float DefeatProgressForDiagnostics => _defeatStartedAt < 0f ? 0f
            : Mathf.Clamp01((Time.time - _defeatStartedAt) / DefeatAnimationDuration);

        public void Configure(Vector3 origin)
        {
            _origin = origin;
            transform.position = origin;
        }

        private void Awake()
        {
            _origin = transform.position;
            _restRotation = transform.rotation;
            _nextCharge = Time.time + 0.4f;
            _idlePhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_defeated)
            {
                return;
            }

            EnsureSplat();
            EnsureStripe();
            EnsureAnimatedVisual();

            var inNotice = _splat != null
                && (_splat.position - transform.position).sqrMagnitude < noticeRadius * noticeRadius;
            ReportAggro(inNotice || _charging || IsWindingUpForDiagnostics);

            if (_charging)
            {
                // Ease the final approach to a stop. Cutting from full charge
                // speed directly to zero made the shell and collision root feel
                // like they hit an invisible wall at chargeDuration.
                const float brakingWindow = 0.18f;
                var timeRemaining = _chargeUntil - Time.time;
                _chargeSpeedWeight = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(timeRemaining / brakingWindow));
                var currentChargeSpeed = chargeSpeed * _chargeSpeedWeight;
                transform.position += _chargeDir * (currentChargeSpeed * Time.deltaTime);
                // Roll only the rendered shell. Rotating the AI root left its
                // trigger and navigation frame sideways after the charge ended.
                _chargeRollDegrees = Mathf.Repeat(_chargeRollDegrees
                    + currentChargeSpeed * Time.deltaTime / 0.55f * Mathf.Rad2Deg, 360f);
                if (Time.time >= _chargeUntil)
                {
                    FinishCharge();
                }
            }
            else
            {
                if (IsWindingUpForDiagnostics)
                {
                    if (!inNotice)
                    {
                        _windupStartedAt = -1f;
                        _nextCharge = Mathf.Max(_nextCharge, Time.time + 0.35f);
                    }
                    else
                    {
                        transform.position = _windupStartPosition
                            - _chargeDir * (windupPullback * WindupProgressForDiagnostics);
                        if (Time.time >= _windupUntil)
                        {
                            BeginCharge();
                        }
                    }
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, _origin, 2.5f * Time.deltaTime);
                    if (inNotice && Time.time >= _nextCharge)
                    {
                        BeginWindup();
                    }
                }

                PoseIdleAndWindup(inNotice);
            }

            if (_stripe != null)
            {
                var anticipation = IsWindingUpForDiagnostics ? WindupProgressForDiagnostics : 0f;
                var warn = _charging || IsWindingUpForDiagnostics;
                var warnAmount = _charging ? 1f : anticipation;
                var warnedScale = Vector3.Scale(_stripeRest, new Vector3(1.95f, 0.55f, 1.95f));
                var target = Vector3.Lerp(_stripeRest, warnedScale, warnAmount);
                _stripe.localScale = Vector3.Lerp(_stripe.localScale, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
                var stripeRoll = Mathf.Sin(Time.time * 2.3f + _idlePhaseOffset) * (warn ? 5f : 3.5f);
                _stripe.localRotation = Quaternion.Slerp(_stripe.localRotation,
                    _stripeRestRotation * Quaternion.Euler(0f, stripeRoll, 0f),
                    1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            if (_animatedVisual != null)
            {
                // Keep the collider/navigation root upright. Idle, anticipation,
                // and recovery poses belong on the rendered body, composed with
                // the charge roll instead of tilting the collision frame.
                var roll = Quaternion.AngleAxis(ChargeRollAngleForVisuals, Vector3.right);
                _animatedVisual.localRotation = _animatedVisualRestRotation * _animatedVisualPose * roll;
            }
        }

        private void BeginWindup()
        {
            if (_splat == null)
            {
                return;
            }

            var to = _splat.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f)
            {
                return;
            }

            _chargeDir = to.normalized;
            _windupStartPosition = transform.position;
            _windupStartedAt = Time.time;
            _windupUntil = Time.time + Mathf.Max(0.1f, windupDuration);
            TempSfxPlayer.PlayWarn();
        }

        private void BeginCharge()
        {
            if (_splat == null || _chargeDir.sqrMagnitude < 0.01f)
            {
                _windupStartedAt = -1f;
                _nextCharge = Time.time + 0.35f;
                return;
            }

            _windupStartedAt = -1f;
            _charging = true;
            _chargeSpeedWeight = 1f;
            _chargeRollDegrees = 0f;
            _chargeRecoveryStartAngle = 0f;
            _chargeUntil = Time.time + chargeDuration;
            _nextCharge = Time.time + cooldown;
        }

        private void FinishCharge()
        {
            if (!_charging)
            {
                return;
            }

            // A completed charge may accumulate several full turns. Settle from
            // the nearest equivalent orientation instead of fading the raw angle
            // to zero, which visibly rewound every rotation during recovery.
            _chargeRecoveryStartAngle = Mathf.DeltaAngle(0f, _chargeRollDegrees);
            _charging = false;
            _chargeSpeedWeight = 0f;
            _chargeEndedAt = Time.time;
        }

        private void PoseIdleAndWindup(bool inNotice)
        {
            var t = Time.time;
            var idleTime = t + _idlePhaseOffset;
            var idleWobble = Quaternion.Euler(
                Mathf.Sin(idleTime * 1.35f) * 5.5f,
                Mathf.Sin(idleTime * 0.9f) * 5.5f,
                Mathf.Sin(idleTime * 1.1f) * 5.5f);
            var targetYaw = _restRotation;
            var targetVisualPose = idleWobble;
            if (inNotice && _splat != null)
            {
                var toPlayer = _splat.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    targetYaw = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    var anticipation = WindupProgressForDiagnostics;
                    var recoil = Mathf.Clamp01(1f - (Time.time - _chargeEndedAt) / 0.28f);
                    // The visible lean tells the player when the roller commits.
                    // Apply it to the body only: rotating the root also tipped its
                    // trigger/capsule and could make contact depend on animation.
                    targetVisualPose = Quaternion.Euler(-38f * anticipation + 12f * recoil,
                        0f, Mathf.Sin(idleTime * 1.1f) * 3f);
                }
            }

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetYaw,
                1f - Mathf.Exp(-7f * Time.deltaTime));
            _animatedVisualPose = Quaternion.Slerp(_animatedVisualPose, targetVisualPose,
                1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_defeated || !_charging)
            {
                return;
            }

            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            var push = _chargeDir * bumpForce + Vector3.up * 3.5f;
            motor.ApplyLaunch(push);
            TempSfxPlayer.PlayOuch();
            GameplayFeedback.Toast("Roller bump!");
            FinishCharge();
        }

        private void OnTriggerStay(Collider other)
        {
            if (_defeated || _charging)
            {
                return;
            }

            // Ground stomp / slam defeats roller while idle near feet.
            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null || !motor.IsGrounded)
            {
                return;
            }

            if (motor.PlanarVelocity.magnitude < 4.5f)
            {
                return;
            }

            Defeat();
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
            GameplayFeedback.Toast("Roller smashed!");
            TempCraftSparkBurst.Spawn(transform.position, 10, 0.4f);
            _charging = false;
            _windupStartedAt = -1f;
            _stripe?.gameObject.SetActive(false);
            var visualTell = GetComponent<EnemyVisualTell>();
            if (visualTell != null) visualTell.enabled = false;
            var warningHalo = FindVisualChild("ART_EnemyWarnHalo");
            if (warningHalo != null) warningHalo.gameObject.SetActive(false);
            var colliders = GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = false;
            }
            StartCoroutine(DefeatRoutine());
        }

        private System.Collections.IEnumerator DefeatRoutine()
        {
            _defeatStartedAt = Time.time;
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var away = _splat != null ? transform.position - _splat.position : -transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();
            var rollAxis = Vector3.Cross(Vector3.up, away).normalized;
            if (rollAxis.sqrMagnitude < 0.001f) rollAxis = Vector3.right;

            while (DefeatProgressForDiagnostics < 1f)
            {
                var t = DefeatProgressForDiagnostics;
                var eased = Mathf.SmoothStep(0f, 1f, t);
                // A small hit pop followed by a grounded side tumble makes the
                // stomp resolve visibly instead of deleting the Roller at impact.
                var hopAndDrop = 0.07f * Mathf.Sin(Mathf.PI * t) - 0.06f * t * t;
                transform.position = startPosition + away * (0.12f * eased)
                    + Vector3.up * hopAndDrop;
                transform.rotation = Quaternion.AngleAxis(58f * eased, rollAxis) * startRotation;
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

        private void EnsureStripe()
        {
            if (_stripe != null)
            {
                return;
            }

            var found = FindVisualChild("ART_RollerStripe");
            if (found != null)
            {
                _stripe = found;
                _stripeRest = _stripe.localScale;
                _stripeRestRotation = _stripe.localRotation;
            }
        }

        private void EnsureAnimatedVisual()
        {
            if (_animatedVisual != null)
            {
                return;
            }

            _animatedVisual = FindVisualChild("ART_MeshyEnemyVisual")
                ?? FindVisualChild("ART_RollerBody");
            if (_animatedVisual != null)
            {
                _animatedVisualRestRotation = _animatedVisual.localRotation;
            }
        }
    }
}
