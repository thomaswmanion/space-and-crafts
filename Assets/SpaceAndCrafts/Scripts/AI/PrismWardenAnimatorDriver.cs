using UnityEngine;

namespace SpaceAndCrafts.AI
{
    /// <summary>
    /// Drives the staged Meshy Prism Warden Generic rig from ScrapMite's existing
    /// movement/combat phases. Keeps travel on the AI root and only poses the rig.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrismWardenAnimatorDriver : MonoBehaviour
    {
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
        private static readonly int WalkRateId = Animator.StringToHash("WalkRate");
        private static readonly int TelegraphId = Animator.StringToHash("Telegraph");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DefeatId = Animator.StringToHash("Defeat");
        private static readonly int TelegraphStateId = Animator.StringToHash("Base Layer.Telegraph");
        private static readonly int HitStateId = Animator.StringToHash("Base Layer.Hit");

        [SerializeField] private Animator animator;
        [SerializeField] private ScrapMite mite;
        [SerializeField, Min(0f)] private float speedDampSeconds = .10f;
        [SerializeField, Range(0f, 1f)] private float hitTriggerThreshold = .55f;

        private float _previousHitWeight;
        private float _previousAttackElapsed = -1f;
        private bool _wasTelegraphing;
        private bool _defeatSent;

        public string CurrentStateForDiagnostics
        {
            get
            {
                if (animator == null || animator.runtimeAnimatorController == null || animator.layerCount == 0)
                    return "animator-unavailable";
                var state = animator.GetCurrentAnimatorStateInfo(0);
                var name = state.IsName("Base Layer.Idle") ? "Idle"
                    : state.IsName("Base Layer.Locomotion") ? "Locomotion"
                    : state.IsName("Base Layer.Telegraph") ? "Telegraph"
                    : state.IsName("Base Layer.Attack") ? "Attack"
                    : state.IsName("Base Layer.Hit") ? "Hit"
                    : state.IsName("Base Layer.Defeat") ? "Defeat"
                    : state.IsName("Base Layer.Knockdown") ? "Knockdown"
                    : state.IsName("Base Layer.Recovery") ? "Recovery" : "Other";
                return name + "@" + state.normalizedTime.ToString("F2");
            }
        }

        private void Awake()
        {
            CacheReferences();
            ResetSignals();
        }

        private void OnEnable()
        {
            CacheReferences();
            ResetSignals();
        }

        private void Update()
        {
            CacheReferences();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            var moveWeight = mite != null && !mite.IsDefeated
                ? Mathf.Clamp01(mite.CommissionedGaitWeight)
                : 0f;
            var walkRate = mite != null && !mite.IsDefeated
                ? Mathf.Clamp(mite.CommissionedGaitRate, .35f, 1.75f)
                : 0f;
            animator.SetFloat(MoveSpeedId, moveWeight, speedDampSeconds, Time.deltaTime);
            animator.SetFloat(WalkRateId, walkRate, speedDampSeconds, Time.deltaTime);

            if (mite == null) return;
            if (!_defeatSent && mite.IsDefeated)
            {
                animator.ResetTrigger(TelegraphId);
                animator.ResetTrigger(AttackId);
                animator.ResetTrigger(HitId);
                animator.SetFloat(MoveSpeedId, 0f);
                animator.SetTrigger(DefeatId);
                _defeatSent = true;
                return;
            }
            if (_defeatSent) return;

            var telegraphing = mite.IsAttackTelegraphingForDiagnostics;
            if (telegraphing && !_wasTelegraphing)
                animator.SetTrigger(TelegraphId);
            _wasTelegraphing = telegraphing;

            var attackElapsed = mite.AttackStrokeElapsedForDiagnostics;
            var hitWeight = mite.ContactFlinchWeightForDiagnostics;
            if (hitWeight >= hitTriggerThreshold && _previousHitWeight < hitTriggerThreshold)
            {
                animator.ResetTrigger(AttackId);
                animator.ResetTrigger(TelegraphId);
                animator.SetTrigger(HitId);
            }
            else if (!IsPlayingOrEntering(HitStateId)
                && hitWeight < .1f && attackElapsed >= 0f
                && (_previousAttackElapsed < 0f || attackElapsed + .02f < _previousAttackElapsed))
            {
                animator.ResetTrigger(TelegraphId);
                animator.SetTrigger(AttackId);
            }

            _previousAttackElapsed = attackElapsed;
            _previousHitWeight = hitWeight;
        }

        private void LateUpdate()
        {
            if (mite == null) mite = GetComponentInParent<ScrapMite>();
            if (mite != null) mite.AlignCommissionedVisualAfterPose();
        }

        private bool IsPlayingOrEntering(int stateHash)
        {
            if (animator == null || animator.layerCount == 0) return false;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == stateHash) return true;
            return animator.IsInTransition(0)
                && animator.GetNextAnimatorStateInfo(0).fullPathHash == stateHash;
        }

        private void CacheReferences()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (mite == null) mite = GetComponentInParent<ScrapMite>();
        }

        private void ResetSignals()
        {
            _previousHitWeight = 0f;
            _previousAttackElapsed = -1f;
            _wasTelegraphing = false;
            _defeatSent = false;
        }
    }
}
