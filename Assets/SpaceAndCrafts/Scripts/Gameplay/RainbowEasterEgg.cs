using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Tiny curiosity egg: jump 3 times on the spot near the marker to unlock a silly toast + sparkle.
    /// </summary>
    public sealed class RainbowEasterEgg : MonoBehaviour
    {
        [SerializeField] private string eggId = "egg_hop_trio";
        [SerializeField] private string unlockToast = "Secret hop code! Splat invents jazz hands.";
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private int hopsRequired = 3;
        [SerializeField] private float hopWindow = 2.2f;

        private int _hops;
        private float _windowUntil;
        private bool _unlocked;
        private Player.SplatMotor _motor;

        private void OnEnable()
        {
            TryBindMotor();
        }

        private void OnDisable()
        {
            if (_motor != null)
            {
                _motor.Jumped -= HandleJumped;
            }
        }

        private void Update()
        {
            if (_unlocked)
            {
                return;
            }

            TryBindMotor();
            if (_hops > 0 && Time.time > _windowUntil)
            {
                _hops = 0;
            }
        }

        private void TryBindMotor()
        {
            if (_motor != null)
            {
                return;
            }

            _motor = FindAnyObjectByType<Player.SplatMotor>();
            if (_motor != null)
            {
                _motor.Jumped += HandleJumped;
            }
        }

        private void HandleJumped()
        {
            if (_unlocked || _motor == null)
            {
                return;
            }

            var delta = _motor.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > radius * radius)
            {
                _hops = 0;
                return;
            }

            if (Time.time > _windowUntil)
            {
                _hops = 0;
            }

            _windowUntil = Time.time + hopWindow;
            _hops++;
            if (_hops < hopsRequired)
            {
                return;
            }

            _unlocked = true;
            SecretWonderRegistry.Ensure().TryClaim(eggId);
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.8f, 18, 0.65f);
            TempSfxPlayer.PlayExpose();
            GameplayFeedback.Toast(unlockToast);
        }
    }
}
