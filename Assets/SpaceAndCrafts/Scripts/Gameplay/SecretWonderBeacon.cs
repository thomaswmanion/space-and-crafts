using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Hidden wonder beacon — short sparkle + toast payoff for curiosity routes.
    /// </summary>
    public sealed class SecretWonderBeacon : MonoBehaviour
    {
        [SerializeField] private string wonderId = "wonder";
        [SerializeField] private string wonderToast = "Hidden rainbow wonder found — nice find!";
        [SerializeField] private float pulseSpeed = 2.4f;
        [SerializeField] private float pulseAmp = 0.08f;

        private bool _claimed;
        private Vector3 _restScale;
        private Renderer[] _renderers;

        public string WonderId => string.IsNullOrWhiteSpace(wonderId) ? gameObject.name : wonderId;

        public void Configure(string id, string toast)
        {
            wonderId = id;
            wonderToast = toast;
        }

        private void Awake()
        {
            _restScale = transform.localScale;
            _renderers = GetComponentsInChildren<Renderer>();
            wonderId = WonderId;

            var col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.1f;
            }
            else
            {
                col.isTrigger = true;
            }

            // Continue restores saved wonders for this scene. Hide already claimed
            // beacons immediately so they cannot reappear as duplicate pickups.
            if (PlayerProgressSave.ShouldRestoreSessionProgress
                && SecretWonderRegistry.Ensure().HasClaimed(wonderId))
            {
                _claimed = true;
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_claimed)
            {
                return;
            }

            var t = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmp;
            transform.localScale = _restScale * t;
            transform.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_claimed || other.GetComponentInParent<Player.SplatMotor>() == null)
            {
                return;
            }

            _claimed = true;
            var registry = SecretWonderRegistry.Ensure();
            registry.TryClaim(wonderId);
            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.5f, 14, 0.55f);
            TempSfxPlayer.PlayExpose();
            GameplayFeedback.Toast(wonderToast);
            foreach (var r in _renderers)
            {
                if (r != null)
                {
                    r.enabled = false;
                }
            }

            Destroy(gameObject, 0.6f);
        }
    }
}
