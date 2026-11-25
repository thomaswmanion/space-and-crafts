using System.Collections;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Idle bob/spin + short activate punch for organic invention roots.
    /// Call PulseActivate on craft complete or teach-pad use.
    /// </summary>
    public sealed class InventionIdlePresenter : MonoBehaviour
    {
        [SerializeField] private string inventionId;
        [SerializeField] private float bobAmplitude = 0.02f;
        [SerializeField] private float bobSpeed = 0.6f;
        [SerializeField] private float spinDegreesPerSecond = 5f;
        [SerializeField] private float activatePunchScale = 1.1f;
        [SerializeField] private float activateDuration = 0.55f;

        private Vector3 _restLocal;
        private Vector3 _restScale;
        private float _phase;
        private float _activateUntil;
        private bool _listening;

        public string InventionId => inventionId;

        public void Configure(string id)
        {
            inventionId = id;
        }

        private void Awake()
        {
            _restLocal = transform.localPosition;
            _restScale = transform.localScale;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            if (!_listening)
            {
                GameEvents.BuildCompleted += HandleBuildCompleted;
                _listening = true;
            }
        }

        private void OnDisable()
        {
            if (_listening)
            {
                GameEvents.BuildCompleted -= HandleBuildCompleted;
                _listening = false;
            }
        }

        private void Update()
        {
            var speed = bobSpeed;
            var spin = spinDegreesPerSecond;
            if (Time.time < _activateUntil)
            {
                speed *= 1.7f;
                spin *= 2f;
            }

            var bob = Mathf.Sin((Time.time * speed) + _phase) * bobAmplitude;
            transform.localPosition = _restLocal + Vector3.up * bob;
            transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
        }

        public void PulseActivate()
        {
            _activateUntil = Time.time + activateDuration;
            StopAllCoroutines();
            StartCoroutine(PunchRoutine());
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.4f, 6, 0.3f);
        }

        private void HandleBuildCompleted(string id)
        {
            if (!InventionCompatibility.Satisfies(inventionId, id))
            {
                return;
            }

            PulseActivate();
        }

        private IEnumerator PunchRoutine()
        {
            var elapsed = 0f;
            while (elapsed < activateDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / activateDuration;
                var overshoot = 1f + (activatePunchScale - 1f) * Mathf.Sin(t * Mathf.PI);
                transform.localScale = _restScale * overshoot;
                yield return null;
            }

            transform.localScale = _restScale;
        }
    }
}
