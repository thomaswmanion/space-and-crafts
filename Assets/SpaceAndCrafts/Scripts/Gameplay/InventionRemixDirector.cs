using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Kingdom novelty: chaining two different invention teach pads grants a brief remix boost.
    /// Makes craft expression denser than one-pad-and-done.
    /// </summary>
    public sealed class InventionRemixDirector : MonoBehaviour
    {
        [SerializeField] private float remixWindowSeconds = 8f;
        [SerializeField] private float remixBoost = 1.28f;
        [SerializeField] private float remixDuration = 2.4f;

        private string _lastPadId;
        private float _lastPadTime;
        private SplatMotor _motor;

        public static InventionRemixDirector Ensure()
        {
            var existing = FindAnyObjectByType<InventionRemixDirector>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("SYS_InventionRemix");
            return go.AddComponent<InventionRemixDirector>();
        }

        private void Awake()
        {
            _motor = FindAnyObjectByType<SplatMotor>();
        }

        /// <summary>Called by InventionTeachPad when Splat activates a pad.</summary>
        public void NotifyPadActivated(string padId)
        {
            if (string.IsNullOrEmpty(padId))
            {
                return;
            }

            var now = Time.time;
            if (!string.IsNullOrEmpty(_lastPadId)
                && _lastPadId != padId
                && now - _lastPadTime <= remixWindowSeconds)
            {
                TriggerRemix();
            }

            _lastPadId = padId;
            _lastPadTime = now;
        }

        private void TriggerRemix()
        {
            if (_motor == null)
            {
                _motor = FindAnyObjectByType<SplatMotor>();
            }

            if (_motor != null)
            {
                _motor.ApplyTemporaryMoveBoost(remixBoost, remixDuration);
            }

            GameplayFeedback.Toast("Remix boost!");
            Debug.Log("[InventionRemix] Combo boost!");
        }
    }
}
