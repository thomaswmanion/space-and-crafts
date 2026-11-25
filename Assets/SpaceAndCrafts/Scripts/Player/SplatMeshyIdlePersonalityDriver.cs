using UnityEngine;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Occasionally plays a single grounded personality gesture while Meshy Splat is idle.
    /// Gestures are short one-shot clips and never change the character's scale or gameplay root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SplatMeshyIdlePersonalityDriver : MonoBehaviour
    {
        private static readonly int PersonalityTriggerId = Animator.StringToHash("Personality");
        private static readonly int PersonalityIdleId = Animator.StringToHash("PersonalityIdle");
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");

        [SerializeField] private Animator animator;
        [SerializeField] private SplatMotor motor;
        [SerializeField, Min(1f)] private float firstGestureDelay = 2.8f;
        [SerializeField, Min(2f)] private float minimumIdleInterval = 6f;
        [SerializeField, Min(2f)] private float maximumIdleInterval = 10f;

        private float _idleTime;
        private float _nextGestureAt;
        private bool _wasEligibleForIdle;

        public float IdleTimeForDiagnostics => _idleTime;
        public float NextGestureAtForDiagnostics => _nextGestureAt;
        public Animator AnimatorForDiagnostics => animator;
        public SplatMotor MotorForDiagnostics => motor;

        private void Awake()
        {
            CacheReferences();
            _nextGestureAt = Mathf.Max(1f, firstGestureDelay);
        }

        private void Update()
        {
            CacheReferences();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            var grounded = motor == null || motor.IsGrounded;
            var speed = motor != null ? motor.PlanarVelocity.magnitude : animator.GetFloat(MoveSpeedId);
            var inLocomotion = animator.IsInTransition(0)
                ? false
                : animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion");
            if (!grounded || speed > 0.12f)
            {
                if (_wasEligibleForIdle)
                {
                    _nextGestureAt = UnityEngine.Random.Range(
                        Mathf.Max(2f, minimumIdleInterval),
                        Mathf.Max(Mathf.Max(2f, minimumIdleInterval), maximumIdleInterval));
                }

                _wasEligibleForIdle = false;
                _idleTime = 0f;
                return;
            }

            // Animator state transitions can span several frames on entry to
            // grounded locomotion (and again after a personality clip). Pause
            // the timer there; treating each transition frame as movement used
            // to replace the intended first delay with a random 7–13 seconds.
            if (!inLocomotion)
            {
                _wasEligibleForIdle = false;
                _idleTime = 0f;
                return;
            }

            _wasEligibleForIdle = true;
            _idleTime += Time.deltaTime;
            if (_idleTime < _nextGestureAt)
            {
                return;
            }

            _idleTime = 0f;
            _nextGestureAt = UnityEngine.Random.Range(
                Mathf.Max(2f, minimumIdleInterval),
                Mathf.Max(Mathf.Max(2f, minimumIdleInterval), maximumIdleInterval));
            animator.SetInteger(PersonalityIdleId, 1);
            animator.SetTrigger(PersonalityTriggerId);
        }

        private void CacheReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
            }

            if (motor == null)
            {
                motor = GetComponentInParent<SplatMotor>();
            }
        }
    }
}
